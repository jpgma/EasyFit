import { Bell } from "lucide-react";
import { useNavigate } from "react-router";
import { EmptyState } from "@/components/EmptyState";
import { PageHeader } from "@/components/PageHeader";
import { formatarDataRelativa } from "@/features/atividades";
import {
  getDestinoNotificacao,
  getTextoNotificacao,
  useMarcarLida,
  useMarcarTodasLidas,
  useNotificacoes,
  type Notificacao,
} from "@/features/notificacoes";
import { getErrorMessage } from "@/services/api";

export function NotificacoesPage() {
  const navigate = useNavigate();
  const notificacoes = useNotificacoes();
  const marcar = useMarcarLida();
  const marcarTodas = useMarcarTodasLidas();

  const abrir = (notificacao: Notificacao) => {
    if (!notificacao.lida) marcar.mutate(notificacao.id);
    void navigate(getDestinoNotificacao(notificacao));
  };

  const lista = notificacoes.data ?? [];

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader
        title="Notificações"
        action={
          lista.some(({ lida }) => !lida) && (
            <button
              type="button"
              className="text-sm font-semibold text-link"
              disabled={marcarTodas.isPending}
              onClick={() => marcarTodas.mutate()}
            >
              Marcar todas como lidas
            </button>
          )
        }
      />

      {notificacoes.isPending && (
        <p role="status" className="py-8 text-center text-sm text-muted">
          Carregando…
        </p>
      )}
      {notificacoes.isError && (
        <p role="alert" className="form-error">
          {getErrorMessage(notificacoes.error)}
        </p>
      )}
      {notificacoes.isSuccess && lista.length === 0 && (
        <div className="card">
          <EmptyState
            icon={Bell}
            title="Tudo quieto por aqui"
            description="Reações, revisões e treinos para avaliar aparecem aqui."
          />
        </div>
      )}
      {lista.length > 0 && (
        <ul className="card divide-y divide-border overflow-hidden">
          {lista.map((notificacao) => (
            <li key={notificacao.id}>
              <button
                type="button"
                className={`flex w-full items-start gap-3 p-4 text-left transition-colors hover:bg-surface-muted ${notificacao.lida ? "" : "bg-soft"}`}
                onClick={() => abrir(notificacao)}
              >
                <span
                  aria-hidden="true"
                  className={`mt-1.5 size-2 shrink-0 rounded-full ${notificacao.lida ? "" : "bg-primary"}`}
                />
                <span className="min-w-0 flex-1">
                  <span
                    className={`block text-sm break-words ${notificacao.lida ? "" : "font-semibold"}`}
                  >
                    {getTextoNotificacao(notificacao)}
                    {!notificacao.lida && (
                      <span className="sr-only"> (não lida)</span>
                    )}
                  </span>
                  <span className="mt-0.5 block text-xs text-muted">
                    {notificacao.grupoNome} ·{" "}
                    {formatarDataRelativa(notificacao.updatedAt)}
                  </span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
