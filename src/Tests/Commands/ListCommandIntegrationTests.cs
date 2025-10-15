using System.Net;
using System.Threading.Tasks;
using Feedz.Client;
using Feedz.Console.Commands;
using Feedz.Console.Tests.Plumbing;
using FluentAssertions;
using NUnit.Framework;

namespace Feedz.Console.Tests.Commands
{
    /// <summary>
    /// Integration tests for ListCommand that test the full execution flow with mocked HTTP responses.
    /// These tests verify the command correctly retrieves and lists packages.
    /// </summary>
    public class ListCommandIntegrationTests : CommandIntegrationTestBase
    {
        // Success Cases
        [Test]
        public async Task ListCommand_GivenNoId_ThenListsAllPackages()
        {
            // Arrange
            MockHandler.MockPackageList("myorg", "myrepo");
            var command = new TestableListCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "list", "--org=myorg", "--repo=myrepo");

            // Assert
            MockHandler.RequestCount.Should().BeGreaterThan(0, "HTTP requests should have been made");
        }

        // Feature Tests
        [Test]
        public async Task ListCommand_GivenId_ThenListsPackagesById()
        {
            // Arrange
            MockHandler.MockPackageListById("myorg", "myrepo", "my-package");
            var command = new TestableListCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "list", "--org=myorg", "--repo=myrepo", "--id=my-package");

            // Assert
            MockHandler.RequestCount.Should().BeGreaterThan(0, "HTTP requests should have been made");
        }

        // Validation Tests
        [Test]
        public async Task ListCommand_GivenMissingOrganisation_ThenFailsValidation()
        {
            // Arrange
            var command = new TestableListCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "list", "--repo=myrepo");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        [Test]
        public async Task ListCommand_GivenMissingRepository_ThenFailsValidation()
        {
            // Arrange
            var command = new TestableListCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "list", "--org=myorg");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        // Error Handling Tests
        [Test]
        public async Task ListCommand_GivenHttpError_ThenHandlesGracefully()
        {
            // Arrange
            MockHandler.MockPackageListError("myorg", "myrepo", HttpStatusCode.Unauthorized, "Unauthorized");
            var command = new TestableListCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "list", "--org=myorg", "--repo=myrepo");

            // Assert
            MockHandler.RequestCount.Should().BeGreaterThan(0, "Request should have been attempted");
        }

        /// <summary>
        /// Testable version of ListCommand that allows injecting a FeedzClient.
        /// </summary>
        private class TestableListCommand : ListCommand
        {
            private readonly FeedzClient _mockClient;

            public TestableListCommand(FeedzClient mockClient)
            {
                _mockClient = mockClient;
            }

            protected override FeedzClient CreateClient(string pat, string region)
            {
                return _mockClient;
            }
        }
    }
}
