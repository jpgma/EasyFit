# 0006 — Autenticação por sessão

**Status:** DECIDIDA. Parcialmente substituída pela decisão 0019 (alteração e redefinição de senha) e pela decisão 0031 (a configuração `Autenticacao:CadastroAberto` foi trocada pela configuração inicial).

## Contexto

A Fase 2 precisa de cadastro, login e sessões persistentes. `arquitetura.md` §6.2 e §12 já definem sessão opaca em cookie `HttpOnly`, hash do token no banco e PBKDF2. A Q004 deixava em aberto a duração e renovação da sessão, os múltiplos dispositivos, a proteção contra CSRF e a recuperação de acesso. Faltava também definir como o cadastro funciona antes de existirem convites (Fase 3), e quem cria a primeira conta quando eles existirem.

## Alternativas consideradas

**Duração da sessão**

- **Absoluta de 30 dias:** mais simples, mas desloga quem usa o app todos os dias.
- **Deslizante, escrevendo a cada requisição:** gera uma escrita no banco por chamada à API.
- **Deslizante, renovando no máximo uma vez por dia:** mantém logado quem usa o app, com uma escrita diária por sessão.

**CSRF**

- **Token sincronizador:** exige estado adicional no frontend e em toda escrita.
- **Exigir um cabeçalho próprio:** simples, mas menos explícito sobre o que é verificado.
- **`SameSite=Lax` com verificação de origem:** sem estado e sem código no frontend, porque os navegadores sempre enviam `Origin` em escritas.

**Cadastro antes dos convites**

- **Aberto até a Fase 3:** só é seguro se nada for implantado antes disso, e não resolve o primeiro usuário.
- **Código único em variável de ambiente:** imita o mockup, mas seria descartado na Fase 3.
- **Trazer os convites para esta fase:** exige também grupos e membros, e o primeiro usuário continua sem solução.
- **Configuração `Autenticacao:CadastroAberto`:** fechado por padrão, e continua útil depois da Fase 3 para criar a primeira conta.

**Recuperação de senha**

- **Link por e-mail, como no mockup:** exige um serviço de e-mail, uma dependência externa para um grupo de até 10 pessoas.
- **Redefinição manual pelo operador:** sem código novo.

## Decisão

- **Sessão:** token de 32 bytes aleatórios, com só o SHA-256 no banco. Vale 30 dias e desliza no máximo uma vez por dia.
- **Dispositivos:** cada login cria uma sessão independente. Sair encerra apenas a sessão atual.
- **Senhas:** PBKDF2-HMAC-SHA256 com 600 mil iterações e sal de 16 bytes, no formato `pbkdf2-sha256$iteracoes$sal$hash`, que permite aumentar o custo sem migração. Entre 8 e 128 caracteres.
- **CSRF:** `SameSite=Lax`, e toda escrita em `/api` precisa de `Origin` igual ao `Host`, ou de `Sec-Fetch-Site: same-origin` quando não há `Origin`. Qualquer outro caso recebe 403 `ORIGEM_INVALIDA`. Todo proxy à frente da API precisa preservar o `Host`, como fazem o Caddy e a configuração do Vite.
- **Tentativas:** 10 por minuto, por IP, somando `entrar` e `registrar`. O IP vem de `X-Forwarded-For`, em que se pode confiar porque só o Caddy alcança o contêiner da aplicação. Com `run.bat --tunnel`, o túnel local também o alcança e repassa o valor definido pela Cloudflare.
- **Cadastro:** `Autenticacao:CadastroAberto` é falso quando não definido. Para criar a primeira conta em produção: ligue, crie a conta e desligue. A Fase 3 passa a exigir convite.
- **Recuperação de senha:** revista pela decisão 0019: o operador gera um link de uso único. Continua sem envio de e-mail.
- **Autorização:** `RequireSessao()` em `Infrastructure/Authentication` verifica apenas a autenticação. O pertencimento ao grupo continua sendo responsabilidade de cada endpoint.

## Impacto

- Nova tabela `sessoes_autenticacao`. Sessões expiradas de um usuário são apagadas quando ele entra, sem tarefa agendada.
- O login não revela se um e-mail existe: resposta e custo são iguais aos de uma senha errada.
- Um login leva cerca de 250 ms no binário Native AOT com o limite de 0,50 CPU, medido em x64 por falta da VM. Falta confirmar em ARM64 real (Q025).
- **Compromisso aceito:** o cadastro responde 409 `EMAIL_JA_CADASTRADO` e, portanto, revela que o e-mail existe. Evitar isso exigiria confirmação por e-mail. A exposição é limitada pelo cadastro fechado e pelo limite de tentativas.
- Um grupo atrás do mesmo IP divide o limite de tentativas, o que é aceitável para até 10 pessoas.
- Trocar de senha chegou com a decisão 0019 e revoga as demais sessões do usuário.
