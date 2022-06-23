using BarotraumaSaveFileBackup.App;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<BarotraumaSaveFileBackupService>();
    })
    .Build();

await host.RunAsync();
