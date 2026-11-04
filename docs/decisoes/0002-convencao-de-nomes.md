# 0002 — Convenção de nomes no código

**Status:** DECIDIDA

## Contexto

O idioma oficial do projeto é pt-BR. O documento de arquitetura original usava português também em nomes estruturais (`Funcionalidades/`, `Infraestrutura/`, `Rotas.cs`), mas não definia uma regra para identificadores. Sem uma regra simples, a mistura de idiomas se torna inconsistente, principalmente com agentes de programação.

## Alternativas consideradas

- **Tudo em português:** vocabulário único, mas cria atrito com os termos do framework (`Get`, `Request`, `Map`).
- **Tudo em inglês:** convencional, mas exige traduzir os conceitos do domínio entre o código e a interface.
- **Domínio em português, técnico em inglês, com estrutura em português:** mantém os nomes do documento original, mas exige decidir caso a caso o que é estrutura.
- **Domínio em português, todo o resto em inglês:** uma única pergunta decide cada nome.

## Decisão

Domínio em português, sem acentos; todo o resto em inglês, inclusive pastas e arquivos estruturais.

> Um participante reconheceria a palavra como conceito do aplicativo? **Sim:** português sem acentos. **Não:** inglês.

Exemplos: `Features/Reacoes/Routes.cs`, `GetReacoesByAtividade()`, `reacoes_atividade.created_at`.

Permanecem em pt-BR, com acentos: documentação, textos da interface, comentários e mensagens. Rotas, campos JSON e códigos de erro seguem a regra e por isso continuam como definidos em `arquitetura.md` (`/api/atividades/{id}/reacao`, `ATIVIDADE_NAO_ENCONTRADA`).

A tabela de exemplos e o glossário ficam em `AGENTS.md`.

## Impacto

- As seções 3.5, 4, 9.3 e 16 de `arquitetura.md` foram atualizadas.
- Colunas técnicas ficam em inglês: `iniciada_em` passa a ser `started_at`.
- Conceitos novos do domínio precisam entrar no glossário para evitar variações de grafia.
