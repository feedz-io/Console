namespace Feedz.Console.Commands.Push;

public record PushOptions
{
    public required string Organisation { get; init; }
    public required string Repository { get; init; }
    public required string Pat { get; init; }
    public required IReadOnlyList<FileInfo> Files { get; init; }
    public bool Force { get; init; }
    public int Timeout { get; init; } = 1800;
}
