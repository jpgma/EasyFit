import { ChevronRight } from "lucide-react";
import { Link } from "react-router";
import { Avatar } from "@/components/Avatar";
import {
  formatarDataHora,
  formatarDuracao,
  situacaoVerificacaoLabels,
  tipoAtividadeLabels,
  type AtividadeResumo,
} from "@/features/atividades";

export function FeedAtividade({
  atividade,
  mostrarGrupo = false,
}: {
  atividade: AtividadeResumo;
  mostrarGrupo?: boolean;
}) {
  const {
    id,
    nomeExibicao,
    tipo,
    duracaoSegundos,
    startedAt,
    situacaoVerificacao,
    grupoNome,
  } = atividade;

  return (
    <Link
      to={`/atividades/${id}`}
      className="card flex items-center gap-3 p-4 transition-colors hover:bg-surface-muted"
    >
      <Avatar nome={nomeExibicao} />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-bold">
          {nomeExibicao}
          {mostrarGrupo && grupoNome && (
            <span className="font-normal text-muted"> · {grupoNome}</span>
          )}
        </p>
        <p className="text-sm text-muted">
          {tipoAtividadeLabels[tipo]} · {formatarDuracao(duracaoSegundos)}
        </p>
        <p className="text-2xs text-muted">{formatarDataHora(startedAt)}</p>
      </div>
      <span className="chip hidden sm:inline">
        {situacaoVerificacaoLabels[situacaoVerificacao]}
      </span>
      <ChevronRight
        aria-hidden="true"
        className="h-5 w-5 shrink-0 text-muted"
      />
    </Link>
  );
}
