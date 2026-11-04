# 0003 — Sem CI nem hooks de git inicialmente

**Status:** DECIDIDA · Afasta-se da recomendação atual da Q003

## Contexto

A Q003 recomenda construir a imagem em CI e publicá-la em um registry. O projeto ainda não tem repositório remoto definido e é mantido por uma pessoa com apoio de agentes de programação.

## Alternativas consideradas

- **CI com publicação de imagem:** a garantia mais forte, mas exige remoto e registry agora.
- **CI apenas para verificações:** também exige remoto.
- **Hooks de git:** automáticos, mas tornam commits e pushes mais lentos e são fáceis de ignorar.
- **Apenas scripts locais:** sem atrito, porém dependem de disciplina.

## Decisão

Apenas scripts locais. `scripts/verify.sh` é a verificação padrão, e `AGENTS.md` exige sua execução antes de concluir qualquer trabalho. As regras principais de arquitetura são testes, de modo que o script as verifica.

A imagem é construída localmente com compilação cruzada para `linux/arm64`. A forma de levá-la até a VM será decidida na Fase 8.

## Impacto

- Nada impede um commit com falhas: a garantia depende de rodar o script.
- A construção não é reproduzida em um ambiente limpo a cada alteração.
- A Q003 continua como PROPOSTA. Reavaliar ao definir o repositório remoto ou antes do primeiro deploy na Oracle Cloud.
