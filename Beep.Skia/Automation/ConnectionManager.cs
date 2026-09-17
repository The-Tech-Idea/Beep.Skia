using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Automation
{
    /// <summary>
    /// A named connection definition used by automation nodes (database, API, file share, ...).
    /// Credentials are referenced by name and resolved through the <see cref="CredentialVault"/>.
    /// </summary>
    public class AutomationConnection
    {
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string CredentialName { get; set; } = string.Empty;
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public AutomationConnection Clone() => new AutomationConnection
        {
            Name = Name,
            Provider = Provider,
            Endpoint = Endpoint,
            Database = Database,
            CredentialName = CredentialName,
            Properties = new Dictionary<string, string>(Properties),
            UpdatedAt = UpdatedAt
        };
    }

    /// <summary>
    /// Registry of named connections with credential resolution through a vault.
    /// </summary>
    public class ConnectionManager
    {
        private readonly Dictionary<string, AutomationConnection> _connections =
            new Dictionary<string, AutomationConnection>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Credential vault used to resolve connection secrets.</summary>
        public CredentialVault Vault { get; }

        public ConnectionManager(CredentialVault vault = null)
        {
            Vault = vault ?? new CredentialVault();
        }

        public int Count => _connections.Count;

        /// <summary>Adds or replaces a connection definition.</summary>
        public void Register(AutomationConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(connection.Name))
                throw new ArgumentException("Connection name is required.", nameof(connection));

            connection.UpdatedAt = DateTime.UtcNow;
            _connections[connection.Name] = connection.Clone();
        }

        /// <summary>Gets a connection by name, or null.</summary>
        public AutomationConnection Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _connections.TryGetValue(name, out var connection) ? connection.Clone() : null;
        }

        /// <summary>Removes a connection.</summary>
        public bool Remove(string name)
            => !string.IsNullOrWhiteSpace(name) && _connections.Remove(name);

        /// <summary>Lists all connections (sorted by name).</summary>
        public IReadOnlyList<AutomationConnection> List()
            => _connections.Values
                .Select(c => c.Clone())
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

        /// <summary>
        /// Resolves a connection into a flat settings dictionary, merging custom properties and
        /// credential values. Missing credentials add a "_warning" entry instead of throwing.
        /// </summary>
        public Dictionary<string, object> Resolve(string name, bool includeSecrets = true)
        {
            var connection = Get(name);
            if (connection == null) return null;

            var settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = connection.Name,
                ["provider"] = connection.Provider,
                ["endpoint"] = connection.Endpoint,
                ["database"] = connection.Database
            };

            foreach (var kvp in connection.Properties)
                settings[kvp.Key] = kvp.Value;

            if (!string.IsNullOrWhiteSpace(connection.CredentialName))
            {
                var credential = Vault?.Get(connection.CredentialName);
                if (credential == null)
                {
                    settings["_warning"] = $"Credential '{connection.CredentialName}' was not found in the vault.";
                }
                else
                {
                    settings["credentialName"] = credential.Name;
                    settings["credentialType"] = credential.Type.ToString();
                    settings["username"] = credential.Username;
                    if (includeSecrets) settings["secret"] = credential.Secret;

                    foreach (var kvp in credential.Metadata)
                        settings["credential." + kvp.Key] = kvp.Value;
                }
            }

            return settings;
        }

        /// <summary>
        /// Performs a configuration-level connection test (provider/endpoint/credential presence).
        /// Provider-specific network probing is left to the host.
        /// </summary>
        public string TestConnection(string name)
        {
            var connection = Get(name);
            if (connection == null) return $"Connection '{name}' was not found.";
            if (string.IsNullOrWhiteSpace(connection.Provider)) return $"Connection '{name}' has no provider.";
            if (string.IsNullOrWhiteSpace(connection.Endpoint)) return $"Connection '{name}' has no endpoint.";

            if (!string.IsNullOrWhiteSpace(connection.CredentialName))
            {
                var credential = Vault?.Get(connection.CredentialName);
                if (credential == null)
                    return $"Connection '{name}': credential '{connection.CredentialName}' is missing.";

                return $"OK: {connection.Provider} -> {connection.Endpoint} (credential '{credential.Name}', {credential.Type})";
            }

            return $"OK: {connection.Provider} -> {connection.Endpoint} (anonymous)";
        }
    }
}
