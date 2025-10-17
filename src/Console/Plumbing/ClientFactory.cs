using Feedz.Client;
using Feedz.Client.Logging;
using Serilog;

namespace Feedz.Console.Plumbing;

public interface IClientFactory
{
    FeedzClient Create(string? pat);
}

public class ClientFactory : IClientFactory
{
    public FeedzClient Create(string? pat)
    {
        var client =  FeedzClient.Create(pat);
        client.Log = new DelegateFeedzLogger(Log.Information, Log.Error);
        return client;
    }
}
