using System.IO.Compression;

namespace BarotraumaSaveFileBackup.App.Serialization
{
    class ZipArchiveBackupSerializer : IBackupSerializer
    {
        private readonly string _outputLocation;

        public ZipArchiveBackupSerializer(string outputLocation)
        {
            if (!Directory.Exists(outputLocation))
                throw new ArgumentException($"'{outputLocation}' cannot be found or is not a directory.", nameof(outputLocation));
            _outputLocation = outputLocation;
        }

        public Task SerializeAsync(string saveFile, string? characterData = null, CancellationToken cancellationToken = default)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");

            var baseFileName = Path.GetFileNameWithoutExtension(saveFile);

            var zipFileName = $"{baseFileName}-{timestamp}.zip";
            var zipFullPath = Path.Combine(_outputLocation, zipFileName);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }


            using var zipFileStream = new FileStream(zipFullPath, FileMode.CreateNew);
            using var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);
            
            zipArchive.CreateEntryFromFile(saveFile, Path.GetFileName(saveFile));
            if (characterData != null)
            {
                zipArchive.CreateEntryFromFile(characterData, Path.GetFileName(characterData));
            }

            return Task.CompletedTask;
        }
    }
}
