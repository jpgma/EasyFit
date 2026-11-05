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
.\run.bat --tunnel  # igual, e publica em https://fit.jpgmaraujo.com.br
.\run.bat logs    # acompanha os logs da aplicação
.\run.bat backup  # grava um backup do banco e das evidências
.\run.bat restore 20261105-030000  # restaura um backup, com o app parado
.\run.bat stop    # para os serviços e mantém os dados
.\run.bat reset   # para os serviços e apaga os dados locais
```

Exige as portas 80 e 443 livres. Usa HTTP em `localhost`, sem aviso de certificado, em vez do HTTPS da produção. Os dados ficam em `%LOCALAPPDATA%\EasyFit\local`.

`--tunnel` publica esta máquina na internet por um túnel da Cloudflare, até `run.bat stop`. Na primeira vez, pede o login na Cloudflare pelo navegador e substitui o registro DNS do domínio. O cadastro sem convite fica fechado: crie a primeira conta com `.\run.bat` antes de publicar. As credenciais do túnel ficam em `%LOCALAPPDATA%\EasyFit\cloudflared`.

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

### Backups

O serviço `backup` grava em `BACKUP_DIR` o dump do banco e um arquivo das evidências, mantém os últimos 7 dias e confere se os arquivos podem ser lidos. As datas nos nomes estão em UTC. Ver `docs/decisoes/0022-backups.md`.

```bash
docker compose --env-file .env -f deploy/compose.yaml run --rm backup backup
```

Agende uma execução diária:

```bash
# Linux (crontab -e), às 3h:
0 3 * * * cd /opt/easyfit && docker compose --env-file .env -f deploy/compose.yaml run --rm backup backup >> /var/log/easyfit-backup.log 2>&1
```

```powershell
# Windows, para a pilha do run.bat, às 3h:
schtasks /create /sc daily /st 03:00 /tn EasyFitBackup /tr "\"D:\Repos\jpgma\EasyFit\run.bat\" backup"
```

Um backup que só existe na mesma máquina não protege contra a perda do disco. Aponte `BACKUP_DIR` para uma pasta sincronizada (OneDrive, Google Drive) ou copie os arquivos para fora regularmente. No `run.bat`, defina a variável de ambiente `BACKUP_DIR` antes de executá-lo; o padrão é `%LOCALAPPDATA%\EasyFit\backups`. Os backups contêm as evidências e os hashes das senhas: mantenha a pasta privada.

Para restaurar:

```bash
docker compose --env-file .env -f deploy/compose.yaml stop app
docker compose --env-file .env -f deploy/compose.yaml run --rm backup restore 20261105-030000
docker compose --env-file .env -f deploy/compose.yaml start app
```

Teste a restauração de tempos em tempos, com a pilha local: `.\run.bat backup`, `.\run.bat reset`, `.\run.bat` e `.\run.bat restore <data>`. Depois, confira se as atividades e as imagens voltaram.

### Senha esquecida

Não há envio de e-mail. O operador gera um link de uso único, válido por 1 hora, e o passa à pessoa. O comando encerra as sessões da conta:

```bash
docker compose --env-file .env -f deploy/compose.yaml run --rm app reset-senha pessoa@exemplo.com
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
