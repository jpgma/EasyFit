import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ApiError,
  getJson,
  postEmpty,
  postJson,
  postJsonEmpty,
  putJson,
} from "@/services/api";

export type Usuario = {
  id: string;
  nomeExibicao: string;
  email: string;
};

export type EntrarData = {
  email: string;
  senha: string;
};

export type RegistrarData = EntrarData & {
  nomeExibicao: string;
  conviteToken?: string;
};

type SessaoResponse = {
  usuario: Usuario;
};

const basePath = "/api/autenticacao";
const sessaoKey = ["autenticacao", "sessao"] as const;

/** O usuário logado, ou `null` quando não há sessão. */
export function useSessao() {
  return useQuery({
    queryKey: sessaoKey,
    queryFn: async ({ signal }): Promise<Usuario | null> => {
      try {
        const { usuario } = await getJson<SessaoResponse>(
          `${basePath}/sessao`,
          signal,
        );
        return usuario;
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) return null;
        throw error;
      }
    },
    staleTime: 5 * 60 * 1000,
    retry: 1,
  });
}

export function useEntrar() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: EntrarData) =>
      postJson<SessaoResponse>(`${basePath}/entrar`, data),
    onSuccess: ({ usuario }) => queryClient.setQueryData(sessaoKey, usuario),
  });
}

export function useRegistrar() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: RegistrarData) =>
      postJson<SessaoResponse>(`${basePath}/registrar`, data),
    onSuccess: ({ usuario }) => queryClient.setQueryData(sessaoKey, usuario),
  });
}

export function useSair() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => postEmpty(`${basePath}/sair`),
    onSuccess: () => {
      queryClient.setQueryData(sessaoKey, null);
      // Nada da conta anterior pode continuar disponível para a próxima.
      queryClient.removeQueries({
        predicate: (query) => query.queryKey[0] !== sessaoKey[0],
      });
    },
  });
}

export function useAlterarNome() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (nomeExibicao: string) =>
      putJson<SessaoResponse>("/api/usuarios/eu", { nomeExibicao }),
    onSuccess: ({ usuario }) => {
      queryClient.setQueryData(sessaoKey, usuario);
      // O nome aparece nas listas e na classificação de todos os grupos.
      void queryClient.invalidateQueries({
        predicate: (query) => query.queryKey[0] !== sessaoKey[0],
      });
    },
  });
}

export function useAlterarSenha() {
  return useMutation({
    mutationFn: (data: { senhaAtual: string; novaSenha: string }) =>
      postJsonEmpty("/api/usuarios/eu/senha", data),
  });
}

export function useRedefinirSenha() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { token: string; novaSenha: string }) =>
      postJsonEmpty(`${basePath}/redefinir-senha`, data),
    // Todas as sessões da conta foram encerradas, inclusive uma aberta neste navegador.
    onSuccess: () => {
      queryClient.setQueryData(sessaoKey, null);
      queryClient.removeQueries({
        predicate: (query) => query.queryKey[0] !== sessaoKey[0],
      });
    },
  });
}
