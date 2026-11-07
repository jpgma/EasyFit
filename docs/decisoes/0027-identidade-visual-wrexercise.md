# 0027 — Identidade visual Wrexercise

**Status:** DECIDIDA. Substitui a paleta da decisão 0025 e as fontes da decisão 0014; o resto delas continua valendo.

## Contexto

Com o nome Wrexercise (decisão 0026), o aplicativo ganhou um ícone novo e um briefing de marca (`docs/prompts/refactor-identity.md`): "uma marca de streetwear urbano encontra um jogo de arcade indie e um clube esportivo de amigos". A paleta da decisão 0025, tirada do ícone antigo, e a Bricolage Grotesque não combinavam com o ícone novo nem com o tom competitivo e brincalhão que o produto quer ter.

## Alternativas consideradas

**Fonte de display**

- **Manter a Bricolage Grotesque:** sem trabalho, mas suave demais para títulos de desafio e números de placar.
- **Barlow Condensed:** vários pesos, mais flexível e menos marcante.
- **Anton:** condensada, forte e expressiva, com um único peso. É a preferida do briefing.

**Contorno e sombra**

- **Cards com sombra difusa, como antes:** limpo, mas genérico.
- **Tudo como adesivo (contorno grosso + sombra sólida):** marcante, mas cansa e compete com a informação.
- **Adesivo só nos destaques:** cards comuns limpos, e contorno grosso nos botões principais, selos, no card da semana e nas conquistas.

## Decisão

- **Paleta:** Asphalt `#222633`, Chalk `#F5F1E8`, Coral `#FF684A`, Electric Violet `#7957E8`, Signal Yellow `#FFD34E`, Lime `#A8D978` e Neutral Grey `#A9B0C2`. Os valores brutos ficam só como variáveis em `styles/index.css`; páginas e componentes usam os tokens semânticos.
- **Funções:** coral é a ação principal (`primary`, `brand`); violeta marca o que é do Jogo (`game`); amarelo, recompensas e conquistas (`reward`, que substitui `accent`); lime, progresso e meta batida (`progress`). Um componente usa uma cor de destaque só.
- **Temas:** claro com Chalk ao fundo e Asphalt no texto; escuro com Asphalt ao fundo, Chalk no texto e superfícies de carvão um pouco mais claras. Os dois seguem o sistema, sem seletor.
- **Contraste:** todo par de texto passa 4,5:1 e todo ícone ou borda de campo passa 3:1, nos dois temas. Por isso, o texto sobre coral é Asphalt (5,3:1); links e foco no tema claro usam um coral escurecido; o cinza do texto secundário no claro é mais escuro que o Neutral Grey; e lime nunca é cor de texto no tema claro (`success` é um verde escurecido). O erro (`danger`) é um vermelho frio, separado do coral.
- **Fontes:** Anton (`@fontsource/anton`, só latino, peso 400) para `h1`, `h2`, números de placar (`font-score`), títulos de desafio e nomes de conquista, em caixa alta. Plus Jakarta Sans continua em todo o resto. A Anton nunca recebe `font-bold`.
- **Linguagem gráfica:** raios moderados; contorno de 2px e sombra sólida (`shadow-sticker`) só em destaques (`card-sticker`, `btn-primary`, `badge-*`, botão de registrar); halftone (`halftone`, `halftone-ink`) em cantos de destaque; marcação tracejada de quadra (`court-line`); explosão de quadrinho (`burst`) só para comemorar.
- **Movimento:** o botão principal afunda ao ser pressionado (`press`); `pop` e `burst` tocam uma vez ao bater a meta, ao registrar um treino e ao mostrar conquistas. A regra global de `prefers-reduced-motion` desliga tudo. Sem biblioteca de animação.
- **Voz:** textos casuais, confiantes e um pouco provocadores em pt-BR, sem gíria demais e sem culpar quem ficou parado. Erros, confirmações destrutivas e textos de segurança continuam diretos.
- **Pixel art** fica para avatares, Jogo, conquistas e momentos especiais (ver `gamificacao.md`). Controles comuns continuam com ícones Lucide.

## Impacto

- O ícone e a paleta voltam a ser do mesmo produto. `theme-color` e o `manifest.webmanifest` usam Chalk e Asphalt.
- Dependência trocada: sai `@fontsource-variable/bricolage-grotesque` e entra `@fontsource/anton` (SIL OFL). Ela só fornece o arquivo da fonte, empacotado no build, sem código em execução e sem servidor externo. O backend e o Native AOT não são afetados.
- Um ícone novo não muda a paleta sozinho: se a identidade mudar, revise os tokens nesta mesma linha.
