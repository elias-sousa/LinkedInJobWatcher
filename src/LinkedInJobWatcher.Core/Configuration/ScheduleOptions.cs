namespace LinkedInJobWatcher.Core.Configuration;

public sealed class ScheduleOptions
{
    public const string SectionName = "Schedule";

    public int IntervalMinutes { get; set; } = 20;

    public int JitterMinutes { get; set; } = 5;

    public int WindowStartHour { get; set; } = 7;

    public int WindowEndHour { get; set; } = 22;

    /// <summary>Days of week the worker is allowed to run. Defaults to Monday-Saturday.</summary>
    public List<DayOfWeek> ActiveDays { get; set; } =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
    ];

    public int MaxSearchesPerCycle { get; set; } = 2;
}
