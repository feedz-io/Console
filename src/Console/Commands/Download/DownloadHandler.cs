using System.IO.Abstractions;
using Feedz.Console.Plumbing;
using Serilog;

namespace Feedz.Console.Commands.Download;

public class DownloadHandler(IClientFactory clientFactory, IFileSystem fileSystem) : IHandler<DownloadOptions>
{
    public async Task<int> Handle(DownloadOptions options)
    {
        try
        {
            var client = clientFactory.Create(options.Pat);
            client.FeedTimeout = TimeSpan.FromSeconds(options.Timeout);

            var repo = client.ScopeToRepository(options.Organisation, options.Repository);

            var package = string.IsNullOrEmpty(options.Version)
                ? await repo.PackageFeed.GetLatest(options.PackageId)
                : await repo.PackageFeed.Get(options.PackageId, options.Version);

            var packageFilename = $"{package.PackageId}.{package.Version}{package.Extension}";
            var destination = fileSystem.Path.GetFullPath(packageFilename);
            Log.Information("Downloading {filename:l} to {destination:l}", packageFilename, fileSystem.Path.GetDirectoryName(destination));

            if (fileSystem.File.Exists(destination))
            {
                Log.Error("The file {filename:l} already exists locally", packageFilename);
                return 1;
            }

            var result = await repo
                .PackageFeed
                .Download(
                    package,
                    options.SimilarPackagePath ?? fileSystem.Directory.GetCurrentDirectory()
                );

            using (result)
            using (var fs = fileSystem.File.OpenWrite(destination))
            {
                result.CopyTo(fs);
            }

            Log.Information("Download completed");
            return 0;
        }
        catch (TaskCanceledException)
        {
            Log.Information("The download time limit was exceeded, specify the --timeout parameter to extend the timeout");
            return 1;
        }
        catch (Exception ex)
        {
            Log.Error("Error downloading package: {message}", ex.Message);
            return 1;
        }
    }
}
