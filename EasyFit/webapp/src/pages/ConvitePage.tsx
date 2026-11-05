import { CloudOff, Target, Ticket, Users } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router";
import { Avatar } from "@/components/Avatar";
import { Logo } from "@/components/BrandMark";
import { EmptyState } from "@/components/EmptyState";
import { SplashScreen } from "@/components/SplashScreen";
import { useSessao } from "@/features/autenticacao";
import { useAceitarConvite, useConsultarConvite } from "@/features/convites";
import { ApiError, getErrorMessage } from "@/services/api";

function ConviteIndisponivel({
  icon = Ticket,
  title,
  description,
}: {
  icon?: typeof Ticket;
  title: string;
  description: string;
}) {
  return (
    <main className="mx-auto flex min-h-dvh max-w-sm flex-col justify-center px-6">
      <div className="card">
        <EmptyState
          icon={icon}
          title={title}
          description={description}
          action={
            <Link to="/" className="btn-secondary">
              Ir para o início
            </Link>
          }
        />
      </div>
    </main>
  );
}

function getIniciais(nome: string): string {
  return nome
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((palavra) => palavra.charAt(0).toUpperCase())
    .join("");
}

export function ConvitePage() {
  const { hash } = useLocation();
  const token = hash.slice(1);
  const navigate = useNavigate();
  const sessao = useSessao();
  const consulta = useConsultarConvite(token);
  const aceitar = useAceitarConvite();

  if (!token) {
    return (
      <ConviteIndisponivel
        title="Convite não encontrado"
        description="Abra o link completo que você recebeu de quem convidou você."
      />
    );
  }

  if (consulta.isPending || sessao.isPending) return <SplashScreen />;

  if (consulta.isError) {
    return consulta.error instanceof ApiError &&
      consulta.error.status === 404 ? (
      <ConviteIndisponivel
        title="Este convite não é válido"
        description="Ele pode ter expirado ou sido substituído. Peça o link atual para quem te chamou."
      />
    ) : (
      <ConviteIndisponivel
        icon={CloudOff}
        title="Não foi possível abrir o convite"
        description={getErrorMessage(consulta.error)}
      />
    );
  }

  const {
    grupoNome,
    convidadoPor,
    totalMembros,
    maxMembros,
    desafioNome,
    membros,
  } = consulta.data;
  const vagas = maxMembros - totalMembros;
  const logado = Boolean(sessao.data);

  return (
    <main className="mx-auto flex min-h-dvh max-w-sm flex-col px-6 py-6">
      <Logo />

      <div className="card mt-8 animate-slide-up overflow-hidden">
        <div className="bg-brand px-6 pt-6 pb-14 text-sm text-on-brand">
          {convidadoPor} te convidou
        </div>
        <div className="-mt-10 px-6 pb-6">
          <div className="grid size-20 place-items-center rounded-3xl border-4 border-surface bg-lime font-display text-2xl font-bold text-on-lime">
            {getIniciais(grupoNome)}
          </div>
          <h1 className="mt-3 text-2xl font-bold break-words">{grupoNome}</h1>
          <p className="mt-2 flex items-center gap-2 text-sm text-muted">
            <Users aria-hidden="true" className="size-4 shrink-0" />
            {totalMembros} de {maxMembros} membros
          </p>
          {desafioNome && (
            <p className="mt-1 flex items-center gap-2 text-sm text-muted">
              <Target aria-hidden="true" className="size-4 shrink-0" />
              <span className="min-w-0 break-words">Meta: {desafioNome}</span>
            </p>
          )}
          <ul className="mt-4 flex flex-wrap gap-x-3 gap-y-2">
            {membros.map((nome, indice) => (
              <li
                key={`${nome}-${indice}`}
                className="flex items-center gap-1.5 text-xs font-medium"
              >
                {/* A prévia é pública e não expõe identificadores: a cor vem do nome. */}
                <Avatar nome={nome} id={nome} size={24} />
                {nome.trim().split(/\s+/)[0]}
              </li>
            ))}
          </ul>
          <p className="mt-5 rounded-2xl bg-surface-muted p-4 text-sm text-muted">
            Ao entrar, você concorda em registrar suas atividades e avaliar as
            dos colegas com sinceridade.
          </p>
        </div>
      </div>

      <div className="mt-auto space-y-2 pt-6">
        {vagas <= 0 && (
          <p role="alert" className="form-error">
            {grupoNome} já tem {maxMembros} membros, o máximo permitido.
          </p>
        )}
        {aceitar.isError && (
          <p role="alert" className="form-error">
            {getErrorMessage(aceitar.error)}
          </p>
        )}

        {logado ? (
          <>
            <button
              type="button"
              className="btn-primary w-full"
              disabled={aceitar.isPending || vagas <= 0}
              onClick={() =>
                aceitar.mutate(token, {
                  onSuccess: ({ grupoId }) =>
                    void navigate(`/grupos/${grupoId}/inicio`, {
                      replace: true,
                    }),
                })
              }
            >
              {aceitar.isPending ? "Entrando…" : "Aceitar convite"}
            </button>
            <Link to="/" className="btn-ghost w-full">
              Agora não
            </Link>
          </>
        ) : (
          vagas > 0 && (
            <>
              <Link
                to="/criar-conta"
                state={{ conviteToken: token }}
                className="btn-primary w-full"
              >
                Criar conta e entrar
              </Link>
              <Link
                to="/entrar"
                state={{
                  redirectTo: `/convite#${token}`,
                  conviteToken: token,
                }}
                className="btn-secondary w-full"
              >
                Já tenho conta
              </Link>
            </>
          )
        )}
      </div>
    </main>
  );
}
