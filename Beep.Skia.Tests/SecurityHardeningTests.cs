using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Beep.Skia.Automation;
using Beep.Skia.Extensions.Marketplace;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Security-focused hardening: decompression bombs in packages and hostile vault documents.
    /// </summary>
    public class SecurityHardeningTests
    {
        private static string NewTempDir(string prefix)
        {
            var path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        // ── Package decompression bombs ──────────────────────────────────────

        private static string BuildPackageWithEntry(string registryDir, string entryName, long uncompressedBytes)
        {
            var packagePath = Path.Combine(registryDir, "bomb.beepkg");
            var manifest = new ExtensionManifest { Id = "bomb.ext", Name = "Bomb", Version = "1.0.0" };

            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
            {
                var manifestEntry = archive.CreateEntry(ExtensionManifest.FileName);
                using (var writer = new StreamWriter(manifestEntry.Open()))
                    writer.Write(System.Text.Json.JsonSerializer.Serialize(manifest));

                var bomb = archive.CreateEntry(entryName);
                using var stream = bomb.Open();
                var buffer = new byte[81920];
                long written = 0;
                while (written < uncompressedBytes)
                {
                    var chunk = (int)Math.Min(buffer.Length, uncompressedBytes - written);
                    stream.Write(buffer, 0, chunk);
                    written += chunk;
                }
            }

            return packagePath;
        }

        [Fact]
        public void Install_RejectsPackageExceedingTotalUncompressedSize()
        {
            var registryDir = NewTempDir("beep_bomb_reg_");
            var installRoot = NewTempDir("beep_bomb_install_");
            try
            {
                // 2 MB of zeros compresses to a few KB: a classic zip bomb shape.
                var package = BuildPackageWithEntry(registryDir, "payload.bin", 2 * 1024 * 1024);

                var manager = new ExtensionPackageManager(installRoot) { MaxTotalUncompressedBytes = 512 * 1024 };
                var result = manager.Install(package);

                Assert.False(result.Success);
                Assert.Contains("total limit", result.Error);
                Assert.False(Directory.Exists(Path.Combine(installRoot, "bomb.ext")));
            }
            finally
            {
                try { Directory.Delete(registryDir, true); } catch { }
                try { Directory.Delete(installRoot, true); } catch { }
            }
        }

        [Fact]
        public void Install_RejectsPackageExceedingPerEntryLimit()
        {
            var registryDir = NewTempDir("beep_bomb_reg_");
            var installRoot = NewTempDir("beep_bomb_install_");
            try
            {
                var package = BuildPackageWithEntry(registryDir, "payload.bin", 2 * 1024 * 1024);

                var manager = new ExtensionPackageManager(installRoot)
                {
                    MaxTotalUncompressedBytes = 100 * 1024 * 1024,
                    MaxEntryUncompressedBytes = 1024 * 1024
                };
                var result = manager.Install(package);

                Assert.False(result.Success);
                Assert.Contains("per-entry limit", result.Error);
            }
            finally
            {
                try { Directory.Delete(registryDir, true); } catch { }
                try { Directory.Delete(installRoot, true); } catch { }
            }
        }

        [Fact]
        public void Install_RejectsPackageWithTooManyEntries()
        {
            var registryDir = NewTempDir("beep_bomb_reg_");
            var installRoot = NewTempDir("beep_bomb_install_");
            try
            {
                var packagePath = Path.Combine(registryDir, "many.beepkg");
                using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
                {
                    var manifestEntry = archive.CreateEntry(ExtensionManifest.FileName);
                    using (var writer = new StreamWriter(manifestEntry.Open()))
                        writer.Write(System.Text.Json.JsonSerializer.Serialize(
                            new ExtensionManifest { Id = "many.ext", Name = "Many", Version = "1.0.0" }));

                    for (var i = 0; i < 50; i++)
                        archive.CreateEntry($"file{i}.txt").Open().Dispose();
                }

                var manager = new ExtensionPackageManager(installRoot) { MaxEntryCount = 10 };
                var result = manager.Install(packagePath);

                Assert.False(result.Success);
                Assert.Contains("entries", result.Error);
            }
            finally
            {
                try { Directory.Delete(registryDir, true); } catch { }
                try { Directory.Delete(installRoot, true); } catch { }
            }
        }

        // ── Credential vault hostile documents ───────────────────────────────

        [Fact]
        public void Vault_Import_MalformedJson_ThrowsInvalidData()
        {
            var vault = new CredentialVault("pass");
            Assert.Throws<InvalidDataException>(() => vault.Import("{ not json }"));
            Assert.Throws<InvalidDataException>(() => vault.Import("[1,2,3]"));
            Assert.Throws<InvalidDataException>(() => vault.Import("null"));
        }

        [Fact]
        public void Vault_Import_InvalidBase64Salt_ThrowsInvalidData()
        {
            var vault = new CredentialVault("pass");
            var json = "{\"Salt\":\"!!!not-base64!!!\",\"Entries\":[]}";

            var ex = Assert.Throws<InvalidDataException>(() => vault.Import(json));
            Assert.Contains("salt", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Vault_Import_EncryptedWithoutPassphrase_ThrowsCryptographic()
        {
            var source = new CredentialVault("secret");
            source.Set(new CredentialEntry { Name = "api", Secret = "s3cret" });
            var exported = source.Export();

            var target = new CredentialVault();
            Assert.Throws<CryptographicException>(() => target.Import(exported));
        }

        [Fact]
        public void Vault_Import_WrongPassphrase_ThrowsCryptographic()
        {
            var source = new CredentialVault("right");
            source.Set(new CredentialEntry { Name = "api", Secret = "s3cret" });

            var target = new CredentialVault("wrong");
            Assert.Throws<CryptographicException>(() => target.Import(source.Export()));
        }

        [Fact]
        public void Vault_Import_TamperedCiphertext_ThrowsCryptographic()
        {
            var source = new CredentialVault("pass");
            source.Set(new CredentialEntry { Name = "api", Secret = "s3cret" });
            var exported = source.Export();

            var document = System.Text.Json.Nodes.JsonNode.Parse(exported);
            var entry = document["Entries"].AsArray()[0];
            var secret = entry["EncryptedSecret"].GetValue<string>();
            Assert.False(string.IsNullOrEmpty(secret), "export should contain an encrypted secret");

            // Flip a character in the middle of the base64 payload (stays valid base64, breaks AES-GCM).
            var chars = secret.ToCharArray();
            var index = chars.Length / 2;
            chars[index] = chars[index] == 'A' ? 'B' : 'A';
            entry["EncryptedSecret"] = new string(chars);

            var target = new CredentialVault("pass");
            Assert.Throws<CryptographicException>(() => target.Import(document.ToJsonString()));
        }

        [Fact]
        public void Vault_RoundTrip_StillWorks()
        {
            var source = new CredentialVault("pass");
            source.Set(new CredentialEntry { Name = "api", Username = "u", Secret = "s3cret" });

            var target = new CredentialVault("pass");
            target.Import(source.Export());

            Assert.Equal("s3cret", target.Get("api").Secret);
            Assert.Equal("u", target.Get("api").Username);
        }

        [Fact]
        public void Vault_Export_DoesNotContainPlaintextSecret()
        {
            var vault = new CredentialVault("pass");
            vault.Set(new CredentialEntry { Name = "api", Secret = "super-secret-value" });

            var exported = vault.Export();

            Assert.DoesNotContain("super-secret-value", exported);
        }
    }
}
