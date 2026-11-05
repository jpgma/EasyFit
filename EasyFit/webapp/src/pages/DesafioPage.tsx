import {
  ArrowLeft,
  Calendar,
  Check,
  Clock,
  CloudOff,
  MapPin,
  Medal,
  Target,
  Users,
  type LucideIcon,
} from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router";
import { Avatar } from "@/components/Avatar";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { EmptyState } from "@/components/EmptyState";
import { ProgressBar } from "@/components/ProgressBar";
import { SplashScreen } from "@/components/SplashScreen";
import { useSessao } from "@/features/autenticacao";
import {
  canGerenciarDesafios,
  formatarData,
  formatarDiaEMes,
  formatarDuracao,
  isDesafioAberto,
  situacaoLabels,
  useCancelarDesafio,
  useDesafio,
  useParticipar,
  useProgresso,
  type Desafio,
  type ProgressoParticipante,
} from "@/features/desafios";
import {
  useClassificacao,
  useHistoricoSemanal,
  type ClassificacaoParticipante,
} from "@/features/progresso";
import { ApiError, getErrorMessage } from "@/services/api";

function Dado({
  icon: Icon,
  titulo,
  valor,
}: {
  icon: LucideIcon;
  titulo: string;
  valor: string;
}) {
  return (
    <div className="rounded-xl bg-surface-muted p-3.5">
      <div className="mb-1 flex items-center gap-2 text-muted">
        <Icon aria-hidden="true" className="h-4 w-4 shrink-0" />
        <span className="text-2xs font-semibold tracking-wide uppercase">
          {titulo}
        </span>
      </div>
      <p className="font-bold">{valor}</p>
    </div>
  );
}

function ParticipanteItem({
  participante,
  meta,
  voce,
}: {
  participante: ProgressoParticipante;
  meta: number;
  voce: boolean;
}) {
  const { nomeExibicao, sessoesVerificadas, sessoesProvisorias, metaCumprida } =
    participante;
  const pendentes = sessoesProvisorias - sessoesVerificadas;

  return (
    <li className="flex items-center gap-3">
      <Avatar nome={nomeExibicao} />
      <div className="min-w-0 flex-1">
        <div className="mb-1 flex items-center justify-between gap-2">
          <p className="min-w-0 truncate text-sm font-semibold">
            {nomeExibicao}
            {voce && <span className="font-normal text-muted"> (você)</span>}
          </p>
          <span className="flex shrink-0 items-center gap-1.5 text-sm font-bold">
            {metaCumprida && (
              <Check aria-label="Meta cumprida" className="h-4 w-4" />
            )}
            {sessoesVerificadas}/{meta}
          </span>
        </div>
        <ProgressBar
          label={`Progresso de ${nomeExibicao}`}
          valor={sessoesVerificadas}
          provisorio={sessoesProvisorias}
          max={meta}
        />
        {pendentes > 0 && (
          <p className="mt-1 text-2xs text-muted">
            +{pendentes} aguardando verificação
          </p>
        )}
      </div>
    </li>
  );
}

