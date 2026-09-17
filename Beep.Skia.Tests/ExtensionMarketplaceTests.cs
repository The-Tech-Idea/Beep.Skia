using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Beep.Skia.Extensions;
using Beep.Skia.Extensions.Marketplace;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the extension marketplace: semantic versions, package building, local registry,
    /// install/update/uninstall, safety checks, and loading installed packages into the host.
    /// </summary>
    public class ExtensionMarketplaceTests
    {
        private sealed class TempScope : IDisposable
        {
            private readonly List<string> _paths = new List<string>();

            public string Dir(string prefix)
            {
                var path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);
                _paths.Add(path);
                return path;
            }

            public void Dispose()
            {
                foreach (var path in _paths)
                {
                    try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { }
                }
            }
        }

        private static ExtensionManifest Manifest(
            string id,
            string version,
            IEnumerable<string> tags = null,
            string minHostVersion = null,
            IEnumerable<string> dependencies = null)
            => new ExtensionManifest
            {
                Id = id,
                Name = id + " Extension",
                Version = version,
                Author = "tests",
                Description = "Test package for " + id,
                Tags = tags?.ToList() ?? new List<string>(),
                MinHostVersion = minHostVersion,
                Dependencies = dependencies?.ToList() ?? new List<string>()
            };

        private static ExtensionPackage BuildPackage(
            TempScope scope,
            string registryDir,
            ExtensionManifest manifest,
            string payload = null,
            byte[] payloadBytes = null,
            string payloadName = "extension.txt")
        {
            var staging = scope.Dir("beep_ext_stage_");
            if (payloadBytes != null)
                File.WriteAllBytes(Path.Combine(staging, payloadName), payloadBytes);
            else
                File.WriteAllText(Path.Combine(staging, payloadName), payload ?? $"payload for {manifest.Id} {manifest.Version}");

            return ExtensionPackageBuilder.Build(staging, manifest, Path.Combine(registryDir, $"{manifest.Id}-{manifest.Version}.beepkg"));
        }

        // ── Semantic versions ────────────────────────────────────────────────

        [Fact]
        public void SemanticVersion_ParsesAndCompares()
        {
            Assert.True(SemanticVersion.TryParse("1.2.3", out var version));
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Patch);

            Assert.True(SemanticVersion.TryParse("1.10.0", out var newer));
            Assert.True(version < newer);
            Assert.True(SemanticVersion.Parse("2.0.0") > SemanticVersion.Parse("1.99.99"));
            Assert.Equal(0, SemanticVersion.Parse("1.2.3").CompareTo(SemanticVersion.Parse("1.2.3")));

            Assert.False(SemanticVersion.TryParse("banana", out _));
            Assert.False(SemanticVersion.TryParse("1.2.3.4", out _));
        }

        [Fact]
        public void SemanticVersion_PrereleaseIsLowerThanRelease()
        {
            Assert.True(SemanticVersion.Parse("1.0.0-beta.2") < SemanticVersion.Parse("1.0.0"));
            Assert.True(SemanticVersion.Parse("1.0.0-alpha") < SemanticVersion.Parse("1.0.0-beta"));
            Assert.True(SemanticVersion.Parse("1.0.0-beta.1") < SemanticVersion.Parse("1.0.0-beta.2"));
        }

        // ── Package building and registry ────────────────────────────────────

        [Fact]
        public void PackageBuilder_WritesManifestAndPayload()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var package = BuildPackage(scope, registryDir, Manifest("sample.ext", "1.0.0", new[] { "flow", "demo" }));

            Assert.True(File.Exists(package.PackagePath));
            Assert.True(package.SizeBytes > 0);
            Assert.Equal(64, package.Sha256.Length);

            var manifest = ExtensionPackageBuilder.ReadManifest(package.PackagePath);
            Assert.Equal("sample.ext", manifest.Id);
            Assert.Equal("1.0.0", manifest.Version);
            Assert.Contains("flow", manifest.Tags);

            using var archive = ZipFile.OpenRead(package.PackagePath);
            Assert.Contains(archive.Entries, e => e.FullName == "extension.txt");
        }

        [Fact]
        public void LocalRegistry_SearchFiltersByQueryAndTag()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            BuildPackage(scope, registryDir, Manifest("flow.tools", "1.0.0", new[] { "flow" }));
            BuildPackage(scope, registryDir, Manifest("db.tools", "1.0.0", new[] { "database" }));

            var registry = new LocalExtensionRegistry(registryDir);

            Assert.Equal(2, registry.Search().Count);
            Assert.Single(registry.Search("flow"));
            Assert.Single(registry.Search(tag: "database"));
            Assert.Empty(registry.Search("nothing-matches"));
        }

        [Fact]
        public void LocalRegistry_GetPackage_ReturnsLatestVersion()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            BuildPackage(scope, registryDir, Manifest("sample.ext", "1.0.0"));
            BuildPackage(scope, registryDir, Manifest("sample.ext", "1.1.0"));
            BuildPackage(scope, registryDir, Manifest("sample.ext", "1.0.5"));

            var registry = new LocalExtensionRegistry(registryDir);

            Assert.Equal("1.1.0", registry.GetPackage("sample.ext").Version);
            Assert.Equal("1.0.5", registry.GetPackage("sample.ext", "1.0.5").Version);
            Assert.Null(registry.GetPackage("missing"));
        }

        // ── Install / update / uninstall ─────────────────────────────────────

        [Fact]
        public void Install_ExtractsFilesAndRecordsCatalog()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");
            var package = BuildPackage(scope, registryDir, Manifest("sample.ext", "1.0.0"));

            var manager = new ExtensionPackageManager(installRoot);
            var result = manager.Install(package.PackagePath);

            Assert.True(result.Success, result.Error);
            Assert.True(manager.IsInstalled("sample.ext"));
            Assert.Equal("1.0.0", manager.GetInstalled("sample.ext").Version);
            Assert.True(File.Exists(Path.Combine(result.Extension.InstallPath, "extension.txt")));
            Assert.Contains(result.Extension.InstallPath, manager.GetLoadDirectories());
            Assert.True(File.Exists(Path.Combine(installRoot, "installed.json")));
        }

        [Fact]
        public void Catalog_PersistsAcrossManagerInstances()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");
            var package = BuildPackage(scope, registryDir, Manifest("sample.ext", "2.3.4"));

            new ExtensionPackageManager(installRoot).Install(package.PackagePath);

            var reopened = new ExtensionPackageManager(installRoot);
            Assert.True(reopened.IsInstalled("sample.ext", "2.3.4"));
            Assert.Single(reopened.GetLoadDirectories());
        }

        [Fact]
        public void Install_DuplicateRequiresForce()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");
            var package = BuildPackage(scope, registryDir, Manifest("sample.ext", "1.0.0"));

            var manager = new ExtensionPackageManager(installRoot);
            Assert.True(manager.Install(package.PackagePath).Success);

            var duplicate = manager.Install(package.PackagePath);
            Assert.False(duplicate.Success);
            Assert.Contains("already installed", duplicate.Error);

            Assert.True(manager.Install(package.PackagePath, force: true).Success);
        }

        [Fact]
        public void Install_MissingManifest_Fails()
        {
            using var scope = new TempScope();
            var installRoot = scope.Dir("beep_install_");
            var packagePath = Path.Combine(scope.Dir("beep_reg_"), "broken.beepkg");

            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("readme.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("no manifest here");
            }

            var result = new ExtensionPackageManager(installRoot).Install(packagePath);

            Assert.False(result.Success);
            Assert.Contains("manifest.json", result.Error);
        }

        [Fact]
        public void Install_ZipSlipEntry_IsRejected()
        {
            using var scope = new TempScope();
            var installRoot = scope.Dir("beep_install_");
            var packagePath = Path.Combine(scope.Dir("beep_reg_"), "evil.beepkg");
            var manifest = Manifest("evil.ext", "1.0.0");

            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
            {
                var manifestEntry = archive.CreateEntry(ExtensionManifest.FileName);
                using (var writer = new StreamWriter(manifestEntry.Open()))
                    writer.Write(JsonSerializer.Serialize(manifest));

                var evil = archive.CreateEntry("../evil.txt");
                using var evilWriter = new StreamWriter(evil.Open());
                evilWriter.Write("escaped!");
            }

            var result = new ExtensionPackageManager(installRoot).Install(packagePath);

            Assert.False(result.Success);
            Assert.Contains("escapes", result.Error);
            Assert.False(File.Exists(Path.Combine(installRoot, "evil.txt")));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(installRoot), "evil.txt")));
        }

        [Fact]
        public void Install_EnforcesMinHostVersion()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");
            var package = BuildPackage(scope, registryDir, Manifest("future.ext", "1.0.0", minHostVersion: "9.0.0"));

            var result = new ExtensionPackageManager(installRoot, hostVersion: "1.5.0").Install(package.PackagePath);

            Assert.False(result.Success);
            Assert.Contains("requires host version", result.Error);
        }

        [Fact]
        public void Install_EnforcesDependencies()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");
            var dependent = BuildPackage(scope, registryDir, Manifest("dependent.ext", "1.0.0", dependencies: new[] { "base.ext" }));
            var basePackage = BuildPackage(scope, registryDir, Manifest("base.ext", "1.0.0"));

            var manager = new ExtensionPackageManager(installRoot);
            var missing = manager.Install(dependent.PackagePath);
            Assert.False(missing.Success);
            Assert.Contains("base.ext", missing.Error);

            Assert.True(manager.Install(basePackage.PackagePath).Success);
            Assert.True(manager.Install(dependent.PackagePath).Success);
        }

        [Fact]
        public void Uninstall_RemovesFilesAndCatalogEntry()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");
            var package = BuildPackage(scope, registryDir, Manifest("sample.ext", "1.0.0"));

            var manager = new ExtensionPackageManager(installRoot);
            var installed = manager.Install(package.PackagePath).Extension;

            var result = manager.Uninstall("sample.ext");

            Assert.True(result.Success, result.Error);
            Assert.False(manager.IsInstalled("sample.ext"));
            Assert.False(Directory.Exists(installed.InstallPath));
            Assert.False(manager.Uninstall("sample.ext").Success);
        }

        [Fact]
        public void Update_InstallsNewerVersionOnly()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");
            var registry = new LocalExtensionRegistry(registryDir);
            BuildPackage(scope, registryDir, Manifest("sample.ext", "1.0.0"));
            registry.Refresh();
            var manager = new ExtensionPackageManager(installRoot, registry);
            Assert.True(manager.InstallFromRegistry("sample.ext").Success);
            Assert.True(manager.IsInstalled("sample.ext", "1.0.0"));

            var upToDate = manager.Update("sample.ext");
            Assert.False(upToDate.Success);
            Assert.Contains("up to date", upToDate.Error);

            BuildPackage(scope, registryDir, Manifest("sample.ext", "1.2.0"));
            registry.Refresh();

            var updated = manager.Update("sample.ext");
            Assert.True(updated.Success, updated.Error);
            Assert.True(manager.IsInstalled("sample.ext", "1.2.0"));
            Assert.Contains("1.2.0", manager.GetInstalled("sample.ext").InstallPath);
        }

        [Fact]
        public void InstallFromRegistry_WithoutRegistry_Fails()
        {
            using var scope = new TempScope();
            var installRoot = scope.Dir("beep_install_");
            var manager = new ExtensionPackageManager(installRoot);

            var result = manager.InstallFromRegistry("anything");

            Assert.False(result.Success);
            Assert.Contains("No registry", result.Error);
        }

        // ── Host integration ─────────────────────────────────────────────────

        [Fact]
        public void ConcurrentInstalls_AllSucceedAndPersistToCatalog()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");

            const int count = 8;
            var packages = Enumerable.Range(0, count)
                .Select(i => BuildPackage(scope, registryDir, Manifest("concurrent" + i + ".ext", "1.0.0")))
                .ToList();

            var manager = new ExtensionPackageManager(installRoot);
            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var results = new System.Collections.Concurrent.ConcurrentBag<ExtensionInstallResult>();

            System.Threading.Tasks.Parallel.ForEach(packages, package =>
            {
                try { results.Add(manager.Install(package.PackagePath)); }
                catch (Exception ex) { errors.Add(ex); }
            });

            Assert.Empty(errors);
            Assert.All(results, r => Assert.True(r.Success, r.Error));
            Assert.Equal(count, manager.Installed.Count);

            // The catalog must be complete and readable by a fresh manager.
            var reopened = new ExtensionPackageManager(installRoot);
            Assert.Equal(count, reopened.Installed.Count);
        }

        [Fact]
        public void ConcurrentReadsAndInstalls_DoNotThrow()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");
            var package = BuildPackage(scope, registryDir, Manifest("concurrent.read.ext", "1.0.0"));

            var manager = new ExtensionPackageManager(installRoot);
            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            System.Threading.Tasks.Parallel.For(0, 60, i =>
            {
                try
                {
                    if (i % 4 == 0) manager.Install(package.PackagePath, force: true);
                    else if (i % 4 == 1) _ = manager.Installed;
                    else if (i % 4 == 2) _ = manager.Log;
                    else _ = manager.GetLoadDirectories();
                }
                catch (Exception ex) { errors.Add(ex); }
            });

            Assert.Empty(errors);
        }

        [Fact]
        public void InstalledPackage_LoadsIntoExtensionHost()
        {
            using var scope = new TempScope();
            var registryDir = scope.Dir("beep_reg_");
            var installRoot = scope.Dir("beep_install_");

            var testAssembly = typeof(ExtensionMarketplaceTests).Assembly.Location;
            var package = BuildPackage(
                scope,
                registryDir,
                Manifest("tests.extension.package", "1.0.0"),
                payloadBytes: File.ReadAllBytes(testAssembly),
                payloadName: Path.GetFileName(testAssembly));

            var manager = new ExtensionPackageManager(installRoot);
            var result = manager.Install(package.PackagePath);
            Assert.True(result.Success, result.Error);

            var host = new SkiaExtensionHost();
            host.LoadFromDirectory(result.Extension.InstallPath);

            Assert.Contains(host.Extensions, e => e.Id == "test.extension" && e.Success);
        }
    }
}
