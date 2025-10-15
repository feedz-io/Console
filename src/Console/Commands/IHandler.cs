namespace Feedz.Console.Commands;

public interface IHandler<in TOptions>
{
    Task<int> Handle(TOptions options);
}
