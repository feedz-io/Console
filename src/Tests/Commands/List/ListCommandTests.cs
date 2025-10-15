using System.Collections.Generic;
using System.Threading.Tasks;
using Feedz.Console.Commands.List;
using Feedz.Console.Tests.Plumbing;
using FluentAssertions;

namespace Feedz.Console.Tests.Commands.List
{
    public class ListCommandTests
    {
        private async Task<ListOptions> ExecuteCommand(params string[] args)
        {
            var handler = new CapturingHandler<ListOptions>();
            var command = new ListCommand(handler);

            var fullArgs = new List<string> { "list" };
            fullArgs.AddRange(args);

            var parseResult = command.Parse(string.Join(" ", fullArgs));
            await parseResult.InvokeAsync();
            return handler.CapturedOptions!;
        }

        [Fact]
        public async Task OrganisationAlias_Org_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task OrganisationAlias_O_Works()
        {
            var options = await ExecuteCommand("-o=myorg", "--repo=myrepo");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task OrganisationAlias_Full_Works()
        {
            var options = await ExecuteCommand("--organisation=myorg", "--repo=myrepo");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task RepositoryAlias_Repo_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task RepositoryAlias_R_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "-r=myrepo");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task RepositoryAlias_Full_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repository=myrepo");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task Pat_IsParsed()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=T-ABC123");

            options.Pat.Should().Be("T-ABC123");
        }

        [Fact]
        public async Task Id_IsParsed()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            options.PackageId.Should().Be("MyPackage");
        }

        [Fact]
        public async Task AllOptions_Work()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=T-ABC123", "--id=MyPackage");

            options.Organisation.Should().Be("myorg");
            options.Repository.Should().Be("myrepo");
            options.Pat.Should().Be("T-ABC123");
            options.PackageId.Should().Be("MyPackage");
        }
    }
}
