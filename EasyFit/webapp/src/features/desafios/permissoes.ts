import type { Papel } from "@/features/grupos";
import type { SituacaoDesafio } from "./useDesafios";

// Só decidem o que mostrar: quem aplica as regras é Features/Desafios/Rules.cs, no backend.

export function canGerenciarDesafios(papel: Papel): boolean {
  return papel === "Proprietario" || papel === "Administrador";
}

export function isDesafioAberto(situacao: SituacaoDesafio): boolean {
  return situacao === "Agendado" || situacao === "Ativo";
}

export const situacaoLabels: Record<SituacaoDesafio, string> = {
  Agendado: "Agendado",
  Ativo: "Ativo",
  Encerrado: "Encerrado",
  Cancelado: "Cancelado",
};
