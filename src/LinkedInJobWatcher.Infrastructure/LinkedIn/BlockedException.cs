namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

/// <summary>Raised when a CAPTCHA, verification challenge, or HTTP 429 is detected.</summary>
public sealed class BlockedException(string message) : Exception(message);
