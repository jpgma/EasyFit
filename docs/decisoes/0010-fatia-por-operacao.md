# 0010 — Fatia organizada por operação

**Status:** DECIDIDA

## Contexto

Cada fatia vertical usava um arquivo por responsabilidade técnica (`Routes.cs`, `Contracts.cs`, `Queries.cs`, `Commands.cs`, `Rules.cs`). Para acompanhar um endpoint, era preciso saltar entre vários arquivos. À medida que as fatias cresceram, a navegação por operação (o que uma rota faz de ponta a ponta) ficou mais importante do que a agrupação por tipo de código.

## Alternativas consideradas

- **Manter Commands/Queries/Contracts/Rules:** familiar e previsível, mas dispersa o fluxo de cada operação.
- **Um arquivo por operação, com `Routes.cs` fino:** handler HTTP, contratos exclusivos, SQL e regras só daquela rota no mesmo arquivo; tipos e helpers reutilizados ficam em arquivos nomeados pelo conceito.
- **Eliminar `Routes.cs` e registrar cada operação em `Program.cs`:** remove um arquivo, mas espalha o mapeamento de URLs e políticas (rate limit, `RequireSessao`) fora da fatia.

## Decisão

Organizar cada fatia por operação HTTP:

- `Routes.cs` permanece fino: grupos de rota, políticas transversais e ligação `Map*` → `Operacao.HandleAsync`.
- Um arquivo por operação (`Registrar.cs`, `CriarGrupo.cs`, …) concentra o fluxo completo daquela rota.
- Arquivos compartilhados na raiz da fatia (`Respostas.cs`, `Sessoes.cs`, `ProgressoDesafio.cs`) só quando duas ou mais operações reutilizam o mesmo tipo ou helper.
- Contratos entre fatias continuam em `Features/<Fatia>/Contracts/` com namespace `Wrexercise.Features.<Fatia>.Contracts`.
- Fatias triviais (ex.: Health) podem ficar só com `Routes.cs`.

Não usar classes agregadoras `*Commands` / `*Queries` nem arquivos `Rules.cs` / `Contracts.cs` na raiz da fatia.

## Impacto

- `Wrexercise/AGENTS.md`, `AGENTS.md` e `docs/arquitetura.md` §3.5 passam a descrever o layout por operação, com template comentado.
- Fatias existentes (`Autenticacao`, `Grupos`, `Convites`, `Desafios`) são migradas na mesma alteração.
- Testes unitários espelham os arquivos de regras puras compartilhadas (`ValidacaoContaTests`, `ProgressoDesafioTests`, etc.).
