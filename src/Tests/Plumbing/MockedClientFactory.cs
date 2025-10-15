using System;
using System.Net.Http;
using Feedz.Client;
using Feedz.Console.Plumbing;

namespace Feedz.Console.Tests.Plumbing
{
    /// <summary>
    /// Test implementation of IClientFactory that creates mock FeedzClient instances for testing.
    /// </summary>
    public class MockedClientFactory : IClientFactory
    {
        public MockHttpMessageHandler MockHandler { get; } = new();

        public FeedzClient? CreatedClient { get; private set; }

        public FeedzClient Create(string? pat)
        {
            var apiClient = new HttpClient(MockHandler) { BaseAddress = new Uri("https://feedz.io/api/") };
            var feedClient = new HttpClient(MockHandler) { BaseAddress = new Uri("https://f.feedz.io/") };

            CreatedClient = FeedzClient.Create(pat, apiClient, feedClient);
            return CreatedClient;
        }
    }
}
