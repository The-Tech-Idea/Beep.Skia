using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Beep.Skia.Automation;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the automation credential vault and connection manager.
    /// </summary>
    public class CredentialVaultTests
    {
        [Fact]
        public void Vault_StoresAndRetrievesCredentials()
        {
            var vault = new CredentialVault();
            vault.Set(new CredentialEntry
            {
                Name = "sql_prod",
                Type = CredentialType.UsernamePassword,
                Username = "svc_etl",
                Secret = "p@ssw0rd!"
            });

            var entry = vault.Get("sql_prod");
            Assert.NotNull(entry);
            Assert.Equal("svc_etl", entry.Username);
            Assert.Equal("p@ssw0rd!", entry.Secret);

            Assert.True(vault.Remove("sql_prod"));
            Assert.Null(vault.Get("sql_prod"));
        }

        [Fact]
        public void Vault_SetIsCaseInsensitiveAndReplaces()
        {
            var vault = new CredentialVault();
            vault.Set(new CredentialEntry { Name = "api_key", Secret = "first" });
            vault.Set(new CredentialEntry { Name = "API_KEY", Secret = "second" });

            Assert.Equal(1, vault.Count);
            Assert.Equal("second", vault.Get("api_key").Secret);
        }

        [Fact]
        public void Vault_ListMasksSecrets()
        {
            var vault = new CredentialVault();
            vault.Set(new CredentialEntry { Name = "token", Secret = "abcdef1234" });

            var listed = vault.List();
            Assert.Single(listed);
            Assert.DoesNotContain("abcdef", listed[0].Secret);
            Assert.EndsWith("34", listed[0].Secret);
        }

        [Fact]
        public void Vault_EmptyName_Throws()
        {
            var vault = new CredentialVault();
            Assert.Throws<ArgumentException>(() => vault.Set(new CredentialEntry { Name = "  " }));
        }

        [Fact]
        public void Vault_EncryptedExport_RoundTripsAndHidesPlaintext()
        {
            var vault = new CredentialVault("correct horse battery staple");
            vault.Set(new CredentialEntry
            {
                Name = "warehouse",
                Type = CredentialType.ApiKey,
                Secret = "super-secret-value",
                Metadata = new Dictionary<string, string> { ["tenant"] = "acme" }
            });

            var json = vault.Export();
            Assert.DoesNotContain("super-secret-value", json);

            var restored = new CredentialVault("correct horse battery staple");
            restored.Import(json);

            var entry = restored.Get("warehouse");
            Assert.NotNull(entry);
            Assert.Equal("super-secret-value", entry.Secret);
            Assert.Equal("acme", entry.Metadata["tenant"]);
        }

        [Fact]
        public void Vault_WrongPassphrase_ThrowsOnImport()
        {
            var vault = new CredentialVault("right-passphrase");
            vault.Set(new CredentialEntry { Name = "x", Secret = "secret" });
            var json = vault.Export();

            var wrong = new CredentialVault("wrong-passphrase");
            Assert.Throws<CryptographicException>(() => wrong.Import(json));
        }

        [Fact]
        public void Vault_EncryptedExport_RequiresPassphraseOnImport()
        {
            var vault = new CredentialVault("passphrase");
            vault.Set(new CredentialEntry { Name = "x", Secret = "secret" });
            var json = vault.Export();

            var noPassphrase = new CredentialVault();
            Assert.Throws<CryptographicException>(() => noPassphrase.Import(json));
        }

        [Fact]
        public void ConnectionManager_ResolvesConnectionWithCredential()
        {
            var vault = new CredentialVault();
            vault.Set(new CredentialEntry
            {
                Name = "sql_cred",
                Type = CredentialType.UsernamePassword,
                Username = "svc",
                Secret = "pw"
            });

            var manager = new ConnectionManager(vault);
            manager.Register(new AutomationConnection
            {
                Name = "warehouse",
                Provider = "SqlServer",
                Endpoint = "srv01",
                Database = "Sales",
                CredentialName = "sql_cred",
                Properties = { ["timeout"] = "30" }
            });

            var settings = manager.Resolve("warehouse");

            Assert.Equal("SqlServer", settings["provider"]);
            Assert.Equal("Sales", settings["database"]);
            Assert.Equal("30", settings["timeout"]);
            Assert.Equal("svc", settings["username"]);
            Assert.Equal("pw", settings["secret"]);
        }

        [Fact]
        public void ConnectionManager_MissingCredential_AddsWarning()
        {
            var manager = new ConnectionManager();
            manager.Register(new AutomationConnection
            {
                Name = "api",
                Provider = "Http",
                Endpoint = "https://example.test",
                CredentialName = "missing"
            });

            var settings = manager.Resolve("api");
            Assert.True(settings.ContainsKey("_warning"));
            Assert.False(settings.ContainsKey("secret"));
        }

        [Fact]
        public void ConnectionManager_TestConnection_ReportsIssues()
        {
            var manager = new ConnectionManager();
            Assert.Contains("not found", manager.TestConnection("nope"));

            manager.Register(new AutomationConnection { Name = "bad", Provider = "Http" });
            Assert.Contains("no endpoint", manager.TestConnection("bad"));

            manager.Register(new AutomationConnection
            {
                Name = "good",
                Provider = "Http",
                Endpoint = "https://api.test"
            });
            Assert.StartsWith("OK", manager.TestConnection("good"));
        }

        [Fact]
        public void Mask_HandlesShortAndLongSecrets()
        {
            Assert.Equal(string.Empty, CredentialVault.Mask(null));
            Assert.Equal("••", CredentialVault.Mask("ab"));
            Assert.Equal("••••••••cd", CredentialVault.Mask("abcdefghcd"));
        }
    }
}
