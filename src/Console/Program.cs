using System.CommandLine;
using System.IO.Abstractions;
using Feedz.Console.Commands.Download;
using Feedz.Console.Commands.List;
using Feedz.Console.Commands.Push;
using Feedz.Console.Plumbing;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var clientFactory = new ClientFactory();
    var fileSystem = new FileSystem();

    var rootCommand = new RootCommand("Feedz.io package management CLI")
    {
        new PushCommand(new PushHandler(clientFactory, fileSystem)),
        new DownloadCommand(new DownloadHandler(clientFactory, fileSystem)),
        new ListCommand(new ListHandler(clientFactory))
    };

    var parseResult = rootCommand.Parse(args);
    return await parseResult.InvokeAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
