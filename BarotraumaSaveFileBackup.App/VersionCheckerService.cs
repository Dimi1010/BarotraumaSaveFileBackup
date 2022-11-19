namespace BarotraumaSaveFileBackup.App
{
    public class VersionCheckerService : IHostedService
    {
        private readonly ILogger<VersionCheckerService> _logger;

        public VersionCheckerService(ILogger<VersionCheckerService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var latestVersion = await ProductInfo.GetLatestVersionAsync(cancellationToken);
            if(latestVersion is null)
            {
                _logger.LogError("Latest product version could not be obtained.");
            }

            if(latestVersion > ProductInfo.ProductVersion)
            {
                _logger.LogInformation(
                    "A new version is available: {}\n" +
                    "Get the latest release at: https://github.com/Dimi1010/BarotraumaSaveFileBackup/releases", latestVersion);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
