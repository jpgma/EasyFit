import { Download, RefreshCw, Smartphone, WifiOff, X } from "lucide-react";
import { useSyncExternalStore } from "react";
import { useConectado } from "@/components/useConectado";
import { useSessao } from "@/features/autenticacao";
import {
  dispensarAvisoInstalacao,
  getModoAvisoInstalacao,
  instalar,
  subscribeInstalacao,
} from "@/services/instalacao";
import {
  aplicarAtualizacao,
  hasAtualizacao,
  subscribeAtualizacao,
} from "@/services/serviceWorker";

const classesAviso =
  "pointer-events-auto flex w-full max-w-md animate-slide-up items-center gap-3 rounded-xl border-2 border-outline bg-surface px-4 py-2.5 text-sm font-medium shadow-sticker";

const classesBotaoPrimario =
  "h-11 shrink-0 rounded-lg border-2 border-outline bg-primary px-4 text-sm font-bold text-on-primary press hover:bg-primary-hover";

/** Avisos de todo o app: falta de conexão, versão nova e convite para instalar o PWA. */
export function AvisosDoApp() {
  const conectado = useConectado();
  const atualizacao = useSyncExternalStore(
    subscribeAtualizacao,
    hasAtualizacao,
  );
  const modoInstalacao = useSyncExternalStore(
    subscribeInstalacao,
    getModoAvisoInstalacao,
  );
  const { data: usuario } = useSessao();

  // Um aviso por vez: a instalação só aparece para quem já entrou, com rede e sem versão nova.
  const instalacao =
    conectado && !atualizacao && usuario ? modoInstalacao : null;

  if (conectado && !atualizacao && !instalacao) return null;

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
          <RefreshCw aria-hidden="true" className="size-5 shrink-0 text-link" />
          <p className="flex-1">Nova versão disponível</p>
          <button
            type="button"
            className={classesBotaoPrimario}
            onClick={aplicarAtualizacao}
          >
            Atualizar
          </button>
        </div>
      )}
      {instalacao && (
        <div role="status" aria-atomic="true" className={classesAviso}>
          <Smartphone
            aria-hidden="true"
            className="size-5 shrink-0 text-link"
          />
          <p className="flex-1">
            {instalacao === "botao"
              ? "Instale o Wrexercise para abrir direto da tela inicial."
              : "Para instalar, toque em Compartilhar e depois em Adicionar à Tela de Início."}
          </p>
          {instalacao === "botao" && (
            <button
              type="button"
              className={`${classesBotaoPrimario} inline-flex items-center gap-1.5`}
              onClick={() => void instalar()}
            >
              <Download aria-hidden="true" className="size-4" />
              Instalar
            </button>
          )}
          <button
            type="button"
            aria-label="Dispensar aviso de instalação"
            className="-mr-2 grid size-11 shrink-0 place-items-center rounded-lg text-muted tap hover:text-foreground"
            onClick={dispensarAvisoInstalacao}
          >
            <X aria-hidden="true" className="size-5" />
          </button>
        </div>
      )}
    </div>
  );
}
