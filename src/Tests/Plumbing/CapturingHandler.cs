using System.Threading.Tasks;
using Feedz.Console.Commands;

namespace Feedz.Console.Tests.Plumbing
{
    public class CapturingHandler<TOptions> : IHandler<TOptions>
    {
        public TOptions? CapturedOptions { get; private set; }

        public Task<int> Handle(TOptions options)
        {
            CapturedOptions = options;
            return Task.FromResult(0);
        }
    }
}
