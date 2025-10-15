using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Feedz.Console.Commands.Push;
using Feedz.Console.Tests.Plumbing;
using FluentAssertions;

namespace Feedz.Console.Tests.Commands.Push
{
    public class PushHandlerTests
    {
        private readonly string testFilePath = Path.Combine(Path.GetTempPath(), "test-package-1.0.0.nupkg");
        private readonly PushHandler handler;
        private readonly MockedClientFactory clientFactory = new();
        private readonly MockFileSystem fileSystem = new();

        public PushHandlerTests()
        {
            fileSystem.AddFile(testFilePath, new MockFileData("test package content"));
            handler = new PushHandler(clientFactory, fileSystem);
        }

        [Fact]
        public async Task PushHandler_GivenValidFile_ThenUploadsSuccessfully()
        {
            // Arrange
            bool uploadCalled = false;
            clientFactory.MockHandler.MockDeltaSignatureNotFound();
            clientFactory.MockHandler.MockSuccessfulUpload("myorg", "myrepo", () => uploadCalled = true);

            var options = new PushOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                Pat = "test-pat",
                Files = new[] { new FileInfo(testFilePath) }
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "Upload should succeed");
            uploadCalled.Should().BeTrue("Upload endpoint should have been called");
            clientFactory.MockHandler.RequestCount.Should().BeGreaterThan(0, "HTTP requests should have been made");
        }

        [Fact]
        public async Task PushHandler_GivenMultipleFiles_ThenUploadsAll()
        {
            // Arrange
            var testFile2 = Path.Combine(Path.GetTempPath(), "test-package-2.0.0.nupkg");
            fileSystem.AddFile(testFile2, new MockFileData("test package content 2"));

            int uploadCount = 0;
            clientFactory.MockHandler.MockDeltaSignatureNotFound();
            clientFactory.MockHandler.MockSuccessfulUpload("myorg", "myrepo", () => uploadCount++);

            var options = new PushOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                Pat = "test-pat",
                Files = new[] { new FileInfo(testFilePath), new FileInfo(testFile2) }
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "Upload should succeed");
            uploadCount.Should().Be(2, "Both files should have been uploaded");
        }

        [Fact]
        public async Task PushHandler_GivenForceFlag_ThenPassesReplaceParameter()
        {
            // Arrange
            bool forceParameterPresent = false;
            clientFactory.MockHandler.MockDeltaSignatureNotFound();

            clientFactory.MockHandler.AddResponse(
                req =>
                {
                    if (req.Method == HttpMethod.Post && req.RequestUri?.ToString().Contains("myorg/myrepo") == true)
                    {
                        forceParameterPresent = req.RequestUri.Query.Contains("replace=true", StringComparison.CurrentCultureIgnoreCase);
                        return true;
                    }

                    return false;
                },
                MockHttpMessageHandlerExtensions.CreateSuccessResponse
            );

            var options = new PushOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                Pat = "test-pat",
                Files = new[] { new FileInfo(testFilePath) },
                Force = true
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "Upload should succeed");
            forceParameterPresent.Should().BeTrue("Force parameter should be included in the request");
        }

        [Fact]
        public async Task PushHandler_GivenTimeout_ThenSetsClientTimeout()
        {
            // Arrange
            clientFactory.MockHandler.MockDeltaSignatureNotFound();
            clientFactory.MockHandler.MockSuccessfulUpload("myorg", "myrepo");

            var options = new PushOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                Pat = "test-pat",
                Files = new[] { new FileInfo(testFilePath) },
                Timeout = 3600
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(0, "Upload should succeed");
            clientFactory.CreatedClient!.FeedTimeout.Should().Be(TimeSpan.FromSeconds(3600), "Timeout should match the specified value");
        }

        [Fact]
        public async Task PushHandler_GivenMissingFile_ThenReturnsError()
        {
            // Arrange
            var options = new PushOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                Pat = "test-pat",
                Files = new[] { new FileInfo("nonexistent.nupkg") }
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(1, "Should return error for missing file");
            clientFactory.MockHandler.RequestCount.Should().Be(0, "No HTTP requests should be made for missing file");
        }

        [Fact]
        public async Task PushHandler_GivenHttpError_ThenReturnsError()
        {
            // Arrange
            clientFactory.MockHandler.MockDeltaSignatureNotFound();
            clientFactory.MockHandler.MockFailedUpload("myorg", "myrepo", HttpStatusCode.Unauthorized, "Unauthorized");

            var options = new PushOptions
            {
                Organisation = "myorg",
                Repository = "myrepo",
                Pat = "test-pat",
                Files = new[] { new FileInfo(testFilePath) }
            };

            // Act
            var exitCode = await handler.Handle(options);

            // Assert
            exitCode.Should().Be(1, "Should return error on HTTP error");
            clientFactory.MockHandler.RequestCount.Should().BeGreaterThan(0, "Request should have been attempted");
        }
    }
}