import { RefreshCw, WifiOff } from "lucide-react";
import { useSyncExternalStore } from "react";
import { useConectado } from "@/components/useConectado";
import {
  aplicarAtualizacao,
  hasAtualizacao,
  subscribeAtualizacao,
} from "@/services/serviceWorker";

const classesAviso =
  "pointer-events-auto flex w-full max-w-md animate-slide-up items-center gap-3 rounded-xl border border-border bg-surface px-4 py-2.5 text-sm font-medium shadow-float";

/** Avisos de todo o app: falta de conexão e versão nova pronta para instalar. */
export function AvisosDoApp() {
  const conectado = useConectado();
  const atualizacao = useSyncExternalStore(
    subscribeAtualizacao,
    hasAtualizacao,
  );

  if (conectado && !atualizacao) return null;

  return (
    <div
      className="pointer-events-none fixed inset-x-0 z-40 flex flex-col items-center gap-2 px-4"
      style={{ top: "calc(env(safe-area-inset-top) + 1rem)" }}
    >
      {!conectado && (
        <div role="status" aria-atomic="true" className={classesAviso}>
          <WifiOff
            aria-hidden="true"
            className="size-5 shrink-0 text-warning"
          />
          <p className="min-h-11 flex-1 content-center">
            Você está sem conexão
          </p>
        </div>
      )}
      {atualizacao && (
        <div role="status" aria-atomic="true" className={classesAviso}>
          <RefreshCw
            aria-hidden="true"
            className="size-5 shrink-0 text-primary"
          />
          <p className="flex-1">Nova versão disponível</p>
          <button
            type="button"
            className="h-11 shrink-0 rounded-lg bg-primary px-4 text-sm font-semibold text-on-primary tap hover:bg-primary-hover"
            onClick={aplicarAtualizacao}
          >
            Atualizar
          </button>
        </div>
      )}
    </div>
  );
}
