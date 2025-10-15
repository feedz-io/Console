using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Feedz.Client;
using Feedz.Console.Commands;
using Feedz.Console.Tests.Plumbing;
using FluentAssertions;
using NUnit.Framework;

namespace Feedz.Console.Tests.Commands
{
    /// <summary>
    /// Integration tests for PushCommand that test the full execution flow with mocked HTTP responses.
    /// </summary>
    public class PushCommandIntegrationTests : CommandIntegrationTestBase
    {
        private string _testFilePath = null!;

        protected override void OnSetup()
        {
            _testFilePath = Path.Combine(Path.GetTempPath(), "test-package-1.0.0.nupkg");
            File.WriteAllText(_testFilePath, "test package content");
        }

        protected override void OnTearDown()
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }

        // Success Cases
        [Test]
        public async Task PushCommand_GivenValidFile_ThenUploadsSuccessfully()
        {
            // Arrange
            bool uploadCalled = false;
            MockHandler.MockDeltaSignatureNotFound();
            MockHandler.MockSuccessfulUpload("myorg", "myrepo", () => uploadCalled = true);

            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--org=myorg", "--repo=myrepo", "--pat=test-pat", $"--file={_testFilePath}");

            // Assert
            uploadCalled.Should().BeTrue("Upload endpoint should have been called");
            MockHandler.RequestCount.Should().BeGreaterThan(0, "HTTP requests should have been made");
        }

        [Test]
        public async Task PushCommand_GivenMultipleFiles_ThenUploadsAll()
        {
            // Arrange
            var testFile2 = Path.Combine(Path.GetTempPath(), "test-package-2.0.0.nupkg");
            File.WriteAllText(testFile2, "test package content 2");

            try
            {
                int uploadCount = 0;
                MockHandler.MockDeltaSignatureNotFound();
                MockHandler.MockSuccessfulUpload("myorg", "myrepo", () => uploadCount++);

                var command = new TestablePushCommand(CreateFeedzClient());

                // Act
                await ExecuteCommand(command, "push", "--org=myorg", "--repo=myrepo", "--pat=test-pat",
                    $"--file={_testFilePath}", $"--file={testFile2}");

                // Assert
                uploadCount.Should().Be(2, "Both files should have been uploaded");
            }
            finally
            {
                if (File.Exists(testFile2))
                    File.Delete(testFile2);
            }
        }

        // Feature Tests
        [Test]
        public async Task PushCommand_GivenForceFlag_ThenPassesReplaceParameter()
        {
            // Arrange
            bool forceParameterPresent = false;
            MockHandler.MockDeltaSignatureNotFound();

            MockHandler.AddResponse(
                req =>
                {
                    if (req.Method == HttpMethod.Post && req.RequestUri?.ToString().Contains("myorg/myrepo") == true)
                    {
                        forceParameterPresent = req.RequestUri.Query.Contains("replace=true", StringComparison.CurrentCultureIgnoreCase);
                        return true;
                    }
                    return false;
                },
                () => MockResponseHelper.CreateSuccessResponse());

            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--org=myorg", "--repo=myrepo", "--pat=test-pat",
                $"--file={_testFilePath}", "--force");

            // Assert
            forceParameterPresent.Should().BeTrue("Force parameter should be included in the request");
        }

        [Test]
        public async Task PushCommand_GivenTimeout_ThenSetsClientTimeout()
        {
            // Arrange
            MockHandler.MockDeltaSignatureNotFound();
            MockHandler.MockSuccessfulUpload("myorg", "myrepo");

            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--org=myorg", "--repo=myrepo", "--pat=test-pat",
                $"--file={_testFilePath}", "--timeout=3600");

            // Assert
            command.ClientTimeoutWasSet.Should().BeTrue("Client timeout should have been set");
            command.ClientTimeout.Should().Be(TimeSpan.FromSeconds(3600), "Timeout should match the specified value");
        }

        // Validation Tests
        [Test]
        public async Task PushCommand_GivenMissingFile_ThenDoesNotAttemptUpload()
        {
            // Arrange
            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--org=myorg", "--repo=myrepo", "--pat=test-pat", "--file=nonexistent.nupkg");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made for missing file");
        }

        [Test]
        public async Task PushCommand_GivenMissingOrganisation_ThenFailsValidation()
        {
            // Arrange
            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--repo=myrepo", "--pat=test-pat", $"--file={_testFilePath}");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        [Test]
        public async Task PushCommand_GivenMissingRepository_ThenFailsValidation()
        {
            // Arrange
            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--org=myorg", "--pat=test-pat", $"--file={_testFilePath}");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        [Test]
        public async Task PushCommand_GivenMissingPat_ThenFailsValidation()
        {
            // Arrange
            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--org=myorg", "--repo=myrepo", $"--file={_testFilePath}");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        [Test]
        public async Task PushCommand_GivenMissingFileParameter_ThenFailsValidation()
        {
            // Arrange
            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--org=myorg", "--repo=myrepo", "--pat=test-pat");

            // Assert
            MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made when validation fails");
        }

        // Error Handling Tests
        [Test]
        public async Task PushCommand_GivenHttpError_ThenHandlesGracefully()
        {
            // Arrange
            MockHandler.MockDeltaSignatureNotFound();
            MockHandler.MockFailedUpload("myorg", "myrepo", HttpStatusCode.Unauthorized, "Unauthorized");

            var command = new TestablePushCommand(CreateFeedzClient());

            // Act
            await ExecuteCommand(command, "push", "--org=myorg", "--repo=myrepo", "--pat=test-pat", $"--file={_testFilePath}");

            // Assert
            MockHandler.RequestCount.Should().BeGreaterThan(0, "Request should have been attempted");
        }

        /// <summary>
        /// Testable version of PushCommand that allows injecting a FeedzClient and tracking timeout behavior.
        /// </summary>
        private class TestablePushCommand : PushCommand
        {
            private readonly FeedzClient _mockClient;

            public TestablePushCommand(FeedzClient mockClient)
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
