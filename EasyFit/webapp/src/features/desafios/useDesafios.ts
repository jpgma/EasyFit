import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { gruposKey, shouldRetry, type Papel } from "@/features/grupos";
import { getJson, postEmpty, postJson } from "@/services/api";

export type SituacaoDesafio = "Agendado" | "Ativo" | "Encerrado" | "Cancelado";

export type Desafio = {
  id: string;
  grupoId: string;
  nome: string;
  descricao: string | null;
  dataInicio: string;
  dataFim: string;
  duracaoMinimaSegundos: number;
  sessoesPorSemana: number;
  situacao: SituacaoDesafio;
  fusoHorario: string;
  papel: Papel;
  participando: boolean;
  totalParticipantes: number;
};

export type ProgressoParticipante = {
  usuarioId: string;
  nomeExibicao: string;
  sessoesVerificadas: number;
  sessoesProvisorias: number;
  metaCumprida: boolean;
};

export type Progresso = {
  sessoesPorSemana: number;
  totalSemanas: number;
  semanaAtual: { numero: number; inicio: string; fim: string } | null;
  participantes: ProgressoParticipante[];
};

export type CriarDesafioData = {
  nome: string;
  descricao: string;
  dataInicio: string;
  dataFim: string;
  duracaoMinimaSegundos: number;
  sessoesPorSemana: number;
};

const desafiosKey = ["desafios"] as const;

function desafioKey(desafioId: string) {
  return [...desafiosKey, desafioId] as const;
}

// Dentro da chave do grupo: quando o grupo muda, a lista de desafios dele é buscada de novo.
function desafiosDoGrupoKey(grupoId: string) {
  return [...gruposKey, grupoId, "desafios"] as const;
}

export function useDesafios(grupoId: string) {
  return useQuery({
    queryKey: desafiosDoGrupoKey(grupoId),
    queryFn: async ({ signal }) => {
      const { desafios } = await getJson<{ desafios: Desafio[] }>(
        `/api/grupos/${grupoId}/desafios`,
        signal,
      );
      return desafios;
    },
    retry: shouldRetry,
  });
}

export function useDesafio(desafioId: string, enabled = true) {
  return useQuery({
    queryKey: desafioKey(desafioId),
    queryFn: ({ signal }) =>
      getJson<Desafio>(`/api/desafios/${desafioId}`, signal),
    enabled,
    retry: shouldRetry,
  });
}

export function useProgresso(desafioId: string) {
  return useQuery({
    queryKey: [...desafioKey(desafioId), "progresso"],
    queryFn: ({ signal }) =>
      getJson<Progresso>(`/api/desafios/${desafioId}/progresso`, signal),
    retry: shouldRetry,
  });
}

export function useCriarDesafio(grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CriarDesafioData) =>
      postJson<Desafio>(`/api/grupos/${grupoId}/desafios`, data),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: desafiosDoGrupoKey(grupoId),
      }),
  });
}

export function useParticipar(desafioId: string, grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => postEmpty(`/api/desafios/${desafioId}/participar`),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: desafioKey(desafioId) }),
        queryClient.invalidateQueries({
          queryKey: desafiosDoGrupoKey(grupoId),
        }),
      ]),
  });
}

export function useCancelarDesafio(desafioId: string, grupoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => postEmpty(`/api/desafios/${desafioId}/cancelar`),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: desafioKey(desafioId) }),
        queryClient.invalidateQueries({
          queryKey: desafiosDoGrupoKey(grupoId),
        }),
      ]),
  });
}
