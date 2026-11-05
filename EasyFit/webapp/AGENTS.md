# Frontend — React + TypeScript

Complementa o `AGENTS.md` da raiz. Os comandos `npm` rodam nesta pasta, e o build gera `../wwwroot`, de onde o backend serve a SPA.

## Estrutura

```text
src/
├── components/   # componentes reutilizáveis, sem regra de negócio
├── features/     # uma pasta por funcionalidade, com index.ts como interface pública
├── pages/        # telas, sempre carregadas sob demanda
├── navigation/   # rotas
├── services/     # cliente HTTP
└── styles/
```

- Uma feature importa outra apenas pelo `index.ts` (`@/features/grupos`). O ESLint verifica isso.
- Use o alias `@/` em importações entre pastas.

## Estado e dados

- Dados remotos: TanStack Query. Estado de interação: estado local do React. Sem Redux.
- Toda chamada à API passa por `services/api.ts`, na mesma origem. Sem CORS.
- Evite polling. Pagine listas.

## Interface

- Mobile-first, com o Apple Human Interface Guidelines como inspiração, sem copiar componentes proprietários.
- As telas seguem o mockup do projeto. Antes de portar uma, confira a tabela de divergências em `docs/decisoes/0014-novo-mockup-como-referencia-visual.md`.
- Tailwind e componentes próprios. Sem bibliotecas de UI pesadas.
- Cores só por tokens semânticos (`bg-surface`, `text-muted`), definidos em `styles/index.css` para os dois temas. Não use `dark:` nem as paletas diretamente em páginas e componentes. A única exceção é a matiz do `Avatar`, que vem do identificador do usuário.
- Nada carregado de servidores externos: fontes e ícones ficam no bundle ou são do sistema. Fontes só com o subconjunto latino.
- Cada reação tem sempre o mesmo símbolo (`tipoReacaoEmojis`): 💪 `Kudos`, 🤨 `Suspeito`, ❌ `Rejeitar`. Use-o ao lado do texto ou sozinho quando faltar espaço; sozinho, inclua um texto para leitores de tela.
- Todo texto visível em pt-BR.

## Desempenho

Pensado para conexões móveis lentas: bundles pequenos, páginas com `lazy`, evidências carregadas só quando solicitadas. O feed mostra apenas miniaturas, geradas no navegador e com `loading="lazy"`; a imagem completa só aparece ao abrir a atividade.

## Sessão e PWA

- A sessão é um cookie `HttpOnly`: o JavaScript nunca vê o token. O ESLint bloqueia o uso de `localStorage`.
- O Service Worker não guarda respostas autenticadas nem imagens privadas.
