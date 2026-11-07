# 0028 — Implantação em outra máquina por SSH e perfil de memória baixa

**Status:** SUBSTITUÍDA pela 0030 (alvo único: Termux)

## Contexto

A VM da Oracle (0005) segue adiada. Há um Galaxy A14 5G com Podroid (guest Alpine arm64, QEMU TCG, VM de 2 GiB) na rede local, que serve de primeiro alvo para rodar a pilha fora do Windows. O guest não pode abrir as portas 80 e 443, e o repositório `Environment` já mostrou que a cota de CPU do Docker trava o `stop` em TCG. Os limites de `compose.yaml` (app 384 MB, banco 768 MB) não cabem nesse alvo junto com o sistema e o Docker.

## Alternativas consideradas

- **Reaproveitar `deploy.bat`:** é fixo na VM Oracle (Ubuntu, Caddy, domínio de produção, usuário `ubuntu`) e exigiria parâmetros demais.
- **Manter o Caddy no alvo:** repete a pilha local, mas custa memória e exige portas altas sem ganho, já que o acesso público passa pelo túnel.
- **Reaproveitar o PostgreSQL do `Environment`:** economiza cerca de 100 MB, mas acopla este repositório a outro e exige criar banco e papel à parte.
- **Baixar os limites do `compose.yaml`:** um único conjunto de valores, porém afeta a VM Oracle e o Windows sem necessidade.
- **`scp` para copiar arquivos:** depende do `sftp-server`, que o guest Alpine pode não ter. Foi usado `tar` por pipe.

## Decisão

- `run.bat --ssh` pergunta nome do perfil, host, porta, usuário (somente `root`), SO (`alpine`, `ubuntu` ou `debian`), arquitetura (`arm64` ou `amd64`), chave SSH, perfil de RAM, nome do túnel e hostname. As respostas, sem segredos, ficam em `%LOCALAPPDATA%\Wrexercise\ssh\<perfil>.env`.
- O alvo recebe app, banco e túnel da Cloudflare, sem Caddy, num projeto Compose próprio (`wrexercise-ssh`). Usa um túnel próprio (padrão `wx-a14`); reutilizar `wx-dev` dividiria o tráfego entre dois conectores. O cadastro sem convite fica fechado.
- O script confere Docker e Compose no alvo e imprime o comando de instalação, mas não instala nada. Confere também se a arquitetura remota é a informada.
- As imagens vão por `docker save | ssh docker load` (0005). As imagens fixadas por versão (PostgreSQL e cloudflared) só são enviadas se faltarem no alvo.
- `deploy/remote-ssh.sh` roda no alvo em `sh` POSIX, gera a senha do banco na primeira vez e mantém a mesma nas seguintes.
- `deploy/compose.baixa-memoria.yaml` aplica os limites mínimos iniciais e remove `cpus`:

| Serviço | Padrão | Baixa |
|---|---:|---:|
| Aplicação | 384 MB | 128 MB |
| PostgreSQL | 768 MB | 160 MB (`shared_buffers=32MB`, `max_connections=10`) |
| Túnel | 64 MB | 32 MB |
| Backup | 256 MB | 128 MB |

O banco ganha `start_period` de 900 s no healthcheck: em QEMU TCG o `initdb` da primeira inicialização leva minutos, e sem isso o Compose o marca como não saudável.

O túnel recebe o ID lido da credencial (`TUNNEL_ID`) em vez do nome, porque o `cloudflared` só resolve o nome com o `cert.pem`, que não vai ao alvo. A credencial no alvo pertence ao usuário 65532 (`nonroot` da imagem do cloudflared), com modo 600.

O pool de conexões do app fica em 5 nesse perfil. O .NET já limita o heap a 75% do limite do contêiner, então não há variável `DOTNET_GCHeapHardLimit`.

## Impacto

- Os valores da tabela são um ponto de partida, ainda não medidos. `run.bat --ssh` mostra `docker stats` e `OOMKilled` no fim para ajustá-los (Q025).
- Exige chave SSH sem senha: o envio da imagem usa o stdin do `ssh`.
- Dados e evidências ficam em `/srv/wrexercise` dentro do guest. Falta confirmar que o disco do Podroid persiste e definir como copiar os backups para fora do telefone (0022).
- O IP do telefone vem do DHCP; reserve o IP no roteador ou refaça o perfil.
- Relacionada: 0005.
