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

### Pilha completa no Windows

Para testar localmente o mesmo que vai para produção (Native AOT, PostgreSQL e Caddy), com o cadastro aberto:

```powershell
.\run.bat         # constrói, migra e sobe em http://localhost
.\run.bat logs    # acompanha os logs da aplicação
.\run.bat stop    # para os serviços e mantém os dados
.\run.bat reset   # para os serviços e apaga os dados locais
```

Exige as portas 80 e 443 livres. Usa HTTP em `localhost`, sem aviso de certificado, em vez do HTTPS da produção. Os dados ficam em `%LOCALAPPDATA%\EasyFit\local`.

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

### Primeira conta

O cadastro fica fechado em produção. Para criar a primeira conta:

1. Defina `CADASTRO_ABERTO=true` no `.env`.
2. Rode `docker compose --env-file .env -f deploy/compose.yaml up -d app`.
3. Crie a conta em `/criar-conta`.
4. Volte para `CADASTRO_ABERTO=false` e repita o passo 2.

Em desenvolvimento, o cadastro já vem aberto. Ver `docs/decisoes/0006-autenticacao-por-sessao.md`.

### Novos membros

A primeira conta cria o primeiro grupo. Depois disso, ninguém precisa do cadastro aberto: o Proprietário ou um Administrador gera um link de convite na tela do grupo, e quem o recebe cria a conta por ele. Cada link vale para uma pessoa, por 7 dias. Ver `docs/decisoes/0008-grupos-e-convites.md`.
