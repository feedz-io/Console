using System.CommandLine;

namespace Feedz.Console.Commands.List;

public class ListCommand : Command
{
    public ListCommand(IHandler<ListOptions> handler) : base("list", "List packages on feedz.io")
    {
        var organisationOption = new Option<string>(
            "--organisation", "--org", "-o")
        {
            Description = "The slug of the organisation",
            Required = true
        };

        var repositoryOption = new Option<string>(
            "--repository", "--repo", "-r")
        {
            Description = "The slug of the repository",
            Required = true
        };

        var patOption = new Option<string?>(
            "--pat")
        {
            Description = "Personal access token to use for authentication if the feed is private"
        };

        var packageIdOption = new Option<string?>(
            "--id")
        {
            Description = "The id of the package to list. If omitted, all packages will be listed"
        };

        Add(organisationOption);
        Add(repositoryOption);
        Add(patOption);
        Add(packageIdOption);

        SetAction(async parseResult =>
        {
            var options = new ListOptions
            {
                Organisation = parseResult.GetValue(organisationOption)!,
                Repository = parseResult.GetValue(repositoryOption)!,
                Pat = parseResult.GetValue(patOption),
                PackageId = parseResult.GetValue(packageIdOption)
            };

            return await handler.Handle(options);
        });
    }
}
