using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Feedz.Console.Tests.Plumbing
{
    /// <summary>
    /// Mock HTTP message handler that allows configuring responses based on request predicates.
    /// Supports factory functions to create fresh responses for each request.
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<(Func<HttpRequestMessage, bool> predicate, Func<HttpResponseMessage> responseFactory)> responses = new();

        public int RequestCount { get; private set; }

        public void AddResponse(Func<HttpRequestMessage, bool> predicate, Func<HttpResponseMessage> responseFactory)
        {
            responses.Add((predicate, responseFactory));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            foreach (var (predicate, responseFactory) in responses)
            {
                if (predicate(request))
                    return Task.FromResult(responseFactory());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Endpoint not mocked", Encoding.UTF8, "text/plain")
            });
        }
    }
}
