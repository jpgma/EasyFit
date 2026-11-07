import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { gruposKey, shouldRetry } from "@/features/grupos";
import { getJson, postJson, putJson } from "@/services/api";
import type {
  ComentarioRevisao,
  ReacoesDetalhe,
  RevisaoAtividadeData,
  TipoReacao,
} from "./types";

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
      // As listas e a fila ficam sob o grupo; a classificação, em uma chave própria.
      void queryClient.invalidateQueries({ queryKey: gruposKey });
      void queryClient.invalidateQueries({
        queryKey: ["progresso-competicao"],
      });
    },
  });
}

function comentariosKey(atividadeId: string) {
  return ["atividades", atividadeId, "comentarios"] as const;
}

export function useComentarios(atividadeId: string) {
  return useQuery({
    queryKey: comentariosKey(atividadeId),
    queryFn: ({ signal }) =>
      getJson<{ comentarios: ComentarioRevisao[] }>(
        `/api/atividades/${atividadeId}/comentarios`,
        signal,
      ),
    retry: shouldRetry,
  });
}

export function useComentar(atividadeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (texto: string) =>
      postJson<ComentarioRevisao>(
        `/api/atividades/${atividadeId}/comentarios`,
        { texto },
      ),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: comentariosKey(atividadeId),
      }),
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
      // As listas e a fila ficam sob o grupo; a classificação, em uma chave própria.
      void queryClient.invalidateQueries({ queryKey: gruposKey });
      void queryClient.invalidateQueries({
        queryKey: ["progresso-competicao"],
      });
    },
  });
}
