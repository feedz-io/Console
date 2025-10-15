using Feedz.Console.Plumbing;
using Serilog;

namespace Feedz.Console.Commands.List;

public class ListHandler(IClientFactory clientFactory) : IHandler<ListOptions>
{
    public async Task<int> Handle(ListOptions options)
    {
        try
        {
            var client = clientFactory.Create(options.Pat);
            var repo = client.ScopeToRepository(options.Organisation, options.Repository);

            var packages = string.IsNullOrEmpty(options.PackageId)
                ? await repo.PackageFeed.All()
                : await repo.PackageFeed.ListByPackageId(options.PackageId);

            foreach (var package in packages)
                Log.Information("{id:l}   {version:l}   {extension:l}", package.PackageId, package.Version, package.Extension);

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("Error listing packages: {message}", ex.Message);
            return 1;
        }
    }
}
