using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Beep.Skia.Automation
{
    /// <summary>
    /// Kind of credential stored in the vault.
    /// </summary>
    public enum CredentialType
    {
        ApiKey,
        UsernamePassword,
        OAuth2,
        Token,
        ConnectionString,
        Certificate
    }

    /// <summary>
    /// A named credential. <see cref="Secret"/> is held in memory; exports encrypt it.
    /// </summary>
    public class CredentialEntry
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public CredentialType Type { get; set; } = CredentialType.ApiKey;
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the secret.
        /// </summary>
        public string Secret { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the clone.
        /// </summary>
        public CredentialEntry Clone() => new CredentialEntry
        {
            Name = Name,
            Type = Type,
            Username = Username,
            Secret = Secret,
            Metadata = new Dictionary<string, string>(Metadata),
            UpdatedAt = UpdatedAt
        };
    }

    /// <summary>
    /// In-memory credential store with passphrase-encrypted export/import (AES + PBKDF2).
    /// Secrets are never included in listings; use <see cref="Get"/> to retrieve them.
    /// </summary>
    public class CredentialVault
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;
        private const string ExportVersion = "1.0";

        private readonly Dictionary<string, CredentialEntry> _entries =
            new Dictionary<string, CredentialEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly string _passphrase;

        /// <summary>Guards all mutable state: workflows may read/write credentials concurrently.</summary>
        private readonly object _sync = new object();

        /// <summary>
        /// Creates a vault. When a passphrase is provided, exports are encrypted.
        /// </summary>
        public CredentialVault(string passphrase = null)
        {
            _passphrase = passphrase;
        }

        /// <summary>Number of stored credentials.</summary>
        public int Count
        {
            get { lock (_sync) { return _entries.Count; } }
        }

        /// <summary>
        /// Adds or replaces a credential.
        /// </summary>
        public void Set(CredentialEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Name))
                throw new ArgumentException("Credential name is required.", nameof(entry));

            entry.UpdatedAt = DateTime.UtcNow;
            lock (_sync) { _entries[entry.Name] = entry.Clone(); }
        }

        /// <summary>
        /// Gets a copy of the credential (including the secret), or null when missing.
        /// </summary>
        public CredentialEntry Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            lock (_sync) { return _entries.TryGetValue(name, out var entry) ? entry.Clone() : null; }
        }

        /// <summary>
        /// Removes a credential.
        /// </summary>
        public bool Remove(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            lock (_sync) { return _entries.Remove(name); }
        }

        /// <summary>
        /// Lists credentials with secrets masked (safe for UI display).
        /// </summary>
        public IReadOnlyList<CredentialEntry> List()
        {
            lock (_sync)
            {
                return _entries.Values
                    .Select(e =>
                    {
                        var copy = e.Clone();
                        copy.Secret = Mask(copy.Secret);
                        return copy;
                    })
                    .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Exports the vault to JSON. Secrets are encrypted when a passphrase was supplied;
        /// otherwise they are stored as-is (development mode).
        /// </summary>
        public string Export()
        {
            if (string.IsNullOrEmpty(_passphrase))
            {
                var plain = new ExportDocument
                {
                    Version = ExportVersion,
                    Salt = null,
                    Entries = _entries.Values.Select(e => ToExportEntry(e, encryptedSecret: null)).ToList()
                };
                return JsonSerializer.Serialize(plain, JsonOptions);
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = DeriveKey(_passphrase, salt);

            var document = new ExportDocument
            {
                Version = ExportVersion,
                Salt = Convert.ToBase64String(salt),
                Entries = _entries.Values
                    .Select(e => ToExportEntry(e, encryptedSecret: Encrypt(e.Secret ?? string.Empty, key)))
                    .ToList()
            };
            return JsonSerializer.Serialize(document, JsonOptions);
        }

        /// <summary>
        /// Imports a vault export, replacing current contents. Throws when the passphrase is wrong.
        /// </summary>
        public void Import(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            ExportDocument document;
            try
            {
                document = JsonSerializer.Deserialize<ExportDocument>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Invalid vault document: " + ex.Message, ex);
            }

            if (document == null)
                throw new InvalidDataException("Invalid vault document.");

            byte[] key = null;
            if (!string.IsNullOrEmpty(document.Salt))
            {
                if (string.IsNullOrEmpty(_passphrase))
                    throw new CryptographicException("This vault export is encrypted; a passphrase is required.");

                byte[] salt;
                try
                {
                    salt = Convert.FromBase64String(document.Salt);
                }
                catch (FormatException ex)
                {
                    throw new InvalidDataException("Invalid vault document: the salt is not valid base64.", ex);
                }

                key = DeriveKey(_passphrase, salt);
            }

            var imported = new Dictionary<string, CredentialEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in document.Entries ?? new List<ExportEntry>())
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;

                string secret = item.EncryptedSecret;
                if (!string.IsNullOrEmpty(item.EncryptedSecret) && key != null)
                {
                    try { secret = Decrypt(item.EncryptedSecret, key); }
                    catch (CryptographicException)
                    {
                        throw new CryptographicException("Failed to decrypt credentials: wrong passphrase or corrupted vault.");
                    }
                }

                imported[item.Name] = new CredentialEntry
                {
                    Name = item.Name,
                    Type = item.Type,
                    Username = item.Username ?? string.Empty,
                    Secret = secret ?? string.Empty,
                    Metadata = item.Metadata ?? new Dictionary<string, string>(),
                    UpdatedAt = item.UpdatedAt
                };
            }

            lock (_sync)
            {
                _entries.Clear();
                foreach (var kvp in imported) _entries[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Masks a secret for display: keeps the last two characters.
        /// </summary>
        public static string Mask(string secret)
        {
            if (string.IsNullOrEmpty(secret)) return string.Empty;
            if (secret.Length <= 2) return new string('•', secret.Length);
            return new string('•', Math.Min(8, secret.Length - 2)) + secret.Substring(secret.Length - 2);
        }

        // ── Crypto helpers ───────────────────────────────────────────────────

        private static byte[] DeriveKey(string passphrase, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        }

        private static string Encrypt(string plaintext, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            byte[] cipher = encryptor.TransformFinalBlock(data, 0, data.Length);

            var payload = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, payload, aes.IV.Length, cipher.Length);
            return Convert.ToBase64String(payload);
        }

        private static string Decrypt(string encoded, byte[] key)
        {
            byte[] payload = Convert.FromBase64String(encoded);
            using var aes = Aes.Create();
            aes.Key = key;

            int ivLength = aes.BlockSize / 8;
            if (payload.Length <= ivLength) throw new CryptographicException("Invalid encrypted payload.");

            var iv = new byte[ivLength];
            Buffer.BlockCopy(payload, 0, iv, 0, ivLength);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] cipher = new byte[payload.Length - ivLength];
            Buffer.BlockCopy(payload, ivLength, cipher, 0, cipher.Length);
            byte[] data = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(data);
        }

        private static ExportEntry ToExportEntry(CredentialEntry entry, string encryptedSecret)
            => new ExportEntry
            {
                Name = entry.Name,
                Type = entry.Type,
                Username = entry.Username,
                EncryptedSecret = encryptedSecret ?? entry.Secret,
                Metadata = entry.Metadata,
                UpdatedAt = entry.UpdatedAt
            };

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private sealed class ExportDocument
        {
            /// <summary>
            /// Gets or sets the version.
            /// </summary>
            public string Version { get; set; }
            /// <summary>
            /// Gets or sets the salt.
            /// </summary>
            public string Salt { get; set; }
            /// <summary>
            /// Gets or sets the entries.
            /// </summary>
            public List<ExportEntry> Entries { get; set; }
        }

        private sealed class ExportEntry
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Gets or sets the type.
            /// </summary>
            public CredentialType Type { get; set; }
            /// <summary>
            /// Gets or sets the username.
            /// </summary>
            public string Username { get; set; }
            /// <summary>
            /// Gets or sets the encrypted secret.
            /// </summary>
            public string EncryptedSecret { get; set; }
            public Dictionary<string, string> Metadata { get; set; }
            /// <summary>
            /// Gets or sets the updated at.
            /// </summary>
            public DateTime UpdatedAt { get; set; }
        }
    }
}
