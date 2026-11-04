using EasyFit.Infrastructure.Json;

namespace EasyFit.Infrastructure.Errors;

internal sealed record ProblemResponse(string Title, int Status, string Code);

/// <summary>Formato único de erro da API: application/problem+json com um código estável.</summary>
internal static class ApiProblem
{
    private const string ContentType = "application/problem+json";

    public static IResult Create(int status, string code, string title) =>
        Results.Json(
            new ProblemResponse(title, status, code),
            AppJsonContext.Default.ProblemResponse,
            ContentType,
            status);

    /// <summary>Para middlewares, que escrevem na resposta sem passar por um endpoint.</summary>
    public static Task WriteAsync(HttpContext context, int status, string code, string title)
    {
        context.Response.StatusCode = status;

        return context.Response.WriteAsJsonAsync(
            new ProblemResponse(title, status, code),
            AppJsonContext.Default.ProblemResponse,
            ContentType,
            context.RequestAborted);
    }

    // A exceção é registrada pelo middleware; o cliente nunca recebe seus detalhes.
    public static Task WriteInternalErrorAsync(HttpContext context) =>
        WriteAsync(context, StatusCodes.Status500InternalServerError, "ERRO_INTERNO", "Erro interno do servidor.");
}
