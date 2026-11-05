import { Camera, Clock } from "lucide-react";
import { Link } from "react-router";
import { Avatar } from "@/components/Avatar";
import {
  formatarDataRelativa,
  formatarDuracao,
  SituacaoBadge,
  tipoAtividadeLabels,
  type AtividadeResumo,
} from "@/features/atividades";

export function FeedAtividade({
  atividade,
  duracaoMinimaSegundos,
}: {
  atividade: AtividadeResumo;
  /** A do desafio atual do grupo, para indicar o que não conta na meta. */
  duracaoMinimaSegundos?: number;
}) {
  const {
    id,
    usuarioId,
    nomeExibicao,
    tipo,
    duracaoSegundos,
    startedAt,
    situacaoVerificacao,
    totalEvidencias,
  } = atividade;
  const abaixoDoMinimo =
    duracaoMinimaSegundos !== undefined &&
    duracaoSegundos < duracaoMinimaSegundos;

  return (
    <Link to={`/atividades/${id}`} className="card block p-4 tap">
      <div className="flex items-start gap-3">
        <Avatar nome={nomeExibicao} id={usuarioId} />
        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-2">
            <p className="truncate text-sm font-semibold">{nomeExibicao}</p>
            <span className="shrink-0 text-xs text-muted">
              {formatarDataRelativa(startedAt)}
            </span>
          </div>
          <p className="mt-0.5 flex flex-wrap items-center gap-x-1.5 text-sm text-muted">
            {tipoAtividadeLabels[tipo]} ·
            <Clock aria-hidden="true" className="size-3.5" />
            {formatarDuracao(duracaoSegundos)}
            {abaixoDoMinimo && (
              <span className="font-medium text-on-warning-soft">
                · abaixo do mínimo
              </span>
            )}
          </p>
          <div className="mt-3 flex items-center justify-between gap-2">
            <SituacaoBadge situacao={situacaoVerificacao} />
            {totalEvidencias > 0 && (
              <span className="flex items-center gap-1 text-xs text-muted">
                <Camera aria-hidden="true" className="size-3.5" />
                {totalEvidencias}
                <span className="sr-only">
                  {totalEvidencias === 1 ? "comprovante" : "comprovantes"}
                </span>
              </span>
            )}
          </div>
        </div>
      </div>
    </Link>
  );
}
