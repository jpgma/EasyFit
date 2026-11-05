import { ChevronRight, Clock, CloudOff, Plus, Target } from "lucide-react";
import { Link, useParams } from "react-router";
import { Avatar } from "@/components/Avatar";
import { EmptyState } from "@/components/EmptyState";
import { SectionTitle } from "@/components/PageHeader";
import { ProgressRing } from "@/components/ProgressRing";
import { SplashScreen } from "@/components/SplashScreen";
import { useAtividadesFeed } from "@/features/atividades";
import { useSessao } from "@/features/autenticacao";
import {
  canGerenciarDesafios,
  FaixaSemana,
  formatarDuracao,
  formatarTempoAteFim,
  useDesafio,
  useProgresso,
} from "@/features/desafios";
import { AvisoAvaliacao, FeedAtividade } from "@/features/feed";
import { useGrupo } from "@/features/grupos";
import { LinhaParticipante, useResumoGrupo } from "@/features/progresso";
import { getErrorMessage } from "@/services/api";

function getSaudacao(agora: Date): string {
  const hora = agora.getHours();
  if (hora < 12) return "Bom dia";
  return hora < 18 ? "Boa tarde" : "Boa noite";
}

function getMotivacao(verificadas: number, provisorias: number, meta: number) {
  if (verificadas >= meta) {
    return "Meta da semana batida! 🎉 Agora é só manter o ritmo.";
  }
  if (provisorias >= meta) return "Meta alcançada, aguardando verificação.";
  const faltam = meta - provisorias;
  return faltam === 1
    ? "Falta só mais um treino!"
    : `Faltam ${faltam} treinos. Dá tempo!`;
}

function SuaSemana({
  grupoId,
  desafioId,
  usuarioId,
}: {
  grupoId: string;
  desafioId: string;
  usuarioId: string;
}) {
  const desafio = useDesafio(desafioId);
  const progresso = useProgresso(desafioId);

  if (progresso.isError || desafio.isError) {
    return (
      <p role="alert" className="form-error">
        {getErrorMessage(progresso.error ?? desafio.error)}
      </p>
    );
  }
  if (!progresso.data || !desafio.data) {
    return (
      <p role="status" className="card p-5 text-sm text-muted">
        Carregando sua semana…
      </p>
    );
  }

  const {
    sessoesPorSemana: meta,
    semanaAtual,
    participantes,
    seusDias,
  } = progresso.data;
  const eu = participantes.find((p) => p.usuarioId === usuarioId);
  const verificadas = eu?.sessoesVerificadas ?? 0;
  const provisorias = eu?.sessoesProvisorias ?? 0;
  const ordenados = [...participantes].sort(
    (a, b) =>
      b.sessoesVerificadas - a.sessoesVerificadas ||
      b.sessoesProvisorias - a.sessoesProvisorias,
  );
  const contando = participantes.filter((p) => p.contaNestaSemana);
  const bateram = contando.filter((p) => p.metaCumprida).length;

  return (
    <div className="grid gap-5 lg:grid-cols-[1.1fr_1fr]">
      <section className="card p-5">
        <div className="flex items-center justify-between gap-2">
          <p className="text-sm font-semibold">Sua semana</p>
          <span className="chip chip-soft">{meta} treinos por semana</span>
        </div>
        <div className="mt-4 flex items-center gap-5">
          <ProgressRing
            label="Seu progresso nesta semana"
            valor={verificadas}
            provisorio={provisorias}
            max={meta}
          >
            <div>
              <p className="font-display text-4xl leading-none font-bold">
                {Math.min(verificadas, meta)}
                <span className="text-xl text-muted">/{meta}</span>
              </p>
              <p className="mt-1 text-2xs text-muted">sessões</p>
            </div>
          </ProgressRing>
          <div className="min-w-0 flex-1">
            <p className="font-display text-xl leading-snug font-bold">
              {getMotivacao(verificadas, provisorias, meta)}
            </p>
            {semanaAtual && (
              <p className="mt-2 flex items-center gap-1.5 text-sm text-muted">
                <Clock aria-hidden="true" className="size-4 shrink-0" />
                Semana fecha em{" "}
                {formatarTempoAteFim(semanaAtual.fim, desafio.data.fusoHorario)}
              </p>
            )}
            <p className="mt-1 text-sm text-muted">
              Mínimo de {formatarDuracao(desafio.data.duracaoMinimaSegundos)}{" "}
              por sessão
            </p>
          </div>
        </div>
        {semanaAtual && (
          <FaixaSemana
            inicio={semanaAtual.inicio}
            diasComTreino={seusDias}
            fusoHorario={desafio.data.fusoHorario}
          />
        )}
        {eu && !eu.contaNestaSemana && (
          <p className="mt-3 rounded-xl bg-surface-muted p-3 text-sm text-muted">
            Você entrou no meio desta semana. Seus treinos contam para a meta a
            partir da próxima segunda.
          </p>
        )}
        {provisorias > verificadas && (
          <p className="mt-3 text-xs text-muted">
            {provisorias - verificadas} aguardando verificação
          </p>
        )}
        <Link
          to={`/grupos/${grupoId}/atividades/nova`}
          className="btn-primary mt-5 w-full"
        >
          <Plus aria-hidden="true" className="size-5" />
          Registrar atividade
        </Link>
      </section>

      <section className="card p-4">
        <div className="flex items-center justify-between gap-2 px-2 pb-2">
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold">
              {desafio.data.nome}
            </p>
            <p className="text-xs text-muted">
              {bateram} de {contando.length} já bateram a meta
            </p>
          </div>
          <Link
            to={`/grupos/${grupoId}`}
            className="flex shrink-0 items-center text-sm font-semibold text-link"
          >
            Ver grupo
            <ChevronRight aria-hidden="true" className="size-4" />
          </Link>
        </div>
        {ordenados.map((p) => (
          <LinhaParticipante
            key={p.usuarioId}
            usuarioId={p.usuarioId}
            nomeExibicao={p.nomeExibicao}
            verificadas={p.sessoesVerificadas}
            provisorias={p.sessoesProvisorias}
            meta={meta}
            voce={p.usuarioId === usuarioId}
            contaNestaSemana={p.contaNestaSemana}
            grupoId={grupoId}
          />
        ))}
      </section>
    </div>
  );
}

