import { Loader2 } from "lucide-react";
import { useState } from "react";
import { canRevisarAtividade, useGrupo } from "@/features/grupos";
import { situacaoVerificacaoLabels } from "@/features/atividades";
import { getErrorMessage } from "@/services/api";
import { tipoReacaoLabels } from "./labels";
import type { TipoReacao } from "./types";
import { useReacoes, useRegistrarReacao, useRevisarAtividade } from "./useReacoes";

const tiposReacao: TipoReacao[] = ["Kudos", "Suspeito", "Rejeitar"];

export function ReacoesAtividade({
  atividadeId,
  grupoId,
  situacaoVerificacao,
  voce,
}: {
  atividadeId: string;
  grupoId: string;
  situacaoVerificacao: string;
  voce: boolean;
}) {
  const reacoes = useReacoes(atividadeId);
  const registrar = useRegistrarReacao(atividadeId);
  const revisar = useRevisarAtividade(atividadeId);
  const grupo = useGrupo(grupoId);
  const [justificativa, setJustificativa] = useState("");
  const [decisao, setDecisao] = useState<"Verificada" | "Rejeitada">(
    "Verificada",
  );

  if (reacoes.isPending) {
    return (
      <section className="mt-6 flex items-center gap-2 text-sm text-muted">
        <Loader2 aria-hidden="true" className="h-4 w-4 animate-spin" />
        Carregando reações…
      </section>
    );
  }

  if (reacoes.isError) {
    return (
      <section className="mt-6">
        <p className="text-sm text-danger">{getErrorMessage(reacoes.error)}</p>
      </section>
    );
  }

  const {
    totalRevisores,
    kudos,
    suspeito,
    rejeitar,
    suaReacao,
    voceERevisor,
    janelaAberta,
    reacoes: lista,
  } = reacoes.data;

  const podeReagir =
    !voce &&
    voceERevisor &&
    janelaAberta &&
    situacaoVerificacao === "AguardandoFeedback";

  const admin =
    grupo.data && canRevisarAtividade(grupo.data.papel)
      ? grupo.data.papel
      : null;
  const mostrarRevisaoAdmin =
    admin !== null && situacaoVerificacao === "EmRevisao";

  return (
    <section className="mt-6">
      <h2 className="mb-3 text-base font-bold">Reações</h2>

      {totalRevisores === 0 ? (
        <p className="text-sm text-muted">
          Não havia revisores no grupo quando a atividade foi registrada; ela foi
          verificada automaticamente.
        </p>
      ) : (
        <>
          <p className="text-sm text-muted">
            {kudos} kudos · {suspeito} suspeito · {rejeitar} rejeição
            {rejeitar === 1 ? "" : "ões"} · {totalRevisores} revisor
            {totalRevisores === 1 ? "" : "es"}
            {!janelaAberta && situacaoVerificacao === "AguardandoFeedback" && (
              <span> · prazo encerrado</span>
            )}
          </p>

          {podeReagir && (
            <div className="mt-4 flex flex-wrap gap-2">
              {tiposReacao.map((tipo) => (
                <button
                  key={tipo}
                  type="button"
                  disabled={registrar.isPending}
                  className={
                    suaReacao === tipo ? "btn-primary" : "btn-secondary"
                  }
                  onClick={() => registrar.mutate(tipo)}
                >
                  {tipoReacaoLabels[tipo]}
                </button>
              ))}
            </div>
          )}

          {registrar.isError && (
            <p className="mt-2 text-sm text-danger">
              {getErrorMessage(registrar.error)}
            </p>
          )}

          {lista.length > 0 && (
            <ul className="card mt-4 divide-y divide-border">
              {lista.map((item) => (
                <li
                  key={item.usuarioId}
                  className="flex items-center justify-between gap-3 px-4 py-3 text-sm"
                >
                  <span className="truncate font-medium">
                    {item.nomeExibicao}
                  </span>
                  <span className="chip shrink-0">
                    {tipoReacaoLabels[item.tipo]}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}

      {mostrarRevisaoAdmin && (
        <div className="card mt-6 p-4">
          <h3 className="text-sm font-bold">Revisão administrativa</h3>
          <p className="mt-1 text-sm text-muted">
            Situação: {situacaoVerificacaoLabels.EmRevisao}
          </p>
          <label className="mt-4 block text-sm">
            <span className="text-muted">Decisão</span>
            <select
              className="input mt-1 w-full"
              value={decisao}
              onChange={(e) =>
                setDecisao(e.target.value as "Verificada" | "Rejeitada")
              }
            >
              <option value="Verificada">Aprovar atividade</option>
              <option value="Rejeitada">Rejeitar atividade</option>
            </select>
          </label>
          <label className="mt-3 block text-sm">
            <span className="text-muted">Justificativa</span>
            <textarea
              className="input mt-1 min-h-20 w-full resize-y"
              maxLength={500}
              value={justificativa}
              onChange={(e) => setJustificativa(e.target.value)}
              placeholder="Explique brevemente a decisão."
            />
          </label>
          <button
            type="button"
            className="btn-primary mt-4 w-full"
            disabled={
              revisar.isPending || justificativa.trim().length === 0
            }
            onClick={() =>
              revisar.mutate({
                decisao,
                justificativa: justificativa.trim(),
              })
            }
          >
            {revisar.isPending ? "Registrando…" : "Registrar decisão"}
          </button>
          {revisar.isError && (
            <p className="mt-2 text-sm text-danger">
              {getErrorMessage(revisar.error)}
            </p>
          )}
        </div>
      )}
    </section>
  );
}
