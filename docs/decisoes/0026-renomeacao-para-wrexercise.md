# 0026 — Renomeação de EasyFit para Wrexercise

**Status:** DECIDIDA.

## Contexto

O aplicativo ganhou uma identidade nova: um clube de amigos que transforma a rotina de exercícios em competição e jogo (decisão 0027). O nome EasyFit soava como um rastreador genérico e não combinava com o ícone novo nem com o Jogo. O nome novo é **Wrexercise**.

O nome antigo aparecia em todo lugar: na interface, nos projetos .NET e namespaces, nas pastas, no repositório do GitHub, na imagem Docker, no banco e no usuário do PostgreSQL, nos diretórios da VM (`/opt/easyfit`, `/srv/easyfit`), no prefixo dos cookies (`ef_`) e no cache do Service Worker.

## Alternativas consideradas

- **Só a marca visível:** troca textos, manifest e título, e mantém o resto. É a opção mais barata, mas deixa dois nomes convivendo para sempre no código, nos logs e na operação.
- **Código e repositório, mantendo os dados de produção:** não exige migração na VM, mas o banco, os diretórios e os cookies continuariam com o nome antigo.
- **Tudo, com uma migração única na VM:** um nome só em todo lugar, ao custo de um script de migração e de um logout geral.

## Decisão

Renomear tudo:

- Pastas e projetos: `Wrexercise/`, `Wrexercise.UnitTests/`, `Wrexercise.IntegrationTests/`, `Wrexercise.slnx`, com namespaces `Wrexercise.*`.
- Operação: imagem `wrexercise:*`, banco e usuário `wrexercise`, `/opt/wrexercise`, `/srv/wrexercise`, `WREXERCISE_IMAGE`, `WREXERCISE_SSH_HOST`, chave `~/.ssh/wrexercise_oracle`.
- Cookies: `wx_session` e `wx_instalacao_dispensada`.
- Service Worker: cache `wrexercise-<versão>`. O `activate` também apaga os caches `easyfit-*` antigos.
- Backups: arquivos `wrexercise-*.dump`. `backup.sh` continua restaurando e apagando os `easyfit-*.dump` anteriores.
- Repositório no GitHub: `jpgma/Wrexercise`.

A VM é migrada uma única vez por `deploy/migrar-nome.sh`, que faz um backup, renomeia o banco e o usuário com um superusuário temporário (o PostgreSQL não renomeia o usuário da própria sessão), move `/opt` e `/srv` e reescreve o `.env`. `remote.sh` se recusa a implantar enquanto `/opt/easyfit` existir, para que um `.env` novo nunca aponte para um banco vazio.

## Impacto

- Todo mundo precisa entrar de novo uma vez, porque o cookie de sessão mudou de nome. As sessões antigas expiram sozinhas no banco.
- O banco de desenvolvimento (`compose.dev.yaml`, projeto `wrexercise-dev`) e a pilha local do `run.bat` (`wrexercise-local`, `%LOCALAPPDATA%\Wrexercise`) começam vazios. Os dados locais antigos ficam onde estavam, até serem apagados à mão.
- O túnel do `run.bat --tunnel` passa a se chamar `wx-dev` e publica em `wx-dev.jpgmaraujo.com.br`, sem tocar no domínio de produção. O túnel `easyfit-local` antigo deixa de ser usado.
- Passos manuais, nesta ordem: rodar `deploy/migrar-nome.sh` na VM; renomear a chave SSH local; rodar `deploy.bat`; renomear o repositório no GitHub e atualizar o `origin`; renomear a pasta local do repositório.
- Os documentos antigos passam a citar os caminhos novos. O nome EasyFit só continua nesta decisão, no script de migração e na limpeza de caches e backups antigos.
