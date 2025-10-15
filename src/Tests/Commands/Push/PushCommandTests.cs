using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Feedz.Console.Commands.Push;
using Feedz.Console.Tests.Plumbing;
using FluentAssertions;

namespace Feedz.Console.Tests.Commands.Push
{
    public class PushCommandTests
    {
        private async Task<PushOptions> ExecuteCommand(params string[] args)
        {
            var handler = new CapturingHandler<PushOptions>();
            var command = new PushCommand(handler);

            var fullArgs = new List<string> { "push" };
            fullArgs.AddRange(args);

            var parseResult = command.Parse(string.Join(" ", fullArgs));
            await parseResult.InvokeAsync();
            return handler.CapturedOptions!;
        }

        [Fact]
        public async Task OrganisationAlias_Org_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task OrganisationAlias_O_Works()
        {
            var options = await ExecuteCommand("-o=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task OrganisationAlias_Full_Works()
        {
            var options = await ExecuteCommand("--organisation=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            options.Organisation.Should().Be("myorg");
        }

        [Fact]
        public async Task RepositoryAlias_Repo_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task RepositoryAlias_R_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "-r=myrepo", "--pat=token", "--file=test.nupkg");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task RepositoryAlias_Full_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repository=myrepo", "--pat=token", "--file=test.nupkg");

            options.Repository.Should().Be("myrepo");
        }

        [Fact]
        public async Task Pat_IsParsed()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=T-ABC123", "--file=test.nupkg");

            options.Pat.Should().Be("T-ABC123");
        }

        [Fact]
        public async Task FileAlias_File_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            options.Files.Should().ContainSingle().Which.Name.Should().Be("test.nupkg");
        }

        [Fact]
        public async Task FileAlias_F_Works()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "-f=test.nupkg");

            options.Files.Should().ContainSingle().Which.Name.Should().Be("test.nupkg");
        }

        [Fact]
        public async Task MultipleFiles_AreParsed()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test1.nupkg", "--file=test2.nupkg", "-f=test3.nupkg");

            options.Files.Should().HaveCount(3);
            options.Files.Select(f => f.Name).Should().Contain("test1.nupkg");
            options.Files.Select(f => f.Name).Should().Contain("test2.nupkg");
            options.Files.Select(f => f.Name).Should().Contain("test3.nupkg");
        }

        [Fact]
        public async Task Force_DefaultsFalse()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            options.Force.Should().BeFalse();
        }

        [Fact]
        public async Task Force_CanBeSet()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg", "--force");

            options.Force.Should().BeTrue();
        }

        [Fact]
        public async Task Timeout_DefaultsTo1800()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            options.Timeout.Should().Be(1800);
        }

        [Fact]
        public async Task Timeout_CanBeCustomized()
        {
            var options = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg", "--timeout=3600");

            options.Timeout.Should().Be(3600);
        }
    }
}
