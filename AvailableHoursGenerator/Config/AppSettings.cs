namespace AvailableHoursGenerator.Config;

internal record class AppSettings
{
    public required string InputDirectory { get; init; }
    public required List<string> SkipSummaryValues { get; init; }
}
