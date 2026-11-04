import { CloudOff } from "lucide-react";
import { Navigate, Outlet } from "react-router";
import { EmptyState } from "@/components/EmptyState";
import { SplashScreen } from "@/components/SplashScreen";
import { useSessao } from "@/features/autenticacao";

type GateProps = {
  allow: "logado" | "anonimo";
  redirectTo: string;
};

// Só decide o que mostrar: quem protege os dados continua sendo o backend.
function SessaoGate({ allow, redirectTo }: GateProps) {
  const { data: usuario, isPending, refetch } = useSessao();

  // Com o dado já em cache, uma falha ao revalidar não tira a pessoa da tela atual.
  if (usuario === undefined) {
    if (isPending) return <SplashScreen />;

    return (
      <main className="flex min-h-dvh flex-col items-center justify-center">
        <EmptyState
          icon={CloudOff}
          title="Sem conexão"
          description="Não foi possível falar com o servidor. Verifique sua conexão e tente novamente."
        />
        <button
          type="button"
          className="btn-secondary"
          onClick={() => void refetch()}
        >
          Tentar novamente
        </button>
      </main>
    );
  }

  const isLogado = usuario !== null;
  return isLogado === (allow === "logado") ? (
    <Outlet />
  ) : (
    <Navigate to={redirectTo} replace />
  );
}

export function RequireSessao() {
  return <SessaoGate allow="logado" redirectTo="/entrar" />;
}

export function RequireAnonimo() {
  return <SessaoGate allow="anonimo" redirectTo="/" />;
}
