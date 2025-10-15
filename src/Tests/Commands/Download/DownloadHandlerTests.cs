using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Threading.Tasks;
using Feedz.Console.Commands.Download;
using Feedz.Console.Tests.Plumbing;
using FluentAssertions;

namespace Feedz.Console.Tests.Commands.Download
{
    public class DownloadHandlerTests
    {
        private readonly string testDirectory = Path.Combine(Path.GetTempPath(), "feedz-download-tests-" + Guid.NewGuid());
        private readonly DownloadHandler handler;
        private readonly MockedClientFactory clientFactory = new();
        private readonly MockFileSystem fileSystem = new();

        public DownloadHandlerTests()
        {
            fileSystem.AddDirectory(testDirectory);
            fileSystem.Directory.SetCurrentDirectory(testDirectory);
            handler = new DownloadHandler(clientFactory, fileSystem);
        }

        [Fact]
        public async Task DownloadHandler_GivenVersion_ThenDownloadsSpecificVersion()
        {
            // Arrange
            bool downloadCalled = false;
            clientFactory.MockHandler.MockPackageMetadata("myorg", "myrepo", "my-package", "1.0.0");
            clientFactory.MockHandler.MockPackageDownload("myorg", "myrepo", "my-package", "1.0.0", onDownload: () => downloadCalled = true);

            var options = new DownloadOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                PackageId = "my-package",
                Version = "1.0.0"
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "Download should succeed");
            downloadCalled.Should().BeTrue("Download endpoint should have been called");
            fileSystem.File.Exists(fileSystem.Path.Combine(testDirectory, "my-package.1.0.0.nupkg")).Should().BeTrue("Package file should have been created");
        }

        [Fact]
        public async Task DownloadHandler_GivenNoVersion_ThenDownloadsLatestVersion()
        {
            // Arrange
            bool downloadCalled = false;
            clientFactory.MockHandler.MockPackageMetadataLatest("myorg", "myrepo", "my-package", "2.0.0");
            clientFactory.MockHandler.MockPackageDownload("myorg", "myrepo", "my-package", "2.0.0", onDownload: () => downloadCalled = true);

            var options = new DownloadOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                PackageId = "my-package"
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "Download should succeed");
            downloadCalled.Should().BeTrue("Download endpoint should have been called");
            fileSystem.File.Exists(fileSystem.Path.Combine(testDirectory, "my-package.2.0.0.nupkg")).Should().BeTrue("Package file should have been created");
        }

        [Fact]
        public async Task DownloadHandler_GivenFileExists_ThenReturnsError()
        {
            // Arrange
            fileSystem.AddFile(fileSystem.Path.Combine(testDirectory, "my-package.1.0.0.nupkg"), new MockFileData("existing content"));
            clientFactory.MockHandler.MockPackageMetadata("myorg", "myrepo", "my-package", "1.0.0");

            var options = new DownloadOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                PackageId = "my-package",
                Version = "1.0.0"
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(1, "Should return error when file exists");
            clientFactory.MockHandler.RequestCount.Should().Be(1, "Only metadata request should have been made");
            fileSystem.File.ReadAllText(fileSystem.Path.Combine(testDirectory, "my-package.1.0.0.nupkg")).Should().Be("existing content", "Existing file should not be overwritten");
        }

        [Fact]
        public async Task DownloadHandler_GivenTimeout_ThenSetsClientTimeout()
        {
            // Arrange
            clientFactory.MockHandler.MockPackageMetadataLatest("myorg", "myrepo", "my-package");
            clientFactory.MockHandler.MockPackageDownload("myorg", "myrepo", "my-package", "2.0.0");

            var options = new DownloadOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                PackageId = "my-package",
                Timeout = 3600
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "Download should succeed");
            clientFactory.CreatedClient!.FeedTimeout.Should().Be(TimeSpan.FromSeconds(3600), "Timeout should match the specified value");
        }

        [Fact]
        public async Task DownloadHandler_GivenNonNupkgExtension_ThenCreatesCorrectFilename()
        {
            // Arrange
            clientFactory.MockHandler.MockPackageMetadata("myorg", "myrepo", "MyPackage", "3.1.4", ".zip");
            clientFactory.MockHandler.MockPackageDownload("myorg", "myrepo", "MyPackage", "3.1.4");

            var options = new DownloadOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                PackageId = "MyPackage",
                Version = "3.1.4"
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "Download should succeed");
            fileSystem.File.Exists(fileSystem.Path.Combine(testDirectory, "MyPackage.3.1.4.zip")).Should().BeTrue("Package file should use correct format: {packageId}.{version}{extension}");
        }

        [Fact]
        public async Task DownloadHandler_GivenHttpError_ThenReturnsError()
        {
            // Arrange
            clientFactory.MockHandler.MockPackageMetadataLatest("myorg", "myrepo", "my-package");
            clientFactory.MockHandler.MockPackageDownloadError("myorg", "myrepo", "my-package", "2.0.0", HttpStatusCode.Unauthorized, "Unauthorized");

            var options = new DownloadOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                PackageId = "my-package"
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(1, "Should return error on HTTP error");
            clientFactory.MockHandler.RequestCount.Should().BeGreaterThan(0, "Request should have been attempted");
        }
    }
}
