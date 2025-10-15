using System.Net;
using System.Threading.Tasks;
using Feedz.Console.Commands.List;
using Feedz.Console.Tests.Plumbing;
using FluentAssertions;

namespace Feedz.Console.Tests.Commands.List
{
    public class ListHandlerTests
    {
        private readonly ListHandler handler;
        private readonly MockedClientFactory clientFactory = new();

        public ListHandlerTests()
        {
            handler = new ListHandler(clientFactory);
        }

        [Fact]
        public async Task ListHandler_GivenNoId_ThenListsAllPackages()
        {
            // Arrange
            clientFactory.MockHandler.MockPackageList("myorg", "myrepo");

            var options = new ListOptions
            {
                Organisation = "myorg",
                Repository = "myrepo"
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "List should succeed");
            clientFactory.MockHandler.RequestCount.Should().BeGreaterThan(0, "HTTP requests should have been made");
        }

        [Fact]
        public async Task ListHandler_GivenId_ThenListsPackagesById()
        {
            // Arrange
            clientFactory.MockHandler.MockPackageListById("myorg", "myrepo", "my-package");

            var options = new ListOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                PackageId = "my-package"
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "List should succeed");
            clientFactory.MockHandler.RequestCount.Should().BeGreaterThan(0, "HTTP requests should have been made");
        }

        [Fact]
        public async Task ListHandler_GivenHttpError_ThenReturnsError()
        {
            // Arrange
            clientFactory.MockHandler.MockPackageListError("myorg", "myrepo", HttpStatusCode.Unauthorized, "Unauthorized");

            var options = new ListOptions
            {
                Organisation = "myorg",
                Repository = "myrepo"
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(1, "Should return error on HTTP error");
            clientFactory.MockHandler.RequestCount.Should().BeGreaterThan(0, "Request should have been attempted");
        }
    }
}
