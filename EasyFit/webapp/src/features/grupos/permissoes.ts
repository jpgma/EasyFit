import type { Papel } from "./useGrupos";

// Só decidem o que mostrar: quem aplica as regras é Features/Grupos/Rules.cs, no backend.

export function canConvidar(papel: Papel): boolean {
  return papel === "Proprietario" || papel === "Administrador";
}

export function canRevisarAtividade(papel: Papel): boolean {
  return papel === "Proprietario" || papel === "Administrador";
}

export function canAlterarPapel(ator: Papel, alvo: Papel): boolean {
  return ator === "Proprietario" && alvo !== "Proprietario";
}

export function canRemover(ator: Papel, alvo: Papel): boolean {
  if (ator === "Proprietario") return alvo !== "Proprietario";
  return ator === "Administrador" && alvo === "Membro";
}

export const papelLabels: Record<Papel, string> = {
  Proprietario: "Proprietário",
  Administrador: "Administrador",
  Membro: "Membro",
};
