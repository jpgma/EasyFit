export type TipoReacao = "Kudos" | "Suspeito" | "Rejeitar";

export type ReacaoItem = {
  usuarioId: string;
  nomeExibicao: string;
  tipo: TipoReacao;
  updatedAt: string;
};

export type ReacoesDetalhe = {
  totalRevisores: number;
  kudos: number;
  suspeito: number;
  rejeitar: number;
  suaReacao: TipoReacao | null;
  voceERevisor: boolean;
  janelaAberta: boolean;
  revisaoEncerraEm: string;
  reacoes: ReacaoItem[];
};

export type RevisaoAtividadeData = {
  decisao: "Verificada" | "Rejeitada";
  justificativa: string;
};
