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
    [Command("push", "Push a package to feedz.io")]
    public class PushCommand : CommandBase
    {
        private List<string> _files = new List<string>();
        private string _org;
        private string _repo;
        private string _pat;
        private string _region;
        private bool _force;
        private int _timeout = 1800;

        protected override void PopulateOptions(OptionSet options)
        {
            options.Add(
                "organisation=|org=|o=",
                () => "The slug of the organisation to push to",
                v => _org = v
            );
            options.Add(
                "repository=|repo=|r=",
                () => "The slug of the repository to push to",
                v => _repo = v
            );
            options.Add(
                "pat=",
                () => "Personal access token to use for authentication",
                v => _pat = v
            );
            options.Add(
                "file=|f=|package=",
                () => "The package to push. Specify multiple times to push multiple packages",
                v => _files.Add(v)
            );
            options.Add(
                "force",
                () => "(Optional) Whether to overwrite any existing package with the same id and version",
                v => _force = v != null
            );
            options.Add(
                "timeout=",
                () => "(Optional) Amount of time wait for the push to complete in seconds (Default 1800)",
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
            _files = _files.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();

            if (!Validate())
                return;

            var client = ClientFactory.Create(_pat, _region);
            client.FeedTimeout = TimeSpan.FromSeconds(_timeout);

            foreach (var f in _files)
            {
                await PushFile(f, client);
            }
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

            if (string.IsNullOrEmpty(_pat))
            {
                Log.Error("Please specify the personal access token to push to using --pat=T-ABCXYZ");
                isValid = false;
            }

            if (_files.Count == 0)
            {
                Log.Error("Please specify a file to push to using --file=My.Package.1.0.0.zip");
                isValid = false;
            }

            return isValid;
        }

        private async Task PushFile(string file, FeedzClient client)
        {
            if (!File.Exists(file))
            {
                Log.Error("The file {file:l} does not exist", file);
                return;
            }

            try
            {
                var repo = client.ScopeToRepository(_org, _repo);
                Log.Information("Pushing {file:l} to {uri:l}", file, repo.Packages.FeedUri.AbsoluteUri);

                using (var fs = File.OpenRead(file))
                    await repo.Packages.Upload(fs, Path.GetFileName(file), _force);

                Log.Information("Pushed {file:l}", file);
            }
            catch (TaskCanceledException)
            {
                Log.Information("The push time limit was exceeded, specify the -timeout parameter to extend the timeout");
            }
            catch (Exception ex)
            {
                Log.Error("Error pushing {file:l}: {message}", file, ex.Message);
            }
        }
    }
}