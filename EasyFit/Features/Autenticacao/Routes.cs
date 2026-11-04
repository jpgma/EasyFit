using System.Threading.RateLimiting;
using EasyFit.Features.Convites.Contracts;
using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Autenticacao;

internal static class AutenticacaoRoutes
{
    private const string TentativasPolicy = "autenticacao-tentativas";
    private const string LoggerCategory = "EasyFit.Autenticacao";

    public static void AddAutenticacaoRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.OnRejected = (rejected, _) => new ValueTask(ApiProblem.WriteAsync(
                rejected.HttpContext,
                StatusCodes.Status429TooManyRequests,
                "MUITAS_TENTATIVAS",
                "Muitas tentativas. Aguarde um minuto e tente novamente."));

            options.AddPolicy(TentativasPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                }));
        });

    public static void MapAutenticacaoRoutes(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/autenticacao");

        group.MapPost("/registrar", RegistrarAsync).RequireRateLimiting(TentativasPolicy);
        group.MapPost("/entrar", EntrarAsync).RequireRateLimiting(TentativasPolicy);
        group.MapPost("/sair", SairAsync);
        group.MapGet("/sessao", GetSessaoAsync).RequireSessao();
    }

    private static async Task<IResult> RegistrarAsync(
        RegistrarRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // Texto em branco conta como ausência de convite; o convite recusado é decidido na transação.
        var conviteToken = string.IsNullOrWhiteSpace(request.ConviteToken) ? null : request.ConviteToken.Trim();

        // O convite é o caminho normal; o cadastro aberto existe para criar a primeira conta.
        if (conviteToken is null && !IsCadastroAberto(configuration))
        {
            return ApiProblem.Create(
                StatusCodes.Status403Forbidden,
                "CADASTRO_FECHADO",
                "Para criar uma conta, você precisa de um convite.");
        }

        if (!AutenticacaoRules.IsValidNomeExibicao(request.NomeExibicao)
            || !AutenticacaoRules.IsValidEmail(request.Email)
            || !AutenticacaoRules.IsValidSenha(request.Senha))
        {
            return InvalidData();
        }

        var result = await AutenticacaoCommands.RegistrarAsync(
            dataSource,
            AutenticacaoRules.NormalizeNomeExibicao(request.NomeExibicao!),
            AutenticacaoRules.NormalizeEmail(request.Email!),
            Passwords.Hash(request.Senha!),
            conviteToken,
            cancellationToken);

        switch (result.Status)
        {
            case RegistroStatus.EmailJaCadastrado:
                return ApiProblem.Create(
                    StatusCodes.Status409Conflict,
                    "EMAIL_JA_CADASTRADO",
                    "Já existe uma conta com este e-mail.");
            case RegistroStatus.ConviteInvalido:
                return ConviteAceite.ConviteInvalido();
            case RegistroStatus.GrupoCheio:
                return GrupoAcesso.GrupoCheio();
        }

        SessaoCookie.Write(context, result.Token!);
        loggerFactory.CreateLogger(LoggerCategory).LogInformation(
            "Conta criada para o usuário {UsuarioId}.", result.Usuario!.Id);

        return Results.Json(
            new SessaoResponse(result.Usuario),
            AppJsonContext.Default.SessaoResponse,
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> EntrarAsync(
        EntrarRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Senha))
        {
            return InvalidData();
        }

        var logger = loggerFactory.CreateLogger(LoggerCategory);

        // Uma senha acima do limite nunca foi aceita no cadastro: não há por que gastar CPU com ela.
        if (request.Senha.Length > AutenticacaoRules.MaxSenhaLength)
        {
            return InvalidCredentials(logger);
        }

        var credenciais = await AutenticacaoQueries.GetCredenciaisByEmailAsync(
            dataSource,
            AutenticacaoRules.NormalizeEmail(request.Email),
            cancellationToken);

        if (credenciais is null)
        {
            Passwords.VerifyDummy(request.Senha);
            return InvalidCredentials(logger);
        }

        if (!Passwords.Verify(request.Senha, credenciais.PasswordHash))
        {
            return InvalidCredentials(logger);
        }

        var token = await AutenticacaoCommands.CreateSessaoAsync(dataSource, credenciais.Id, cancellationToken);
        SessaoCookie.Write(context, token);
        logger.LogInformation("Usuário {UsuarioId} entrou.", credenciais.Id);

        return Results.Ok(new SessaoResponse(
            new UsuarioResponse(credenciais.Id, credenciais.NomeExibicao, credenciais.Email)));
    }

    private static async Task<IResult> SairAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var token = SessaoCookie.Read(context);
        if (!string.IsNullOrEmpty(token))
        {
            await AutenticacaoCommands.RevokeSessaoAsync(dataSource, token, cancellationToken);
        }

        SessaoCookie.Delete(context);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSessaoAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var usuario = await AutenticacaoQueries.GetUsuarioAsync(
            dataSource,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        // A chave estrangeira apaga as sessões junto com o usuário; só uma corrida chega aqui.
        return usuario is null
            ? SessaoFilter.NotAuthenticated()
            : Results.Ok(new SessaoResponse(usuario));
    }

    private static bool IsCadastroAberto(IConfiguration configuration) =>
        bool.TryParse(configuration["Autenticacao:CadastroAberto"], out var aberto) && aberto;

    private static IResult InvalidData() => ApiProblem.Create(
        StatusCodes.Status400BadRequest,
        "DADOS_INVALIDOS",
        "Verifique os dados informados.");

    private static IResult InvalidCredentials(ILogger logger)
    {
        logger.LogInformation("Tentativa de entrada recusada.");
        return ApiProblem.Create(
            StatusCodes.Status401Unauthorized,
            "CREDENCIAIS_INVALIDAS",
            "E-mail ou senha incorretos.");
    }
}
