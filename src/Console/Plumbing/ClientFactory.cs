using Feedz.Client;

namespace Feedz.Console.Plumbing;

public interface IClientFactory
{
    FeedzClient Create(string? pat);
}

public class ClientFactory : IClientFactory
{
    public FeedzClient Create(string? pat)
    {
        return FeedzClient.Create(pat);
    }
}
