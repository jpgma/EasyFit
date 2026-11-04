import { ArrowLeft, CloudOff, LogOut, UserPlus, Users } from "lucide-react";
import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { Avatar } from "@/components/Avatar";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { EmptyState } from "@/components/EmptyState";
import { SplashScreen } from "@/components/SplashScreen";
import { useSessao } from "@/features/autenticacao";
import {
  canAlterarPapel,
  canConvidar,
  canRemover,
  papelLabels,
  useAlterarPapel,
  useGrupo,
  useMembros,
  useRemoverMembro,
  useSairDoGrupo,
  useTransferirGrupo,
  type Membro,
} from "@/features/grupos";
import { ApiError, getErrorMessage } from "@/services/api";

type Acao =
  | { tipo: "promover" | "rebaixar" | "remover" | "transferir"; membro: Membro }
  | { tipo: "sair" };

function getTextosDaAcao(acao: Acao, grupoNome: string) {
  if (acao.tipo === "sair") {
    return {
      title: "Sair do grupo?",
      message: `Você deixará de ver ${grupoNome}. Para voltar, precisará de um novo convite.`,
      confirmLabel: "Sair do grupo",
    };
  }

  const nome = acao.membro.nomeExibicao;
  switch (acao.tipo) {
    case "promover":
      return {
        title: `Tornar ${nome} administrador?`,
        message: "Administradores podem convidar pessoas e remover membros.",
        confirmLabel: "Tornar administrador",
      };
    case "rebaixar":
      return {
        title: `Tornar ${nome} membro?`,
        message:
          "Essa pessoa deixará de poder convidar e remover. Os convites que ela já criou continuam válidos.",
        confirmLabel: "Tornar membro",
      };
    case "remover":
      return {
        title: `Remover ${nome} do grupo?`,
        message:
          "Essa pessoa perderá o acesso ao grupo e os convites pendentes dela serão cancelados.",
        confirmLabel: "Remover",
      };
    case "transferir":
      return {
        title: `Transferir o grupo para ${nome}?`,
        message:
          "Essa pessoa passará a ser a proprietária e você se tornará administrador. Só ela poderá desfazer isso.",
        confirmLabel: "Transferir",
      };
  }
}

