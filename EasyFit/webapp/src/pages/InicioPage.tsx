import { LogOut, Users } from "lucide-react";
import { useState } from "react";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { EmptyState } from "@/components/EmptyState";
import { useSair, useSessao } from "@/features/autenticacao";
import { getErrorMessage } from "@/services/api";

export function InicioPage() {
  const { data: usuario } = useSessao();
  const sair = useSair();
  const [confirmingSair, setConfirmingSair] = useState(false);

  if (!usuario) return null;

  return (
    <main className="mx-auto min-h-dvh max-w-2xl animate-fade-in">
      <header className="flex items-center justify-between gap-3 px-5 pt-6">
        <div className="min-w-0">
          <p className="text-sm text-muted">Olá,</p>
          <h1 className="truncate text-2xl font-bold">
            {usuario.nomeExibicao}
          </h1>
        </div>
        <button
          type="button"
          className="btn-ghost shrink-0"
          onClick={() => setConfirmingSair(true)}
        >
          <LogOut aria-hidden="true" className="h-5 w-5" />
          Sair
        </button>
      </header>

      <div className="px-5 pt-6">
        <div className="card">
          <EmptyState
            icon={Users}
            title="Você ainda não faz parte de um grupo"
            description="Em breve você poderá criar um grupo ou entrar em um com um convite."
          />
        </div>
      </div>

      <ConfirmDialog
        open={confirmingSair}
        title="Sair da conta?"
        message="Você precisará entrar novamente com seu e-mail e senha."
        confirmLabel="Sair"
        pending={sair.isPending}
        error={sair.isError ? getErrorMessage(sair.error) : undefined}
        onConfirm={() => sair.mutate()}
        onCancel={() => {
          sair.reset();
          setConfirmingSair(false);
        }}
      />
    </main>
  );
}
