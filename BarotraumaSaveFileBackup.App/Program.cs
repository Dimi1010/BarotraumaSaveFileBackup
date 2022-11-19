using BarotraumaSaveFileBackup.App;
using BarotraumaSaveFileBackup.App.Serialization;
using Microsoft.Extensions.Options;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<BarotraumaSaveFileBackupOptions>(context.Configuration.GetRequiredSection(BarotraumaSaveFileBackupOptions.ConfigurationKey));

        services.PostConfigure<BarotraumaSaveFileBackupOptions>(options =>
        {
            options.BarotraumaSaveFileFolder = Environment.ExpandEnvironmentVariables(options.BarotraumaSaveFileFolder);
            options.BarotraumaBackupFolder = Environment.ExpandEnvironmentVariables(options.BarotraumaBackupFolder);
        });

        services.AddTransient<IBackupSerializer, ZipArchiveBackupSerializer>(services =>
        {
            var options = services.GetRequiredService<IOptions<BarotraumaSaveFileBackupOptions>>();
            var outputLocation = options.Value.BarotraumaBackupFolder;
            return new ZipArchiveBackupSerializer(outputLocation);
        });

        services.AddHostedService<BarotraumaSaveFileBackupService>();
    })
    .Build();

await host.RunAsync();
