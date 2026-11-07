import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { gruposKey, shouldRetry } from "@/features/grupos";
import { deleteEmpty, getJson, postJson } from "@/services/api";

export type Convite = {
  token: string;
  criadoPor: string;
  createdAt: string;
  expiresAt: string;
};

export type ConviteConsulta = {
  grupoNome: string;
  convidadoPor: string;
  totalMembros: number;
  maxMembros: number;
  desafioNome: string | null;
  membros: string[];
};

function conviteKey(grupoId: string) {
  return ["convites", grupoId] as const;
}

function convitePath(grupoId: string) {
  return `/api/grupos/${grupoId}/convite`;
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

export function useConvite(grupoId: string, enabled: boolean) {
  return useQuery({
    queryKey: conviteKey(grupoId),
    queryFn: async ({ signal }) => {
      const { convite } = await getJson<{ convite: Convite | null }>(
        convitePath(grupoId),
        signal,
      );
      return convite;
    },
    enabled,
    retry: shouldRetry,
  });
}

export function useGerarConvite(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => postJson<Convite>(convitePath(grupoId)),
    onSuccess: (convite) =>
      queryClient.setQueryData(conviteKey(grupoId), convite),
  });
}

export function useRevogarConvite(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => deleteEmpty(convitePath(grupoId)),
    onSuccess: () => queryClient.setQueryData(conviteKey(grupoId), null),
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
