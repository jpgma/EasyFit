import {
  Check,
  Clock,
  CloudOff,
  Copy,
  Link as LinkIcon,
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
  useConvites,
  useCriarConvite,
  useRevogarConvite,
  type Convite,
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
  const convites = useConvites(id, podeConvidar);
  const criar = useCriarConvite(id);
  const revogar = useRevogarConvite(id);
  const [link, setLink] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [copyFailed, setCopyFailed] = useState(false);
  const [revogando, setRevogando] = useState<Convite | null>(null);

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
            description="Peça a um administrador do grupo para gerar um convite."
          />
        )}
      </div>
    );
  }

  const vagas = grupo.data.maxMembros - grupo.data.totalMembros;

  const gerar = () => {
    setCopied(false);
    setCopyFailed(false);
    criar.mutate(undefined, {
      onSuccess: ({ token }) => setLink(buildConviteLink(token)),
    });
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
        text: `Entre no grupo ${grupo.data.nome} no EasyFit:`,
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
              ? "Gere um link para cada pessoa que você quer convidar."
              : "Remova um membro para poder convidar outra pessoa."}
          </p>
        </div>
      </div>

      {vagas > 0 && (
        <section className="mt-4">
          {link ? (
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
                Vale para uma pessoa, por 7 dias. Copie agora: este link não
                será mostrado de novo.
              </p>

              <button
                type="button"
                className="btn-ghost mt-2 w-full"
                disabled={criar.isPending}
                onClick={gerar}
              >
                Gerar outro link
              </button>
            </>
          ) : (
            <button
              type="button"
              className="btn-primary w-full"
              disabled={criar.isPending}
              onClick={gerar}
            >
              <LinkIcon aria-hidden="true" className="h-5 w-5" />
              {criar.isPending ? "Gerando…" : "Gerar link de convite"}
            </button>
          )}

          {criar.isError && (
            <p role="alert" className="form-error mt-3">
              {getErrorMessage(criar.error)}
            </p>
          )}
        </section>
      )}

      <section className="mt-6">
        <h2 className="mb-3 text-lg font-bold">Convites pendentes</h2>

        {convites.isPending && (
          <p role="status" className="py-4 text-center text-sm text-muted">
            Carregando…
          </p>
        )}

        {convites.isError && (
          <p role="alert" className="form-error">
            {getErrorMessage(convites.error)}
          </p>
        )}

        {convites.data?.length === 0 && (
          <p className="text-sm text-muted">Nenhum convite pendente.</p>
        )}

        {convites.data && convites.data.length > 0 && (
          <ul className="card divide-y divide-border">
            {convites.data.map((convite) => (
              <li key={convite.id} className="flex items-center gap-3 p-4">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-bold">
                    Criado por {convite.criadoPor}
                  </p>
                  <p className="text-2xs text-muted">
                    Expira em {formatDate(convite.expiresAt)}
                  </p>
                </div>
                <button
                  type="button"
                  className="btn-ghost shrink-0 text-sm"
                  onClick={() => setRevogando(convite)}
                >
                  Cancelar
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      <ConfirmDialog
        open={revogando !== null}
        title="Cancelar este convite?"
        message="O link deixará de funcionar imediatamente."
        confirmLabel="Cancelar convite"
        pending={revogar.isPending}
        error={revogar.isError ? getErrorMessage(revogar.error) : undefined}
        onConfirm={() => {
          if (revogando) {
            revogar.mutate(revogando.id, {
              onSuccess: () => {
                // O link na tela pode ser justamente o que acabou de ser cancelado.
                setLink(null);
                setRevogando(null);
              },
            });
          }
        }}
        onCancel={() => {
          revogar.reset();
          setRevogando(null);
        }}
      />
    </div>
  );
}
