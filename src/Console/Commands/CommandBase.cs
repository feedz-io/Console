using System.Linq;
using System.Threading.Tasks;
using Feedz.Util.Extensions;
using NDesk.Options;
using Serilog;

namespace Feedz.Console.Commands
{
    public abstract class CommandBase : ICommand
    {
        private bool _printHelp;

        public async Task Execute(string[] args)
        {
            var options = new OptionSet();
            options.Add("help", () => "Show the options for this command", v => _printHelp = true);
            PopulateOptions(options);

            if (args.Length == 0)
            {
                PrintHelp(options);
                return;
            }

            
            var remaining = options.Parse(args);
            if (remaining.Any())
            {
                Log.Error($"Unrecognised {(remaining.Count == 1 ? "option" : "options")} {remaining.Join(", ")}");
                PrintHelp(options);
                return;
            }

            if (_printHelp)
            {
                PrintHelp(options);
                return;
            }

            await Execute();
        }

        protected abstract void PopulateOptions(OptionSet options);

        protected abstract Task Execute();

        static void PrintHelp(OptionSet options)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Usage: ");
            options.WriteOptionDescriptions(System.Console.Out);
        }

    }
}