import { ChevronRight, LogOut, Plus } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router";
import { Avatar } from "@/components/Avatar";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { PageHeader, SectionTitle } from "@/components/PageHeader";
import { useSair, useSessao } from "@/features/autenticacao";
import { ConviteForm } from "@/features/convites";
import { papelLabels, useGrupos } from "@/features/grupos";
import { getErrorMessage } from "@/services/api";

export function PerfilPage() {
  const { data: usuario } = useSessao();
  const grupos = useGrupos();
  const sair = useSair();
  const [confirmandoSair, setConfirmandoSair] = useState(false);

  if (!usuario) return null;

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader title="Perfil" />

      <div className="card flex items-center gap-4 p-5">
        <Avatar nome={usuario.nomeExibicao} id={usuario.id} size={72} />
        <div className="min-w-0">
          <p className="truncate font-display text-xl font-bold">
            {usuario.nomeExibicao}
          </p>
          <p className="truncate text-sm text-muted">{usuario.email}</p>
        </div>
      </div>

      <SectionTitle>Seus grupos</SectionTitle>
      {grupos.isError && (
        <p role="alert" className="form-error">
          {getErrorMessage(grupos.error)}
        </p>
      )}
      {grupos.data && grupos.data.length > 0 && (
        <ul className="card divide-y divide-border overflow-hidden">
          {grupos.data.map((grupo) => (
            <li key={grupo.id}>
              <Link
                to={`/grupos/${grupo.id}`}
                className="flex items-center gap-3 p-4 transition-colors hover:bg-surface-muted"
              >
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold">{grupo.nome}</p>
                  <p className="text-xs text-muted">
                    {grupo.totalMembros}{" "}
                    {grupo.totalMembros === 1 ? "membro" : "membros"} ·{" "}
                    {papelLabels[grupo.papel]}
                  </p>
                </div>
                <ChevronRight
                  aria-hidden="true"
                  className="size-4 shrink-0 text-muted"
                />
              </Link>
            </li>
          ))}
        </ul>
      )}
      <Link to="/grupos/novo" className="btn-secondary mt-3 w-full">
        <Plus aria-hidden="true" className="size-5" />
        Criar grupo
      </Link>

      <div className="mt-4">
        <ConviteForm />
      </div>

      <button
        type="button"
        className="mt-6 flex h-12 w-full items-center justify-center gap-2 rounded-xl text-sm font-semibold text-danger hover:bg-danger-soft"
        onClick={() => setConfirmandoSair(true)}
      >
        <LogOut aria-hidden="true" className="size-4" />
        Sair da conta
      </button>

      <ConfirmDialog
        open={confirmandoSair}
        title="Sair da conta?"
        message="Você pode entrar de novo quando quiser. Seus treinos continuam salvos."
        confirmLabel="Sair"
        pending={sair.isPending}
        error={sair.isError ? getErrorMessage(sair.error) : undefined}
        onConfirm={() => sair.mutate()}
        onCancel={() => {
          sair.reset();
          setConfirmandoSair(false);
        }}
      />
    </div>
  );
}
