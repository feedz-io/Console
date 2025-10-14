using System.Collections.Generic;
using System.Threading.Tasks;
using Feedz.Console.Commands;
using FluentAssertions;
using NDesk.Options;
using NUnit.Framework;

namespace Feedz.Console.Tests.Commands
{
    public class PushCommandTests
    {
        private async Task<TestablePushCommand> ExecuteCommand(params string[] args)
        {
            var command = new TestablePushCommand();
            var commands = new List<CommandInfo>
            {
                new CommandInfo("push", "Push a package", command)
            };

            var fullArgs = new List<string> { "push" };
            fullArgs.AddRange(args);

            await Program.Execute(fullArgs.ToArray(), commands);
            return command;
        }

        [Test]
        public async Task OrganisationAlias_Org_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task OrganisationAlias_O_Works()
        {
            var command = await ExecuteCommand("-o=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task OrganisationAlias_Full_Works()
        {
            var command = await ExecuteCommand("--organisation=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            command.Organisation.Should().Be("myorg");
        }

        [Test]
        public async Task RepositoryAlias_Repo_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task RepositoryAlias_R_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "-r=myrepo", "--pat=token", "--file=test.nupkg");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task RepositoryAlias_Full_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repository=myrepo", "--pat=token", "--file=test.nupkg");

            command.Repository.Should().Be("myrepo");
        }

        [Test]
        public async Task Pat_IsParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=T-ABC123", "--file=test.nupkg");

            command.Pat.Should().Be("T-ABC123");
        }

        [Test]
        public async Task FileAlias_File_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            command.Files.Should().ContainSingle().Which.Should().Be("test.nupkg");
        }

        [Test]
        public async Task FileAlias_F_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "-f=test.nupkg");

            command.Files.Should().ContainSingle().Which.Should().Be("test.nupkg");
        }

        [Test]
        public async Task FileAlias_Package_Works()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--package=test.nupkg");

            command.Files.Should().ContainSingle().Which.Should().Be("test.nupkg");
        }

        [Test]
        public async Task MultipleFiles_AreParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test1.nupkg", "--file=test2.nupkg", "-f=test3.nupkg");

            command.Files.Should().HaveCount(3);
            command.Files.Should().Contain("test1.nupkg");
            command.Files.Should().Contain("test2.nupkg");
            command.Files.Should().Contain("test3.nupkg");
        }

        [Test]
        public async Task Force_DefaultsFalse()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            command.Force.Should().BeFalse();
        }

        [Test]
        public async Task Force_CanBeSet()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg", "--force");

            command.Force.Should().BeTrue();
        }

        [Test]
        public async Task Timeout_DefaultsTo1800()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg");

            command.Timeout.Should().Be(1800);
        }

        [Test]
        public async Task Timeout_CanBeCustomized()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg", "--timeout=3600");

            command.Timeout.Should().Be(3600);
        }

        [Test]
        public async Task Region_IsParsed()
        {
            var command = await ExecuteCommand("--org=myorg", "--repo=myrepo", "--pat=token", "--file=test.nupkg", "--region=us-west");

            command.Region.Should().Be("us-west");
        }

        private class TestablePushCommand : PushCommand
        {
            public string? Organisation { get; private set; }
            public string? Repository { get; private set; }
            public string? Pat { get; private set; }
            public string? Region { get; private set; }
            public List<string> Files { get; } = new List<string>();
            public bool Force { get; private set; }
            public int Timeout { get; private set; } = 1800;

            protected override void PopulateOptions(OptionSet options)
            {
                base.PopulateOptions(options);

                // Capture the values after base populates options
                options.Add("capture", v =>
                {
                    Organisation = GetFieldValue<string>("_org");
                    Repository = GetFieldValue<string>("_repo");
                    Pat = GetFieldValue<string>("_pat");
                    Region = GetFieldValue<string>("_region");
                    Files.AddRange(GetFieldValue<List<string>>("_files"));
                    Force = GetFieldValue<bool>("_force");
                    Timeout = GetFieldValue<int>("_timeout");
                });
            }

            protected override Task Execute()
            {
                // Capture final values
                Organisation = GetFieldValue<string>("_org");
                Repository = GetFieldValue<string>("_repo");
                Pat = GetFieldValue<string>("_pat");
                Region = GetFieldValue<string>("_region");
                Files.Clear();
                Files.AddRange(GetFieldValue<List<string>>("_files"));
                Force = GetFieldValue<bool>("_force");
                Timeout = GetFieldValue<int>("_timeout");

                return Task.CompletedTask;
            }

            private T GetFieldValue<T>(string fieldName)
            {
                var field = typeof(PushCommand).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (T)field!.GetValue(this)!;
            }
        }
    }
}
