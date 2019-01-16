using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Feedz.Client;
using Feedz.Console.Plumbing;
using NDesk.Options;
using Serilog;

namespace Feedz.Console.Commands
{
    [Command("download", "Download a package from feedz.io")]
    public class DownloadCommand : CommandBase
    {
        private string _org;
        private string _repo;
        private string _pat;
        private string _region;
        private string _id;
        private string _similarPackagePath;
        private string _version;
        private int _timeout = 1800;

        protected override void PopulateOptions(OptionSet options)
        {
            options.Add(
                "organisation=|org=|o=",
                () => "The slug of the organisation to download from",
                v => _org = v
            );
            options.Add(
                "repository=|repo=|r=",
                () => "The slug of the repository to download from",
                v => _repo = v
            );
            options.Add(
                "pat=",
                () => "(Optional) Personal access token to use for authentication if the feed is private",
                v => _pat = v
            );
            options.Add(
                "id=",
                () => "The id of the package to download",
                v => _id = v
            );
            options.Add(
                "version",
                () => "(Optional) The version to download. If not specified, the latest release version will be downloaded.",
                v => _version = v
            );
            options.Add(
                "similarPackagePath",
                () => "(Optional) Delta compression uses a local package that is similar to the package to be downloaded to reduce the transfer size. If this option is a file, it will be used as the basis for comparison. If this is a directory, it will be searched for a file to use. Of omitted the current directory is used.",
                v => _similarPackagePath = v
            );
            options.Add(
                "timeout=",
                () => "(Optional) Amount of time wait for the download to complete in seconds (Default 1800)",
                (int v) => _timeout = v
            );
            options.Add(
                "region=",
                () => "(Optional) The region to store the package in (beta)",
                v => _region = v
            );
        }

        protected override async Task Execute()
        {
            if (!Validate())
                return;

            var client = ClientFactory.Create(_pat, _region);

            var repo = client.ScopeToRepository(_org, _repo);

            var package = string.IsNullOrEmpty(_version)
                ? await repo.Packages.GetLatest(_id)
                : await repo.Packages.Get(_id, _version);

            var packageFilename = $"{package.PackageId}.{package.Version}{package.Extension}";
            var destination = Path.GetFullPath(packageFilename);
            Log.Information("Downloading {filename:l} to {destination:l}", packageFilename, Path.GetDirectoryName(destination));

            if (File.Exists(destination))
            {
                Log.Error("The file {filename:l} already exists locally", packageFilename);
                return;
            }
            
            client.FeedTimeout = TimeSpan.FromSeconds(_timeout);
            var result = await repo
                .Packages
                .Download(
                    package,
                    _similarPackagePath ?? Environment.CurrentDirectory
                );

            using (result)
            using (var fs = File.OpenWrite(destination))
            {
                result.CopyTo(fs);
            }
            
            Log.Information("Download completed");
        }

        private bool Validate()
        {
            var isValid = true;
            if (string.IsNullOrEmpty(_org))
            {
                Log.Error("Please specify the organisation to push to using --organisation=YourOrganisation");
                isValid = false;
            }

            if (string.IsNullOrEmpty(_repo))
            {
                Log.Error("Please specify the repository to push to using --repository=YourRepository");
                isValid = false;
            }

            if (string.IsNullOrEmpty(_id))
            {
                Log.Error("Please specify the id of the package to download using --id=MyPackage");
                isValid = false;
            }

            return isValid;
        }
    }
}