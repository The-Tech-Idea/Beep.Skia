using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Beep.Skia.Extensions.Marketplace
{
    /// <summary>
    /// A source of extension packages. Implemented offline by <see cref="LocalExtensionRegistry"/>;
    /// an HTTP-backed implementation can plug in for a hosted marketplace.
    /// </summary>
    public interface IExtensionRegistry
    {
        /// <summary>Registry display name.</summary>
        string Name { get; }

        /// <summary>Searches packages by text (id/name/description/tags) and/or tag.</summary>
        IReadOnlyList<ExtensionPackage> Search(string query = null, string tag = null, int max = 50);

        /// <summary>Gets a specific version, or the newest version when <paramref name="version"/> is null.</summary>
        ExtensionPackage GetPackage(string id, string version = null);

        /// <summary>Copies the package archive to a local path.</summary>
        bool TryDownload(ExtensionPackage package, string targetPath, out string error);
    }

    /// <summary>
    /// Offline registry backed by a folder of .beepkg files. Useful for tests, air-gapped installs,
    /// and as a local cache in front of a hosted marketplace.
    /// </summary>
    public class LocalExtensionRegistry : IExtensionRegistry
    {
        private readonly string _root;
        private readonly List<ExtensionPackage> _packages = new List<ExtensionPackage>();

        public LocalExtensionRegistry(string root, bool scanOnCreate = true)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            if (scanOnCreate) Refresh();
        }

        public string Name => "local";

        /// <summary>Folder containing the .beepkg files.</summary>
        public string Root => _root;

        /// <summary>Packages currently indexed, newest version per id first.</summary>
        public IReadOnlyList<ExtensionPackage> Packages => _packages.AsReadOnly();

        /// <summary>Rescans the registry folder.</summary>
        public void Refresh()
        {
            _packages.Clear();
            if (!Directory.Exists(_root)) return;

            foreach (var file in Directory.GetFiles(_root, "*" + ExtensionPackageBuilder.PackageExtension))
            {
                try
                {
                    var manifest = ExtensionPackageBuilder.ReadManifest(file);
                    if (manifest == null || manifest.Validate() != null) continue;

                    _packages.Add(new ExtensionPackage
                    {
                        Manifest = manifest,
                        PackagePath = Path.GetFullPath(file),
                        SizeBytes = new FileInfo(file).Length,
                        Sha256 = ExtensionPackageBuilder.ComputeSha256(file)
                    });
                }
                catch
                {
                    // A malformed package must not break the registry.
                }
            }
        }

        public IReadOnlyList<ExtensionPackage> Search(string query = null, string tag = null, int max = 50)
        {
            IEnumerable<ExtensionPackage> results = _packages;

            if (!string.IsNullOrWhiteSpace(query))
            {
                var needle = query.Trim();
                results = results.Where(p =>
                    Contains(p.Id, needle) ||
                    Contains(p.Name, needle) ||
                    Contains(p.Manifest?.Description, needle) ||
                    (p.Manifest?.Tags?.Any(t => Contains(t, needle)) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var needle = tag.Trim();
                results = results.Where(p => p.Manifest?.Tags?.Any(t => Contains(t, needle)) ?? false);
            }

            return results
                .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(p => Parse(p.Version))
                .Take(Math.Max(1, max))
                .ToList()
                .AsReadOnly();
        }

        public ExtensionPackage GetPackage(string id, string version = null)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var matches = _packages.Where(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(version))
            {
                return matches.FirstOrDefault(p =>
                    string.Equals(p.Version, version, StringComparison.OrdinalIgnoreCase));
            }

            return matches.OrderByDescending(p => Parse(p.Version)).FirstOrDefault();
        }

        public bool TryDownload(ExtensionPackage package, string targetPath, out string error)
        {
            error = null;
            if (package == null) { error = "Package is required."; return false; }
            if (string.IsNullOrWhiteSpace(targetPath)) { error = "Target path is required."; return false; }
            if (!File.Exists(package.PackagePath)) { error = $"Package file not found: {package.PackagePath}"; return false; }

            try
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(targetPath));
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.Copy(package.PackagePath, targetPath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool Contains(string value, string needle)
            => !string.IsNullOrEmpty(value) && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

        private static SemanticVersion Parse(string version)
            => SemanticVersion.TryParse(version, out var parsed) ? parsed : new SemanticVersion(0, 0, 0);
    }
}
