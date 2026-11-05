# 0014 — Novo mockup como referência visual

**Status:** DECIDIDA. Substitui a decisão 0007.

## Contexto

A decisão 0007 adotou o mockup `easyfit-mockup-main` como referência de aparência e fluxos. Agora existe um mockup mais completo, `fitas-e-amigos`, mantido fora deste repositório: um protótipo navegável, só de frontend, com dados de exemplo e outra identidade visual. Ele usa TanStack Start com SSR, Radix, shadcn, sonner e Google Fonts, e parte de regras de produto diferentes das decididas aqui.

## Alternativas consideradas

**Código do mockup**

- **Copiar:** rápido, mas traz SSR, carregamento externo de fontes e várias dependências de interface, proibidos ou indesejados neste repositório.
- **Reescrever cada tela na estrutura do repositório, como na decisão 0007:** mais trabalho, sem dependências novas de interface.

**Grupo atual nas abas**

- **Estado de "grupo ativo" guardado no navegador:** contraria a decisão 0008.
- **Abas por grupo na URL (`/grupos/:id/inicio`):** quem tem um grupo vê exatamente o mockup; um seletor só aparece a partir de dois grupos.

**Fontes**

- **Do sistema:** nenhum peso adicional, mas perde boa parte da identidade do mockup.
- **Bricolage Grotesque e Plus Jakarta Sans no bundle, só com o subconjunto latino:** 68 KB no primeiro acesso, sem servidores externos.

**Tokens de cor**

- **Adotar os nomes do mockup:** `muted` passaria de texto a fundo, invertendo o significado atual.
- **Manter os nomes do repositório:** os valores mudam, e os nomes novos (`success`, `warning`, `info`, `lime`) são acrescentados.

## Decisão

O mockup `fitas-e-amigos` define a aparência e os fluxos das telas. `arquitetura.md`, `AGENTS.md` e o glossário prevalecem em regras de negócio, nomes, segurança e dependências.

- **Paleta:** valores OKLCH do mockup atrás dos tokens semânticos de `styles/index.css`. Pares de texto foram escurecidos onde o mockup não atinge o contraste WCAG AA, conferidos nos dois temas.
- **Tema escuro:** o mockup só tem o tema claro. O escuro foi derivado dele e acompanha o sistema, sem seletor manual.
- **Avatares:** a matiz vem do identificador do usuário, em `--avatar-hue`, e a luminosidade vem do tema. É a única exceção à regra de usar apenas tokens de cor.
- **Reações na interface:** `Kudos` aparece como "Dar moral" e "deu moral"; no código, na API e no banco continua `Kudos`. Cada reação tem sempre o mesmo símbolo, usado ao lado do texto ou sozinho quando falta espaço: 💪 `Kudos`, 🤨 `Suspeito`, ❌ `Rejeitar`.
- **Componentes:** próprios. Sem Radix, shadcn, sonner, recharts nem TanStack Router.

Divergências conhecidas, em que o mockup não deve ser seguido:

| Mockup | O que vale |
|---|---|
| Google Fonts | Fontes no bundle |
| "Esqueci minha senha" na entrada | Fora do MVP (decisão 0006) |
| Grupos de 2 a 10 amigos | Até 10; um grupo pode ter só uma pessoa (decisão 0012) |
| Situações `pendente`, `verificada`, `revisao`, `rejeitada` | `AguardandoFeedback`, `Verificada`, `EmRevisao`, `Contestada`, `Rejeitada` |
| Limites fixos de 2 Kudos e 2 rejeições | Regra de consenso de `arquitetura.md` §8.3 e §8.4 |
| Progresso conta tudo que não foi rejeitado | Definitivo e provisório separados (`arquitetura.md` §8.5) |
| Seis tipos de atividade | Nove tipos de `arquitetura.md` §7.5 |
| Um único grupo por pessoa | Vários grupos, escolhidos pela URL (decisão 0008) |
| E-mail dos membros visível | Só o próprio; colegas veem nome e papel (decisão 0008) |
| Tela `/estados` | Página de demonstração, não portada |
| Imagens do Unsplash | Só evidências enviadas pelos membros |

## Impacto

- Novas dependências de build `@fontsource-variable/bricolage-grotesque` e `@fontsource-variable/plus-jakarta-sans`. Resolvem o carregamento das fontes sem servidores externos e só fornecem os arquivos, então não há código de terceiros em execução. O backend não é afetado, e Native AOT não se aplica. Seria possível copiar os arquivos para o repositório, mas a dependência deixa a origem e a licença (SIL OFL) explícitas.
- `ProgressBar` foi substituída por `ProgressRing` e `Segments`. Novos `PageHeader`, `HistoryChart`, `Toast` e `navigation/AppLayout`.
- A tela Início passou a ser por grupo. A lista de grupos, a criação de grupo e o campo de convite foram para o Perfil.
- O formulário de registro não pede mais o tipo de comprovação: sem arquivo envia `RegistroManual`; com arquivo envia `Fotografia`. `CapturaDeTela` continua válida na API.
- O mockup não é versionado aqui. Se ele mudar, esta decisão continua valendo, e novas divergências entram na tabela.
- **Compromisso aceito:** com mais de um grupo, recarregar a página em uma tela fora de um grupo volta as abas ao primeiro grupo da lista, porque o último visitado fica só em memória.
