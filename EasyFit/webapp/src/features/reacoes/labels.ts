import type { TipoReacao } from "./types";

/** Cada reação é sempre representada pelo mesmo símbolo, com ou sem texto ao lado. */
export const tipoReacaoEmojis: Record<TipoReacao, string> = {
  Kudos: "💪",
  Suspeito: "🤨",
  Rejeitar: "❌",
};

export const tipoReacaoLabels: Record<TipoReacao, string> = {
  // Na interface é "moral"; no código, na API e no banco continua Kudos.
  Kudos: "Moral",
  Suspeito: "Suspeito",
  Rejeitar: "Rejeitar",
};
