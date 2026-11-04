# Modelo de dados — EasyFit

Este documento descreve o esquema em uso e cresce junto com as migrações. O modelo planejado está em `arquitetura.md`, seções 7 e 9. A fonte da verdade são os arquivos em `EasyFit/Infrastructure/Database/Migrations/`.

## `usuarios`

| Coluna | Tipo | Observações |
|---|---|---|
| `id` | `uuid` | Chave primária |
| `nome_exibicao` | `text` | Obrigatória |
| `email` | `text` | Único, sem diferenciar maiúsculas de minúsculas |
| `password_hash` | `text` | Obrigatória |
| `created_at` | `timestamptz` | Padrão `now()` |

## `schema_migrations`

Tabela de controle do executor de migrações. Ver `decisoes/0001-migracoes-sql.md`.
