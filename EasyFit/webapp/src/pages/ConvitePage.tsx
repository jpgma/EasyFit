import { ArrowRight, Check, CloudOff, Ticket, Users } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router";
import { BrandMark } from "@/components/BrandMark";
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
    <main className="flex min-h-dvh flex-col items-center justify-center">
      <EmptyState icon={icon} title={title} description={description} />
      <Link to="/" className="btn-secondary">
        Ir para o início
      </Link>
    </main>
  );
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
        description="Ele pode ter expirado, já ter sido usado ou ter sido cancelado. Peça um novo link a quem convidou você."
      />
    ) : (
      <ConviteIndisponivel
        icon={CloudOff}
        title="Não foi possível abrir o convite"
        description={getErrorMessage(consulta.error)}
      />
    );
  }

  const { grupoNome, convidadoPor, totalMembros, maxMembros } = consulta.data;
  const vagas = maxMembros - totalMembros;
  const logado = Boolean(sessao.data);

  return (
    <main className="flex min-h-dvh flex-col">
      <div className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center px-6 py-8">
        <div className="mb-8 animate-slide-up text-center">
          <p className="mb-3 text-sm font-semibold text-muted">
            Você foi convidado!
          </p>
          <div className="mb-4">
            <BrandMark />
          </div>
          <h1 className="text-2xl font-bold break-words">{grupoNome}</h1>
          <p className="mt-1.5 text-muted">Grupo privado de desafios fitness</p>
        </div>

        <div className="card mb-4 animate-slide-up space-y-3 p-5 [animation-delay:0.1s]">
          <p className="text-sm font-bold break-words">
            {convidadoPor} convidou você
          </p>
          <p className="flex items-center gap-2 text-sm text-muted">
            <Users aria-hidden="true" className="h-4 w-4 shrink-0" />
            {totalMembros} de {maxMembros} membros
          </p>
        </div>

        <p className="mb-6 flex animate-slide-up items-start gap-2.5 text-sm text-muted [animation-delay:0.2s]">
          <Check aria-hidden="true" className="mt-0.5 h-5 w-5 shrink-0" />
          Ao entrar, você concorda em registrar suas atividades e avaliar as dos
          colegas com sinceridade.
        </p>

        {vagas <= 0 && (
          <p role="alert" className="form-error mb-4">
            Este grupo atingiu o número máximo de membros.
          </p>
        )}

        {aceitar.isError && (
          <p role="alert" className="form-error mb-4">
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
                    void navigate(`/grupos/${grupoId}`, { replace: true }),
                })
              }
            >
              {aceitar.isPending ? "Entrando…" : "Aceitar convite e entrar"}
              <ArrowRight aria-hidden="true" className="h-5 w-5" />
            </button>
            <Link to="/" className="btn-ghost mt-2 w-full">
              Agora não
            </Link>
          </>
        ) : (
          vagas > 0 && (
            <div className="space-y-3">
              <Link
                to="/criar-conta"
                state={{ conviteToken: token }}
                className="btn-primary w-full"
              >
                Criar conta e entrar
                <ArrowRight aria-hidden="true" className="h-5 w-5" />
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
            </div>
          )
        )}
      </div>
    </main>
  );
}
