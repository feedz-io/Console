using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Feedz.Client;
using Feedz.Console.Commands;
using NUnit.Framework;

namespace Feedz.Console.Tests.Plumbing
{
    /// <summary>
    /// Base class for command integration tests that provides common test infrastructure.
    /// </summary>
    public abstract class CommandIntegrationTestBase
    {
        protected MockHttpMessageHandler MockHandler { get; private set; } = null!;

        [SetUp]
        public void BaseSetup()
        {
            MockHandler = new MockHttpMessageHandler();
            OnSetup();
        }

        [TearDown]
        public void BaseTearDown()
        {
            OnTearDown();
        }

        /// <summary>
        /// Override to add test-specific setup logic.
        /// </summary>
        protected virtual void OnSetup()
        {
        }

        /// <summary>
        /// Override to add test-specific teardown logic.
        /// </summary>
        protected virtual void OnTearDown()
        {
        }

        /// <summary>
        /// Executes a command through Program.Execute with the given arguments.
        /// </summary>
        protected async Task ExecuteCommand<TCommand>(TCommand command, string commandName, params string[] args)
            where TCommand : ICommand
        {
            var fullArgs = new List<string> { commandName };
            fullArgs.AddRange(args);

            var commands = new List<CommandInfo>
            {
                new CommandInfo(commandName, $"{commandName} command", command)
            };

            await Program.Execute(fullArgs.ToArray(), commands);
        }

        /// <summary>
        /// Creates a testable command that injects a mock FeedzClient.
        /// </summary>
        protected TCommand CreateTestableCommand<TCommand>(FeedzClient? mockClient = null)
            where TCommand : CommandBase
        {
            var feedzClient = mockClient ?? new FeedzClientTestBuilder()
                .WithMockHandler(MockHandler)
                .Build();

            return (TCommand)(Activator.CreateInstance(typeof(TCommand), feedzClient) ?? throw new InvalidOperationException($"Failed to create instance of {typeof(TCommand).Name}"));
        }

        /// <summary>
        /// Creates a FeedzClient for testing.
        /// </summary>
        protected FeedzClient CreateFeedzClient(string? pat = null)
        {
            var builder = new FeedzClientTestBuilder()
                .WithMockHandler(MockHandler);

            if (pat != null)
                builder.WithPat(pat);

            return builder.Build();
        }

        /// <summary>
        /// Base testable command wrapper that allows injecting a FeedzClient.
        /// </summary>
        protected abstract class TestableCommandBase<TCommand> : CommandBase
            where TCommand : CommandBase
        {
            protected readonly FeedzClient MockClient;
            protected readonly TCommand? InnerCommand;

            protected TestableCommandBase(FeedzClient mockClient)
            {
                MockClient = mockClient;
            }

            protected abstract FeedzClient CreateClient(string pat, string region);
        }
    }

    /// <summary>
    /// Testable command wrapper with timeout tracking capability.
    /// </summary>
    public abstract class TestableCommandWithTimeout<TCommand> : CommandBase
        where TCommand : CommandBase
    {
        protected readonly FeedzClient MockClient;

        protected TestableCommandWithTimeout(FeedzClient mockClient)
        {
            MockClient = mockClient;
        }

        public bool ClientTimeoutWasSet { get; protected set; }
        public TimeSpan ClientTimeout { get; protected set; }

        protected void CaptureTimeout()
        {
            if (MockClient != null)
            {
                ClientTimeout = MockClient.FeedTimeout;
                ClientTimeoutWasSet = true;
            }
        }
    }
}
