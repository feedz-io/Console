using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Feedz.Console.Commands;
using Serilog;
using Serilog.Exceptions;

namespace Feedz.Console
{
    public record CommandInfo(string Name, string Description, ICommand Instance);

    public class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .WriteTo.Console(outputTemplate: "{Message:lk}{NewLine}{Exception}")
                .MinimumLevel.Information()
                .CreateLogger();

            var commands = (from t in typeof(Program).Assembly.GetTypes()
                where typeof(ICommand).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract
                let a = (CommandAttribute) t.GetCustomAttribute(typeof(CommandAttribute))
                select new CommandInfo(
                    a.Name,
                    a.Description,
                    (ICommand)Activator.CreateInstance(t)
                )).ToList();

            await Execute(args, commands);
        }

        public static async Task Execute(string[] args, List<CommandInfo> commands)
        {
            var command = args.Length == 0
                ? null
                : commands.FirstOrDefault(c => c.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

            if (command != null)
            {
                await command.Instance.Execute(args.Skip(1).ToArray());
                return;
            }

            Log.Information($"Usage: Feedz <command> [options]");
            Log.Information("");
            Log.Information("Available commands are:");
            foreach (var cmd in commands.OrderBy(a => a.Name))
            {
                Log.Information("  " + cmd.Name.ToLower());
                Log.Information("    " + cmd.Description);
            }

            System.Console.WriteLine("");
        }
    }
}