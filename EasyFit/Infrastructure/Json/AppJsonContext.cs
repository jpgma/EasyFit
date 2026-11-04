using System.Text.Json;
using System.Text.Json.Serialization;
using EasyFit.Features.Autenticacao;
using EasyFit.Features.Convites;
using EasyFit.Features.Grupos;
using EasyFit.Features.Health;
using EasyFit.Infrastructure.Errors;

namespace EasyFit.Infrastructure.Json;

// Todo tipo serializado pela API precisa estar registrado aqui (exigência do Native AOT).
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AlterarPapelRequest))]
[JsonSerializable(typeof(ConviteConsultaResponse))]
[JsonSerializable(typeof(ConviteCriadoResponse))]
[JsonSerializable(typeof(ConviteTokenRequest))]
[JsonSerializable(typeof(ConvitesResponse))]
[JsonSerializable(typeof(CriarGrupoRequest))]
[JsonSerializable(typeof(EntrarGrupoResponse))]
[JsonSerializable(typeof(EntrarRequest))]
[JsonSerializable(typeof(GrupoResponse))]
[JsonSerializable(typeof(GruposResponse))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(MembrosResponse))]
[JsonSerializable(typeof(ProblemResponse))]
[JsonSerializable(typeof(RegistrarRequest))]
[JsonSerializable(typeof(SessaoResponse))]
[JsonSerializable(typeof(TransferirGrupoRequest))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
