# 0015 — Reações durante a revisão

**Status:** DECIDIDA. Altera a decisão 0012.

## Contexto

Pela decisão 0012, a primeira reação negativa levava a atividade a `EmRevisao`, e nesse estado nenhuma reação era mais aceita. Com dois ou mais revisores, o limite de rejeições nunca podia ser atingido: a primeira rejeição fechava a votação antes da segunda. Na prática, só o administrador conseguia rejeitar, e a fórmula de Q012 só valia com um revisor.

## Alternativas consideradas

- **Uma negativa fecha a votação, e o administrador decide sempre:** simples, mas descarta a fórmula de rejeições e deixa atividades paradas quando o administrador não aparece.
- **Só `Suspeito` leva a `EmRevisao`, e `Rejeitar` apenas soma:** o limite funciona, mas uma rejeição isolada ficaria invisível para o administrador.
- **Manter as reações abertas em `EmRevisao` até o fim do prazo:** o limite funciona, o administrador vê a disputa desde a primeira negativa e pode decidir antes.

## Decisão

- As reações são aceitas em `AguardandoFeedback` e `EmRevisao` enquanto a janela de 48 horas estiver aberta.
- A situação é recalculada a partir das contagens a cada reação, inclusive em `EmRevisao`:
  - rejeições iguais ou acima do limite: `Rejeitada`;
  - alguma reação negativa abaixo do limite: `EmRevisao`;
  - nenhuma negativa, com todos os revisores tendo reagido: `Verificada`;
  - nenhuma negativa, com reações faltando: `AguardandoFeedback`.
- Uma reação pode ser trocada até o fim da janela, mas não removida. Trocar a única negativa encerra a revisão pelas regras acima.
- `Verificada` e `Rejeitada` são finais: novas reações recebem `REACAO_ENCERRADA`.
- Depois da janela, `EmRevisao` só muda por revisão administrativa. O administrador pode decidir a qualquer momento, mesmo com a janela aberta.
- `ReacoesResponse` passa a informar `RejeicoesNecessarias`, para a interface mostrar o limite real.

## Impacto

- Sem migração: só muda `ConsensoAtividade.DeterminarSituacao` e a condição de `RegistrarReacao`.
- A página da atividade mostra "Trocar reação" e o limite de rejeições durante a revisão.
- Uma atividade pode sair de `EmRevisao` sem decisão de um administrador, o que também a retira da fila dele.
