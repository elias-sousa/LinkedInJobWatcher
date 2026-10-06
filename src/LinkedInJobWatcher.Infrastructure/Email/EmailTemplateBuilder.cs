using System.Net;
using System.Text;
using LinkedInJobWatcher.Core.Models;
using LinkedInJobWatcher.Core.Text;

namespace LinkedInJobWatcher.Infrastructure.Email;

/// <summary>Builds the HTML + plain-text bodies for every outgoing e-mail. No MailKit dependency, so it is unit-testable on its own.</summary>
public static class EmailTemplateBuilder
{
    private const string CandidateName = "Elias Rodrigues de Sousa";
    private const int DescriptionExcerptLength = 400;

    public static (string Subject, string Html, string PlainText) BuildNewItemsEmail(IReadOnlyList<ScoredItem> items)
    {
        var ordered = items.OrderByDescending(i => i.Score)
            .ThenByDescending(i => i.Item.PublishedAt)
            .ToList();

        var best = ordered.First().Score;
        var subject = $"[Vagas .NET] {ordered.Count} {(ordered.Count == 1 ? "nova vaga" : "novas vagas")} – melhor match: {best} pts";

        var html = new StringBuilder();
        html.Append("""<!DOCTYPE html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"></head>""");
        html.Append("""<body style="margin:0;padding:0;background-color:#f4f5f7;font-family:Segoe UI,Arial,sans-serif;color:#1d2329;">""");
        html.Append("""<div style="max-width:640px;margin:0 auto;padding:24px 16px;">""");
        html.Append($"""<h2 style="margin:0 0 16px;">{ordered.Count} {(ordered.Count == 1 ? "nova vaga compatível" : "novas vagas compatíveis")} encontradas</h2>""");

        foreach (var scored in ordered)
        {
            html.Append(BuildCardHtml(scored));
        }

        html.Append("""<p style="font-size:12px;color:#6b7280;margin-top:24px;">Monitoramento automático e somente leitura do LinkedIn. Verifique sempre a vaga diretamente antes de se candidatar.</p>""");
        html.Append("</div></body></html>");

        var text = new StringBuilder();
        text.AppendLine($"{ordered.Count} {(ordered.Count == 1 ? "nova vaga compatível" : "novas vagas compatíveis")} encontradas");
        text.AppendLine();
        foreach (var scored in ordered)
        {
            text.AppendLine(BuildCardPlainText(scored));
            text.AppendLine(new string('-', 40));
        }

        return (subject, html.ToString(), text.ToString());
    }

    public static (string Subject, string Html, string PlainText) BuildSessionExpiredEmail()
    {
        const string subject = "[LinkedIn Job Watcher] Sessão expirada – login necessário";
        var html = WrapSimpleMessage(
            "Sessão do LinkedIn expirada",
            "A sessão salva no perfil dedicado do Firefox expirou (redirecionamento para login/checkpoint). " +
            "A busca por vagas foi pausada até que você faça login novamente.<br><br>" +
            "No computador onde a aplicação roda, execute:<br><code>dotnet run -- login</code>");
        var text = "Sessão do LinkedIn expirada. A busca foi pausada. Execute 'dotnet run -- login' para autenticar novamente.";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string PlainText) BuildBlockedEmail(string reason, TimeSpan backoffDuration)
    {
        const string subject = "[LinkedIn Job Watcher] Possível bloqueio detectado";
        var html = WrapSimpleMessage(
            "Possível bloqueio/CAPTCHA detectado",
            $"Motivo: {WebUtility.HtmlEncode(reason)}<br><br>" +
            $"A aplicação vai aguardar aproximadamente {FormatTimeSpan(backoffDuration)} antes de tentar novamente (backoff exponencial).");
        var text = $"Possível bloqueio detectado ({reason}). Próxima tentativa em aproximadamente {FormatTimeSpan(backoffDuration)}.";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string PlainText) BuildTestEmail()
    {
        const string subject = "[LinkedIn Job Watcher] E-mail de teste";
        var html = WrapSimpleMessage("E-mail de teste", "Se você está lendo isto, a configuração de SMTP está funcionando corretamente.");
        var text = "E-mail de teste - a configuração de SMTP está funcionando corretamente.";
        return (subject, html, text);
    }

