# 0025 — Paleta tirada do ícone do aplicativo

**Status:** DECIDIDA. Substitui a paleta da decisão 0014; o resto dela continua valendo.

## Contexto

A decisão 0014 trouxe do mockup uma paleta verde. O aplicativo ganhou um ícone próprio (`EasyFit/webapp/icon-source.png`): carta creme com um braço flexionado, sobre raios laranja e dourados, cartas azul e roxa atrás e contorno ameixa. Com a interface verde e o ícone laranja, a tela inicial do PWA, a aba do navegador e as telas não pareciam o mesmo produto.

## Alternativas consideradas

**Cor principal**

- **Vermelho-alaranjado da borda do ícone:** mais forte, mas fica perto do vermelho de erro.
- **Roxo das cartas de trás:** interface mais calma, com o laranja só na marca.
- **Laranja do fundo do ícone:** a cor que mais aparece no ícone.

**Neutros**

- **Cinza puro:** o laranja carrega sozinho a identidade.
- **Quentes:** creme no tema claro, tirado da carta; ameixa escuro no tema escuro, tirado do contorno.

**Destaque secundário (`lime`)**

- **Manter o verde-limão:** destoa do ícone.
- **Roxo:** contraste forte com o laranja.
- **Dourado dos raios:** mesma família quente do laranja.

## Decisão

- **Principal:** laranja do ícone. No tema claro fica um pouco mais escuro, para o texto branco nos botões passar no WCAG AA; no escuro é o laranja vivo, com texto escuro.
- **Marca (`brand`):** vermelho-alaranjado escuro, igual nos dois temas, com texto creme.
- **Neutros quentes:** creme no tema claro e ameixa escuro no escuro, com pouca saturação.
- **Destaque:** dourado. O token `lime` passa a se chamar `accent`, porque o nome antigo descrevia a cor e não a função.
- **Situações:** erro (`danger`) vira carmim, mais frio que o laranja, para não ser confundido com destaque. Aviso (`warning`) continua âmbar e só aparece com ícone ou texto. Informação (`info`) usa o roxo-azulado das cartas. Sucesso continua verde.
- **Contraste:** todo par de texto passa 4,5:1 e todo ícone ou borda de campo passa 3:1, nos dois temas.

Os valores ficam em `EasyFit/webapp/src/styles/index.css`. O `theme-color` de `index.html` e as cores do `manifest.webmanifest` acompanham os fundos e a marca.

## Impacto

- O mockup continua definindo telas e fluxos, mas não as cores.
- Um ícone novo gerado por `scripts/gerar-icones.sh` não muda a paleta sozinho: se a cor dominante mudar, revise os tokens nesta mesma linha.
