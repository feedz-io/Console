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
    [Command("list", "List the packages on feedz.io")]
    public class ListCommand : CommandBase
    {
        private string _org;
        private string _repo;
        private string _pat;
        private string _region;
        private string _id;

        protected override void PopulateOptions(OptionSet options)
        {
            options.Add(
                "organisation=|org=|o=",
                () => "Organisation to push to",
                v => _org = v
            );
            options.Add(
                "repository=|repo=|r=",
                () => "Repository to push to",
                v => _repo = v
            );
            options.Add(
                "pat=",
                () => "(Optional) Personal access token to use for authentication if the feed is private",
                v => _pat = v
            );
            options.Add(
                "id=",
                () => "(Optional) The id of the package to list. If omitted, all packages will be listed",
                v => _id = v
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

            var packages = string.IsNullOrEmpty(_id)
                ? await repo.Packages.All()
                : await repo.Packages.ListByPackageId(_id);

            foreach (var package in packages)
                Log.Information("{id:l}   {version:l}   {extension:l}", package.PackageId, package.Version, package.Extension);
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

            return isValid;
        }
    }
}