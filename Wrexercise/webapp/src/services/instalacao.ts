// Instalação do PWA. O Chromium avisa com `beforeinstallprompt` uma única vez, logo ao carregar,
// por isso o evento é guardado aqui, antes de a página de configurações existir.

type PedidoInstalacao = Event & {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
};

export type ModoInstalacao = "botao" | "ios" | null;

let pedido: PedidoInstalacao | null = null;
let instalado = false;
let dispensada = false;
const ouvintes = new Set<() => void>();

function notificar() {
  ouvintes.forEach((ouvinte) => ouvinte());
}

function isStandalone(): boolean {
  return (
    window.matchMedia("(display-mode: standalone)").matches ||
    (navigator as Navigator & { standalone?: boolean }).standalone === true
  );
}

// O Safari do iOS não tem `beforeinstallprompt`: a instalação é pelo menu Compartilhar.
function isIos(): boolean {
  return (
    /iPhone|iPad|iPod/.test(navigator.userAgent) ||
    (navigator.userAgent.includes("Macintosh") && navigator.maxTouchPoints > 1)
  );
}

// A dispensa do aviso de instalação fica num cookie comum, não em `localStorage`, que o ESLint
// bloqueia. Não é credencial: só evita repetir o aviso. Ver docs/decisoes/0021-pwa-e-service-worker.md.
const cookieDispensa = "wx_instalacao_dispensada";
const segundosDispensa = 30 * 24 * 60 * 60;

function isDispensadaNoCookie(): boolean {
  return document.cookie
    .split(";")
    .some((parte) => parte.trim().startsWith(`${cookieDispensa}=`));
}

export function registrarInstalacao() {
  dispensada = isDispensadaNoCookie();
  window.addEventListener("beforeinstallprompt", (event) => {
    event.preventDefault();
    pedido = event as PedidoInstalacao;
    notificar();
  });
  window.addEventListener("appinstalled", () => {
    pedido = null;
    instalado = true;
    notificar();
  });
}

export function subscribeInstalacao(ouvinte: () => void): () => void {
  ouvintes.add(ouvinte);
  return () => ouvintes.delete(ouvinte);
}

/** Como oferecer a instalação neste navegador, ou `null` quando não há o que oferecer. */
export function getModoInstalacao(): ModoInstalacao {
  if (instalado || isStandalone()) return null;
  if (pedido) return "botao";
  return isIos() ? "ios" : null;
}

/** Como oferecer a instalação no aviso de todo o app: some por 30 dias depois de dispensado. */
export function getModoAvisoInstalacao(): ModoInstalacao {
  return dispensada ? null : getModoInstalacao();
}

export function dispensarAvisoInstalacao() {
  const seguro = window.location.protocol === "https:" ? "; Secure" : "";
  document.cookie = `${cookieDispensa}=1; Max-Age=${segundosDispensa}; Path=/; SameSite=Lax${seguro}`;
  dispensada = true;
  notificar();
}

export async function instalar(): Promise<void> {
  if (!pedido) return;
  const atual = pedido;
  await atual.prompt();
  // O mesmo evento não pode ser usado duas vezes.
  pedido = null;
  notificar();
}
