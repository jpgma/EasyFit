import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { gruposKey, shouldRetry } from "@/features/grupos";
import { getJson, postFormData, postJson } from "@/services/api";
import type { Atividade, AtividadeResumo, CriarAtividadeData } from "./types";

const atividadesKey = ["atividades"] as const;

function atividadeKey(id: string) {
  return [...atividadesKey, id] as const;
}

function atividadesDoGrupoKey(grupoId: string) {
  return [...gruposKey, grupoId, "atividades"] as const;
}

export function useAtividadesFeed(grupoId: string) {
  return useInfiniteQuery({
    queryKey: atividadesDoGrupoKey(grupoId),
    queryFn: async ({ pageParam, signal }) => {
      const params = new URLSearchParams({ limite: "20" });
      if (pageParam) params.set("cursor", pageParam);
      const { atividades, proximoCursor } = await getJson<{
        atividades: AtividadeResumo[];
        proximoCursor: string | null;
      }>(`/api/grupos/${grupoId}/atividades?${params}`, signal);
      return { atividades, proximoCursor };
    },
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.proximoCursor,
    retry: shouldRetry,
  });
}

export function useAtividadesRecentes(enabled: boolean) {
  return useInfiniteQuery({
    queryKey: [...atividadesKey, "recentes"],
    queryFn: async ({ pageParam, signal }) => {
      const params = new URLSearchParams({ limite: "10" });
      if (pageParam) params.set("cursor", pageParam);
      const { atividades, proximoCursor } = await getJson<{
        atividades: AtividadeResumo[];
        proximoCursor: string | null;
      }>(`/api/atividades/recentes?${params}`, signal);
      return { atividades, proximoCursor };
    },
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.proximoCursor,
    enabled,
    retry: shouldRetry,
  });
}

export function useAtividade(id: string) {
  return useQuery({
    queryKey: atividadeKey(id),
    queryFn: ({ signal }) =>
      getJson<Atividade>(`/api/atividades/${id}`, signal),
    retry: shouldRetry,
  });
}

export function useCriarAtividade(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CriarAtividadeData) =>
      postJson<Atividade>(`/api/grupos/${grupoId}/atividades`, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: atividadesDoGrupoKey(grupoId),
      });
      void queryClient.invalidateQueries({
        queryKey: [...atividadesKey, "recentes"],
      });
      void queryClient.invalidateQueries({ queryKey: ["desafios"] });
    },
  });
}

export function useAnexarEvidencia(atividadeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (arquivo: File) => {
      const form = new FormData();
      form.append("arquivo", arquivo);
      return postFormData<{ id: string; ordem: number; url: string }>(
        `/api/atividades/${atividadeId}/evidencias`,
        form,
      );
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: atividadeKey(atividadeId),
      });
    },
  });
}
