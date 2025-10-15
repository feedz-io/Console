namespace Feedz.Console.Commands.List;

public record ListOptions
{
    public required string Organisation { get; init; }
    public required string Repository { get; init; }
    public string? Pat { get; init; }
    public string? PackageId { get; init; }
}
