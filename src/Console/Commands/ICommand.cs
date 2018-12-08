using System;
using System.Threading.Tasks;

namespace Feedz.Console.Commands
{
    public interface ICommand
    {
        Task Execute(string[] args);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
        
        public string Name { get; }
        public string Description { get; }
    }
}