using System.CommandLine;

namespace Feedz.Console.Commands.Download;

public class DownloadCommand : Command
{
    public DownloadCommand(IHandler<DownloadOptions> handler) : base("download", "Download a package from feedz.io")
    {
        var organisationOption = new Option<string>("--organisation", "--org", "-o")
        {
            Description = "The slug of the organisation to download from",
            Required = true
        };

        var repositoryOption = new Option<string>("--repository", "--repo", "-r")
        {
            Description = "The slug of the repository to download from",
            Required = true
        };

        var patOption = new Option<string?>("--pat")
        {
            Description = "Personal access token to use for authentication if the feed is private"
        };

        var packageIdOption = new Option<string>("--id")
        {
            Description = "The id of the package to download",
            Required = true
        };

        var versionOption = new Option<string?>("--version")
        {
            Description = "The version to download. If not specified, the latest release version will be downloaded"
        };

        var similarPackagePathOption = new Option<string?>("--similar-package-path")
        {
            Description = "Path to a similar package or directory for delta compression"
        };

        var timeoutOption = new Option<int>("--timeout")
        {
            Description = "Time to wait for the download to complete in seconds",
            DefaultValueFactory = _ => 1800
        };

        Add(organisationOption);
        Add(repositoryOption);
        Add(patOption);
        Add(packageIdOption);
        Add(versionOption);
        Add(similarPackagePathOption);
        Add(timeoutOption);

        SetAction(async parseResult =>
        {
            var options = new DownloadOptions
            {
                Organisation = parseResult.GetValue(organisationOption)!,
                Repository = parseResult.GetValue(repositoryOption)!,
                Pat = parseResult.GetValue(patOption),
                PackageId = parseResult.GetValue(packageIdOption)!,
                Version = parseResult.GetValue(versionOption),
                SimilarPackagePath = parseResult.GetValue(similarPackagePathOption),
                Timeout = parseResult.GetValue(timeoutOption)
            };

            return await handler.Handle(options);
        });
    }
}