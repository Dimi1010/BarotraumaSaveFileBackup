namespace BarotraumaSaveFileBackup.App.Serialization
{
    class FileBackupSerializer : IBackupSerializer
    {
        private const string BackupExtension = ".bak";

        private readonly string? _outputLocation;

        public FileBackupSerializer(string? outputLocation)
        {
            if (string.IsNullOrEmpty(outputLocation) && !Directory.Exists(outputLocation))
                throw new ArgumentException($"'{outputLocation}' cannot be found or is not a directory.", nameof(outputLocation));
            _outputLocation = outputLocation;
        }

        public Task SerializeAsync(string saveFile, string? characterData = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var files = new List<string>
            {
                saveFile,
            };

            if (characterData != null)
                files.Add(characterData);

            return SerializeAsync(files, cancellationToken);
        }

        private Task SerializeAsync(IEnumerable<string> files, CancellationToken cancellationToken = default)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            foreach (var file in files)
            {
                var baseFileName = Path.GetFileNameWithoutExtension(file);
                var fileExtension = Path.GetExtension(file);

                var newFileName = $"{baseFileName}-{timestamp}{fileExtension}{BackupExtension}";
                var newFullPath = Path.Combine(GetOutputDirectory(file), newFileName);

                File.Copy(file, newFullPath);
            }

            return Task.CompletedTask;
        }

        private string GetOutputDirectory(string file)
        {
            if (string.IsNullOrEmpty(_outputLocation))
            {
                var saveFileDir = Path.GetDirectoryName(file);

                if (saveFileDir == null)
                    throw new DirectoryNotFoundException("Output directory is not set and can't be calculated based on the file.");

                return saveFileDir;
            }
            return _outputLocation;
        }
    }
}
