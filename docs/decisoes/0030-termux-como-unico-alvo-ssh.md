# 0030 — Termux como único alvo do `run.bat --ssh`

**Status:** DECIDIDA

## Contexto

A 0029 moveu a pilha do Galaxy A14 do Podroid (Alpine em QEMU TCG, Docker) para o Termux nativo. O caminho com Docker, o perfil de memória baixa e a escolha de SO ficaram sem uso e confundiam: um perfil `a14` salvo com `alpine` e porta 9922 fazia o `run.bat --ssh` falhar com "Connection refused".

## Alternativas consideradas

- Manter os dois caminhos: código morto e mais perguntas, sem alvo que o use.

## Decisão

- `run.bat --ssh` só implanta no Termux (arm64, sem root). Pergunta host, porta (padrão 8022), usuário (`whoami`) e chave.
- Removidos `deploy/remote-ssh.sh` e `deploy/compose.baixa-memoria.yaml`. `deploy.bat` e `deploy/remote.sh` (VM com Caddy) continuam.
- Perfis salvos antigos continuam legíveis: `S_OS` e `S_RAM` são ignorados. Se o usuário estiver vazio, o script pergunta.

## Impacto

Substitui a 0028. Atualiza `docs/funcionalidades.md`, `docs/arquitetura.md` e `docs/questoes-abertas.md`.
