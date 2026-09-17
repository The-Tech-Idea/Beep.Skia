using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace Beep.Skia.Extensions.Marketplace
{
    /// <summary>An extension installed into the local install root.</summary>
    public class InstalledExtension
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
        public bool Enabled { get; set; } = true;

        public override string ToString() => $"{Id} {Version}";
    }

    /// <summary>Outcome of an install/update/uninstall operation.</summary>
    public class ExtensionInstallResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public InstalledExtension Extension { get; set; }

        public static ExtensionInstallResult Ok(InstalledExtension extension)
            => new ExtensionInstallResult { Success = true, Extension = extension };

        public static ExtensionInstallResult Fail(string error)
            => new ExtensionInstallResult { Success = false, Error = error };
    }

    /// <summary>A single line in the package-manager operation log.</summary>
    public class InstallLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public bool Success { get; set; }

        public override string ToString() => $"{Timestamp:HH:mm:ss} {(Success ? "OK " : "ERR")} {Action} {Detail}";
    }

    /// <summary>
    /// Installs, updates, and uninstalls extension packages. Packages are extracted to
    /// <c>installRoot/&lt;id&gt;/&lt;version&gt;</c>; the active version of each extension is tracked in
    /// <c>installed.json</c>. Extract paths are validated against zip-slip entries.
    /// </summary>
    public class ExtensionPackageManager
    {
        private const string CatalogFileName = "installed.json";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly Dictionary<string, InstalledExtension> _installed =
            new Dictionary<string, InstalledExtension>(StringComparer.OrdinalIgnoreCase);
        private readonly List<InstallLogEntry> _log = new List<InstallLogEntry>();

        /// <summary>Guards the catalog, the operation log, and catalog writes.</summary>
        private readonly object _sync = new object();

        public ExtensionPackageManager(string installRoot, IExtensionRegistry registry = null, string hostVersion = "1.0.0")
        {
            if (string.IsNullOrWhiteSpace(installRoot)) throw new ArgumentException("Install root is required.", nameof(installRoot));
            InstallRoot = Path.GetFullPath(installRoot);
            Registry = registry;
            HostVersion = SemanticVersion.TryParse(hostVersion, out var parsed) ? parsed : new SemanticVersion(1, 0, 0);
            Directory.CreateDirectory(InstallRoot);
            LoadCatalog();
        }

        /// <summary>Root folder that holds installed extensions.</summary>
        public string InstallRoot { get; }

        /// <summary>Optional registry used by <see cref="InstallFromRegistry"/> and <see cref="Update"/>.</summary>
        public IExtensionRegistry Registry { get; set; }

        /// <summary>Maximum uncompressed size of a single package entry (default 256 MB).</summary>
        public long MaxEntryUncompressedBytes { get; set; } = 256L * 1024 * 1024;

        /// <summary>Maximum total uncompressed size of a package (default 512 MB). Guards against zip bombs.</summary>
        public long MaxTotalUncompressedBytes { get; set; } = 512L * 1024 * 1024;

        /// <summary>Maximum number of entries in a package (default 10,000).</summary>
        public int MaxEntryCount { get; set; } = 10_000;

        /// <summary>Host version used for manifest min-host-version checks.</summary>
        public SemanticVersion HostVersion { get; }

        /// <summary>Installed extensions keyed by id (active version).</summary>
        public IReadOnlyList<InstalledExtension> Installed
        {
            get { lock (_sync) { return _installed.Values.ToList().AsReadOnly(); } }
        }

        /// <summary>Operation log, oldest first.</summary>
        public IReadOnlyList<InstallLogEntry> Log
        {
            get { lock (_sync) { return _log.ToList().AsReadOnly(); } }
        }

        public InstalledExtension GetInstalled(string id)
            => id != null && _installed.TryGetValue(id, out var extension) ? extension : null;

        public bool IsInstalled(string id, string version = null)
        {
            var installed = GetInstalled(id);
            if (installed == null) return false;
            return version == null || string.Equals(installed.Version, version, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Folders that should be scanned by SkiaExtensionHost for the active extensions.</summary>
        public IReadOnlyList<string> GetLoadDirectories()
            => _installed.Values
                .Where(e => e.Enabled && Directory.Exists(e.InstallPath))
                .Select(e => e.InstallPath)
                .ToList()
                .AsReadOnly();

        /// <summary>Installs a package archive. Validates the manifest, host version, and dependencies.</summary>
        public ExtensionInstallResult Install(string packagePath, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(packagePath)) return Fail("install", "Package path is required.");
            if (!File.Exists(packagePath)) return Fail("install", $"Package file not found: {packagePath}");

            ExtensionManifest manifest;
            try
            {
                manifest = ExtensionPackageBuilder.ReadManifest(packagePath);
            }
            catch (Exception ex)
            {
                return Fail("install", "Package is not a readable archive: " + ex.Message);
            }

            if (manifest == null) return Fail("install", "Package does not contain " + ExtensionManifest.FileName + ".");

            var validationError = manifest.Validate();
            if (validationError != null) return Fail("install", validationError);

            if (manifest.MinHostVersion != null &&
                SemanticVersion.TryParse(manifest.MinHostVersion, out var minimum) &&
                HostVersion < minimum)
            {
                return Fail("install", $"Extension '{manifest.Id}' requires host version {minimum} or newer (host is {HostVersion}).");
            }

            foreach (var dependency in manifest.Dependencies ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(dependency)) continue;
                if (!IsInstalled(dependency))
                    return Fail("install", $"Extension '{manifest.Id}' requires '{dependency}', which is not installed.");
            }

            var existing = GetInstalled(manifest.Id);
            if (existing != null && !force)
            {
                if (string.Equals(existing.Version, manifest.Version, StringComparison.OrdinalIgnoreCase))
                    return Fail("install", $"Extension '{manifest.Id}' {manifest.Version} is already installed.");
                return Fail("install", $"Extension '{manifest.Id}' {existing.Version} is installed; use Update instead.");
            }

            var targetDirectory = Path.Combine(InstallRoot, manifest.Id, manifest.Version);
            try
            {
                if (Directory.Exists(targetDirectory)) Directory.Delete(targetDirectory, recursive: true);
                Directory.CreateDirectory(targetDirectory);
                ExtractSafely(packagePath, targetDirectory);
            }
            catch (Exception ex)
            {
                // Leave no partial state behind: remove the version folder and the (now empty) extension folder.
                try { if (Directory.Exists(targetDirectory)) Directory.Delete(targetDirectory, recursive: true); } catch { }
                try
                {
                    var extensionDirectory = Path.Combine(InstallRoot, manifest.Id);
                    if (Directory.Exists(extensionDirectory) && !Directory.EnumerateFileSystemEntries(extensionDirectory).Any())
                        Directory.Delete(extensionDirectory);
                }
                catch { }
                return Fail("install", "Extraction failed: " + ex.Message);
            }

            var installed = new InstalledExtension
            {
                Id = manifest.Id,
                Name = manifest.Name,
                Version = manifest.Version,
                InstallPath = targetDirectory,
                InstalledAt = DateTime.UtcNow,
                Enabled = true
            };
            lock (_sync)
            {
                _installed[manifest.Id] = installed;
                SaveCatalog();
            }
            Record("install", $"{installed.Id} {installed.Version} → {installed.InstallPath}", true);
            return ExtensionInstallResult.Ok(installed);
        }

        /// <summary>Downloads a package from the registry and installs it.</summary>
        public ExtensionInstallResult InstallFromRegistry(string id, string version = null, bool force = false)
        {
            if (Registry == null) return Fail("install", "No registry configured.");
            var package = Registry.GetPackage(id, version);
            if (package == null) return Fail("install", $"Package '{id}{(version != null ? " " + version : string.Empty)}' was not found in the registry.");

            var tempPath = Path.Combine(Path.GetTempPath(), $"{id}-{package.Version}-{Guid.NewGuid():N}.beepkg");
            try
            {
                if (!Registry.TryDownload(package, tempPath, out var downloadError))
                    return Fail("install", "Download failed: " + downloadError);
                return Install(tempPath, force);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }

        /// <summary>Removes an installed extension and its files.</summary>
        public ExtensionInstallResult Uninstall(string id)
        {
            var installed = GetInstalled(id);
            if (installed == null) return Fail("uninstall", $"Extension '{id}' is not installed.");

            try
            {
                var extensionDirectory = Path.Combine(InstallRoot, installed.Id);
                if (Directory.Exists(extensionDirectory)) Directory.Delete(extensionDirectory, recursive: true);
            }
            catch (Exception ex)
            {
                return Fail("uninstall", "Could not remove extension files: " + ex.Message);
            }

            lock (_sync)
            {
                _installed.Remove(installed.Id);
                SaveCatalog();
            }
            Record("uninstall", $"{installed.Id} {installed.Version}", true);
            return ExtensionInstallResult.Ok(installed);
        }

        /// <summary>Installs the newest registry version when it is newer than the installed one.</summary>
        public ExtensionInstallResult Update(string id, bool force = false)
        {
            if (Registry == null) return Fail("update", "No registry configured.");

            var installed = GetInstalled(id);
            if (installed == null) return Fail("update", $"Extension '{id}' is not installed.");

            var latest = Registry.GetPackage(id);
            if (latest == null) return Fail("update", $"Package '{id}' was not found in the registry.");

            if (!SemanticVersion.TryParse(latest.Version, out var latestVersion) ||
                !SemanticVersion.TryParse(installed.Version, out var currentVersion))
            {
                return Fail("update", "Could not compare versions.");
            }

            if (latestVersion <= currentVersion)
                return Fail("update", $"Extension '{id}' is already up to date ({installed.Version}).");

            var result = InstallFromRegistry(id, latest.Version, force: true);
            if (result.Success) Record("update", $"{id} {installed.Version} → {result.Extension.Version}", true);
            return result;
        }

        /// <summary>
        /// Extracts a package while rejecting entries that would escape the target folder and
        /// packages whose uncompressed size or entry count looks like a decompression bomb.
        /// </summary>
        private void ExtractSafely(string packagePath, string targetDirectory)
        {
            var targetRoot = Path.GetFullPath(targetDirectory);
            if (!targetRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                targetRoot += Path.DirectorySeparatorChar;

            using var archive = ZipFile.OpenRead(packagePath);

            if (archive.Entries.Count > MaxEntryCount)
                throw new InvalidOperationException($"Package contains {archive.Entries.Count} entries, which exceeds the limit of {MaxEntryCount}.");

            long totalBytes = 0;
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/", StringComparison.Ordinal))
                    continue;

                if (entry.Length > MaxEntryUncompressedBytes)
                    throw new InvalidOperationException($"Package entry '{entry.FullName}' expands to {entry.Length} bytes, which exceeds the per-entry limit.");

                totalBytes += entry.Length;
                if (totalBytes > MaxTotalUncompressedBytes)
                    throw new InvalidOperationException($"Package expands to more than {MaxTotalUncompressedBytes} bytes, which exceeds the total limit.");

                var destination = Path.GetFullPath(Path.Combine(targetDirectory, entry.FullName));
                if (!destination.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Package entry '{entry.FullName}' escapes the install directory.");

                var directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                entry.ExtractToFile(destination, overwrite: true);
            }
        }

        private void LoadCatalog()
        {
            var path = Path.Combine(InstallRoot, CatalogFileName);
            if (!File.Exists(path)) return;

            try
            {
                var list = JsonSerializer.Deserialize<List<InstalledExtension>>(File.ReadAllText(path), JsonOptions);
                if (list == null) return;
                foreach (var extension in list)
                {
                    if (extension == null || string.IsNullOrWhiteSpace(extension.Id)) continue;
                    if (string.IsNullOrWhiteSpace(extension.InstallPath))
                        extension.InstallPath = Path.Combine(InstallRoot, extension.Id, extension.Version);
                    _installed[extension.Id] = extension;
                }
            }
            catch
            {
                // A corrupt catalog must not prevent the manager from working.
            }
        }

        private void SaveCatalog()
        {
            try
            {
                var path = Path.Combine(InstallRoot, CatalogFileName);
                File.WriteAllText(path, JsonSerializer.Serialize(_installed.Values.ToList(), JsonOptions));
            }
            catch
            {
                // Persistence failures are non-fatal for the current session.
            }
        }

        private ExtensionInstallResult Fail(string action, string error)
        {
            Record(action, error, false);
            return ExtensionInstallResult.Fail(error);
        }

        private void Record(string action, string detail, bool success)
        {
            lock (_sync) { _log.Add(new InstallLogEntry { Action = action, Detail = detail, Success = success }); }
        }
    }
}
