import { CloudOff, Plus, Users } from "lucide-react";
import { Link, Navigate } from "react-router";
import { EmptyState } from "@/components/EmptyState";
import { SplashScreen } from "@/components/SplashScreen";
import { ConviteForm } from "@/features/convites";
import { useGrupos } from "@/features/grupos";
import { getErrorMessage } from "@/services/api";

/** As abas são por grupo: a raiz só decide para onde levar quem acabou de entrar. */
export function RaizPage() {
  const grupos = useGrupos();

  if (grupos.isPending) return <SplashScreen />;

  if (grupos.isError) {
    return (
      <div className="card">
        <EmptyState
          icon={CloudOff}
          title="Você está sem conexão?"
          description={getErrorMessage(grupos.error)}
        />
      </div>
    );
  }

  const [primeiro] = grupos.data;
  if (primeiro)
    return <Navigate to={`/grupos/${primeiro.id}/inicio`} replace />;

  return (
    <div className="mx-auto max-w-xl space-y-4">
      <div className="card">
        <EmptyState
          icon={Users}
          title="Você ainda não faz parte de um grupo"
          description="Crie um grupo para seus amigos ou use o convite que você recebeu."
          action={
            <Link to="/grupos/novo" className="btn-primary">
              <Plus aria-hidden="true" className="size-5" />
              Criar grupo
            </Link>
          }
        />
      </div>
      <ConviteForm />
    </div>
  );
}
