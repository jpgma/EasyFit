# 0023 — Gamificação opcional (modo Jogo)

**Status:** DECIDIDA (direção geral), parcialmente substituída por `0024-cartas-e-avatar-importado.md` (destino da Energia, consumíveis, apostas, interações e avatar). Detalhes e fases em `../gamificacao.md`.

## Contexto

A proposta inicial de gamificação trazia carteira com livro-razão, loja, consumíveis, duelos com aposta, compromissos, pegadinhas e eventos de grupo. Ela é grande demais para o princípio "simples o suficiente para uma única pessoa manter" e arrisca distrair do objetivo, que é treinar com regularidade.

Ao mesmo tempo, o aplicativo já calcula na leitura tudo o que uma recompensa precisa: atividades verificadas, metas cumpridas, sequências e conquistas (decisão 0018). A afirmação de `funcionalidades.md` §9.3 de que "a aplicação não usa pontos" se refere à classificação, que deve continuar intocada.

## Alternativas consideradas

**Moeda**

- **Carteira com livro-razão imutável:** flexível, mas exige processar eventos, chaves de idempotência, transações compensatórias e reconciliação.
- **Só desbloqueios por conquista, sem moeda:** a mais simples, mas não deixa escolha à pessoa.
- **Energia calculada na leitura menos a soma das compras:** reaproveita as consultas existentes. A única tabela nova é a de compras.

**Como apresentar**

- **Integrar ao aplicativo para todos:** muda a experiência de quem não quer jogo.
- **Tela inicial alternativa escolhida por cada pessoa:** discreta. O modo App fica exatamente como está.

**Arte**

- **Desenho próprio ou encomendado:** depende de um trabalho que o projeto não tem.
- **Pixel-art desenhado em código:** sem arquivos, mas cada item novo dá muito trabalho.
- **Pacote de sprites CC0 versionado no repositório:** sem licença a gerenciar e sem rede externa.

## Decisão

- **Modo da tela inicial** `App` ou `Jogo`, guardado em `usuarios.modo_inicio`, com padrão `App`. O modo muda só a tela inicial do grupo. Todos têm avatar e acumulam Energia.
- **Energia calculada na leitura.** O saldo é a Energia ganha menos a soma de `compras`. Só atividades `Verificada` geram Energia. Os qualificadores de bônus consideram as atividades não rejeitadas em ordem de `started_at`, então a Energia ganha nunca diminui. A regra fica numa função pura e testável.
- **Catálogo da loja no código**, sem tabela. Compras permanentes e únicas por item, numa transação que bloqueia a linha do usuário e verifica o saldo.
- **Separação total da competição oficial.** A Energia não entra na classificação e nenhuma mecânica altera verificação, metas, sequências ou atividades. Não existe ranking de Energia.
- **Interações só inofensivas:** Toca aqui, Rival da semana sem aposta e Desafio extra com aceite. Ficam descartados consumíveis, pegadinhas, duelos, compromissos com aposta e missões diárias.
- **Sprites CC0** no bundle, com o arquivo de licença, compostos no navegador.
- **Nova fatia `Jogo`** (backend e frontend). Cada tabela (`avatares`, `compras`, `toques`, `rivais`, `desafios_extras`) entra só na fase que precisa dela.

## Impacto

- Fases G1 a G6 em `gamificacao.md` §9. Cada uma atualiza `funcionalidades.md`, `modelo-dados.md` e as migrações quando for implementada.
- `AGENTS.md` ganha a fatia `Jogo` e os termos novos no glossário.
- `arquitetura.md` §18 ganha a Fase 9, opcional.
- O componente `Avatar` passa a mostrar a cabeça do avatar quando a pessoa já configurou o seu (fase G2).
