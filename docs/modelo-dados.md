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

## `sessoes_autenticacao`

Uma linha por sessão: cada dispositivo tem a sua. Ver `decisoes/0006-autenticacao-por-sessao.md`.

| Coluna | Tipo | Observações |
|---|---|---|
| `id` | `uuid` | Chave primária |
| `usuario_id` | `uuid` | Referencia `usuarios`, apagada junto com o usuário. Indexada |
| `token_hash` | `bytea` | SHA-256 do token, único. O token em si nunca é gravado |
| `created_at` | `timestamptz` | Padrão `now()` |
| `expires_at` | `timestamptz` | Obrigatória. Renovada com o uso |
| `revoked_at` | `timestamptz` | Preenchida ao sair |

## `grupos`

| Coluna | Tipo | Observações |
|---|---|---|
| `id` | `uuid` | Chave primária |
| `nome` | `text` | Obrigatória |
| `criador_id` | `uuid` | Referencia `usuarios`. Não muda com a transferência da propriedade |
| `fuso_horario` | `text` | Nome IANA, validado contra `pg_timezone_names` |
| `created_at` | `timestamptz` | Padrão `now()` |

## `membros_grupo`

Uma linha por usuário e grupo. Sair do grupo preenche `removed_at`, e voltar reaproveita a linha. Ver `decisoes/0008-grupos-e-convites.md`.

| Coluna | Tipo | Observações |
|---|---|---|
| `grupo_id` | `uuid` | Parte da chave primária. Referencia `grupos` |
| `usuario_id` | `uuid` | Parte da chave primária. Referencia `usuarios`. Indexada entre os membros ativos |
| `papel` | `text` | `Proprietario`, `Administrador` ou `Membro`. Um único `Proprietario` ativo por grupo, garantido por índice |
| `joined_at` | `timestamptz` | Padrão `now()`. Renovada ao voltar |
| `removed_at` | `timestamptz` | Preenchida ao sair ou ser removido |

## `convites_grupo`

Um convite só vale enquanto `used_at` e `revoked_at` estiverem vazias e `expires_at` estiver no futuro.

| Coluna | Tipo | Observações |
|---|---|---|
| `id` | `uuid` | Chave primária |
| `grupo_id` | `uuid` | Referencia `grupos`. Indexada |
| `criador_id` | `uuid` | Referencia `usuarios` |
| `token_hash` | `bytea` | SHA-256 do token, único. O token em si nunca é gravado |
| `created_at` | `timestamptz` | Padrão `now()` |
| `expires_at` | `timestamptz` | Obrigatória. 7 dias depois da criação |
| `used_at` | `timestamptz` | Preenchida quando o convite é aceito |
| `usado_por_id` | `uuid` | Referencia `usuarios`. Quem aceitou |
| `revoked_at` | `timestamptz` | Preenchida ao cancelar, ou quando quem criou deixa o grupo |

## `schema_migrations`

Tabela de controle do executor de migrações. Ver `decisoes/0001-migracoes-sql.md`.
