import { CheckCircle2, CloudOff } from "lucide-react";
import { Link, useParams } from "react-router";
import { Avatar } from "@/components/Avatar";
import { EmptyState } from "@/components/EmptyState";
import { PageHeader } from "@/components/PageHeader";
import { Segments } from "@/components/Segments";
import { SplashScreen } from "@/components/SplashScreen";
import {
  EvidenciasAtividade,
  formatarDataRelativa,
  formatarDuracao,
  tipoAtividadeLabels,
  TipoAtividadeIcone,
  useAtividade,
  useFilaAvaliacao,
} from "@/features/atividades";
import { useProgresso } from "@/features/desafios";
import { useResumoGrupo } from "@/features/progresso";
import { ReacoesAtividade } from "@/features/reacoes";
import { getErrorMessage } from "@/services/api";

function SemanaDoAutor({
  desafioId,
  usuarioId,
  nome,
}: {
  desafioId: string;
  usuarioId: string;
  nome: string;
}) {
  const progresso = useProgresso(desafioId);
  const participante = progresso.data?.participantes.find(
    (p) => p.usuarioId === usuarioId,
  );
  if (!progresso.data || !participante) return null;
  const meta = progresso.data.sessoesPorSemana;

  return (
    <div className="card mt-4 flex items-center gap-3 p-4">
      <Avatar nome={nome} id={usuarioId} size={40} />
      <div className="min-w-0 flex-1">
        <p className="mb-1.5 text-sm">
          <b>{nome.trim().split(/\s+/)[0]}</b> está com{" "}
          {participante.sessoesVerificadas} de {meta} nesta semana
        </p>
        <Segments
          label={`Progresso de ${nome}`}
          valor={participante.sessoesVerificadas}
          provisorio={participante.sessoesProvisorias}
          max={meta}
        />
      </div>
    </div>
  );
}

export function RevisarPage() {
  const { id: grupoId = "", atividadeId = "" } = useParams();
  const atividade = useAtividade(atividadeId);
  const fila = useFilaAvaliacao(grupoId);
  const resumo = useResumoGrupo(grupoId);

  if (atividade.isPending) return <SplashScreen />;

  if (atividade.isError) {
    return (
      <div className="card">
        <EmptyState
          icon={CloudOff}
          title="Não foi possível carregar"
          description={getErrorMessage(atividade.error)}
          action={
            <Link to={`/grupos/${grupoId}/inicio`} className="btn-secondary">
              Voltar ao início
            </Link>
          }
        />
      </div>
    );
  }

  const {
    usuarioId,
    nomeExibicao,
    tipo,
    duracaoSegundos,
    startedAt,
    descricao,
    situacaoVerificacao,
    voce,
    evidencias,
  } = atividade.data;
  const naFila = fila.data ?? [];
  const avaliada = !naFila.some(({ id }) => id === atividadeId);
  const proxima = naFila.find(({ id }) => id !== atividadeId);

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader
        title="Avaliar atividade"
        back={`/grupos/${grupoId}/inicio`}
        subtitle={`${naFila.length} na fila`}
      />
      <p className="mb-4 text-sm text-muted">
        Confira o registro com calma. Na dúvida, prefira “Suspeito” e explique
        nos comentários em vez de rejeitar.
      </p>

      <div className="card overflow-hidden">
        <EvidenciasAtividade evidencias={evidencias} />
        <div className="p-5">
          <div className="flex items-center gap-3">
            <TipoAtividadeIcone tipo={tipo} className="size-11" />
            <div className="min-w-0 flex-1">
              <p className="font-semibold">
                {tipoAtividadeLabels[tipo]} · {formatarDuracao(duracaoSegundos)}
              </p>
              <p className="text-xs text-muted">
                {formatarDataRelativa(startedAt)}
              </p>
            </div>
          </div>
          {descricao && (
            <p className="mt-3 text-sm break-words">“{descricao}”</p>
          )}
        </div>
      </div>

      {resumo.data?.desafio && (
        <SemanaDoAutor
          desafioId={resumo.data.desafio.id}
          usuarioId={usuarioId}
          nome={nomeExibicao}
        />
      )}

      <div className="mt-5">
        <ReacoesAtividade
          atividadeId={atividadeId}
          grupoId={grupoId}
          situacaoVerificacao={situacaoVerificacao}
          voce={voce}
          nomeAutor={nomeExibicao}
        />
      </div>

      {fila.isSuccess &&
        avaliada &&
        (proxima ? (
          <Link
            to={`/grupos/${grupoId}/revisar/${proxima.id}`}
            className="btn-secondary mt-4 w-full"
          >
            Próxima avaliação
          </Link>
        ) : (
          <div className="card mt-5">
            <EmptyState
              icon={CheckCircle2}
              title="Nenhuma avaliação pendente"
              description="Você está em dia com o grupo."
              action={
                <Link to={`/grupos/${grupoId}/inicio`} className="btn-primary">
                  Voltar ao início
                </Link>
              }
            />
          </div>
        ))}
    </div>
  );
}
