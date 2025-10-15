using System.CommandLine;

namespace Feedz.Console.Commands.Push;

public class PushCommand : Command
{
    public PushCommand(IHandler<PushOptions> handler) : base("push", "Push packages to feedz.io")
    {
        var organisationOption = new Option<string>(
            "--organisation", "--org", "-o")
        {
            Description = "The slug of the organisation to push to",
            Required = true
        };

        var repositoryOption = new Option<string>(
            "--repository", "--repo", "-r")
        {
            Description = "The slug of the repository to push to",
            Required = true
        };

        var patOption = new Option<string>(
            "--pat")
        {
            Description = "Personal access token to use for authentication",
            Required = true
        };

        var filesOption = new Option<FileInfo[]>(
            "--files", "--file", "-f")
        {
            Description = "Package file(s) to push. Specify multiple times to push multiple packages",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.OneOrMore
        };

        var forceOption = new Option<bool>(
            "--force")
        {
            Description = "Overwrite any existing package with the same id and version"
        };

        var timeoutOption = new Option<int>(
            "--timeout")
        {
            Description = "Time to wait for the push to complete in seconds",
            DefaultValueFactory = _ => 1800
        };

        Add(organisationOption);
        Add(repositoryOption);
        Add(patOption);
        Add(filesOption);
        Add(forceOption);
        Add(timeoutOption);

        SetAction(async parseResult =>
        {
            var options = new PushOptions
            {
                Organisation = parseResult.GetValue(organisationOption)!,
                Repository = parseResult.GetValue(repositoryOption)!,
                Pat = parseResult.GetValue(patOption)!,
                Files = parseResult.GetValue(filesOption)!.ToList(),
                Force = parseResult.GetValue(forceOption),
                Timeout = parseResult.GetValue(timeoutOption)
            };

            return await handler.Handle(options);
        });
    }
}
