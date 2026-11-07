// Registro do Service Worker e aviso de nova versão. Ver docs/decisoes/0021-pwa-e-service-worker.md.

let aguardando: ServiceWorker | null = null;
const ouvintes = new Set<() => void>();

function definirAguardando(worker: ServiceWorker | null) {
  aguardando = worker;
  ouvintes.forEach((ouvinte) => ouvinte());
}

/** Para `useSyncExternalStore`: há uma versão nova instalada, esperando a pessoa aceitar. */
export function subscribeAtualizacao(ouvinte: () => void): () => void {
  ouvintes.add(ouvinte);
  return () => ouvintes.delete(ouvinte);
}

export function hasAtualizacao(): boolean {
  return aguardando !== null;
}

/** Ativa a versão nova; a página recarrega quando ela assume. */
export function aplicarAtualizacao() {
  aguardando?.postMessage("SKIP_WAITING");
}

function observar(registration: ServiceWorkerRegistration) {
  // Sem controlador, é a primeira instalação: não há versão antiga a substituir.
  if (registration.waiting && navigator.serviceWorker.controller)
    definirAguardando(registration.waiting);

  registration.addEventListener("updatefound", () => {
    const novo = registration.installing;
    novo?.addEventListener("statechange", () => {
      if (novo.state === "installed" && navigator.serviceWorker.controller)
        definirAguardando(novo);
    });
  });

  // Quem deixa o app aberto por dias também descobre a versão nova, sem polling.
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible")
      void registration.update().catch(() => undefined);
  });
}

export function registrarServiceWorker() {
  // Em desenvolvimento, o cache atrapalharia o recarregamento do Vite.
  if (!import.meta.env.PROD || !("serviceWorker" in navigator)) return;

  let recarregando = false;
  navigator.serviceWorker.addEventListener("controllerchange", () => {
    // Só recarrega quando havia uma versão antiga; a primeira instalação não muda nada na tela.
    if (recarregando || !aguardando) return;
    recarregando = true;
    window.location.reload();
  });

  window.addEventListener("load", () => {
    navigator.serviceWorker
      .register("/sw.js")
      .then(observar)
      .catch(() => undefined);
  });
}
