namespace BarotraumaSaveFileBackup.App
{
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
        
        public bool BackupSingleplayerSaves { get; }
        public bool BackupMultiplayerSaves { get; }

        private AutoDisposableList<FileSystemWatcher> _fileSystemWatchers = new AutoDisposableList<FileSystemWatcher>();

        public BarotraumaSaveFileBackupService(IConfiguration configuration, ILogger<BarotraumaSaveFileBackupService> logger)
        {
            _logger = logger;

            var backupServiceConfiguration = configuration.GetRequiredSection("BackupService");

            BarotraumaSaveFileFolder = backupServiceConfiguration.GetValue<string>("BarotraumaSaveFileFolder");
            if (BarotraumaSaveFileFolder == null)
                throw new ArgumentException("BarotraumaSaveFileFolder is not set.");

            BarotraumaSaveFileFolder = Environment.ExpandEnvironmentVariables(BarotraumaSaveFileFolder);
            if (!Directory.Exists(BarotraumaSaveFileFolder))
                throw new ArgumentException($"BarotraumaSaveFileFolder '{BarotraumaSaveFileFolder}' cannot be found or is not a directory.");

            BackupSingleplayerSaves = backupServiceConfiguration.GetValue("BackupSingleplayerSaves", false);
            BackupMultiplayerSaves = backupServiceConfiguration.GetValue("BackupMultiplayerSaves", false);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (BackupSingleplayerSaves)
            {
                _fileSystemWatchers.Add(SetupWatcher(BarotraumaSaveFileFolder, SaveFileFilter));
                _fileSystemWatchers.Add(SetupWatcher(BarotraumaSaveFileFolder, CharacterDataFilter));
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
            _logger.LogDebug("Performing backup for file: {}", e.Name);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");

            string? newExtension = Path.GetExtension(e.Name) switch
            {
                SaveFileExtension => BackupSaveFileExtension,
                CharacterDataExtension => BackupCharacterDataFileExtension,
                _ => null,
            };

            if (newExtension == null) return;

            string directory = Path.GetDirectoryName(e.FullPath);
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
            catch (IOException ex)
            {
                _logger.LogWarning(exception: ex, "Copy operation failed. Retry in 3 seconds...");
                await Task.Delay(3000);
                File.Copy(e.FullPath, newFullPath, true);
            }
            _logger.LogDebug("Backup successful.");
        }

        private void HandleError(object sender, ErrorEventArgs e) => LogException(e.GetException());

        private void LogException(Exception ex) => _logger.LogError(exception: ex, message: null);

        private FileSystemWatcher SetupWatcher(string path, string fileFilter, bool enabled = false)
        {
            var watcher = new FileSystemWatcher(path, fileFilter)
            {
                NotifyFilter = NotifyFilters.LastWrite,
            };

            try
            {
                watcher.Created += PerformBackup;
                watcher.Changed += PerformBackup;
                watcher.Error += HandleError;

                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents = enabled;
                return watcher;
            }
            catch (Exception ex)
            {
                watcher.Dispose();
                throw;
            }
        }
    }
}