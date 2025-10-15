using System;
using System.Net.Http;
using Feedz.Client;

namespace Feedz.Console.Tests.Plumbing
{
    /// <summary>
    /// Builder for creating mock FeedzClient instances for testing.
    /// </summary>
    public class FeedzClientTestBuilder
    {
        private MockHttpMessageHandler? _mockHandler;
        private string _pat = "test-pat";

        public FeedzClientTestBuilder WithMockHandler(MockHttpMessageHandler handler)
        {
            _mockHandler = handler;
            return this;
        }

        public FeedzClientTestBuilder WithPat(string pat)
        {
            _pat = pat;
            return this;
        }

        public FeedzClient Build()
        {
            if (_mockHandler == null)
                throw new InvalidOperationException("MockHandler must be set before building");

            var apiClient = new HttpClient(_mockHandler) { BaseAddress = new Uri("https://feedz.io/api/") };
            var feedClient = new HttpClient(_mockHandler) { BaseAddress = new Uri("https://f.feedz.io/") };

            return FeedzClient.Create(_pat, apiClient, feedClient);
        }
    }
}
