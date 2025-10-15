using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Feedz.Console.Tests.Plumbing
{
    /// <summary>
    /// Helper methods for creating common mock HTTP responses.
    /// </summary>
    public static class MockResponseHelper
    {
        /// <summary>
        /// Adds a mock response for the delta signature endpoint that returns 404 to force full upload.
        /// </summary>
        public static void MockDeltaSignatureNotFound(this MockHttpMessageHandler handler)
        {
            handler.AddResponse(
                req => req.Method == HttpMethod.Get && req.RequestUri?.ToString().Contains("/delta-signature") == true,
                () => new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        /// <summary>
        /// Adds a mock response for successful package upload.
        /// </summary>
        public static void MockSuccessfulUpload(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, Action? onUpload = null)
        {
            handler.AddResponse(
                req => req.Method == HttpMethod.Post && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}") == true,
                () =>
                {
                    onUpload?.Invoke();
                    return CreateSuccessResponse();
                });
        }

        /// <summary>
        /// Adds a mock response for failed package upload with specific status code.
        /// </summary>
        public static void MockFailedUpload(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, HttpStatusCode statusCode, string? errorMessage = null)
        {
            handler.AddResponse(
                req => req.Method == HttpMethod.Post && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}") == true,
                () => new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(errorMessage ?? statusCode.ToString(), Encoding.UTF8, "text/plain")
                });
        }

        /// <summary>
        /// Creates a standard success response for package upload.
        /// </summary>
        public static HttpResponseMessage CreateSuccessResponse()
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"packageId\":\"test-package\",\"version\":\"1.0.0\",\"extension\":\".nupkg\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        }

        /// <summary>
        /// Adds a mock response for listing all packages in a repository.
        /// </summary>
        public static void MockPackageList(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, string? packagesJson = null)
        {
            var defaultJson = "[{\"packageId\":\"test-package\",\"version\":\"1.0.0\",\"extension\":\".nupkg\"}," +
                             "{\"packageId\":\"test-package\",\"version\":\"2.0.0\",\"extension\":\".nupkg\"}]";

            handler.AddResponse(
                req => req.Method == HttpMethod.Get && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}/packages") == true && req.RequestUri.ToString().Contains("/packages/") == false,
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(packagesJson ?? defaultJson, Encoding.UTF8, "application/json")
                });
        }

        /// <summary>
        /// Adds a mock response for listing packages by package ID.
        /// </summary>
        public static void MockPackageListById(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, string packageId, string? packagesJson = null)
        {
            var defaultJson = $"[{{\"packageId\":\"{packageId}\",\"version\":\"1.0.0\",\"extension\":\".nupkg\"}}," +
                             $"{{\"packageId\":\"{packageId}\",\"version\":\"2.0.0\",\"extension\":\".nupkg\"}}]";

            handler.AddResponse(
                req => req.Method == HttpMethod.Get && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}/packages/{packageId}") == true && req.RequestUri.ToString().Contains("/download") == false,
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(packagesJson ?? defaultJson, Encoding.UTF8, "application/json")
                });
        }

        /// <summary>
        /// Adds a mock response for package list returning an error.
        /// </summary>
        public static void MockPackageListError(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, HttpStatusCode statusCode, string? errorMessage = null)
        {
            handler.AddResponse(
                req => req.Method == HttpMethod.Get && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}/packages") == true,
                () => new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(errorMessage ?? statusCode.ToString(), Encoding.UTF8, "text/plain")
                });
        }

        /// <summary>
        /// Adds a mock response for getting package metadata by version.
        /// </summary>
        public static void MockPackageMetadata(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, string packageId, string version, string extension = ".nupkg")
        {
            var json = $"{{\"packageId\":\"{packageId}\",\"version\":\"{version}\",\"extension\":\"{extension}\"}}";

            handler.AddResponse(
                req => req.Method == HttpMethod.Get && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}/packages/{packageId}/{version}") == true && req.RequestUri.ToString().Contains("/download") == false,
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
        }

        /// <summary>
        /// Adds a mock response for getting latest package metadata.
        /// </summary>
        public static void MockPackageMetadataLatest(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, string packageId, string version = "2.0.0", string extension = ".nupkg")
        {
            var json = $"{{\"packageId\":\"{packageId}\",\"version\":\"{version}\",\"extension\":\"{extension}\"}}";

            handler.AddResponse(
                req => req.Method == HttpMethod.Get && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}/packages/{packageId}/latest") == true,
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
        }

        /// <summary>
        /// Adds a mock response for downloading a package.
        /// </summary>
        public static void MockPackageDownload(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, string packageId, string version, string? content = null, Action? onDownload = null)
        {
            handler.AddResponse(
                req => req.Method == HttpMethod.Get && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}/packages/{packageId}/{version}/download") == true,
                () =>
                {
                    onDownload?.Invoke();
                    var responseContent = content ?? "mock package content";
                    var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(responseContent)));
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = streamContent
                    };
                });
        }

        /// <summary>
        /// Adds a mock response for package download failure.
        /// </summary>
        public static void MockPackageDownloadError(this MockHttpMessageHandler handler, string orgSlug, string repoSlug, string packageId, string version, HttpStatusCode statusCode, string? errorMessage = null)
        {
            handler.AddResponse(
                req => req.Method == HttpMethod.Get && req.RequestUri?.ToString().Contains($"{orgSlug}/{repoSlug}/packages/{packageId}/{version}/download") == true,
                () => new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(errorMessage ?? statusCode.ToString(), Encoding.UTF8, "text/plain")
                });
        }
    }
}
