using LinkedInJobWatcher.Core.Models;

namespace LinkedInJobWatcher.Core.Abstractions;

public interface INotifier
{
    /// <summary>Sends a single e-mail listing all new approved items for the cycle. Never called with an empty list.</summary>
    Task SendNewItemsAsync(IReadOnlyList<ScoredItem> items, CancellationToken cancellationToken);

    Task SendSessionExpiredAlertAsync(CancellationToken cancellationToken);

    Task SendBlockedAlertAsync(string reason, TimeSpan backoffDuration, CancellationToken cancellationToken);

    Task SendTestEmailAsync(CancellationToken cancellationToken);
}
