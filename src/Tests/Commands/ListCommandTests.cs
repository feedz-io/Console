using System.Collections.Generic;
using System.Threading.Tasks;
using Feedz.Console.Commands;
using FluentAssertions;
using NDesk.Options;
using NUnit.Framework;

namespace Feedz.Console.Tests.Commands
{
    public class ListCommandTests
    {
        private async Task<TestableListCommand> ExecuteCommand(params string[] args)
        {
            var command = new TestableListCommand();
            var commands = new List<CommandInfo>
            {
                new CommandInfo("list", "List packages", command)
            };

            var fullArgs = new List<string> { "list" };
            fullArgs.AddRange(args);

            await Program.Execute(fullArgs.ToArray(), commands);
            return command;
        }

        [Test]
        public async Task OrganisationAlias_Org_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task OrganisationAlias_O_Works()
        {
            var command = await ExecuteCommand("-o=myorg", "--repo=myrepo");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task OrganisationAlias_Full_Works()
        {
            var command = await ExecuteCommand("--organisation=myorg", "--repo=myrepo");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task RepositoryAlias_Repo_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task RepositoryAlias_R_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "-r=myrepo");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task RepositoryAlias_Full_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repository=myrepo");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task Pat_IsParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=T-ABC123");

            command.Pat.Should().Be("T-ABC123");
        }

        [Test]
        public async Task Id_IsParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--id=MyPackage");

            command.Id.Should().Be("MyPackage");
        }

        [Test]
        public async Task Region_IsParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--region=us-west");

            command.Region.Should().Be("us-west");
        }

        [Test]
        public async Task AllOptions_Work()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=T-ABC123", "--id=MyPackage", "--region=us-west");

            command.Organisation.Should().Be("myorg");
            command.Repository.Should().Be("myrepo");
            command.Pat.Should().Be("T-ABC123");
            command.Id.Should().Be("MyPackage");
            command.Region.Should().Be("us-west");
        }

        private class TestableListCommand : ListCommand
        {
            public string? Organisation { get; private set; }
            public string? Repository { get; private set; }
            public string? Pat { get; private set; }
            public string? Region { get; private set; }
            public string? Id { get; private set; }

            protected override Task Execute()
            {
                // Capture values using reflection
                Organisation = GetFieldValue<string>("_org");
                Repository = GetFieldValue<string>("_repo");
                Pat = GetFieldValue<string>("_pat");
                Region = GetFieldValue<string>("_region");
                Id = GetFieldValue<string>("_id");

                return Task.CompletedTask;
            }

            private T GetFieldValue<T>(string fieldName)
            {
                var field = typeof(ListCommand).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (T)field!.GetValue(this)!;
            }
        }
    }
}
