# 0005 — VM de produção e implantação via SSH

**Status:** PROPOSTA · Adiada: a VM não foi criada por falta de capacidade na Oracle, e `deploy.bat` nunca foi executado

## Contexto

A Fase 8 previa decidir a configuração da VM (Q001) e como levar a imagem até ela (Q003). A Q003 recomendava CI com registry, mas a decisão 0003 dispensou o CI. O objetivo é o menor custo possível, dentro do Always Free da Oracle Cloud.

Em 2026, a Oracle reduziu a franquia Ampere A1 de 4 OCPUs e 24 GB para 2 OCPUs e 12 GB. Os limites de serviço da conta são maiores que a franquia e não impedem excedê-la: quem cria a instância é responsável por respeitar os valores.

## Alternativas consideradas

Para a distribuição da imagem:

- **Registry com CI:** a garantia mais forte, mas exige remoto, registry e pipeline.
- **Registry sem CI:** acrescenta um serviço e uma credencial sem resolver um problema real com uma única VM.
- **Compilar na VM:** contraria a regra de não instalar SDK no servidor.
- **`docker save` por SSH:** sem serviços adicionais; reenvia a imagem inteira a cada implantação.

Para o tamanho da VM:

- **2 OCPUs e 12 GB:** o máximo gratuito, mas a região `sa-saopaulo-1`, que tem um único domínio de disponibilidade, recusou a criação por falta de capacidade.
- **1 OCPU e 6 GB:** a recomendação inicial da Q001, mais fácil de alocar e suficiente para os limites dos contêineres.

## Decisão

- **VM:** `VM.Standard.A1.Flex` com 1 OCPU, 6 GB e volume de inicialização de 200 GB, Ubuntu 24.04 ARM64, em `sa-saopaulo-1`, preparada por `deploy/cloud-init.yaml`.
- **Rede:** portas 22, 80 e 443 abertas; SSH somente com chave.
- **Distribuição:** `deploy.bat` constrói a imagem localmente e a envia com `docker save | ssh docker load`. A imagem recebe como tag o commit atual, com o sufixo `-dirty` quando há alterações sem commit.
- **Segredos:** a senha do banco é gerada na VM na primeira implantação e fica em `/opt/easyfit/.env`.
- **DNS:** `fit.jpgmaraujo.com.br` é um registro A na Cloudflare, sem proxy; o Caddy obtém o certificado diretamente.

## Impacto

- A implantação depende de uma máquina Windows com Docker Desktop e da chave SSH.
- Nada verifica o código automaticamente antes de implantar: rode `scripts/verify.sh --full` antes.
- As 5 imagens mais recentes ficam na VM para reverter. As migrações só avançam, portanto reverter uma imagem não desfaz o esquema do banco.
- `deploy.bat` usa o domínio como host SSH. Ativar o proxy da Cloudflare exige definir `EASYFIT_SSH_HOST` com o IP da VM, além de configurar o SSL como Full (strict).
- A Oracle pode recuperar instâncias Always Free ociosas: CPU, rede e memória abaixo de 20% durante 7 dias. Uma aplicação pequena pode se enquadrar. Migrar para Pay As You Go elimina o risco, mas torna possíveis cobranças acidentais. Essa decisão continua pendente.
- Os backups continuam em aberto (Q002) e precisam estar resolvidos antes de haver dados reais.
