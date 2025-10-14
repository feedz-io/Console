using System.Collections.Generic;
using System.Threading.Tasks;
using Feedz.Console.Commands;
using FluentAssertions;
using NDesk.Options;
using NUnit.Framework;

namespace Feedz.Console.Tests.Commands
{
    public class DownloadCommandTests
    {
        private async Task<TestableDownloadCommand> ExecuteCommand(params string[] args)
        {
            var command = new TestableDownloadCommand();
            var commands = new List<CommandInfo>
            {
                new CommandInfo("download", "Download a package", command)
            };

            var fullArgs = new List<string> { "download" };
            fullArgs.AddRange(args);

            await Program.Execute(fullArgs.ToArray(), commands);
            return command;
        }

        [Test]
        public async Task OrganisationAlias_Org_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task OrganisationAlias_O_Works()
        {
            var command = await ExecuteCommand("-o=myorg", "--repo=myrepo", "--id=MyPackage");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task OrganisationAlias_Full_Works()
        {
            var command = await ExecuteCommand("--organisation=myorg", "--repo=myrepo", "--id=MyPackage");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task RepositoryAlias_Repo_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task RepositoryAlias_R_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "-r=myrepo", "--id=MyPackage");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task RepositoryAlias_Full_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repository=myrepo", "--id=MyPackage");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task Pat_IsParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--pat=T-ABC123");

            command.Pat.Should().Be("T-ABC123");
        }

        [Test]
        public async Task Id_IsParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            command.Id.Should().Be("MyPackage");
        }

        [Test]
        public async Task Version_ShouldAcceptValue()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--version=1.2.3");

            command.Version.Should().Be("1.2.3");
        }

        [Test]
        public async Task SimilarPackagePath_ShouldAcceptValue()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--similarPackagePath=/path/to/package");

            command.SimilarPackagePath.Should().Be("/path/to/package");
        }

        [Test]
        public async Task Timeout_DefaultsTo1800()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            command.Timeout.Should().Be(1800);
        }

        [Test]
        public async Task Timeout_CanBeCustomized()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--timeout=3600");

            command.Timeout.Should().Be(3600);
        }

        [Test]
        public async Task Region_IsParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage", "--region=us-west");

            command.Region.Should().Be("us-west");
        }

        private class TestableDownloadCommand : DownloadCommand
        {
            public string? Organisation { get; private set; }
            public string? Repository { get; private set; }
            public string? Pat { get; private set; }
            public string? Region { get; private set; }
            public string? Id { get; private set; }
            public string? Version { get; private set; }
            public string? SimilarPackagePath { get; private set; }
            public int Timeout { get; private set; } = 1800;

            protected override Task Execute()
            {
                // Capture values using reflection
                Organisation = GetFieldValue<string>("_org");
                Repository = GetFieldValue<string>("_repo");
                Pat = GetFieldValue<string>("_pat");
                Region = GetFieldValue<string>("_region");
                Id = GetFieldValue<string>("_id");
                Version = GetFieldValue<string>("_version");
                SimilarPackagePath = GetFieldValue<string>("_similarPackagePath");
                Timeout = GetFieldValue<int>("_timeout");

                return Task.CompletedTask;
            }

            private T GetFieldValue<T>(string fieldName)
            {
                var field = typeof(DownloadCommand).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (T)field!.GetValue(this)!;
            }
        }
    }
}
