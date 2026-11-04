# 0012 — Verificação comunitária

**Status:** DECIDIDA

## Contexto

A Fase 6 precisa de reações, consenso, transições de `situacao_verificacao`, revisão administrativa e encerramento da janela de 48 horas. `arquitetura.md` §8.3–8.4 e §10.5 definem a fórmula de rejeições e os endpoints de reação; a Fase 5 deixou `revisores_atividade`, `reacoes_atividade` e `historico_revisao` para esta fase (decisão 0011). Q012–Q015 e Q013 estavam abertas ou em proposta.

## Alternativas consideradas

**Grupos com poucos membros**

- **Bloquear criação de atividade sem ≥2 revisores:** alinhado à recomendação genérica de Q013 para desafios, mas impede treino solo no grupo.
- **Sempre aplicar a fórmula:** com um revisor, uma rejeição invalida a atividade.
- **Zero revisores → verificada imediata; ≥1 revisor → consenso:** permite grupo de uma pessoa; demais casos seguem §8.3.

**Encerramento da janela**

- **Só ao ler/listar atividades:** simples, mas atividades podem ficar pendentes para sempre sem tráfego.
- **`BackgroundService` no monólito:** encerra periodicamente; sem fila externa.
- **Cron no host:** operação extra fora da aplicação.

**Revisão administrativa**

- **Reutilizar `PUT /reacao` com papel admin:** mistura papéis comunitário e administrativo.
- **`POST /api/atividades/{id}/revisao`:** contrato explícito, auditoria em `historico_revisao`.

**Estado `Contestada`**

- **Usar para rejeição parcial:** sobrepõe `EmRevisao`.
- **Reservar para contestação futura:** Fase 6 não transiciona para `Contestada`.

## Decisão

**Revisores (snapshot)**

- Na criação da atividade, inserir em `revisores_atividade` todos os membros com `removed_at IS NULL` no grupo, **exceto o autor**.
- Se não houver revisores: `RegistroManual` → `Verificada` na criação; `Fotografia` / `CapturaDeTela` permanecem em `AguardandoFeedback` até o autor anexar evidência, quando passam a `Verificada` (sem janela de reações).

**Consenso (Q012)**

- Tipos: `Kudos`, `Suspeito`, `Rejeitar`.
- Uma reação por revisor (`UNIQUE (atividade_id, usuario_id)`).
- `CalculateRejeicoesNecessarias(n) = min(n, max(2, ceil(n/2)))`.
- Transições (função pura, reaplicada após cada reação e no encerramento):
  - Rejeições ≥ limite → `Rejeitada`.
  - Algum `Suspeito` ou `Rejeitar` abaixo do limite → `EmRevisao`.
  - Todas as reações registradas são `Kudos` → `Verificada`.
  - Prazo encerrado e nenhuma reação negativa → `Verificada`.
  - Com suspeita ou rejeição pendente de resolução, **não** auto-verificar ao fim do prazo (§8.4).
- `Contestada` permanece no esquema, mas **não** é usada nesta fase.

**Grupos pequenos (Q013)**

- 0 revisores: verificada na criação.
- 1 revisor: a fórmula exige 1 rejeição para invalidar.
- ≥2 revisores: fórmula padrão.

**Janela (Q015)**

- 48 horas a partir de `submitted_at` (definido na criação).
- Reações alteráveis enquanto a janela estiver aberta **e** `situacao_verificacao = AguardandoFeedback` (negativas podem já ter movido para `EmRevisao`; nesse caso novas reações comunitárias não se aplicam — só revisão admin).

**Revisão administrativa (Q014)**

- Quem: `Proprietario` ou `Administrador` do grupo.
- Quando: `situacao_verificacao = EmRevisao`.
- `POST /api/atividades/{id}/revisao` com `{ "decisao": "Verificada" | "Rejeitada", "justificativa": "..." }` — justificativa obrigatória, até 500 caracteres.
- Registro imutável em `historico_revisao`; decisão **não** é revertida pela API no MVP.

**Encerramento automático**

- Serviço em segundo plano na aplicação (intervalo ~10 min), idempotente.
- Mesma lógica de encerramento invocada ao final de `PUT /reacao` para resposta consistente.

**API**

| Operação | Quem | Contrato |
|---|---|---|
| Registrar ou alterar reação | Revisor no snapshot | `PUT /api/atividades/{id}/reacao` — `{ "tipo": "Kudos" \| "Suspeito" \| "Rejeitar" }` |
| Listar reações | Membro do grupo | `GET /api/atividades/{id}/reacoes` |
| Revisão admin | Proprietário ou administrador | `POST /api/atividades/{id}/revisao` |

Erros relevantes: `NAO_REVISOR`, `AUTOR_NAO_REAGE`, `JANELA_ENCERRADA`, `REVISAO_NAO_PENDENTE`, `SEM_PERMISSAO_REVISAO`.

**Backfill (migração 007)**

- Atividades existentes: popular revisores com membros atuais (exceto autor); recomputar situação (grupo solo → `Verificada`).

## Impacto

- Migração `007-create-verificacao.sql`.
- Fatia `Features/Reacoes/`.
- `CriarAtividade` grava snapshot e auto-verifica sem revisores.
- Frontend: reações e revisão admin na página de atividade.
- Q012, Q013, Q014 e Q015 marcadas como decididas neste ADR.
