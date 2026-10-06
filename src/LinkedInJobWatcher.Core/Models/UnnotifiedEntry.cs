namespace LinkedInJobWatcher.Core.Models;

/// <summary>A previously-approved item that is saved ("seen") but not yet notified - e.g. because the last send attempt failed.</summary>
public sealed record UnnotifiedEntry(Guid Id, ScoredItem Item);
