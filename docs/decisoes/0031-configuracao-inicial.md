# 0031 — Configuração inicial (primeiro usuário)

**Status:** DECIDIDA. Substitui a configuração `Autenticacao:CadastroAberto` da decisão 0006 e a nota correspondente da decisão 0008.

## Contexto

A decisão 0006 deixou `Autenticacao:CadastroAberto` para criar a primeira conta: o operador liga a opção, cria a conta e desliga. É um passo manual e frágil, que abre o cadastro a qualquer pessoa enquanto estiver ligado e exige recriar o serviço duas vezes. Desde a decisão 0008, as demais contas entram por convite, então a opção só serve a esse primeiro uso.

## Alternativas consideradas

- **Manter `CadastroAberto`:** nada a construir, mas continua sendo um interruptor que pode ser esquecido ligado.
- **Código de configuração impresso no log:** protege um servidor exposto antes de o dono abrir o app (padrão do Jenkins e do Portainer), mas exige que o operador leia os logs.
- **Comando `criar-usuario` na linha de comando (como `reset-senha`):** sem endpoint público, mas deixa o operador digitar a senha no terminal.
- **Tela de configuração inicial, aberta só enquanto não há usuários:** sem configuração e sem passo manual.

## Decisão

- Existe uma tela `/configuracao-inicial`, apoiada por duas rotas anônimas em `/api/autenticacao/configuracao-inicial`:
  - `GET` devolve `{ "pendente": true }` enquanto a tabela `usuarios` está vazia.
  - `POST` cria o primeiro `Usuario` e a `Sessao` na mesma transação. Um lock consultivo (`pg_advisory_xact_lock`) serializa as tentativas simultâneas, e só a primeira encontra a tabela vazia. Com algum usuário já criado, responde 409 `CONFIGURACAO_JA_CONCLUIDA`.
- Enquanto está pendente, o frontend redireciona `/entrar` e `/criar-conta` para a tela. Depois, a tela redireciona para `/entrar`.
- O primeiro usuário é um `Usuario` comum, sem papel global. Ele cria o `Grupo`, vira `Proprietario` e gera os convites.
- `Autenticacao:CadastroAberto` deixa de existir. `POST /api/autenticacao/registrar` sem convite sempre responde 403 `CADASTRO_FECHADO`.
- A rota de `POST` usa o mesmo limite de tentativas de `entrar` e `registrar`.

## Impacto

- Sem migração nem tabela nova. A pergunta "há usuários?" vem direto de `usuarios`.
- Sai a variável `CADASTRO_ABERTO` de `deploy/compose.yaml`, `.env.example`, `run.bat` e `deploy/remote-termux.sh`. O ambiente de desenvolvimento também passa pela tela.
- **Compromisso aceito:** não há código de configuração. Num servidor exposto à internet antes de o dono abrir o app, quem acessar primeiro cria a conta. O operador deve abrir o endereço logo após a implantação. Se esse risco deixar de ser aceitável, o código no log (alternativa acima) pode ser acrescentado sem mudar o restante do desenho.
- Recuperar o acesso do primeiro usuário continua sendo o `reset-senha` da decisão 0019. Se todos os usuários forem apagados do banco, a tela volta a abrir.
