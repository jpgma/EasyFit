# 0001 — Migrações SQL

**Status:** DECIDIDA · Fecha a Q005

## Contexto

O projeto usa SQL explícito, sem ORM, e precisa de migrações versionadas, com execução explícita, seguras em caso de interrupção e compatíveis com Native AOT.

## Alternativas consideradas

- **Ferramenta externa (Flyway, dbmate):** madura, mas acrescenta um runtime ou binário à imagem e ao deploy.
- **Scripts aplicados à mão com `psql`:** sem código novo, mas sem controle confiável do que foi aplicado.
- **Executor próprio dentro da aplicação:** pouco código, sem dependências, usa o mesmo Npgsql.

## Decisão

Executor próprio, exposto como o subcomando `migrate` do binário da aplicação.

- Arquivos `NNN-kebab-case.sql` em `Infrastructure/Database/Migrations/`, incorporados ao binário e numerados em sequência.
- Apenas para frente: não há scripts de reversão. Corrige-se com uma nova migração, ou restaurando o backup.
- Cada migração roda em sua própria transação, e um lock consultivo do PostgreSQL impede execuções simultâneas.
- A tabela `schema_migrations` guarda versão, nome, checksum SHA-256 e data. O executor se recusa a continuar se um arquivo aplicado mudou ou sumiu.
- Nunca roda na inicialização da aplicação. Aplica-se durante o deploy:

```bash
docker compose --env-file .env -f deploy/compose.yaml run --rm app migrate
```

## Impacto

- Sem dependências novas, e o mesmo binário funciona em qualquer ambiente.
- Comandos que não podem rodar em transação, como `CREATE INDEX CONCURRENTLY`, não são suportados. Com o volume de dados previsto, isso não é um problema.
- Migrações já aplicadas não podem ser editadas, nem mesmo para corrigir um comentário.
