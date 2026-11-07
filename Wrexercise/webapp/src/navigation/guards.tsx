import { WifiOff } from "lucide-react";
import { Navigate, Outlet, useLocation } from "react-router";
import { EmptyState } from "@/components/EmptyState";
import { SplashScreen } from "@/components/SplashScreen";
import { useSessao } from "@/features/autenticacao";

type GateProps = {
  allow: "logado" | "anonimo";
  redirectTo: string;
};

// Só decide o que mostrar: quem protege os dados continua sendo o backend.
function SessaoGate({ allow, redirectTo }: GateProps) {
  const { data: usuario, isPending, fetchStatus, refetch } = useSessao();
  const location = useLocation();

  // Com o dado já em cache, uma falha ao revalidar não tira a pessoa da tela atual.
  if (usuario === undefined) {
    // Sem rede, a consulta fica pausada até a conexão voltar: em vez de carregar para sempre, avisa.
    if (isPending && fetchStatus !== "paused") return <SplashScreen />;

    return (
      <main className="flex min-h-dvh flex-col items-center justify-center">
        <EmptyState
          icon={WifiOff}
          title="Você está sem conexão"
          description="Confira sua internet. Assim que voltar, a gente atualiza tudo."
          action={
            <button
              type="button"
              className="btn-secondary"
              onClick={() => void refetch()}
            >
              Tentar de novo
            </button>
          }
        />
      </main>
    );
  }

  const isLogado = usuario !== null;
  return isLogado === (allow === "logado") ? (
    <Outlet />
  ) : (
    <Navigate to={getStateRedirect(location.state) ?? redirectTo} replace />
  );
}

/** Quem veio de um convite volta para ele depois de entrar. */
function getStateRedirect(state: unknown): string | undefined {
  const target = (state as { redirectTo?: unknown } | null)?.redirectTo;
  // Só caminhos internos: "//site" seria interpretado como outro domínio.
  return typeof target === "string" &&
    target.startsWith("/") &&
    !target.startsWith("//")
    ? target
    : undefined;
}

export function RequireSessao() {
  return <SessaoGate allow="logado" redirectTo="/entrar" />;
}

export function RequireAnonimo() {
  return <SessaoGate allow="anonimo" redirectTo="/" />;
}