function ProgressoSection({ desafio }: { desafio: Desafio }) {
  const { data: usuario } = useSessao();
  const progresso = useProgresso(desafio.id);

  return (
    <section className="mt-6">
      <h2 className="mb-1 text-base font-bold">Progresso da semana</h2>

      {progresso.isPending && (
        <p role="status" className="py-8 text-center text-sm text-muted">
          Carregando…
        </p>
      )}

      {progresso.isError && (
        <p role="alert" className="form-error mt-2">
          {getErrorMessage(progresso.error)}
        </p>
      )}

      {progresso.data && (
        <>
          <p className="mb-3 text-sm text-muted">
            {progresso.data.semanaAtual
              ? `Semana ${progresso.data.semanaAtual.numero} de ${progresso.data.totalSemanas}, de ${formatarDiaEMes(progresso.data.semanaAtual.inicio)} a ${formatarDiaEMes(progresso.data.semanaAtual.fim)}`
              : `${progresso.data.totalSemanas} ${progresso.data.totalSemanas === 1 ? "semana" : "semanas"} no total`}
          </p>

          {progresso.data.participantes.length === 0 ? (
            <div className="card">
              <EmptyState
                icon={Users}
                title="Ninguém participando"
                description="Quem participar do desafio aparecerá aqui."
              />
            </div>
          ) : (
            <ul className="card space-y-4 p-4">
              {progresso.data.participantes.map((participante) => (
                <ParticipanteItem
                  key={participante.usuarioId}
                  participante={participante}
                  meta={progresso.data.sessoesPorSemana}
                  voce={participante.usuarioId === usuario?.id}
                />
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  );
}

function ClassificacaoSection({ desafioId }: { desafioId: string }) {
  const { data: usuario } = useSessao();
  const classificacao = useClassificacao(desafioId);

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-base font-bold">Classificação</h2>

      {classificacao.isPending && (
        <p role="status" className="py-6 text-center text-sm text-muted">
          Carregando…
        </p>
      )}

      {classificacao.isError && (
        <p role="alert" className="form-error">
          {getErrorMessage(classificacao.error)}
        </p>
      )}

      {classificacao.data &&
        (classificacao.data.participantes.length === 0 ? (
          <p className="card p-4 text-sm text-muted">
            Ainda não há participantes neste desafio.
          </p>
        ) : (
          <ol className="card divide-y divide-border">
            {classificacao.data.participantes.map((item) => (
              <ClassificacaoItem
                key={item.usuarioId}
                item={item}
                voce={item.usuarioId === usuario?.id}
              />
            ))}
          </ol>
        ))}
    </section>
  );
}

function ClassificacaoItem({
  item,
  voce,
}: {
  item: ClassificacaoParticipante;
  voce: boolean;
}) {
  return (
    <li className="flex items-center gap-3 p-4">
      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-surface-muted text-sm font-bold">
        {item.posicao}
      </span>
      <Avatar nome={item.nomeExibicao} />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-semibold">
          {item.nomeExibicao}
          {voce && <span className="font-normal text-muted"> (você)</span>}
        </p>
        <p className="text-2xs text-muted">
          {item.semanasCumpridas}{" "}
          {item.semanasCumpridas === 1 ? "semana" : "semanas"} cumpridas ·{" "}
          {item.sessoesVerificadas} verificadas · sequência {item.sequenciaSemanas}
        </p>
      </div>
      {item.posicao === 1 && (
        <Medal aria-label="Primeiro lugar" className="h-5 w-5 shrink-0" />
      )}
    </li>
  );
}

function HistoricoSection({ desafioId }: { desafioId: string }) {
  const historico = useHistoricoSemanal(desafioId);

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-base font-bold">Seu histórico semanal</h2>

      {historico.isPending && (
        <p role="status" className="py-6 text-center text-sm text-muted">
          Carregando…
        </p>
      )}

      {historico.isError && (
        <p role="alert" className="form-error">
          {getErrorMessage(historico.error)}
        </p>
      )}

      {historico.data && (
        <ul className="card space-y-4 p-4">
          {historico.data.semanas.map((semana) => (
            <li key={semana.numero}>
              <div className="mb-1 flex items-center justify-between gap-2 text-sm">
                <span className="font-semibold">
                  Semana {semana.numero}
                  {semana.metaCumprida && (
                    <Check
                      aria-label="Meta cumprida"
                      className="ml-1.5 inline h-4 w-4"
                    />
                  )}
                </span>
                <span className="text-muted">
                  {formatarDiaEMes(semana.inicio)} – {formatarDiaEMes(semana.fim)}
                </span>
              </div>
              <ProgressBar
                label={`Semana ${semana.numero}`}
                valor={semana.sessoesVerificadas}
                provisorio={semana.sessoesProvisorias}
                max={historico.data.sessoesPorSemana}
              />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function DesafioConteudo({ desafio }: { desafio: Desafio }) {
  const participar = useParticipar(desafio.id, desafio.grupoId);
  const cancelar = useCancelarDesafio(desafio.id, desafio.grupoId);
  const [cancelando, setCancelando] = useState(false);

  const aberto = isDesafioAberto(desafio.situacao);

  const fecharDialogo = () => {
    cancelar.reset();
    setCancelando(false);
  };

  return (
    <main className="mx-auto min-h-dvh max-w-2xl animate-fade-in px-5 py-6">
      <Link to={`/grupos/${desafio.grupoId}`} className="btn-ghost -ml-2">
        <ArrowLeft aria-hidden="true" className="h-5 w-5" />
        Grupo
      </Link>

      <header className="card mt-4 p-5">
        <div className="flex items-start justify-between gap-3">
          <h1 className="min-w-0 text-xl font-bold break-words">
            {desafio.nome}
          </h1>
          <span className="chip">{situacaoLabels[desafio.situacao]}</span>
        </div>

        {desafio.descricao && (
          <p className="mt-2 text-sm break-words whitespace-pre-line text-muted">
            {desafio.descricao}
          </p>
        )}

        <div className="mt-5 grid grid-cols-2 gap-3">
          <Dado
            icon={Target}
            titulo="Sessões"
            valor={`${desafio.sessoesPorSemana} por semana`}
          />
          <Dado
            icon={Clock}
            titulo="Duração mín."
            valor={formatarDuracao(desafio.duracaoMinimaSegundos)}
          />
          <Dado
            icon={Calendar}
            titulo="Início"
            valor={formatarData(desafio.dataInicio)}
          />
          <Dado
            icon={Calendar}
            titulo="Término"
            valor={formatarData(desafio.dataFim)}
          />
        </div>

        <p className="mt-3 flex items-center gap-2 text-sm text-muted">
          <MapPin aria-hidden="true" className="h-4 w-4 shrink-0" />
          {desafio.fusoHorario.replaceAll("_", " ")}
        </p>
      </header>

      {aberto && !desafio.participando && (
        <div className="mt-4">
          <button
            type="button"
            className="btn-primary w-full"
            disabled={participar.isPending}
            onClick={() => participar.mutate()}
          >
            {participar.isPending ? "Entrando…" : "Participar do desafio"}
          </button>
          {participar.isError && (
            <p role="alert" className="form-error mt-3">
              {getErrorMessage(participar.error)}
            </p>
          )}
        </div>
      )}

      <ProgressoSection desafio={desafio} />
      <ClassificacaoSection desafioId={desafio.id} />
      {desafio.participando && <HistoricoSection desafioId={desafio.id} />}

      {aberto && canGerenciarDesafios(desafio.papel) && (
        <section className="mt-6">
          <button
            type="button"
            className="btn-ghost w-full"
            onClick={() => setCancelando(true)}
          >
            Cancelar desafio
          </button>
        </section>
      )}

      <ConfirmDialog
        open={cancelando}
        title="Cancelar este desafio?"
        message="Ninguém mais poderá participar, e isso não pode ser desfeito. O desafio continua visível no histórico do grupo."
        confirmLabel="Cancelar desafio"
        pending={cancelar.isPending}
        error={cancelar.isError ? getErrorMessage(cancelar.error) : undefined}
        onConfirm={() =>
          cancelar.mutate(undefined, { onSuccess: fecharDialogo })
        }
        onCancel={fecharDialogo}
      />
    </main>
  );
}

export function DesafioPage() {
  const { id = "" } = useParams();
  const desafio = useDesafio(id);

  if (desafio.isPending) return <SplashScreen />;

  if (desafio.isError) {
    const notFound =
      desafio.error instanceof ApiError && desafio.error.status === 404;
    return (
      <main className="flex min-h-dvh flex-col items-center justify-center">
        <EmptyState
          icon={notFound ? Target : CloudOff}
          title={
            notFound ? "Desafio não encontrado" : "Não foi possível carregar"
          }
          description={
            notFound
              ? "Este desafio não existe ou você não faz parte do grupo dele."
              : getErrorMessage(desafio.error)
          }
        />
        <Link to="/" className="btn-secondary">
          Ir para o início
        </Link>
      </main>
    );
  }

  return <DesafioConteudo desafio={desafio.data} />;
}
