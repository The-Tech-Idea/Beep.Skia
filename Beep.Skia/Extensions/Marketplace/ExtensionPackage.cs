using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace Beep.Skia.Extensions.Marketplace
{
    /// <summary>
    /// Manifest describing an extension package. Stored as manifest.json inside a .beepkg archive.
    /// </summary>
    public class ExtensionManifest
    {
        public const string FileName = "manifest.json";

        /// <summary>
        /// Gets or sets the schema version.
        /// </summary>
        public int SchemaVersion { get; set; } = 1;
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public string Version { get; set; } = "1.0.0";
        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        public string Author { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>Minimum host version required (semantic version).</summary>
        public string MinHostVersion { get; set; }

        /// <summary>Extension ids this package depends on.</summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the published at.
        /// </summary>
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Validates required fields. Returns null when valid, otherwise the error.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(Id)) return "Manifest 'id' is required.";
            if (string.IsNullOrWhiteSpace(Name)) return "Manifest 'name' is required.";
            if (string.IsNullOrWhiteSpace(Version)) return "Manifest 'version' is required.";
            if (!SemanticVersion.TryParse(Version, out _)) return $"Manifest 'version' ('{Version}') is not a semantic version.";
            if (!string.IsNullOrWhiteSpace(MinHostVersion) && !SemanticVersion.TryParse(MinHostVersion, out _))
                return $"Manifest 'minHostVersion' ('{MinHostVersion}') is not a semantic version.";
            return null;
        }
    }

    /// <summary>An extension package: manifest plus its source archive.</summary>
    public class ExtensionPackage
    {
        /// <summary>
        /// Gets or sets the manifest.
        /// </summary>
        public ExtensionManifest Manifest { get; set; } = new ExtensionManifest();
        /// <summary>
        /// Gets or sets the package path.
        /// </summary>
        public string PackagePath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the size bytes.
        /// </summary>
        public long SizeBytes { get; set; }
        /// <summary>
        /// Gets or sets the sha256.
        /// </summary>
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id => Manifest?.Id ?? string.Empty;
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => Manifest?.Name ?? string.Empty;
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public string Version => Manifest?.Version ?? string.Empty;

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"{Id} {Version}";
    }

    /// <summary>Creates .beepkg archives (zip with manifest.json at the root).</summary>
    public static class ExtensionPackageBuilder
    {
        public const string PackageExtension = ".beepkg";

        /// <summary>
        /// Builds a package from a directory. The manifest is written as manifest.json and
        /// every other file in the directory is included.
        /// </summary>
        public static ExtensionPackage Build(string sourceDirectory, ExtensionManifest manifest, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory)) throw new ArgumentException("Source directory is required.", nameof(sourceDirectory));
            if (!Directory.Exists(sourceDirectory)) throw new DirectoryNotFoundException(sourceDirectory);
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));

            var error = manifest.Validate();
            if (error != null) throw new InvalidOperationException(error);

            var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            if (File.Exists(outputPath)) File.Delete(outputPath);

            using (var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                var manifestEntry = archive.CreateEntry(ExtensionManifest.FileName);
                using (var stream = manifestEntry.Open())
                {
                    var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                }

                foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
                    if (string.Equals(relative, ExtensionManifest.FileName, StringComparison.OrdinalIgnoreCase)) continue;
                    archive.CreateEntryFromFile(file, relative);
                }
            }

            return new ExtensionPackage
            {
                Manifest = manifest,
                PackagePath = Path.GetFullPath(outputPath),
                SizeBytes = new FileInfo(outputPath).Length,
                Sha256 = ComputeSha256(outputPath)
            };
        }

        /// <summary>
        /// Gets or sets the compute sha256.
        /// </summary>
        public static string ComputeSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }

        /// <summary>
        /// Reads a manifest from a package archive without extracting it.
        /// Returns null when the archive is missing, corrupt, or has no manifest.
        /// </summary>
        public static ExtensionManifest ReadManifest(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath)) return null;

            try
            {
                using var archive = ZipFile.OpenRead(packagePath);
                var entry = archive.Entries.FirstOrDefault(e =>
                    string.Equals(e.FullName, ExtensionManifest.FileName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(e.Name, ExtensionManifest.FileName, StringComparison.OrdinalIgnoreCase));
                if (entry == null) return null;

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<ExtensionManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception)
            {
                // A corrupt or unreadable archive must not take the caller down.
                return null;
            }
        }
    }
}
