# Backend — .NET 10 Native AOT

Complementa o `AGENTS.md` da raiz.

## Estrutura de uma fatia

```text
Features/Reacoes/
├── Routes.cs      # MapReacoesRoutes(): endpoints e fluxo HTTP
├── Contracts.cs   # records de request e response
├── Queries.cs     # SQL de leitura
├── Commands.cs    # SQL de escrita e transações
└── Rules.cs       # regras puras, sem I/O
```

Crie apenas os arquivos necessários. `Infrastructure/` contém somente recursos compartilhados.

`webapp/` é um projeto Node independente, excluído dos itens padrão em `EasyFit.csproj`. Não remova essa exclusão: sem ela, os arquivos de `node_modules/` entram na publicação.

Fluxo de um endpoint: autenticação → validação do contrato → regra de negócio → SQL → resposta. Regras de negócio ficam em `Rules.cs`, testáveis sem banco.

**Contratos entre fatias:** quando outra fatia realmente precisar de um tipo, coloque-o no namespace `EasyFit.Features.<Fatia>.Contracts`. É o único namespace de outra fatia que pode ser referenciado.

## Native AOT

- Aviso de AOT ou trimming é erro de implementação. Não o suprima sem justificativa documentada.
- Sem reflexão dinâmica, `Assembly.Load` ou bibliotecas incompatíveis com trimming.
- Todo tipo serializado em JSON é registrado em `Infrastructure/Json/AppJsonContext.cs`.
- Serialize sempre contratos explícitos, nunca estruturas internas.

## SQL

- Npgsql direto, com `NpgsqlDataSource` injetado. Parâmetros posicionais (`$1`, `$2`).
- SQL fica junto da fatia que o usa.
- Projete só as colunas necessárias. Feeds usam paginação por cursor.
- Várias escritas relacionadas usam transação explícita.
- O `NpgsqlSlimDataSourceBuilder` só tem os recursos habilitados em `DataSourceFactory`; habilite o que faltar lá (por exemplo, arrays).

## Migrações

- Arquivos em `Infrastructure/Database/Migrations/`, no formato `NNN-kebab-case.sql`, com numeração sequencial.
- Só avançam: nunca edite uma migração já aplicada, crie outra. O executor recusa arquivos alterados.
- Nunca rodam na inicialização. Aplique com `dotnet run --project EasyFit -- migrate` ou `docker compose run --rm app migrate`.

## Erros

Use `ApiProblem` (`application/problem+json` com `code`). Códigos em `MAIUSCULAS_COM_SUBLINHADO`, títulos em pt-BR, sem detalhes internos.

## Testes

- `EasyFit.UnitTests`: regras puras e testes de arquitetura.
- `EasyFit.IntegrationTests`: HTTP e SQL contra o PostgreSQL de desenvolvimento (`scripts/dev-db.sh`), com um banco descartável por execução.
