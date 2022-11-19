using BarotraumaSaveFileBackup.App.Serialization;
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
        private const string SaveFileFilter = $"*{SaveFileExtension}";
        
        private readonly IBackupSerializer _backupSerializer;
        private readonly ILogger<BarotraumaSaveFileBackupService> _logger;

        public string BarotraumaSaveFileFolder { get; }
        
        public bool BackupSingleplayerSaves { get; }
        public bool BackupMultiplayerSaves { get; }

        private readonly AutoDisposableList<FileSystemWatcher> _fileSystemWatchers = new();
        private readonly HashSet<string> _activeBackups = new();

        public BarotraumaSaveFileBackupService(IOptions<BarotraumaSaveFileBackupOptions> options, IBackupSerializer backupSerializer, ILogger<BarotraumaSaveFileBackupService> logger)
        {
            _backupSerializer = backupSerializer;
            _logger = logger;

            BarotraumaSaveFileFolder = options.Value.BarotraumaSaveFileFolder;
            if (string.IsNullOrWhiteSpace(BarotraumaSaveFileFolder))
                throw new ArgumentException("BarotraumaSaveFileFolder is not set.");

            if (!Directory.Exists(BarotraumaSaveFileFolder))
                throw new ArgumentException($"BarotraumaSaveFileFolder '{BarotraumaSaveFileFolder}' cannot be found or is not a directory.");

            BackupSingleplayerSaves = options.Value.BackupSingleplayerSaves;
            BackupMultiplayerSaves = options.Value.BackupMultiplayerSaves;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (BackupSingleplayerSaves)
            {
                _fileSystemWatchers.Add(SetupWatcher(BarotraumaSaveFileFolder));
            }

            if (BackupMultiplayerSaves)
            {
                var multiplayerFolder = Path.Join(BarotraumaSaveFileFolder, "Multiplayer");
                _fileSystemWatchers.Add(SetupWatcher(multiplayerFolder, multiplayer: true));
            }

            foreach (var item in _fileSystemWatchers)
            {
                item.EnableRaisingEvents = true;
            }

            _logger.LogInformation("Service started. Monitor status: [ Singleplayer: {}; Multiplayer: {} ]", BackupSingleplayerSaves, BackupMultiplayerSaves);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _fileSystemWatchers.Dispose();
            _logger.LogInformation("Service stopped.");
            return Task.CompletedTask;
        }

        private async void PerformBackup(object sender, FileSystemEventArgs e, bool multiplayer)
        {
            if(e.Name == null)
            {
                _logger.LogError("Attempted backup with no file path.");
                return;
            }    

            _logger.LogInformation("Save file {} updated.", e.Name);
            
            lock(_activeBackups)
            {
                if (_activeBackups.Contains(e.Name))
                {
                    _logger.LogInformation("Save file {} already has an active backup operation running.", e.Name);
                    return;
                }
                else
                {
                    _logger.LogInformation("Queueing backup operation for savefile {}. Backup will start in 5 seconds.", e.Name);
                    _activeBackups.Add(e.Name);
                }
            }

            try
            {
                // 5 seconds delay to hopefully have the game process and write all its files.
                await Task.Delay(5000);

                _logger.LogInformation("Performing backup for savefile: {}", e.Name);

                var saveFile = e.FullPath;
                var characterData = multiplayer ? 
                    $"{Path.GetDirectoryName(e.FullPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(e.FullPath)}_CharacterData.xml" 
                    : null;

                _logger.LogTrace("Save operation started with:\nSavefile Path: {}\nCharacter Data Path: {}", saveFile, characterData);

                if (characterData != null)
                {
                    for(var attempts = 1; attempts <= 3; attempts++)
                    {
                        if (!File.Exists(characterData))
                        {
                            var retrySeconds = 2;
                            _logger.LogWarning("Character data not found. Retrying in {} seconds...", retrySeconds);
                            await Task.Delay(retrySeconds * 1000);
                        }
                    }

                    if(!File.Exists(characterData))
                    {
                        _logger.LogError("Character data not found. Backup cancelled.");
                        return;
                    }
                }

                await _backupSerializer.SerializeAsync(saveFile, characterData);
            }
            finally
            {
                lock (_activeBackups)
                {
                    _activeBackups.Remove(e.Name);
                }
            }
            
            _logger.LogInformation("Backup successful.");
        }

        private void HandleError(object sender, ErrorEventArgs e) => LogException(e.GetException());

        private void LogException(Exception ex) => _logger.LogError(exception: ex, message: "FileSystemWatcher threw an exception.");

        /// <summary>
        /// Sets up a <see cref="FileSystemWatcher"/> to watch for Barotrauma save files in the provided directory.
        /// </summary>
        /// <param name="path">Full path to the provided directory to watch.</param>
        /// <param name="multiplayer">True if the folder will save multiplayer data.</param>
        /// <param name="enabled">True if the watcher should be immediately enabled.</param>
        /// <returns>A new <see cref="FileSystemWatcher"/> object that monitors the given directory for barotrauma saves.</returns>
        private FileSystemWatcher SetupWatcher(string path, bool multiplayer = false, bool enabled = false)
        {
            var watcher = new FileSystemWatcher(path, SaveFileFilter)
            {
                NotifyFilter = NotifyFilters.LastWrite,
            };

            try
            {
                // TODO: Check if created makes the event fire twice.
                watcher.Created += (s, e) => PerformBackup(s, e, multiplayer);
                watcher.Changed += (s, e) => PerformBackup(s, e, multiplayer); ;
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