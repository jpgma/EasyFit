import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { gruposKey, shouldRetry } from "@/features/grupos";
import { deleteEmpty, getJson, postJson } from "@/services/api";

export type ConviteCriado = {
  id: string;
  token: string;
  expiresAt: string;
};

export type Convite = {
  id: string;
  criadoPor: string;
  createdAt: string;
  expiresAt: string;
};

export type ConviteConsulta = {
  grupoNome: string;
  convidadoPor: string;
  totalMembros: number;
  maxMembros: number;
};

function convitesKey(grupoId: string) {
  return ["convites", grupoId] as const;
}

function convitesPath(grupoId: string) {
  return `/api/grupos/${grupoId}/convites`;
}

/**
 * O token vai no fragmento do link, que o navegador nunca envia ao servidor:
 * assim ele não aparece em logs de acesso nem no cabeçalho Referer.
 */
export function buildConviteLink(token: string): string {
  return `${window.location.origin}/convite#${token}`;
}

/** Aceita tanto o link inteiro, colado de uma mensagem, quanto só o token. */
export function extractConviteToken(input: string): string {
  const value = input.trim();
  const hashIndex = value.indexOf("#");
  return hashIndex === -1 ? value : value.slice(hashIndex + 1);
}

export function useConsultarConvite(token: string) {
  return useQuery({
    // É um POST só para que o token nunca faça parte de uma URL.
    queryKey: ["convites", "consulta", token],
    queryFn: () =>
      postJson<ConviteConsulta>("/api/convites/consultar", { token }),
    enabled: token !== "",
    retry: shouldRetry,
  });
}

export function useConvites(grupoId: string, enabled: boolean) {
  return useQuery({
    queryKey: convitesKey(grupoId),
    queryFn: async ({ signal }) => {
      const { convites } = await getJson<{ convites: Convite[] }>(
        convitesPath(grupoId),
        signal,
      );
      return convites;
    },
    enabled,
    retry: shouldRetry,
  });
}

export function useCriarConvite(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => postJson<ConviteCriado>(convitesPath(grupoId)),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: convitesKey(grupoId) }),
  });
}

export function useRevogarConvite(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (conviteId: string) =>
      deleteEmpty(`${convitesPath(grupoId)}/${conviteId}`),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: convitesKey(grupoId) }),
  });
}

export function useAceitarConvite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (token: string) =>
      postJson<{ grupoId: string }>("/api/grupos/entrar", { token }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: gruposKey }),
  });
}
