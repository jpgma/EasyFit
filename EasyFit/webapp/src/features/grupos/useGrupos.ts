import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ApiError,
  deleteEmpty,
  getJson,
  postEmpty,
  postJson,
  postJsonEmpty,
  putJsonEmpty,
} from "@/services/api";

export type Papel = "Proprietario" | "Administrador" | "Membro";

export type GrupoResumo = {
  id: string;
  nome: string;
  papel: Papel;
  totalMembros: number;
};

export type Grupo = GrupoResumo & {
  fusoHorario: string;
  maxMembros: number;
};

export type Membro = {
  usuarioId: string;
  nomeExibicao: string;
  papel: Papel;
  joinedAt: string;
};

export type CriarGrupoData = {
  nome: string;
  fusoHorario: string;
};

const basePath = "/api/grupos";

export const gruposKey = ["grupos"] as const;

function grupoKey(grupoId: string) {
  return [...gruposKey, grupoId] as const;
}

/** Erros que a API já decidiu, como 404 e 403, não melhoram com uma nova tentativa. */
export function shouldRetry(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError && error.status < 500) return false;
  return failureCount < 1;
}

export function useGrupos() {
  return useQuery({
    queryKey: gruposKey,
    queryFn: async ({ signal }) => {
      const { grupos } = await getJson<{ grupos: GrupoResumo[] }>(
        basePath,
        signal,
      );
      return grupos;
    },
    retry: shouldRetry,
  });
}

export function useGrupo(grupoId: string) {
  return useQuery({
    queryKey: grupoKey(grupoId),
    queryFn: ({ signal }) => getJson<Grupo>(`${basePath}/${grupoId}`, signal),
    retry: shouldRetry,
  });
}

export function useMembros(grupoId: string) {
  return useQuery({
    queryKey: [...grupoKey(grupoId), "membros"],
    queryFn: async ({ signal }) => {
      const { membros } = await getJson<{ membros: Membro[] }>(
        `${basePath}/${grupoId}/membros`,
        signal,
      );
      return membros;
    },
    retry: shouldRetry,
  });
}

export function useCriarGrupo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CriarGrupoData) => postJson<Grupo>(basePath, data),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: gruposKey, exact: true }),
  });
}

export function useAlterarPapel(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      usuarioId,
      papel,
    }: {
      usuarioId: string;
      papel: Exclude<Papel, "Proprietario">;
    }) =>
      putJsonEmpty(`${basePath}/${grupoId}/membros/${usuarioId}/papel`, {
        papel,
      }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: grupoKey(grupoId) }),
  });
}

export function useRemoverMembro(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (usuarioId: string) =>
      deleteEmpty(`${basePath}/${grupoId}/membros/${usuarioId}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: gruposKey }),
  });
}

export function useTransferirGrupo(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (usuarioId: string) =>
      postJsonEmpty(`${basePath}/${grupoId}/transferir`, { usuarioId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: gruposKey }),
  });
}

export function useSairDoGrupo(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => postEmpty(`${basePath}/${grupoId}/sair`),
    // Só a lista: a tela do grupo ainda está aberta, e sem acesso uma nova busca dos dados dele daria erro.
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: gruposKey, exact: true }),
  });
}
