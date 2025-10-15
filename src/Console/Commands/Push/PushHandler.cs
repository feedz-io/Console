using System.IO.Abstractions;
using Feedz.Console.Plumbing;
using Serilog;

namespace Feedz.Console.Commands.Push;

public class PushHandler(IClientFactory clientFactory, IFileSystem fileSystem) : IHandler<PushOptions>
{
    public async Task<int> Handle(PushOptions options)
    {
        var client = clientFactory.Create(options.Pat);
        client.FeedTimeout = TimeSpan.FromSeconds(options.Timeout);

        var hasErrors = false;
        foreach (var file in options.Files)
        {
            var success = await PushFile(file, client, options);
            if (!success)
                hasErrors = true;
        }

        return hasErrors ? 1 : 0;
    }

    private async Task<bool> PushFile(FileInfo file, Client.FeedzClient client, PushOptions options)
    {
        if (!fileSystem.File.Exists(file.FullName))
        {
            Log.Error("The file {file:l} does not exist", file.FullName);
            return false;
        }

        try
        {
            var repo = client.ScopeToRepository(options.Organisation, options.Repository);
            Log.Information("Pushing {file:l} to {uri:l}", file.FullName, repo.PackageFeed.FeedUri.AbsoluteUri);

            using (var fs = fileSystem.File.OpenRead(file.FullName))
                await repo.PackageFeed.Upload(fs, file.Name, options.Force);

            Log.Information("Pushed {file:l}", file.FullName);
            return true;
        }
        catch (TaskCanceledException)
        {
            Log.Information("The push time limit was exceeded, specify the --timeout parameter to extend the timeout");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error("Error pushing {file:l}: {message}", file.FullName, ex.Message);
            return false;
        }
    }
}
