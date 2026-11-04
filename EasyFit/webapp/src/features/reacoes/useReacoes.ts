import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { shouldRetry } from "@/features/grupos";
import { getJson, postJson, putJson } from "@/services/api";
import type { ReacoesDetalhe, RevisaoAtividadeData, TipoReacao } from "./types";

function reacoesKey(atividadeId: string) {
  return ["atividades", atividadeId, "reacoes"] as const;
}

export function useReacoes(atividadeId: string) {
  return useQuery({
    queryKey: reacoesKey(atividadeId),
    queryFn: ({ signal }) =>
      getJson<ReacoesDetalhe>(`/api/atividades/${atividadeId}/reacoes`, signal),
    retry: shouldRetry,
  });
}

export function useRegistrarReacao(atividadeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (tipo: TipoReacao) =>
      putJson<ReacoesDetalhe>(`/api/atividades/${atividadeId}/reacao`, {
        tipo,
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(reacoesKey(atividadeId), data);
      void queryClient.invalidateQueries({
        queryKey: ["atividades", atividadeId],
      });
      void queryClient.invalidateQueries({ queryKey: ["desafios"] });
      void queryClient.invalidateQueries({ queryKey: ["atividades"] });
    },
  });
}

export function useRevisarAtividade(atividadeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: RevisaoAtividadeData) =>
      postJson<{ situacaoVerificacao: string }>(
        `/api/atividades/${atividadeId}/revisao`,
        data,
      ),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: reacoesKey(atividadeId),
      });
      void queryClient.invalidateQueries({
        queryKey: ["atividades", atividadeId],
      });
      void queryClient.invalidateQueries({ queryKey: ["desafios"] });
      void queryClient.invalidateQueries({ queryKey: ["atividades"] });
    },
  });
}
