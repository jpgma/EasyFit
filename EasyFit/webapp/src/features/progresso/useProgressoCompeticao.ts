import { useQuery } from "@tanstack/react-query";
import { shouldRetry } from "@/features/grupos";
import { getJson } from "@/services/api";
import type { Classificacao, HistoricoSemanal, ResumoGrupo } from "./types";

const progressoCompeticaoKey = ["progresso-competicao"] as const;

function classificacaoKey(desafioId: string) {
  return [...progressoCompeticaoKey, "classificacao", desafioId] as const;
}

function historicoKey(desafioId: string) {
  return [...progressoCompeticaoKey, "historico", desafioId] as const;
}

function resumoGrupoKey(grupoId: string) {
  return [...progressoCompeticaoKey, "resumo", grupoId] as const;
}

export function useClassificacao(desafioId: string) {
  return useQuery({
    queryKey: classificacaoKey(desafioId),
    queryFn: ({ signal }) =>
      getJson<Classificacao>(
        `/api/desafios/${desafioId}/classificacao`,
        signal,
      ),
    retry: shouldRetry,
  });
}

export function useHistoricoSemanal(desafioId: string) {
  return useQuery({
    queryKey: historicoKey(desafioId),
    queryFn: ({ signal }) =>
      getJson<HistoricoSemanal>(`/api/desafios/${desafioId}/historico`, signal),
    retry: shouldRetry,
  });
}

export function useResumoGrupo(grupoId: string, enabled = true) {
  return useQuery({
    queryKey: resumoGrupoKey(grupoId),
    queryFn: ({ signal }) =>
      getJson<ResumoGrupo>(`/api/grupos/${grupoId}/resumo`, signal),
    enabled,
    retry: shouldRetry,
  });
}
