# LinkedIn Job Watcher

Aplicação **.NET 8 (Worker Service)** que roda localmente no Windows, monitora o LinkedIn em
busca de vagas/posts compatíveis com um perfil de Desenvolvedor .NET Sênior e envia um e-mail
com as novidades de cada ciclo. A aplicação é **somente leitura**: nunca curte, comenta, segue
ou se candidata automaticamente a nada.

## ⚠️ Aviso importante

A automação de acesso ao LinkedIn (mesmo somente leitura) pode violar os **Termos de Uso do
LinkedIn**. Esta aplicação foi pensada para **uso pessoal, de baixo volume** (poucas buscas por
ciclo, pausas aleatórias, intervalos de ~20 minutos, janela de horário limitada) e nunca deve
ser usada para scraping em massa, revenda de dados ou qualquer ação automatizada que não seja
estritamente a leitura de posts/vagas já públicos para o próprio usuário. Use por sua conta e
risco, sabendo que a conta do LinkedIn pode ser limitada ou bloqueada pelo LinkedIn caso o uso
seja considerado abusivo.

## Arquitetura

```
LinkedInJobWatcher/
  src/
    LinkedInJobWatcher.Worker/         # Host, Program.cs (comandos: login | run | once | dry-run | test-email), appsettings.json
    LinkedInJobWatcher.Core/           # Modelos, interfaces, scoring determinístico, parser de datas relativas, extração de contatos
    LinkedInJobWatcher.Infrastructure/ # Scrapers Playwright/Firefox, seletores do LinkedIn, SQLite (EF Core), e-mail (MailKit)
  tests/
    LinkedInJobWatcher.Tests/          # xUnit + FluentAssertions, fixtures HTML em tests/Fixtures
```

Pontos de design relevantes:

- **Parsing separado do scraping**: o HTML de cada post/vaga é capturado pelo Playwright e
  interpretado por `PostHtmlParser`/`JobHtmlParser` (usando AngleSharp), independentes do
  navegador. Isso permite testar o parsing com arquivos HTML de exemplo, sem abrir o LinkedIn.
- **Seletores isolados**: toda classe/atributo CSS usado para encontrar elementos no LinkedIn
  está em [`LinkedInSelectors.cs`](src/LinkedInJobWatcher.Infrastructure/LinkedIn/LinkedInSelectors.cs).
  Quando o LinkedIn mudar o layout, é ali que se conserta.
- **Scoring determinístico**: `DeterministicJobClassifier` usa regras e pesos configuráveis via
  `appsettings.json` (seção `Filters`). A interface `IJobClassifier` permite no futuro plugar um
  classificador por LLM sem tocar no resto do pipeline (desligado por padrão).
- **Deduplicação**: por `Source+ExternalId`, por hash SHA-256 do texto normalizado (pega
  reposts/republicações) e por empresa+título normalizados nos últimos N dias.
- **Reenvio seguro**: um item só é marcado como "notificado" depois que o e-mail é enviado com
  sucesso. Se o envio falhar, o item continua pendente e é reenviado (sem re-raspar ou
  repontuar) no próximo ciclo.

## Pré-requisitos

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Uma conta de e-mail com SMTP habilitado (Gmail com senha de app, ou Outlook/Office 365)

## Instalação

```powershell
cd LinkedInJobWatcher
dotnet build

# Instala o Firefox usado pelo Playwright (feito uma vez, no projeto Worker)
cd src/LinkedInJobWatcher.Worker
pwsh bin/Debug/net8.0/playwright.ps1 install firefox
```

> Se `playwright.ps1` não existir ainda, rode `dotnet build` primeiro (o pacote `Microsoft.Playwright`
> gera esse script no diretório de saída do build).

## Primeiro login

A aplicação usa um **perfil de Firefox dedicado** (pasta `./browser-profile`, configurável em
`LinkedIn:BrowserProfileDir`), nunca o seu perfil pessoal do Firefox — assim ele não fica
bloqueado enquanto a aplicação está rodando.

