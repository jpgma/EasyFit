using System.Text.Json;
using System.Text.Json.Serialization;
using EasyFit.Features.Atividades;
using EasyFit.Features.Autenticacao;
using EasyFit.Features.Convites;
using EasyFit.Features.Desafios;
using EasyFit.Features.Grupos;
using EasyFit.Features.Health;
using EasyFit.Features.Progresso;
using EasyFit.Features.Reacoes;
using EasyFit.Infrastructure.Errors;

namespace EasyFit.Infrastructure.Json;

// Todo tipo serializado pela API precisa estar registrado aqui (exigência do Native AOT).
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AlterarPapelRequest))]
[JsonSerializable(typeof(AtividadeResponse))]
[JsonSerializable(typeof(AtividadesPaginadasResponse))]
[JsonSerializable(typeof(CriarAtividadeRequest))]
[JsonSerializable(typeof(EvidenciaCriadaResponse))]
[JsonSerializable(typeof(ComentarioRequest))]
[JsonSerializable(typeof(ComentarioResponse))]
[JsonSerializable(typeof(ComentariosResponse))]
[JsonSerializable(typeof(ConviteConsultaResponse))]
[JsonSerializable(typeof(ConviteAtualResponse))]
[JsonSerializable(typeof(ConviteResponse))]
[JsonSerializable(typeof(ConviteTokenRequest))]
[JsonSerializable(typeof(CriarDesafioRequest))]
[JsonSerializable(typeof(CriarGrupoRequest))]
[JsonSerializable(typeof(DesafioResponse))]
[JsonSerializable(typeof(DesafiosResponse))]
[JsonSerializable(typeof(EditarDesafioRequest))]
[JsonSerializable(typeof(EntrarGrupoResponse))]
[JsonSerializable(typeof(FilaAvaliacaoResponse))]
[JsonSerializable(typeof(EntrarRequest))]
[JsonSerializable(typeof(GrupoResponse))]
[JsonSerializable(typeof(GruposResponse))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(MembrosResponse))]
[JsonSerializable(typeof(ProblemResponse))]
[JsonSerializable(typeof(ClassificacaoParticipanteResponse))]
[JsonSerializable(typeof(ClassificacaoResponse))]
[JsonSerializable(typeof(ClassificacaoResumoItemResponse))]
[JsonSerializable(typeof(DesafioDestaqueResponse))]
[JsonSerializable(typeof(HistoricoSemanaResponse))]
[JsonSerializable(typeof(HistoricoSemanalResponse))]
[JsonSerializable(typeof(ProgressoResumoResponse))]
[JsonSerializable(typeof(ProgressoResponse))]
[JsonSerializable(typeof(ResumoGrupoResponse))]
[JsonSerializable(typeof(ReacaoRequest))]
[JsonSerializable(typeof(ReacaoItemResponse))]
[JsonSerializable(typeof(ReacoesResponse))]
[JsonSerializable(typeof(RevisaoRegistradaResponse))]
[JsonSerializable(typeof(RevisaoRequest))]
[JsonSerializable(typeof(RegistrarRequest))]
[JsonSerializable(typeof(SessaoResponse))]
[JsonSerializable(typeof(TransferirGrupoRequest))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
