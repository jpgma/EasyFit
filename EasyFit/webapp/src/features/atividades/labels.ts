import type {
  SituacaoVerificacao,
  TipoAtividade,
  TipoComprovacao,
} from "./types";

export const tipoAtividadeLabels: Record<TipoAtividade, string> = {
  Musculacao: "Musculação",
  Corrida: "Corrida",
  Caminhada: "Caminhada",
  Ciclismo: "Ciclismo",
  Natacao: "Natação",
  Esporte: "Esporte",
  Yoga: "Yoga",
  TreinoEmCasa: "Treino em casa",
  Outro: "Outro",
};

export const tiposAtividade = Object.keys(
  tipoAtividadeLabels,
) as TipoAtividade[];

export const tipoComprovacaoLabels: Record<TipoComprovacao, string> = {
  RegistroManual: "Registro manual",
  Fotografia: "Fotografia",
  CapturaDeTela: "Captura de tela",
};

export const situacaoVerificacaoLabels: Record<SituacaoVerificacao, string> = {
  AguardandoFeedback: "Aguardando feedback",
  Verificada: "Verificada",
  EmRevisao: "Em revisão",
  Contestada: "Contestada",
  Rejeitada: "Rejeitada",
};

export function formatarDuracao(segundos: number): string {
  const horas = Math.floor(segundos / 3600);
  const minutos = Math.floor((segundos % 3600) / 60);
  if (horas > 0 && minutos > 0) return `${horas} h ${minutos} min`;
  if (horas > 0) return `${horas} h`;
  return `${minutos} min`;
}

export function formatarDataHora(iso: string): string {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(iso));
}