```powershell
cd src/LinkedInJobWatcher.Worker
dotnet run -- login
```

Isso abre o Firefox **visível**. Faça login manualmente no LinkedIn (incluindo a verificação em
duas etapas, se for solicitada) e, quando estiver vendo seu feed, volte ao terminal e pressione
ENTER. A sessão fica salva no perfil dedicado e será reaproveitada pelos outros comandos.

A senha do LinkedIn **nunca** é armazenada pela aplicação — só os cookies de sessão do próprio
Firefox, na pasta do perfil dedicado.

Se a sessão expirar (LinkedIn redireciona para `/login` ou `/checkpoint`), a aplicação para a
busca, registra um log e envia **um** e-mail de alerta a cada 12h (configurável) pedindo um novo
login.

## Configuração do SMTP

As credenciais de e-mail **não devem** ir para `appsettings.json` em texto puro. Use User
Secrets (desenvolvimento) ou variáveis de ambiente (produção/serviço).

### User Secrets (recomendado para rodar localmente)

```powershell
cd src/LinkedInJobWatcher.Worker
dotnet user-secrets init
dotnet user-secrets set "Email:SmtpUsername" "seuemail@outlook.com"
dotnet user-secrets set "Email:SmtpPassword" "sua-senha-de-app-ou-senha"
dotnet user-secrets set "Email:FromAddress" "seuemail@outlook.com"
dotnet user-secrets set "Email:ToAddress" "seuemail@outlook.com"
```

### Variáveis de ambiente (alternativa, útil para Windows Service/Agendador)

```powershell
setx LinkedInJobWatcher_Email__SmtpUsername "seuemail@outlook.com"
setx LinkedInJobWatcher_Email__SmtpPassword "sua-senha-de-app-ou-senha"
```

(O separador `__` mapeia para `Email:SmtpUsername` etc. na configuração do .NET.)

### Gmail

1. Ative a verificação em duas etapas na conta Google.
2. Gere uma **senha de app** em https://myaccount.google.com/apppasswords.
3. Configure:
   - `Email:SmtpHost` = `smtp.gmail.com`
   - `Email:SmtpPort` = `587`
   - `Email:UseSsl` = `true` (STARTTLS)
   - `Email:SmtpUsername` = seu e-mail do Gmail
   - `Email:SmtpPassword` = a senha de app gerada (não é a senha normal da conta)

### Outlook / Office 365

- `Email:SmtpHost` = `smtp.office365.com`
- `Email:SmtpPort` = `587`
- `Email:UseSsl` = `true` (STARTTLS)
- `Email:SmtpUsername`/`Email:SmtpPassword` = suas credenciais (uma senha de app pode ser
  necessária se a conta tiver MFA)

Teste a configuração com:

```powershell
dotnet run -- test-email
```

## Como rodar

Comandos disponíveis (executar dentro de `src/LinkedInJobWatcher.Worker`):

| Comando | O que faz |
|---|---|
| `dotnet run -- login` | Abre o Firefox visível para login manual (uma vez). |
| `dotnet run -- run` | Execução contínua (padrão), respeitando janela de horário, intervalo e jitter. |
| `dotnet run -- once` | Executa um único ciclo completo (com e-mail) e encerra. Útil para testes. |
| `dotnet run -- dry-run` | Executa um ciclo e **imprime no console** o que seria enviado, sem mandar e-mail e sem gravar nada no banco. |
| `dotnet run -- test-email` | Envia um e-mail de teste para validar a configuração de SMTP. |

Sem argumentos, o comportamento padrão é `run`.

## Ajustando a configuração

Edite `src/LinkedInJobWatcher.Worker/appsettings.json`:

- **`LinkedIn`**: perfil do navegador, modo headless, termos de busca de posts/vagas, máximo de
  itens por busca, filtros de data/remoto/experiência das vagas.
