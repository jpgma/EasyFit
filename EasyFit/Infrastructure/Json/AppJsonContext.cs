using System.Text.Json;
using System.Text.Json.Serialization;
using EasyFit.Features.Autenticacao;
using EasyFit.Features.Health;
using EasyFit.Infrastructure.Errors;

namespace EasyFit.Infrastructure.Json;

// Todo tipo serializado pela API precisa estar registrado aqui (exigência do Native AOT).
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(EntrarRequest))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ProblemResponse))]
[JsonSerializable(typeof(RegistrarRequest))]
[JsonSerializable(typeof(SessaoResponse))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
