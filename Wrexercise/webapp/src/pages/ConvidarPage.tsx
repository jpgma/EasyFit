import {
  Check,
  Clock,
  CloudOff,
  Copy,
  Link as LinkIcon,
  RefreshCw,
  Share2,
  UserPlus,
  Users,
} from "lucide-react";
import { useState } from "react";
import { useParams } from "react-router";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { EmptyState } from "@/components/EmptyState";
import { PageHeader } from "@/components/PageHeader";
import { SplashScreen } from "@/components/SplashScreen";
import {
  buildConviteLink,
  useConvite,
  useGerarConvite,
  useRevogarConvite,
} from "@/features/convites";
import { canConvidar, useGrupo } from "@/features/grupos";
import { getErrorMessage } from "@/services/api";

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString("pt-BR", {
    day: "numeric",
    month: "long",
  });
}

export function ConvidarPage() {
  const { id = "" } = useParams();
  const grupo = useGrupo(id);
  const podeConvidar = grupo.data ? canConvidar(grupo.data.papel) : false;
  const convite = useConvite(id, podeConvidar);
  const gerar = useGerarConvite(id);
  const revogar = useRevogarConvite(id);
  const [copied, setCopied] = useState(false);
  const [copyFailed, setCopyFailed] = useState(false);
  const [confirmando, setConfirmando] = useState<"gerar" | "cancelar" | null>(
    null,
  );

  if (grupo.isPending) return <SplashScreen />;

  if (grupo.isError || !podeConvidar) {
    return (
      <div className="mx-auto max-w-xl">
        <PageHeader title="Convidar amigos" back={`/grupos/${id}`} />
        {grupo.isError ? (
          <EmptyState
            icon={CloudOff}
            title="Não foi possível carregar"
            description={getErrorMessage(grupo.error)}
          />
        ) : (
          <EmptyState
            icon={Users}
            title="Só administradores podem convidar"
            description="Peça a um administrador do grupo o link de convite."
          />
        )}
      </div>
    );
  }

  const vagas = grupo.data.maxMembros - grupo.data.totalMembros;
  const link = convite.data ? buildConviteLink(convite.data.token) : null;

  const gerarLink = () => {
    setCopied(false);
    setCopyFailed(false);
    gerar.mutate(undefined, { onSuccess: () => setConfirmando(null) });
  };

  const copiar = async () => {
    if (!link) return;
    try {
      await navigator.clipboard.writeText(link);
      setCopied(true);
      setCopyFailed(false);
    } catch {
      setCopyFailed(true);
    }
  };

  const compartilhar = async () => {
    if (!link) return;
    try {
      await navigator.share({
        title: `Convite para ${grupo.data.nome}`,
        text: `Entre no grupo ${grupo.data.nome} no Wrexercise:`,
        url: link,
      });
    } catch {
      // Cancelar o compartilhamento também cai aqui; o link continua na tela para copiar.
    }
  };

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader
        title="Convidar amigos"
        subtitle={grupo.data.nome}
        back={`/grupos/${id}`}
      />

      <div className="card mt-4 flex items-center gap-3 p-4">
        <UserPlus aria-hidden="true" className="h-5 w-5 shrink-0 text-muted" />
        <div>
          <p className="text-sm font-bold">
            {vagas > 0
              ? `${vagas} ${vagas === 1 ? "vaga disponível" : "vagas disponíveis"}`
              : "Grupo cheio"}
          </p>
          <p className="text-2xs text-muted">
            {vagas > 0
              ? "O mesmo link serve para todo mundo que você quer chamar."
              : "Remova um membro para poder convidar outra pessoa."}
          </p>
        </div>
      </div>

      <section className="mt-4">
        {convite.isPending && (
          <p role="status" className="py-4 text-center text-sm text-muted">
            Carregando…
          </p>
        )}
        {convite.isError && (
          <p role="alert" className="form-error">
            {getErrorMessage(convite.error)}
          </p>
        )}

        {convite.isSuccess && !link && vagas > 0 && (
          <div className="card p-5 text-center">
            <p className="text-sm text-muted">
              O grupo não tem um link válido. Gere um para convidar seus amigos.
            </p>
            <button
              type="button"
              className="btn-primary mt-4 w-full"
              disabled={gerar.isPending}
              onClick={gerarLink}
            >
              <LinkIcon aria-hidden="true" className="h-5 w-5" />
              {gerar.isPending ? "Gerando…" : "Gerar link de convite"}
            </button>
          </div>
        )}

        {link && (
          <>
            <label htmlFor="link-convite" className="input-label">
              Link de convite
            </label>
            <div className="card flex items-center gap-3 p-3">
              <LinkIcon
                aria-hidden="true"
                className="h-5 w-5 shrink-0 text-muted"
              />
              <input
                id="link-convite"
                readOnly
                value={link}
                onFocus={(event) => event.target.select()}
                className="min-w-0 flex-1 truncate bg-transparent text-sm outline-none"
              />
            </div>

            <div className="mt-3 flex gap-3">
              <button
                type="button"
                className="btn-primary flex-1"
                onClick={() => void copiar()}
              >
                {copied ? (
                  <Check aria-hidden="true" className="h-5 w-5" />
                ) : (
                  <Copy aria-hidden="true" className="h-5 w-5" />
                )}
                {copied ? "Copiado!" : "Copiar"}
              </button>
              {"share" in navigator && (
                <button
                  type="button"
                  className="btn-secondary flex-1"
                  onClick={() => void compartilhar()}
                >
                  <Share2 aria-hidden="true" className="h-5 w-5" />
                  Compartilhar
                </button>
              )}
            </div>

            {copyFailed && (
              <p role="alert" className="input-error">
                Não foi possível copiar. Selecione o link acima e copie
                manualmente.
              </p>
            )}

            <p className="mt-3 flex items-start gap-1.5 text-2xs text-muted">
              <Clock
                aria-hidden="true"
                className="mt-0.5 h-3.5 w-3.5 shrink-0"
              />
              Vale até {formatDate(convite.data!.expiresAt)}. Criado por{" "}
              {convite.data!.criadoPor}.
            </p>

            <div className="mt-4 grid grid-cols-2 gap-2">
              {vagas > 0 && (
                <button
                  type="button"
                  className="btn-ghost"
                  onClick={() => setConfirmando("gerar")}
                >
                  <RefreshCw aria-hidden="true" className="size-4" />
                  Gerar novo
                </button>
              )}
              <button
                type="button"
                className="btn-ghost text-danger"
                onClick={() => setConfirmando("cancelar")}
              >
                Cancelar link
              </button>
            </div>
          </>
        )}

        {gerar.isError && confirmando === null && (
          <p role="alert" className="form-error mt-3">
            {getErrorMessage(gerar.error)}
          </p>
        )}
      </section>

      <ConfirmDialog
        open={confirmando === "gerar"}
        title="Gerar um novo link?"
        message="O link atual deixará de funcionar. Quem já entrou continua no grupo."
        confirmLabel="Gerar novo link"
        pending={gerar.isPending}
        error={gerar.isError ? getErrorMessage(gerar.error) : undefined}
        onConfirm={gerarLink}
        onCancel={() => {
          gerar.reset();
          setConfirmando(null);
        }}
      />
      <ConfirmDialog
        open={confirmando === "cancelar"}
        title="Cancelar este link?"
        message="Ele deixará de funcionar imediatamente. Quem já entrou continua no grupo."
        confirmLabel="Cancelar link"
        pending={revogar.isPending}
        error={revogar.isError ? getErrorMessage(revogar.error) : undefined}
        onConfirm={() =>
          revogar.mutate(undefined, { onSuccess: () => setConfirmando(null) })
        }
        onCancel={() => {
          revogar.reset();
          setConfirmando(null);
        }}
      />
    </div>
  );
}