export function GrupoPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const { data: usuario } = useSessao();
  const grupo = useGrupo(id);
  const membros = useMembros(id);
  const alterarPapel = useAlterarPapel(id);
  const remover = useRemoverMembro(id);
  const transferir = useTransferirGrupo(id);
  const sair = useSairDoGrupo(id);
  const [acao, setAcao] = useState<Acao | null>(null);

  if (grupo.isPending) return <SplashScreen />;

  if (grupo.isError) {
    const notFound =
      grupo.error instanceof ApiError && grupo.error.status === 404;
    return (
      <main className="flex min-h-dvh flex-col items-center justify-center">
        <EmptyState
          icon={notFound ? Users : CloudOff}
          title={
            notFound ? "Grupo não encontrado" : "Não foi possível carregar"
          }
          description={
            notFound
              ? "Este grupo não existe ou você não faz parte dele."
              : getErrorMessage(grupo.error)
          }
        />
        <Link to="/" className="btn-secondary">
          Ir para o início
        </Link>
      </main>
    );
  }

  const { nome, papel, totalMembros, maxMembros, fusoHorario } = grupo.data;

  const mutacaoAtiva = !acao
    ? undefined
    : acao.tipo === "promover" || acao.tipo === "rebaixar"
      ? alterarPapel
      : acao.tipo === "remover"
        ? remover
        : acao.tipo === "transferir"
          ? transferir
          : sair;

  const fecharDialogo = () => {
    alterarPapel.reset();
    remover.reset();
    transferir.reset();
    sair.reset();
    setAcao(null);
  };

  const confirmar = () => {
    if (!acao) return;
    const options = { onSuccess: fecharDialogo };
    switch (acao.tipo) {
      case "promover":
      case "rebaixar":
        alterarPapel.mutate(
          {
            usuarioId: acao.membro.usuarioId,
            papel: acao.tipo === "promover" ? "Administrador" : "Membro",
          },
          options,
        );
        break;
      case "remover":
        remover.mutate(acao.membro.usuarioId, options);
        break;
      case "transferir":
        transferir.mutate(acao.membro.usuarioId, options);
        break;
      case "sair":
        sair.mutate(undefined, {
          onSuccess: () => void navigate("/", { replace: true }),
        });
        break;
    }
  };

  const textos = acao
    ? getTextosDaAcao(acao, nome)
    : { title: "", message: "", confirmLabel: "" };

  return (
    <main className="mx-auto min-h-dvh max-w-2xl animate-fade-in px-5 py-6">
      <Link to="/" className="btn-ghost -ml-2">
        <ArrowLeft aria-hidden="true" className="h-5 w-5" />
        Início
      </Link>

      <header className="card mt-4 p-5">
        <h1 className="text-xl font-bold break-words">{nome}</h1>
        <p className="mt-1 flex items-center gap-1.5 text-sm text-muted">
          <Users aria-hidden="true" className="h-4 w-4 shrink-0" />
          {totalMembros} de {maxMembros} membros
        </p>
        <p className="mt-1 text-sm text-muted">
          Fuso horário: {fusoHorario.replaceAll("_", " ")}
        </p>
      </header>

      {canConvidar(papel) && (
        <Link
          to={`/grupos/${id}/convidar`}
          className="btn-secondary mt-4 w-full"
        >
          <UserPlus aria-hidden="true" className="h-5 w-5" />
          Convidar amigos
        </Link>
      )}

      <section className="mt-6">
        <h2 className="mb-3 text-base font-bold">Membros</h2>

        {membros.isPending && (
          <p role="status" className="py-8 text-center text-sm text-muted">
            Carregando…
          </p>
        )}

        {membros.isError && (
          <p role="alert" className="form-error">
            {getErrorMessage(membros.error)}
          </p>
        )}

        {membros.data && (
          <ul className="card divide-y divide-border">
            {membros.data.map((membro) => {
              const voce = membro.usuarioId === usuario?.id;
              const podeAlterar = canAlterarPapel(papel, membro.papel);
              const podeRemover = canRemover(papel, membro.papel);
              return (
                <li key={membro.usuarioId} className="p-4">
                  <div className="flex items-center gap-3">
                    <Avatar nome={membro.nomeExibicao} />
                    <p className="min-w-0 flex-1 truncate text-sm font-bold">
                      {membro.nomeExibicao}
                      {voce && (
                        <span className="font-normal text-muted"> (você)</span>
                      )}
                    </p>
                    <span className="chip">{papelLabels[membro.papel]}</span>
                  </div>

                  {(podeAlterar || podeRemover) && (
                    <div className="mt-2 flex flex-wrap justify-end gap-1">
                      {podeAlterar && membro.papel === "Membro" && (
                        <button
                          type="button"
                          className="btn-ghost text-sm"
                          onClick={() => setAcao({ tipo: "promover", membro })}
                        >
                          Tornar administrador
                        </button>
                      )}
                      {podeAlterar && membro.papel === "Administrador" && (
                        <button
                          type="button"
                          className="btn-ghost text-sm"
                          onClick={() => setAcao({ tipo: "rebaixar", membro })}
                        >
                          Tornar membro
                        </button>
                      )}
                      {papel === "Proprietario" && (
                        <button
                          type="button"
                          className="btn-ghost text-sm"
                          onClick={() =>
                            setAcao({ tipo: "transferir", membro })
                          }
                        >
                          Transferir grupo
                        </button>
                      )}
                      {podeRemover && (
                        <button
                          type="button"
                          className="btn-ghost text-sm"
                          onClick={() => setAcao({ tipo: "remover", membro })}
                        >
                          Remover
                        </button>
                      )}
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <section className="mt-6">
        {papel === "Proprietario" ? (
          <p className="text-sm text-muted">
            Para sair do grupo, transfira a propriedade para outro membro. Se
            você estiver sozinho, ainda não é possível sair nem apagar o grupo.
          </p>
        ) : (
          <button
            type="button"
            className="btn-ghost w-full"
            onClick={() => setAcao({ tipo: "sair" })}
          >
            <LogOut aria-hidden="true" className="h-5 w-5" />
            Sair do grupo
          </button>
        )}
      </section>

      <ConfirmDialog
        open={acao !== null}
        title={textos.title}
        message={textos.message}
        confirmLabel={textos.confirmLabel}
        pending={mutacaoAtiva?.isPending ?? false}
        error={
          mutacaoAtiva?.isError
            ? getErrorMessage(mutacaoAtiva.error)
            : undefined
        }
        onConfirm={confirmar}
        onCancel={fecharDialogo}
      />
    </main>
  );
}
