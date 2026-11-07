import { useSyncExternalStore } from "react";

function subscribe(ouvinte: () => void): () => void {
  window.addEventListener("online", ouvinte);
  window.addEventListener("offline", ouvinte);
  return () => {
    window.removeEventListener("online", ouvinte);
    window.removeEventListener("offline", ouvinte);
  };
}

/** `false` só quando o navegador sabe que está sem rede; `true` não garante que o servidor responda. */
export function useConectado(): boolean {
  return useSyncExternalStore(subscribe, () => navigator.onLine);
}
