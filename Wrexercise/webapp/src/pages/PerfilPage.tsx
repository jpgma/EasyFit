import {
  Award,
  ChevronRight,
  Flame,
  Plus,
  Settings,
  Timer,
  Trophy,
  type LucideIcon,
} from "lucide-react";
import { Link } from "react-router";
import { Avatar } from "@/components/Avatar";
import { HistoryChart } from "@/components/HistoryChart";
import { PageHeader, SectionTitle } from "@/components/PageHeader";
import { useSessao } from "@/features/autenticacao";
import { ConviteForm } from "@/features/convites";
import { formatarSemanaCurta } from "@/features/desafios";
import { papelLabels, useGrupos } from "@/features/grupos";
import { SinoNotificacoes } from "@/features/notificacoes";
import { useMeuPerfil } from "@/features/usuarios";
import { getErrorMessage } from "@/services/api";

function Numero({
  icon: Icon,
  valor,
  rotulo,
}: {
  icon: LucideIcon;
  valor: string;
  rotulo: string;
}) {
  return (
    <div className="card p-3">
      <Icon aria-hidden="true" className="size-4 text-primary" />
      <p className="mt-2 font-display text-xl font-bold">{valor}</p>
      <p className="text-2xs leading-tight text-muted">{rotulo}</p>
    </div>
  );
}

export function PerfilPage() {
  const { data: usuario } = useSessao();
  const perfil = useMeuPerfil();
  const grupos = useGrupos();

  if (!usuario) return null;

  return (
    <div className="mx-auto max-w-4xl">
      <PageHeader
        title="Perfil"
        action={
          <div className="flex gap-2">
            <SinoNotificacoes />
            <Link
              to="/perfil/configuracoes"
              aria-label="Configurações"
              className="grid size-10 place-items-center rounded-full bg-surface shadow-card tap"
            >
              <Settings aria-hidden="true" className="size-5" />
            </Link>
          </div>
        }
      />

      <div className="grid gap-x-6 lg:grid-cols-2">
        <div>
          <div className="card flex items-center gap-4 p-5">
            <Avatar nome={usuario.nomeExibicao} id={usuario.id} size={72} />
            <div className="min-w-0">
              <p className="truncate font-display text-xl font-bold">
                {usuario.nomeExibicao}
              </p>
              <p className="truncate text-sm text-muted">{usuario.email}</p>
              {perfil.data && (
                <p className="mt-1 text-xs text-muted">
                  No Wrexercise desde{" "}
                  {new Date(perfil.data.membroDesde).toLocaleDateString(
                    "pt-BR",
                    {
                      month: "short",
                      year: "numeric",
                    },
                  )}
                </p>
              )}
            </div>
          </div>

          {perfil.isError && (
            <p role="alert" className="form-error mt-4">
              {getErrorMessage(perfil.error)}
            </p>
          )}
          {perfil.data && (
            <>
              <div className="mt-4 grid grid-cols-3 gap-3">
                <Numero
                  icon={Trophy}
                  valor={String(perfil.data.totalTreinos)}
                  rotulo="treinos verificados"
                />
                <Numero
                  icon={Timer}
                  valor={`${Math.round(perfil.data.totalSegundos / 3600)}h`}
                  rotulo="em movimento"
                />
                <Numero
                  icon={Flame}
                  valor={String(perfil.data.melhorSequencia)}
                  rotulo="melhor sequência de semanas"
                />
              </div>
              <div className="mt-4">
                <HistoryChart
                  titulo="Consistência semanal"
                  colunas={perfil.data.semanas.map((semana) => ({
                    chave: semana.inicio,
                    rotulo: formatarSemanaCurta(semana.inicio),
                    valor: semana.sessoesVerificadas,
                    provisorio: semana.sessoesProvisorias,
                  }))}
                />
              </div>
            </>
          )}
        </div>

        <div>
          {perfil.data && (
            <>
              <SectionTitle>Conquistas</SectionTitle>
              {perfil.data.conquistas.length === 0 ? (
                <p className="card p-4 text-sm text-muted">
                  Continue treinando: suas conquistas aparecem aqui.
                </p>
              ) : (
                <ul className="space-y-3">
                  {perfil.data.conquistas.map((conquista) => (
                    <li
                      key={conquista.codigo}
                      className="card flex items-center gap-3 p-4"
                    >
                      <div className="grid size-11 shrink-0 place-items-center rounded-xl bg-accent text-on-accent">
                        <Award aria-hidden="true" className="size-5" />
                      </div>
                      <div className="min-w-0">
                        <p className="text-sm font-semibold">
                          {conquista.titulo}
                        </p>
                        <p className="text-xs text-muted">
                          {conquista.descricao}
                        </p>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </>
          )}

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
                      <p className="truncate text-sm font-semibold">
                        {grupo.nome}
                      </p>
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
        </div>
      </div>
    </div>
  );
}