- **`Filters`**: idade máxima (dias), pontuação mínima para notificar, pesos de cada critério e
  as listas de palavras-chave usadas no matching (tudo em texto, pode editar livremente).
- **`Schedule`**: intervalo entre ciclos, variação aleatória (jitter), janela de horário e dias
  ativos, número máximo de buscas por ciclo.
- **`Email`**: host/porta SMTP, remetente, destinatário (as credenciais ficam em User Secrets).
- **`Storage`**: caminho do banco SQLite e retenção de registros antigos.

## Instalando como serviço

### Opção A — Agendador de Tarefas do Windows (mais simples)

1. Publique a aplicação: `dotnet publish -c Release -o publish` (dentro de
   `src/LinkedInJobWatcher.Worker`).
2. Abra o **Agendador de Tarefas** → Criar Tarefa:
   - Disparador: "Ao fazer logon" (ou na inicialização do sistema).
   - Ação: iniciar `publish\LinkedInJobWatcher.Worker.exe` com o argumento `run`.
   - Em "Configurações", marque "Executar tarefa o mais rápido possível após um início agendado
     perdido" e, se quiser, "Reiniciar a cada X minutos em caso de falha".

### Opção B — Windows Service

1. Adicione o pacote `Microsoft.Extensions.Hosting.WindowsServices` ao projeto Worker e, em
   `Program.cs`, adicione `builder.Services.AddWindowsService();` antes de `builder.Build()`
   (necessário apenas para o comando `run` operando como serviço do Windows; os demais comandos
   continuam rodando como console comum).
2. Publique: `dotnet publish -c Release -o publish`.
3. Registre o serviço (PowerShell como Administrador):
   ```powershell
   sc.exe create LinkedInJobWatcher binPath= "C:\caminho\publish\LinkedInJobWatcher.Worker.exe run" start= auto
   sc.exe start LinkedInJobWatcher
   ```
4. **Importante**: como o Playwright precisa de um perfil de navegador e, no modo headless, não
   precisa de uma sessão interativa, rodar como serviço funciona bem — mas o primeiro
   `dotnet run -- login` precisa ser feito manualmente (interativo) antes de registrar o serviço,
   com o mesmo usuário/pasta de perfil que o serviço vai usar.

## Ajustando os seletores quando o LinkedIn mudar o layout

O LinkedIn muda a estrutura do HTML com frequência. Se a aplicação parar de encontrar posts ou
vagas (logs mostrando 0 itens capturados), siga esta ordem:

1. Rode `dotnet run -- dry-run` com `LinkedIn:Headless` = `false` temporariamente, para ver o
   Firefox navegando e observar visualmente a página de resultados.
2. Abra o DevTools do Firefox (F12) na página de busca e inspecione os elementos reais:
   cartão de post/vaga, nome do autor, data relativa, texto, etc.
   Dê preferência a atributos estáveis (`data-urn`, `data-occludable-job-id`, `aria-label`,
   `role`) em vez de classes CSS geradas (essas mudam a cada deploy do LinkedIn).
3. Atualize as constantes correspondentes em
   [`LinkedInSelectors.cs`](src/LinkedInJobWatcher.Infrastructure/LinkedIn/LinkedInSelectors.cs).
4. Rode os testes de `PostHtmlParserTests`/`JobHtmlParserTests` com as fixtures atualizadas em
   `tests/LinkedInJobWatcher.Tests/Fixtures/` para confirmar que o parsing continua funcionando
   antes de rodar contra o LinkedIn de verdade.

## Testes

```powershell
dotnet test
```

Cobertura inclui: parser de datas relativas (PT/EN, incluindo o limite de 48h), scoring
(aprovação/reprovação por nível, presencial, stack), extração de e-mail/links/nome de contato
(incluindo ofuscações), deduplicação (por id, hash de conteúdo e empresa+título) e geração do
HTML/texto do e-mail.
