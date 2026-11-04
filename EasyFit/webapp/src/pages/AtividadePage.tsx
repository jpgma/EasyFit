import { ArrowLeft, CloudOff } from "lucide-react";
import { Link, useParams } from "react-router";
import { Avatar } from "@/components/Avatar";
import { EmptyState } from "@/components/EmptyState";
import { SplashScreen } from "@/components/SplashScreen";
import {
  formatarDataHora,
  formatarDuracao,
  situacaoVerificacaoLabels,
  tipoAtividadeLabels,
  tipoComprovacaoLabels,
  useAtividade,
} from "@/features/atividades";
import { ApiError, getErrorMessage } from "@/services/api";

export function AtividadePage() {
  const { id = "" } = useParams();
  const atividade = useAtividade(id);

  if (atividade.isPending) return <SplashScreen />;

  if (atividade.isError) {
    const notFound =
      atividade.error instanceof ApiError && atividade.error.status === 404;
    return (
      <main className="flex min-h-dvh flex-col items-center justify-center px-5">
        <EmptyState
          icon={notFound ? CloudOff : CloudOff}
          title={
            notFound ? "Atividade não encontrada" : "Não foi possível carregar"
          }
          description={
            notFound
              ? "Esta atividade não existe ou você não tem acesso a ela."
              : getErrorMessage(atividade.error)
          }
        />
        <Link to="/" className="btn-secondary mt-4">
          Ir para o início
        </Link>
      </main>
    );
  }

  const {
    nomeExibicao,
    grupoId,
    grupoNome,
    tipo,
    duracaoSegundos,
    startedAt,
    descricao,
    tipoComprovacao,
    situacaoVerificacao,
    submittedAt,
    voce,
    evidencias,
  } = atividade.data;

  return (
    <main className="mx-auto min-h-dvh max-w-2xl animate-fade-in px-5 py-6">
      <Link to={`/grupos/${grupoId}`} className="btn-ghost -ml-2">
        <ArrowLeft aria-hidden="true" className="h-5 w-5" />
        {grupoNome}
      </Link>

      <header className="card mt-4 p-5">
        <div className="flex items-center gap-3">
          <Avatar nome={nomeExibicao} />
          <div className="min-w-0 flex-1">
            <p className="truncate font-bold">
              {nomeExibicao}
              {voce && <span className="font-normal text-muted"> (você)</span>}
            </p>
            <p className="text-sm text-muted">
              {tipoAtividadeLabels[tipo]} · {formatarDuracao(duracaoSegundos)}
            </p>
          </div>
          <span className="chip">
            {situacaoVerificacaoLabels[situacaoVerificacao]}
          </span>
        </div>

        <dl className="mt-4 space-y-2 text-sm">
          <div className="flex justify-between gap-3">
            <dt className="text-muted">Início</dt>
            <dd className="font-medium">{formatarDataHora(startedAt)}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted">Enviada</dt>
            <dd className="font-medium">{formatarDataHora(submittedAt)}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted">Comprovação</dt>
            <dd className="font-medium">
              {tipoComprovacaoLabels[tipoComprovacao]}
            </dd>
          </div>
        </dl>

        {descricao && (
          <p className="mt-4 text-sm leading-relaxed">{descricao}</p>
        )}
      </header>

      {evidencias.length > 0 && (
        <section className="mt-6">
          <h2 className="mb-3 text-base font-bold">Evidências</h2>
          <ul className="grid gap-3 sm:grid-cols-2">
            {evidencias.map((evidencia) => (
              <li key={evidencia.id} className="card overflow-hidden">
                <img
                  src={evidencia.url}
                  alt={`Evidência ${evidencia.ordem}`}
                  className="aspect-[4/3] w-full object-cover"
                  loading="lazy"
                />
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  );
}
