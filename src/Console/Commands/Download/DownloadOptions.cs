namespace Feedz.Console.Commands.Download;

public record DownloadOptions
{
    public required string Organisation { get; init; }
    public required string Repository { get; init; }
    public string? Pat { get; init; }
    public required string PackageId { get; init; }
    public string? Version { get; init; }
    public string? SimilarPackagePath { get; init; }
    public int Timeout { get; init; } = 1800;
}
