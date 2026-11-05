import {
  Check,
  ChevronRight,
  CloudOff,
  LogOut,
  Plus,
  Target,
  Ticket,
  Users,
} from "lucide-react";
import { useState } from "react";
import { Link, useNavigate } from "react-router";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { EmptyState } from "@/components/EmptyState";
import { TextField } from "@/components/TextField";
import { useSair, useSessao } from "@/features/autenticacao";
import { extractConviteToken } from "@/features/convites";
import { useAtividadesRecentes } from "@/features/atividades";
import { FeedAtividade } from "@/features/feed";
import { situacaoLabels } from "@/features/desafios";
import { papelLabels, useGrupos, type GrupoResumo } from "@/features/grupos";
import { useResumoGrupo } from "@/features/progresso";
import { ProgressBar } from "@/components/ProgressBar";
import { getErrorMessage } from "@/services/api";

function GrupoCard({ grupo }: { grupo: GrupoResumo }) {
  const resumo = useResumoGrupo(grupo.id);

  return (
    <li>
      <Link
        to={
          resumo.data?.desafio
            ? `/desafios/${resumo.data.desafio.id}`
            : `/grupos/${grupo.id}`
        }
        className="card block p-4 transition-colors hover:bg-surface-muted"
      >
        <div className="flex items-center gap-3">
          <div className="min-w-0 flex-1">
            <p className="truncate font-bold">{grupo.nome}</p>
            <p className="text-sm text-muted">
              {grupo.totalMembros}{" "}
              {grupo.totalMembros === 1 ? "membro" : "membros"} ·{" "}
              {papelLabels[grupo.papel]}
            </p>
          </div>
          <ChevronRight
            aria-hidden="true"
            className="h-5 w-5 shrink-0 text-muted"
          />
        </div>

        {resumo.isPending && (
          <p role="status" className="mt-3 text-2xs text-muted">
            Carregando desafio…
          </p>
        )}

        {resumo.isError && (
          <p role="alert" className="form-error mt-3 text-sm">
            {getErrorMessage(resumo.error)}
          </p>
        )}

        {resumo.data && !resumo.data.desafio && (
          <p className="mt-3 text-sm text-muted">
            Nenhum desafio neste grupo ainda.
          </p>
        )}

        {resumo.data?.desafio && (
          <div className="mt-4 border-t border-border pt-4">
            <div className="flex items-start gap-2">
              <Target
                aria-hidden="true"
                className="mt-0.5 h-4 w-4 shrink-0 text-muted"
              />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-semibold">
                  {resumo.data.desafio.nome}
                </p>
                <p className="text-2xs text-muted">
                  {situacaoLabels[
                    resumo.data.desafio.situacao as keyof typeof situacaoLabels
                  ] ?? resumo.data.desafio.situacao}
                </p>
              </div>
            </div>

            {resumo.data.meuProgresso && resumo.data.sessoesPorSemana && (
              <div className="mt-3">
                <div className="mb-1 flex items-center justify-between text-sm">
                  <span>
                    {resumo.data.meuProgresso.semanaNumero
                      ? `Semana ${resumo.data.meuProgresso.semanaNumero}`
                      : "Esta semana"}
                  </span>
                  <span className="flex items-center gap-1 font-semibold">
                    {resumo.data.meuProgresso.metaCumprida && (
                      <Check aria-label="Meta cumprida" className="h-4 w-4" />
                    )}
                    {resumo.data.meuProgresso.sessoesVerificadas}/
                    {resumo.data.sessoesPorSemana}
                  </span>
                </div>
                <ProgressBar
                  label="Seu progresso neste desafio"
                  valor={resumo.data.meuProgresso.sessoesVerificadas}
                  provisorio={resumo.data.meuProgresso.sessoesProvisorias}
                  max={resumo.data.sessoesPorSemana}
                />
              </div>
            )}

            {resumo.data.classificacao.length > 0 && (
              <ol className="mt-3 space-y-1 text-2xs text-muted">
                {resumo.data.classificacao.map((item) => (
                  <li key={item.posicao}>
                    {item.posicao}. {item.nomeExibicao} · {item.semanasCumpridas}{" "}
                    {item.semanasCumpridas === 1 ? "semana" : "semanas"}
                  </li>
                ))}
              </ol>
            )}
          </div>
        )}
      </Link>
    </li>
  );
}