    private static string BuildCardHtml(ScoredItem scored)
    {
        var item = scored.Item;
        var badges = BuildBadges(scored);
        var badgesHtml = string.Join(" ", badges.Select(b =>
            $"""<span style="display:inline-block;background:#eef2ff;color:#3730a3;border-radius:12px;padding:2px 10px;font-size:12px;margin:0 4px 4px 0;">{WebUtility.HtmlEncode(b)}</span>"""));

        var age = item.PublishedAt.HasValue ? FormatAge(DateTimeOffset.UtcNow - item.PublishedAt.Value) : item.RelativeDateText;
        var titleLine = item.Source == SourceType.Job
            ? $"{WebUtility.HtmlEncode(item.Title ?? "(sem título)")} — {WebUtility.HtmlEncode(item.Company ?? "empresa não identificada")}"
            : $"Post de {WebUtility.HtmlEncode(item.AuthorName ?? "autor desconhecido")}";

        var excerpt = Truncate(item.FullText, DescriptionExcerptLength);
        var reasonsHtml = string.Join("", scored.Reasons.Select(r => $"<li>{WebUtility.HtmlEncode(r)}</li>"));

        var mailto = BuildMailtoLink(scored);
        var mailtoButtonHtml = mailto is null
            ? string.Empty
            : $"""<a href="{mailto}" style="display:inline-block;background:#111827;color:#ffffff;text-decoration:none;padding:8px 14px;border-radius:6px;font-size:13px;margin-right:8px;">Responder por e-mail</a>""";

        var contactLineHtml = BuildContactLineHtml(scored.Contact);

        return $"""
            <div style="border:1px solid #e5e7eb;border-radius:10px;padding:16px;margin-bottom:16px;background:#ffffff;">
              <div style="font-size:12px;color:#6b7280;margin-bottom:6px;">Pontuação: <strong style="color:#111827;">{scored.Score} pts</strong> · publicado há {WebUtility.HtmlEncode(age)}</div>
              <div style="font-size:16px;font-weight:600;margin-bottom:8px;">{titleLine}</div>
              <div style="margin-bottom:10px;">{badgesHtml}</div>
              <div style="font-size:13px;color:#374151;margin-bottom:10px;">{WebUtility.HtmlEncode(excerpt)}</div>
              {contactLineHtml}
              <div style="margin:12px 0;">
                <a href="{item.Url}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;padding:8px 14px;border-radius:6px;font-size:13px;margin-right:8px;">Abrir {(item.Source == SourceType.Job ? "vaga" : "post")}</a>
                {mailtoButtonHtml}
              </div>
              <details style="font-size:12px;color:#6b7280;">
                <summary>Motivos da pontuação</summary>
                <ul>{reasonsHtml}</ul>
              </details>
            </div>
            """;
    }

    private static string BuildContactLineHtml(ContactInfo? contact)
    {
        if (contact is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(contact.ContactName))
        {
            parts.Add($"Contato: {WebUtility.HtmlEncode(contact.ContactName)}");
        }

        if (!string.IsNullOrWhiteSpace(contact.Email))
        {
            parts.Add($"E-mail: {WebUtility.HtmlEncode(contact.Email)}");
        }

        if (!string.IsNullOrWhiteSpace(contact.ApplicationUrl))
        {
            parts.Add($"""Inscrição ({WebUtility.HtmlEncode(contact.ApplicationPlatform ?? "link")}): <a href="{contact.ApplicationUrl}">{WebUtility.HtmlEncode(contact.ApplicationUrl)}</a>""");
        }

        if (!string.IsNullOrWhiteSpace(contact.SalaryRange))
        {
            parts.Add($"Faixa salarial: {WebUtility.HtmlEncode(contact.SalaryRange)}");
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return $"""<div style="font-size:13px;color:#111827;margin-bottom:6px;">{string.Join("<br>", parts)}</div>""";
    }

    private static string BuildCardPlainText(ScoredItem scored)
    {
        var item = scored.Item;
        var builder = new StringBuilder();
        var title = item.Source == SourceType.Job
            ? $"{item.Title ?? "(sem título)"} - {item.Company ?? "empresa não identificada"}"
            : $"Post de {item.AuthorName ?? "autor desconhecido"}";

        builder.AppendLine(title);
        builder.AppendLine($"Pontuação: {scored.Score} pts");
        builder.AppendLine($"Link: {item.Url}");

        if (scored.Contact is { } contact)
        {
            if (!string.IsNullOrWhiteSpace(contact.ContactName))
            {
                builder.AppendLine($"Contato: {contact.ContactName}");
            }

            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                builder.AppendLine($"E-mail: {contact.Email}");
            }

            if (!string.IsNullOrWhiteSpace(contact.ApplicationUrl))
            {
                builder.AppendLine($"Inscrição: {contact.ApplicationUrl}");
            }
        }

        builder.AppendLine(Truncate(item.FullText, DescriptionExcerptLength));
        return builder.ToString();
    }

