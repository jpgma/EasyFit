# EasyFit

Aplicação web privada para pequenos grupos de amigos manterem uma rotina de exercícios, com desafios compartilhados, registro de atividades e validação comunitária.

.NET 10 Native AOT · PostgreSQL · React PWA · Docker Compose

- Regras do repositório: [`AGENTS.md`](AGENTS.md)
- Arquitetura: [`docs/arquitetura.md`](docs/arquitetura.md)
- Questões em aberto: [`docs/questoes-abertas.md`](docs/questoes-abertas.md)
- Decisões: [`docs/decisoes/`](docs/decisoes/)

## Pré-requisitos

- .NET SDK 10.0.403 ou superior na faixa 10.0.4xx
- Node.js 24
- Docker
- No Windows, Git Bash para executar os scripts

## Desenvolvimento

```bash
scripts/dev-db.sh                                  # PostgreSQL na porta 5433
dotnet run --project EasyFit -- migrate            # aplica as migrações
dotnet run --project EasyFit                       # API em http://localhost:8080
cd EasyFit/webapp && npm ci && npm run dev         # interface em http://localhost:5173
```

## Verificação

Não há CI. Rode antes de concluir qualquer trabalho:

```bash
scripts/verify.sh          # formatação, lint, build, testes unitários, build do frontend
scripts/verify.sh --full   # também testes de integração e imagem arm64
```

## Produção

```bash
cp .env.example .env       # preencha os valores
docker buildx build --platform linux/arm64 -f deploy/Dockerfile -t easyfit:latest --load .
docker compose --env-file .env -f deploy/compose.yaml run --rm app migrate
docker compose --env-file .env -f deploy/compose.yaml up -d
```
