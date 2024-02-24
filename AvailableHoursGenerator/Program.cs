using AvailableHoursGenerator;
using AvailableHoursGenerator.Config;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddAppSettings(builder.Configuration);
builder.Services.AddHostedService<Program>();
await builder.Build().RunAsync();

partial class Program(
    IOptions<AppSettings> appSettings
) : BackgroundService
{
    public static readonly DateTime Start = DateTime.UtcNow.AddDays(-1).Date;
    public static readonly DateTime End = Start.AddDays(90);
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("1: Show All Meeting Summary Values");
        Console.WriteLine("2: Run Job");
        Console.Write("Make a selection: ");
        string? answer = Console.ReadLine();
        switch (answer)
        {
            case "1":
                ShowAllMeetingSummaryValues();
                break;
            case "2":
                RunJob();
                break;
            default:
                throw new ArgumentException();
        }

        

        Environment.Exit(0);
        return Task.CompletedTask;
    }

    private void RunJob()
    {
        HalfHourBlock.GetAllBlocks(); // Force initialization

        Console.WriteLine("Skipping the following:");
        foreach (string skipSummary in appSettings.Value.SkipSummaryValues)
        {
            Console.WriteLine(skipSummary);
        }

        DirectoryInfo dir = new(appSettings.Value.InputDirectory);
        if (!dir.Exists) { throw new DirectoryNotFoundException(); }

        HashSet<string> excludedEvents = [];

        Console.WriteLine("Loading calendars");
        foreach (FileInfo file in dir.GetFiles("*.ics"))
        {
            Console.WriteLine($"Loading calendar {file.Name}");
            using FileStream fileStream = file.OpenRead();
            Calendar calendar = Calendar.Load(fileStream);

            foreach (CalendarEvent calEvent in calendar.Events)
            {
                string summary = calEvent.Summary;
                if (excludedEvents.Contains(summary.ToLower())) { continue; }

                Console.WriteLine($"Event: {summary}");

                IEnumerable<Occurrence> occurrences = calEvent.GetOccurrences(Start, End);

                bool hasOccurrence = false;
                foreach (Occurrence occurrence in occurrences)
                {
                    if (excludedEvents.Contains(summary.ToLower())) { continue; }

                    hasOccurrence = true;
                    DateTime startUTC = occurrence.Period.StartTime.AsUtc;
                    DateTime endUTC = occurrence.Period.EndTime?.AsUtc ?? startUTC;

                    if (HalfHourBlock.GetMatchingBlocks(startUTC, endUTC).Any(b => b.IsFree))
                    {
                        Console.Write("Skip? (Y to skip, Enter to keep)");
                        if (Console.ReadLine()?.ToLower() == "y")
                        {
                            excludedEvents.Add(summary.ToLower());
                            continue;
                        }
                    }

                    Console.WriteLine($"  {startUTC} - {endUTC}");
                    HalfHourBlock.ReserveBlock(startUTC, endUTC);
                }

                if (!hasOccurrence)
                {
                    DateTime startUTC = calEvent.Start.AsUtc;
                    DateTime endUTC = calEvent.End?.AsUtc ?? startUTC;
                    Console.WriteLine($"  {startUTC} - {endUTC}");
                    HalfHourBlock.ReserveBlock(startUTC, endUTC);
                }
            }
        }

        foreach (var block in HalfHourBlock.GetAllBlocks())
        {
            DateTime start = new DateTimeOffset(block.StartDateUTC, TimeSpan.Zero).ToLocalTime().DateTime;
            DateTime end = new DateTimeOffset(block.EndDateUTC, TimeSpan.Zero).ToLocalTime().DateTime;

            Console.WriteLine($"{start} - {end} {(block.IsFree ? "Free" : "Reserved")}");
        }
    }

    private void ShowAllMeetingSummaryValues()
    {
        HashSet<string> meetingSummaries = [];

        DirectoryInfo dir = new(appSettings.Value.InputDirectory);
        if (!dir.Exists) { throw new DirectoryNotFoundException(); }

        foreach (FileInfo file in dir.GetFiles("*.ics"))
        {
            using FileStream fileStream = file.OpenRead();
            Calendar calendar = Calendar.Load(fileStream);

            foreach (CalendarEvent calEvent in calendar.Events)
            {
                string? summary = calEvent.Summary;
                if (string.IsNullOrWhiteSpace(summary))
                {
                    continue;
                }
                if (appSettings.Value.SkipSummaryValues.Contains(summary.ToLower()))
                {
                    continue;
                }
                meetingSummaries.Add(summary.ToLower().Trim());
            }
        }

        Console.WriteLine(JsonSerializer.Serialize(meetingSummaries.OrderBy(x => x), new JsonSerializerOptions { WriteIndented = true }));
    }
}
