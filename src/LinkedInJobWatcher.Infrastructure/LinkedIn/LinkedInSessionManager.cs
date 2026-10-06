using LinkedInJobWatcher.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedInJobWatcher.Infrastructure.LinkedIn;

/// <summary>
/// Owns the Playwright/Firefox persistent browser context used to talk to LinkedIn with an
/// already-authenticated session. The profile directory is dedicated to this application
/// (never the user's real Firefox profile, which would be locked while in use).
/// </summary>
public sealed class LinkedInSessionManager(IOptions<LinkedInOptions> options, ILogger<LinkedInSessionManager> logger) : IAsyncDisposable
{
    private readonly LinkedInOptions _options = options.Value;
    private IPlaywright? _playwright;
    private IBrowserContext? _context;

    public async Task<IBrowserContext> GetOrCreateContextAsync(bool? headlessOverride = null)
    {
        if (_context is not null)
        {
            return _context;
        }

        _playwright ??= await Playwright.CreateAsync();

        var profileDir = Path.GetFullPath(_options.BrowserProfileDir);
        Directory.CreateDirectory(profileDir);

        var headless = headlessOverride ?? _options.Headless;

        logger.LogInformation("Abrindo Firefox (headless={Headless}) com perfil em {ProfileDir}", headless, profileDir);

        _context = await _playwright.Firefox.LaunchPersistentContextAsync(profileDir, new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = headless,
            ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
            Locale = "pt-BR"
        });

        return _context;
    }

    /// <summary>
    /// Navigates to <paramref name="url"/> and throws <see cref="SessionExpiredException"/> if LinkedIn
    /// redirected to a login/checkpoint page instead, or <see cref="BlockedException"/> on an HTTP 429
    /// or a visible verification challenge.
    /// </summary>
    public async Task<IResponse?> GoToAsync(IPage page, string url)
    {
        var response = await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        if (response is not null && response.Status == 429)
        {
            throw new BlockedException("HTTP 429 (muitas requisições) recebido do LinkedIn");
        }

        var finalUrl = page.Url;
        if (LinkedInSelectors.Session.ExpiredUrlMarkers.Any(marker => finalUrl.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            throw new SessionExpiredException($"Sessão expirada: redirecionado para {finalUrl}");
        }

        if (await LooksLikeChallengeAsync(page))
        {
            throw new BlockedException("Página de verificação/CAPTCHA detectada");
        }

        return response;
    }

    private static async Task<bool> LooksLikeChallengeAsync(IPage page)
    {
        var title = await page.TitleAsync();
        return title.Contains("verify", StringComparison.OrdinalIgnoreCase)
               || title.Contains("captcha", StringComparison.OrdinalIgnoreCase)
               || title.Contains("security check", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Opens Firefox visibly so the user can log in (including 2FA) exactly once; the session persists to the profile dir.</summary>
    public async Task RunInteractiveLoginAsync()
    {
        var context = await GetOrCreateContextAsync(headlessOverride: false);
        var page = await context.NewPageAsync();
        await page.GotoAsync(LinkedInSelectors.Login.Url);

        Console.WriteLine();
        Console.WriteLine("Faça login no LinkedIn na janela do Firefox que foi aberta (inclua a verificação em duas etapas, se solicitada).");
        Console.WriteLine("Quando terminar e estiver vendo seu feed do LinkedIn, volte aqui e pressione ENTER.");
        Console.ReadLine();

        if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase) || page.Url.Contains("/checkpoint", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("A página ainda parece estar em login/checkpoint ({Url}). Verifique se o login foi concluído.", page.Url);
        }
        else
        {
            logger.LogInformation("Sessão salva em {ProfileDir}.", Path.GetFullPath(_options.BrowserProfileDir));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