export function InicioPage() {
  const { id = "" } = useParams();
  const { data: usuario } = useSessao();
  const grupo = useGrupo(id);
  const resumo = useResumoGrupo(id);
  const feed = useAtividadesFeed(id, 4);

  if (grupo.isPending || !usuario) return <SplashScreen />;

  if (grupo.isError) {
    return (
      <div className="card">
        <EmptyState
          icon={CloudOff}
          title="Não foi possível carregar"
          description={getErrorMessage(grupo.error)}
          action={
            <Link to="/" className="btn-secondary">
              Voltar
            </Link>
          }
        />
      </div>
    );
  }

  const agora = new Date();
  const hoje = new Intl.DateTimeFormat("pt-BR", {
    weekday: "short",
    day: "numeric",
    month: "long",
  })
    .format(agora)
    .replace(".", "");
  const primeiroNome = usuario.nomeExibicao.trim().split(/\s+/)[0];
  const desafio = resumo.data?.desafio;
  const atividades = feed.data?.pages[0]?.atividades ?? [];

  return (
    <>
      <header className="flex items-center gap-3">
        <Avatar nome={usuario.nomeExibicao} id={usuario.id} size={44} />
        <div className="min-w-0">
          <p className="text-sm text-muted first-letter:uppercase">{hoje}</p>
          <h1 className="truncate text-2xl font-bold">
            {getSaudacao(agora)}, {primeiroNome}!
          </h1>
        </div>
      </header>

      <div className="mt-6">
        {resumo.isError && (
          <p role="alert" className="form-error">
            {getErrorMessage(resumo.error)}
          </p>
        )}
        {resumo.data && desafio && (
          <SuaSemana
            grupoId={id}
            desafioId={desafio.id}
            usuarioId={usuario.id}
          />
        )}
        {resumo.data && !desafio && (
          <div className="card">
            <EmptyState
              icon={Target}
              title="Nenhum desafio ativo"
              description={
                canGerenciarDesafios(grupo.data.papel)
                  ? "O grupo está sem meta no momento. Crie um desafio para começar."
                  : "O grupo está sem meta no momento. Peça a um administrador para criar um desafio."
              }
              action={
                canGerenciarDesafios(grupo.data.papel) ? (
                  <Link
                    to={`/grupos/${id}/desafios/novo`}
                    className="btn-primary"
                  >
                    Criar desafio
                  </Link>
                ) : undefined
              }
            />
          </div>
        )}
      </div>

      <AvisoAvaliacao grupoId={id} />

      <SectionTitle
        action={
          <Link
            to={`/grupos/${id}/atividades`}
            className="text-sm font-semibold text-link"
          >
            Ver todas
          </Link>
        }
      >
        Atividades recentes
      </SectionTitle>
      {feed.isError && (
        <p role="alert" className="form-error">
          {getErrorMessage(feed.error)}
        </p>
      )}
      {feed.data && atividades.length === 0 && (
        <p className="card p-4 text-sm text-muted">
          Quando alguém do grupo registrar um treino, ele aparece aqui.
        </p>
      )}
      <div className="grid gap-3 lg:grid-cols-2">
        {atividades.map((atividade) => (
          <FeedAtividade
            key={atividade.id}
            atividade={atividade}
            duracaoMinimaSegundos={desafio?.duracaoMinimaSegundos}
          />
        ))}
      </div>
    </>
  );
}