export function InicioPage() {
  const { data: usuario } = useSessao();
  const sair = useSair();
  const grupos = useGrupos();
  const recentes = useAtividadesRecentes((grupos.data?.length ?? 0) > 0);
  const navigate = useNavigate();
  const [confirmingSair, setConfirmingSair] = useState(false);
  const [convite, setConvite] = useState("");

  if (!usuario) return null;

  const conviteToken = extractConviteToken(convite);

  return (
    <main className="mx-auto min-h-dvh max-w-2xl animate-fade-in pb-8">
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

      <section className="px-5 pt-6">
        <h2 className="mb-3 text-base font-bold">Seus grupos</h2>

        {grupos.isPending && (
          <p role="status" className="py-8 text-center text-sm text-muted">
            Carregando…
          </p>
        )}

        {grupos.isError && (
          <div className="card">
            <EmptyState
              icon={CloudOff}
              title="Não foi possível carregar"
              description={getErrorMessage(grupos.error)}
            />
          </div>
        )}

        {grupos.data?.length === 0 && (
          <div className="card">
            <EmptyState
              icon={Users}
              title="Você ainda não faz parte de um grupo"
              description="Crie um grupo para seus amigos ou use o convite que você recebeu."
            />
          </div>
        )}

        {grupos.data && grupos.data.length > 0 && (
          <ul className="space-y-3">
            {grupos.data.map((grupo) => (
              <GrupoCard key={grupo.id} grupo={grupo} />
            ))}
          </ul>
        )}

        <Link to="/grupos/novo" className="btn-secondary mt-4 w-full">
          <Plus aria-hidden="true" className="h-5 w-5" />
          Criar grupo
        </Link>
      </section>

      {grupos.data && grupos.data.length > 0 && (
        <section className="px-5 pt-8">
          <h2 className="mb-3 text-base font-bold">Últimas atividades</h2>

          {recentes.isPending && (
            <p role="status" className="py-6 text-center text-sm text-muted">
              Carregando…
            </p>
          )}

          {recentes.isError && (
            <p role="alert" className="form-error">
              {getErrorMessage(recentes.error)}
            </p>
          )}

          {recentes.data &&
            recentes.data.pages.every((p) => p.atividades.length === 0) && (
              <p className="card p-4 text-sm text-muted">
                Ainda não há atividades nos seus grupos.
              </p>
            )}

          {recentes.data &&
            recentes.data.pages.some((p) => p.atividades.length > 0) && (
              <ul className="space-y-3">
                {recentes.data.pages.flatMap((pagina) =>
                  pagina.atividades.map((atividade) => (
                    <li key={atividade.id}>
                      <FeedAtividade atividade={atividade} mostrarGrupo />
                    </li>
                  )),
                )}
              </ul>
            )}
        </section>
      )}

      <section className="px-5 pt-8">
        <h2 className="mb-3 text-base font-bold">Tenho um convite</h2>
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            if (conviteToken) void navigate(`/convite#${conviteToken}`);
          }}
          className="space-y-3"
        >
          <TextField
            label="Link do convite"
            icon={Ticket}
            type="text"
            autoComplete="off"
            placeholder="Cole aqui o link que você recebeu"
            value={convite}
            onChange={(event) => setConvite(event.target.value)}
          />
          <button
            type="submit"
            className="btn-primary w-full"
            disabled={!conviteToken}
          >
            Continuar
          </button>
        </form>
      </section>

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
