namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

/// <summary>
/// Every CSS selector that touches LinkedIn's DOM lives here. LinkedIn changes its markup
/// frequently and without notice, so when scraping starts coming back empty the first thing
/// to check is this file - prefer selectors based on stable attributes (data-urn, data-job-id,
/// aria-label, role) over generated class names, which rotate constantly.
/// </summary>
public static class LinkedInSelectors
{
    public static class Login
    {
        public const string Url = "https://www.linkedin.com/login";
        public const string FeedUrl = "https://www.linkedin.com/feed/";
    }

    public static class Session
    {
        public static readonly string[] ExpiredUrlMarkers = ["/login", "/checkpoint", "/authwall"];
    }

    public static class Posts
    {
        public const string SearchUrlTemplate =
            "https://www.linkedin.com/search/results/content/?keywords={0}&datePosted=%22{1}%22&sortBy=%22date_posted%22";

        /// <summary>Each result card. LinkedIn renders the post urn on a data-urn attribute somewhere up the tree.</summary>
        public const string ResultCard = "div[data-chameleon-result-urn]";

        public const string ResultCardUrnAttribute = "data-chameleon-result-urn";

        public const string AuthorNameLink = "span.update-components-actor__name span[aria-hidden='true']";

        public const string AuthorHeadline = "span.update-components-actor__description";

        public const string AuthorProfileLink = "a.update-components-actor__meta-link";

        public const string TextContainer = "div.update-components-text";

        public const string SeeMoreButton = "button[aria-label='ver mais'], button[aria-label='see more'], button.feed-shared-inline-show-more-text__see-more-less-toggle";

        public const string RelativeDate = "span.update-components-actor__sub-description span[aria-hidden='true']";

        public const string PostPermalinkAnchor = "a.app-aware-link[href*='/feed/update/']";

        /// <summary>Scroll container for the results list (the whole window on the search results page).</summary>
        public const string ResultsScrollContainer = "main";
    }

    public static class Jobs
    {
        public const string SearchUrlTemplate =
            "https://www.linkedin.com/jobs/search/?keywords={0}&f_TPR={1}&f_WT={2}&f_E={3}&sortBy=DD";

        public const string ResultCard = "li[data-occludable-job-id]";

        public const string ResultCardIdAttribute = "data-occludable-job-id";

        public const string CardTitle = "a.job-card-list__title, a[aria-label][href*='/jobs/view/']";

        public const string CardCompany = "span.job-card-container__primary-description, div.job-card-container__company-name";

        public const string CardLocation = "li.job-card-container__metadata-item";

        public const string CardSimplifiedApplyBadge = "span.job-card-container__apply-method, li[aria-label*='Candidatura simplificada'], li[aria-label*='Easy Apply']";

        public const string CardPostedDate = "time, span.job-card-container__listed-status";

        public const string DetailsPane = "div.jobs-search__job-details--container";

        public const string DetailsTitle = "h1.job-details-jobs-unified-top-card__job-title, h1.t-24";

        public const string DetailsCompany = "a.job-details-jobs-unified-top-card__company-name, div.job-details-jobs-unified-top-card__company-name";

        public const string DetailsPrimaryDescription = "div.job-details-jobs-unified-top-card__primary-description-container";

        public const string DetailsFullText = "div.jobs-description__content, div.jobs-box__html-content";

        public const string DetailsRecruiterCard = "div.job-details-people-who-can-help__section a.app-aware-link";

        public const string ResultsScrollContainer = "div.jobs-search-results-list";
    }
}
