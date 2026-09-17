using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Triggers
{
    /// <summary>
    /// File-system trigger: fires when files matching a filter are created, changed,
    /// deleted, or renamed under a watched directory.
    /// Configuration: Path, Filter (default *.*), IncludeSubdirectories.
    /// </summary>
    public class FileWatchTrigger : TriggerBase
    {
        public override string Name => "File Watch Trigger";
        public override TriggerType TriggerType => TriggerType.FileSystem;

        /// <summary>Directory to watch.</summary>
        public string Path { get; set; } = ".";

        /// <summary>File filter (e.g., *.csv).</summary>
        public string Filter { get; set; } = "*.*";

        /// <summary>Whether to watch subdirectories.</summary>
        public bool IncludeSubdirectories { get; set; }

        private FileSystemWatcher _watcher;

        public override Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        {
            base.InitializeAsync(configuration, cancellationToken);
            Path = GetConfig("Path", Path);
            Filter = GetConfig("Filter", Filter);
            IncludeSubdirectories = GetConfig("IncludeSubdirectories", IncludeSubdirectories);
            return Task.FromResult(true);
        }

        public override Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Directory.Exists(Path)
                ? ValidationResult.Success()
                : ValidationResult.Failure($"Directory does not exist: {Path}"));

        public override Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsActive) return Task.FromResult(true);
            if (!Directory.Exists(Path))
            {
                RaiseError(new DirectoryNotFoundException($"Directory does not exist: {Path}"));
                return Task.FromResult(false);
            }

            try
            {
                _watcher = new FileSystemWatcher(Path, Filter)
                {
                    IncludeSubdirectories = IncludeSubdirectories,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                };
                _watcher.Created += OnChanged;
                _watcher.Changed += OnChanged;
                _watcher.Deleted += OnChanged;
                _watcher.Renamed += OnRenamed;
                _watcher.EnableRaisingEvents = true;
                IsActive = true;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                RaiseError(ex, "Failed to start file watcher.");
                return Task.FromResult(false);
            }
        }

        public override Task<bool> StopAsync(CancellationToken cancellationToken = default)
        {
            if (_watcher != null)
            {
                try
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnChanged;
                    _watcher.Changed -= OnChanged;
                    _watcher.Deleted -= OnChanged;
                    _watcher.Renamed -= OnRenamed;
                    _watcher.Dispose();
                }
                catch { }
                _watcher = null;
            }

            IsActive = false;
            return Task.FromResult(true);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            RaiseTriggered(new Dictionary<string, object>
            {
                ["changeType"] = e.ChangeType.ToString(),
                ["fullPath"] = e.FullPath,
                ["name"] = e.Name
            });
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            RaiseTriggered(new Dictionary<string, object>
            {
                ["changeType"] = "Renamed",
                ["fullPath"] = e.FullPath,
                ["oldFullPath"] = e.OldFullPath,
                ["name"] = e.Name
            });
        }

        public override Dictionary<string, object> GetConfigurationSchema()
            => new Dictionary<string, object>
            {
                ["Path"] = "Directory to watch",
                ["Filter"] = "File filter, e.g., *.csv",
                ["IncludeSubdirectories"] = "Watch subdirectories (bool)"
            };
    }
}
