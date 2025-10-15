using System.Collections.Generic;
using System.Threading.Tasks;
using Feedz.Console.Commands.Download;
using Feedz.Console.Tests.Plumbing;
using FluentAssertions;

namespace Feedz.Console.Tests.Commands.Download
{
    public class DownloadCommandTests
    {
        private async Task<DownloadOptions> ExecuteCommand(params string[] args)
        {
            var handler = new CapturingHandler<DownloadOptions>();
            var command = new DownloadCommand(handler);

            var fullArgs = new List<string> { "download" };
            fullArgs.AddRange(args);

            var parseResult = command.Parse(string.Join(" ", fullArgs));
            await parseResult.InvokeAsync();
            return handler.CapturedOptions!;
        }

        [Fact]
        public async Task OrganisationAlias_Org_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task OrganisationAlias_O_Works()
        {
            var options = await ExecuteCommand("-o=myorg", "--repo=myrepo", "--id=MyPackage");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task OrganisationAlias_Full_Works()
        {
            var options = await ExecuteCommand("--organisation=myorg", "--repo=myrepo", "--id=MyPackage");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task RepositoryAlias_Repo_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task RepositoryAlias_R_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "-r=myrepo", "--id=MyPackage");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task RepositoryAlias_Full_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repository=myrepo", "--id=MyPackage");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task Pat_IsParsed()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--pat=T-ABC123");

            options.Pat.Should().Be("T-ABC123");
        }

        [Fact]
        public async Task Id_IsParsed()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            options.PackageId.Should().Be("MyPackage");
        }

        [Fact]
        public async Task Version_ShouldAcceptValue()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--version=1.2.3");

            options.Version.Should().Be("1.2.3");
        }

        [Fact]
        public async Task SimilarPackagePath_ShouldAcceptValue()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--similar-package-path=/path/to/package");

            options.SimilarPackagePath.Should().Be("/path/to/package");
        }

        [Fact]
        public async Task Timeout_DefaultsTo1800()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            options.Timeout.Should().Be(1800);
        }

        [Fact]
        public async Task Timeout_CanBeCustomized()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--timeout=3600");

            options.Timeout.Should().Be(3600);
        }
    }
}
