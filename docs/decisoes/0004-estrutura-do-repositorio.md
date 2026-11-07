# 0004 — Estrutura do repositório

**Status:** DECIDIDA · Altera a seção 4 de `arquitetura.md`

## Contexto

A estrutura inicial separava o código em `src/`, `tests/` e `web/`. Com um único executável e dois projetos de teste, essas pastas só acrescentavam um nível de navegação, e o frontend ficava longe do projeto que o compila e serve.

## Alternativas consideradas

- **`src/`, `tests/` e `web/`:** convencional, mas com pastas que contêm um único projeto ou quase isso.
- **Projetos na raiz, com o frontend em `web/` na raiz:** elimina o nível extra, mas mantém separado o que é implantado junto.
- **Projetos na raiz, com o frontend dentro do projeto da aplicação:** o que é implantado junto fica junto.

## Decisão

```text
Wrexercise/                 # aplicação: API e frontend
├── Features/
├── Infrastructure/
├── webapp/                 # código-fonte do frontend
└── wwwroot/                # gerado a partir de webapp/, fora do git
Wrexercise.UnitTests/
Wrexercise.IntegrationTests/
deploy/
scripts/
docs/
```

`Wrexercise.csproj` exclui `webapp/**` de seus itens padrão.

## Impacto

- Sem essa exclusão, o SDK Web trataria os arquivos JSON de `webapp/`, incluindo os de `node_modules/`, como conteúdo a publicar. A exclusão precisa permanecer enquanto o frontend estiver dentro do projeto.
- O frontend continua sendo um projeto Node independente: os comandos `npm` rodam em `Wrexercise/webapp/`.
- Testes continuam fora do projeto da aplicação, para não entrarem no binário Native AOT.
