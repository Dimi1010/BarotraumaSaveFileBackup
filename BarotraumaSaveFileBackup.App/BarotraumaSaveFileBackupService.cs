using Microsoft.Extensions.Options;

namespace BarotraumaSaveFileBackup.App
{

    public class BarotraumaSaveFileBackupOptions
    {
        public const string ConfigurationKey = "BackupService";

        public string BarotraumaSaveFileFolder { get; set; } = "";

        public string BarotraumaBackupFolder { get; set; } = "";

        public bool BackupSingleplayerSaves { get; set; } = false;

        public bool BackupMultiplayerSaves { get; set; } = false;
    }

    public class BarotraumaSaveFileBackupService : IHostedService
    {
        private const string SaveFileExtension = ".save";
        private const string CharacterDataExtension = ".xml";
        private const string BackupSaveFileExtension = ".save.bak";
        private const string BackupCharacterDataFileExtension = ".xml.bak";

        private const string SaveFileFilter = $"*{SaveFileExtension}";
        private const string CharacterDataFilter = $"*{CharacterDataExtension}";

        private readonly ILogger<BarotraumaSaveFileBackupService> _logger;

        public string BarotraumaSaveFileFolder { get; }
        public string BarotraumaBackupFolder { get; }
        
        public bool BackupSingleplayerSaves { get; }
        public bool BackupMultiplayerSaves { get; }

        private AutoDisposableList<FileSystemWatcher> _fileSystemWatchers = new AutoDisposableList<FileSystemWatcher>();

        public BarotraumaSaveFileBackupService(IOptions<BarotraumaSaveFileBackupOptions> options, ILogger<BarotraumaSaveFileBackupService> logger)
        {
            _logger = logger;

            BarotraumaSaveFileFolder = options.Value.BarotraumaSaveFileFolder;
            if (string.IsNullOrWhiteSpace(BarotraumaSaveFileFolder))
                throw new ArgumentException("BarotraumaSaveFileFolder is not set.");

            BarotraumaSaveFileFolder = Environment.ExpandEnvironmentVariables(BarotraumaSaveFileFolder);
            if (!Directory.Exists(BarotraumaSaveFileFolder))
                throw new ArgumentException($"BarotraumaSaveFileFolder '{BarotraumaSaveFileFolder}' cannot be found or is not a directory.");

            BarotraumaBackupFolder = options.Value.BarotraumaBackupFolder;
            if (string.IsNullOrEmpty(BarotraumaBackupFolder))
                BarotraumaBackupFolder = BarotraumaSaveFileFolder;

            if (!Directory.Exists(BarotraumaBackupFolder))
                throw new ArgumentException($"BarotraumaBackupFolder '{BarotraumaBackupFolder}' cannot be found or is not a directory.");

            BackupSingleplayerSaves = options.Value.BackupSingleplayerSaves;
            BackupMultiplayerSaves = options.Value.BackupMultiplayerSaves;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (BackupSingleplayerSaves)
            {
                _fileSystemWatchers.Add(SetupWatcher(BarotraumaSaveFileFolder, SaveFileFilter));
            }

            if (BackupMultiplayerSaves)
            {
                var multiplayerFolder = Path.Join(BarotraumaSaveFileFolder, "Multiplayer");
                _fileSystemWatchers.Add(SetupWatcher(multiplayerFolder, SaveFileFilter));
                _fileSystemWatchers.Add(SetupWatcher(multiplayerFolder, CharacterDataFilter));
            }

            foreach (var item in _fileSystemWatchers)
            {
                item.EnableRaisingEvents = true;
            }

            _logger.LogInformation("Service started.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _fileSystemWatchers.Dispose();
            _logger.LogInformation("Service stopped.");
            return Task.CompletedTask;
        }

        private async void PerformBackup(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("Performing backup for file: {}", e.Name);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");

            string? newExtension = Path.GetExtension(e.Name) switch
            {
                SaveFileExtension => BackupSaveFileExtension,
                CharacterDataExtension => BackupCharacterDataFileExtension,
                _ => null,
            };

            if (newExtension == null) return;

            string? directory = Path.GetDirectoryName(e.FullPath);
            if(directory == null)
            {
                _logger.LogError("No parent directory found for file: {} when performing backup.", e.FullPath);
                return;
            }

            string newName = $"{Path.GetFileNameWithoutExtension(e.Name)}-{timestamp}{newExtension}";
            string newFullPath = Path.Combine(directory, newName);
            try
            {
                File.Copy(e.FullPath, newFullPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(exception: ex, "Copy operation failed. Retry in 3 seconds...");
                await Task.Delay(3000);

                try
                {
                    File.Copy(e.FullPath, newFullPath, true);
                }
                catch (Exception ex2)
                {
                    _logger.LogError(exception: ex2, "Copy operation failed.");
                    return;
                }
            }
            _logger.LogInformation("Backup successful.");
        }

        private void HandleError(object sender, ErrorEventArgs e) => LogException(e.GetException());

        private void LogException(Exception ex) => _logger.LogError(exception: ex, message: "FileSystemWatcher threw an exception.");

        private FileSystemWatcher SetupWatcher(string path, string fileFilter, bool enabled = false)
        {
            var watcher = new FileSystemWatcher(path, fileFilter)
            {
                NotifyFilter = NotifyFilters.LastWrite,
            };

            try
            {
                // TODO: Check if created makes the event fire twice.
                watcher.Created += PerformBackup;
                watcher.Changed += PerformBackup;
                watcher.Error += HandleError;

                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents = enabled;
                return watcher;
            }
            catch (Exception)
            {
                watcher.Dispose();
                throw;
            }
        }
    }
}