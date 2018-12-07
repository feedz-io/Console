using Feedz.Client;

namespace Feedz.Console.Plumbing
{
    public class ClientFactory
    {
        public static FeedzClient Create(string pat, string region)
        {
            var useXyz = !string.IsNullOrEmpty(region) && region.StartsWith("xyz");
            if (useXyz)
                region = region.Substring(3);

            return useXyz
                ? FeedzClient.Create(pat, "https://feedz.xyz/", "https://f.feedz.xyz/")
                : FeedzClient.Create(pat);
        }
    }
}