using BarotraumaSaveFileBackup.App;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<BarotraumaSaveFileBackupOptions>(context.Configuration.GetRequiredSection(BarotraumaSaveFileBackupOptions.ConfigurationKey));
        services.AddHostedService<BarotraumaSaveFileBackupService>();
    })
    .Build();

await host.RunAsync();