    private static List<string> BuildBadges(ScoredItem scored)
    {
        var normalized = TextNormalizer.Normalize(scored.Item.FullText);
        var badges = new List<string>();

        if (normalized.Contains("remoto") || normalized.Contains("remote") || normalized.Contains("home office"))
        {
            badges.Add("Remoto");
        }

        if (normalized.Contains("senior") || normalized.Contains("sênior"))
        {
            badges.Add("Sênior");
        }

        if (normalized.Contains(".net core"))
        {
            badges.Add(".NET Core");
        }

        if (normalized.Contains(".net framework"))
        {
            badges.Add(".NET Framework");
        }

        if (scored.Item is JobItem { SimplifiedApplication: true })
        {
            badges.Add("Candidatura simplificada");
        }

        switch (scored.Contact?.EmploymentType)
        {
            case EmploymentType.Clt:
                badges.Add("CLT");
                break;
            case EmploymentType.Pj:
                badges.Add("PJ");
                break;
            case EmploymentType.Cooperado:
                badges.Add("Cooperado");
                break;
        }

        return badges;
    }

    private static string? BuildMailtoLink(ScoredItem scored)
    {
        var email = scored.Contact?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var subject = scored.Contact?.SubjectHint ?? $"Candidatura – Desenvolvedor .NET Sênior – {CandidateName}";
        var greeting = string.IsNullOrWhiteSpace(scored.Contact?.ContactName) ? "Olá" : $"Olá, {scored.Contact!.ContactName}";
        var jobTitle = scored.Item.Title ?? "a vaga divulgada";

        var body = $"{greeting},\n\n" +
                   $"Meu nome é {CandidateName}, sou Desenvolvedor .NET Sênior com mais de 10 anos de experiência em backend " +
                   "(C#, .NET, ASP.NET Core, SQL Server, Azure/AWS). Vi a publicação sobre " + jobTitle +
                   " e gostaria de me candidatar.\n\nSegue meu contato para mais detalhes.\n\nAtenciosamente,\n" + CandidateName;

        return $"mailto:{Uri.EscapeDataString(email)}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
    }

    private static string WrapSimpleMessage(string title, string bodyHtml)
    {
        return $"""
            <!DOCTYPE html><html lang="pt-BR"><head><meta charset="utf-8"></head>
            <body style="font-family:Segoe UI,Arial,sans-serif;color:#1d2329;background:#f4f5f7;padding:24px;">
              <div style="max-width:560px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:10px;padding:20px;">
                <h2 style="margin-top:0;">{WebUtility.HtmlEncode(title)}</h2>
                <p>{bodyHtml}</p>
              </div>
            </body></html>
            """;
    }

    private static string Truncate(string text, int maxLength)
    {
        var singleLine = string.Join(' ', text.Split(default(char[]?), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return singleLine.Length <= maxLength ? singleLine : singleLine[..maxLength].TrimEnd() + "…";
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalHours < 1)
        {
            return $"{Math.Max(1, (int)age.TotalMinutes)} min";
        }

        return age.TotalDays < 1 ? $"{(int)age.TotalHours} h" : $"{(int)age.TotalDays} d";
    }

    private static string FormatTimeSpan(TimeSpan span)
        => span.TotalHours >= 1 ? $"{span.TotalHours:0.#} horas" : $"{span.TotalMinutes:0} minutos";
}
