using System;
using System.IO;
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
    /// Integration tests for DownloadCommand that test the full execution flow with mocked HTTP responses.
    /// These tests verify the command correctly downloads packages and handles errors.
    /// </summary>
    public class DownloadCommandIntegrationTests : CommandIntegrationTestBase
    {
        private string _testDirectory = null!;

        protected override void OnSetup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "feedz-download-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(_testDirectory);
            Directory.SetCurrentDirectory(_testDirectory);
        }

        protected override void OnTearDown()
        {
            try
            {
                Directory.SetCurrentDirectory(Path.GetTempPath());
                if (Directory.Exists(_testDirectory))
                    Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Success Cases
        [Test]
        public async Task DownloadCommand_GivenVersion_ThenDownloadsSpecificVersion()
        {
            // Arrange
            bool downloadCalled = false;
            MockHandler.MockPackageMetadata("myorg", "myrepo", "my-package", "1.0.0");
            MockHandler.MockPackageDownload("myorg", "myrepo", "my-package", "1.0.0", onDownload: () => downloadCalled = true);

            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--org=myorg", "--repo=myrepo", "--id=my-package", "--version=1.0.0");

            // Assert
            downloadCalled.Should().BeTrue("Download endpoint should have been called");
            File.Exists("my-package.1.0.0.nupkg").Should().BeTrue("Package file should have been created");
        }

        [Test]
        public async Task DownloadCommand_GivenNoVersion_ThenDownloadsLatestVersion()
        {
            // Arrange
            bool downloadCalled = false;
            MockHandler.MockPackageMetadataLatest("myorg", "myrepo", "my-package", "2.0.0");
            MockHandler.MockPackageDownload("myorg", "myrepo", "my-package", "2.0.0", onDownload: () => downloadCalled = true);

            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--org=myorg", "--repo=myrepo", "--id=my-package");

            // Assert
            downloadCalled.Should().BeTrue("Download endpoint should have been called");
            File.Exists("my-package.2.0.0.nupkg").Should().BeTrue("Package file should have been created");
        }

        // Feature Tests
        [Test]
        public async Task DownloadCommand_GivenFileExists_ThenDoesNotDownload()
        {
            // Arrange
            File.WriteAllText("my-package.1.0.0.nupkg", "existing content");
            MockHandler.MockPackageMetadata("myorg", "myrepo", "my-package", "1.0.0");

            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--org=myorg", "--repo=myrepo", "--id=my-package", "--version=1.0.0");

            // Assert
            MockHandler.RequestCount.Should().Be(1, "Only metadata request should have been made");
            File.ReadAllText("my-package.1.0.0.nupkg").Should().Be("existing content", "Existing file should not be overwritten");
        }

        [Test]
        public async Task DownloadCommand_GivenTimeout_ThenSetsClientTimeout()
        {
            // Arrange
            MockHandler.MockPackageMetadataLatest("myorg", "myrepo", "my-package");
            MockHandler.MockPackageDownload("myorg", "myrepo", "my-package", "2.0.0");

            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--org=myorg", "--repo=myrepo", "--id=my-package", "--timeout=3600");

            // Assert
            command.ClientTimeoutWasSet.Should().BeTrue("Client timeout should have been set");
            command.ClientTimeout.Should().Be(TimeSpan.FromSeconds(3600), "Timeout should match the specified value");
        }

        [Test]
        public async Task DownloadCommand_GivenNonNupkgExtension_ThenCreatesCorrectFilename()
        {
            // Arrange
            MockHandler.MockPackageMetadata("myorg", "myrepo", "MyPackage", "3.1.4", ".zip");
            MockHandler.MockPackageDownload("myorg", "myrepo", "MyPackage", "3.1.4");

            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--org=myorg", "--repo=myrepo", "--id=MyPackage", "--version=3.1.4");

            // Assert
            File.Exists("MyPackage.3.1.4.zip").Should().BeTrue("Package file should use correct format: {packageId}.{version}{extension}");
        }

        // Validation Tests
        [Test]
        public async Task DownloadCommand_GivenMissingOrganisation_ThenFailsValidation()
        {
            // Arrange
            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--repo=myrepo", "--id=my-package");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        [Test]
        public async Task DownloadCommand_GivenMissingRepository_ThenFailsValidation()
        {
            // Arrange
            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--org=myorg", "--id=my-package");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        [Test]
        public async Task DownloadCommand_GivenMissingId_ThenFailsValidation()
        {
            // Arrange
            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--org=myorg", "--repo=myrepo");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        // Error Handling Tests
        [Test]
        public async Task DownloadCommand_GivenHttpError_ThenHandlesGracefully()
        {
            // Arrange
            MockHandler.MockPackageMetadataLatest("myorg", "myrepo", "my-package");
            MockHandler.MockPackageDownloadError("myorg", "myrepo", "my-package", "2.0.0", HttpStatusCode.Unauthorized, "Unauthorized");

            var command = new TestableDownloadCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "download", "--org=myorg", "--repo=myrepo", "--id=my-package");

            // Assert
            MockHandler.RequestCount.Should().BeGreaterThan(0, "Request should have been attempted");
        }

        /// <summary>
        /// Testable version of DownloadCommand that allows injecting a FeedzClient and tracking behavior.
        /// </summary>
        private class TestableDownloadCommand : DownloadCommand
        {
            private readonly FeedzClient _mockClient;

            public TestableDownloadCommand(FeedzClient mockClient)
            {
                _mockClient = mockClient;
            }

            public bool ClientTimeoutWasSet { get; private set; }
            public TimeSpan ClientTimeout { get; private set; }

            protected override FeedzClient CreateClient(string pat, string region)
            {
                return _mockClient;
            }

            protected override async Task Execute()
            {
                await base.Execute();

                if (_mockClient != null)
                {
                    ClientTimeout = _mockClient.FeedTimeout;
                    ClientTimeoutWasSet = true;
                }
            }
        }
    }
}
