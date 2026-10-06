namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

public sealed class SessionExpiredException(string message) : Exception(message);
