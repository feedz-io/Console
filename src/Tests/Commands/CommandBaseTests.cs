using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Feedz.Console.Commands;
using FluentAssertions;
using NDesk.Options;
using NUnit.Framework;

namespace Feedz.Console.Tests.Commands
{
    public class CommandBaseTests
    {
        private StringWriter _output;

        [SetUp]
        public void Setup()
        {
            _output = new StringWriter();
            System.Console.SetOut(_output);
        }

        [TearDown]
        public void TearDown()
        {
            _output.Dispose();
        }

        [Test]
        public async Task EmptyArgs_DisplaysHelp()
        {
            var command = new TestCommand();
            var commands = new List<CommandInfo>
            {
                new CommandInfo("test", "Test command", command)
            };

            await Program.Execute(new[] { "test" }, commands);

            _output.ToString().Should().Contain("Usage:");
            command.ExecuteCalled.Should().BeFalse();
        }

        [Test]
        public async Task HelpFlag_DisplaysHelp()
        {
            var command = new TestCommand();
            var commands = new List<CommandInfo>
            {
                new CommandInfo("test", "Test command", command)
            };

            await Program.Execute(new[] { "test", "--help" }, commands);

            _output.ToString().Should().Contain("Usage:");
            command.ExecuteCalled.Should().BeFalse();
        }

        [Test]
        public async Task UnrecognizedOption_DisplaysError()
        {
            var command = new TestCommand();
            var commands = new List<CommandInfo>
            {
                new CommandInfo("test", "Test command", command)
            };

            await Program.Execute(new[] { "test", "--unknown" }, commands);

            command.ExecuteCalled.Should().BeFalse();
        }

        [Test]
        public async Task ValidOptions_CallsExecute()
        {
            var command = new TestCommand();
            var commands = new List<CommandInfo>
            {
                new CommandInfo("test", "Test command", command)
            };

            await Program.Execute(new[] { "test", "--test=value" }, commands);

            command.ExecuteCalled.Should().BeTrue();
            command.TestValue.Should().Be("value");
        }

        private class TestCommand : CommandBase
        {
            public bool ExecuteCalled { get; private set; }
            public string TestValue { get; private set; }

            protected override void PopulateOptions(OptionSet options)
            {
                options.Add("test=", v => TestValue = v);
            }

            protected override Task Execute()
            {
                ExecuteCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}
