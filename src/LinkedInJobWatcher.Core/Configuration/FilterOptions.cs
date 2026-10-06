namespace LinkedInJobWatcher.Core.Configuration;

public sealed class FilterOptions
{
    public const string SectionName = "Filters";

    public int MaxAgeDays { get; set; } = 2;

    public int MinScore { get; set; } = 60;

    public ScoringWeights Weights { get; set; } = new();

    public List<string> DotNetKeywords { get; set; } =
    [
        ".net", "dotnet", "asp.net", "c#", "csharp", ".net core", ".net framework",
        ".net 6", ".net 7", ".net 8", ".net 9"
    ];

    public List<string> JobIndicatorKeywords { get; set; } =
    [
        "vaga", "vagas", "oportunidade", "oportunidades", "contratando", "hiring", "we're hiring",
        "were hiring", "procuramos", "buscamos", "envie seu currículo", "envie seu curriculo",
        "job", "position", "estamos contratando", "recrutamento"
    ];

    public List<string> RemoteKeywords { get; set; } =
    [
        "remoto", "remota", "home office", "100% remoto", "full remote", "remote"
    ];

    public List<string> SeniorKeywords { get; set; } =
    [
        "senior", "sênior", "sr.", "sr ", "especialista", "lead técnico", "lead tecnico", "tech lead"
    ];

    public List<string> StackKeywords { get; set; } =
    [
        "sql server", "mongodb", "postgresql", "postgres", "azure", "aws", "microsserviços",
        "microservicos", "microservices", "service bus", "sqs", "mensageria", "entity framework",
        "ef core", "dapper"
    ];

    public List<string> FinanceKeywords { get; set; } =
    [
        "banco", "bancos", "crédito", "credito", "pagamentos", "pagamento", "pix", "fintech", "seguros", "seguradora"
    ];

    public List<string> JuniorLevelKeywords { get; set; } =
    [
        "júnior", "junior", "pleno", "estágio", "estagio", "trainee"
    ];

    public List<string> NonDotNetFocusKeywords { get; set; } =
    [
        "front-end", "frontend", "front end", "mobile", "android", "ios", "java ", "python", "php"
    ];

    public List<string> OnSiteKeywords { get; set; } =
    [
        "presencial obrigatório", "presencial obrigatorio", "100% presencial", "somente presencial"
    ];

    public List<string> HybridKeywords { get; set; } =
    [
        "híbrido", "hibrido", "hybrid"
    ];

    public List<string> RioDeJaneiroKeywords { get; set; } =
    [
        "rio de janeiro", " rj", "rj,", "rj.", "são gonçalo", "sao goncalo", "niterói", "niteroi"
    ];
}
