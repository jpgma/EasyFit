import { ListChecks } from "lucide-react";
import { Link, useParams } from "react-router";
import { EmptyState } from "@/components/EmptyState";
import { PageHeader } from "@/components/PageHeader";
import { useAtividadesFeed } from "@/features/atividades";
import { FeedAtividade } from "@/features/feed";
import { getErrorMessage } from "@/services/api";

export function AtividadesPage() {
  const { id = "" } = useParams();
  const feed = useAtividadesFeed(id);
  const atividades = feed.data?.pages.flatMap((p) => p.atividades) ?? [];

  return (
    <>
      <PageHeader title="Atividades" subtitle="Todos os treinos do grupo" />

      {feed.isPending && (
        <p role="status" className="py-8 text-center text-sm text-muted">
          Carregando…
        </p>
      )}
      {feed.isError && (
        <p role="alert" className="form-error">
          {getErrorMessage(feed.error)}
        </p>
      )}
      {feed.data && atividades.length === 0 && (
        <div className="card">
          <EmptyState
            icon={ListChecks}
            title="Nenhuma atividade ainda"
            description="Quando alguém do grupo registrar um treino, ele aparece aqui."
            action={
              <Link
                to={`/grupos/${id}/atividades/nova`}
                className="btn-primary"
              >
                Registrar a primeira
              </Link>
            }
          />
        </div>
      )}

      <div className="grid gap-3 lg:grid-cols-2">
        {atividades.map((atividade) => (
          <FeedAtividade key={atividade.id} atividade={atividade} />
        ))}
      </div>

      {feed.hasNextPage && (
        <button
          type="button"
          className="btn-secondary mt-3 w-full"
          disabled={feed.isFetchingNextPage}
          onClick={() => void feed.fetchNextPage()}
        >
          {feed.isFetchingNextPage ? "Carregando…" : "Carregar mais"}
        </button>
      )}
    </>
  );
}
