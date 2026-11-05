# 0007 — Mockup como referência visual

**Status:** SUBSTITUÍDA pela decisão 0014

## Contexto

`arquitetura.md` §5.2 define uma direção visual, mas não uma identidade concreta. O projeto passou a ter um mockup navegável, mantido fora deste repositório (`easyfit-mockup-main`), com todas as telas, paleta e componentes. Ele foi gerado com dados falsos e não segue as regras do repositório, então é preciso definir o que vale quando os dois divergem.

## Alternativas consideradas

- **Copiar o código do mockup:** rápido, mas traz dependências e padrões proibidos, como Google Fonts, Supabase e estado global em contexto.
- **Usar apenas como inspiração:** preserva o repositório, mas deixa sem resposta quem decide a aparência.
- **Referência de aparência e fluxos, com a arquitetura prevalecendo em regras e nomes:** cada tela é reescrita na estrutura do repositório.

## Decisão

O mockup define a aparência e os fluxos das telas. `arquitetura.md`, `AGENTS.md` e o glossário prevalecem em regras de negócio, nomes, segurança e dependências.

- **Paleta:** as cores do mockup ficam em `@theme`, em `styles/index.css`. Páginas e componentes usam tokens semânticos (`bg-surface`, `text-muted`), nunca `dark:` nem uma paleta diretamente.
- **Tema escuro:** o mockup só tem o tema claro. O escuro foi criado neste repositório e acompanha o sistema, sem seletor manual.
- **Acessibilidade:** textos e bordas de campos foram escurecidos onde o mockup não atinge o contraste WCAG AA.

Divergências conhecidas, em que o mockup não deve ser seguido:

| Mockup | O que vale |
|---|---|
| Inter pelo Google Fonts | Fontes do sistema |
| Link "Esqueceu a senha?" | Fora do MVP (decisão 0006) |
| Senha com mínimo de 6 caracteres | Mínimo de 8 (decisão 0006) |
| Reação `moral` | `Kudos` |
| Situações `pending`, `verified`, `under_review`, `rejected` | `AguardandoFeedback`, `Verificada`, `EmRevisao`, `Contestada`, `Rejeitada` |
| Uma reação define a situação da atividade | Regra de consenso de `arquitetura.md` §8.3 |
| Seis tipos de atividade | Nove tipos de `arquitetura.md` §7.5 |
| Código de convite curto e reutilizável (`PTPRAIA-2026`) | Link com token aleatório de uso único (decisão 0008) |
| Um único link de convite por grupo | Um link por pessoa, mostrado uma só vez (decisão 0008) |
| Etapa separada para aceitar o convite depois do cadastro | Uma etapa só para quem cria a conta; aceitar é só para quem já tem conta (decisão 0008) |
| E-mail de quem convidou e dos membros visível | Só nome e papel (decisão 0008) |
| Um único grupo por pessoa | Vários grupos, escolhidos pela URL (decisão 0008) |
| Botão de convidar para todos os membros | Só para Proprietário e Administrador (decisão 0008) |
| Um único desafio por grupo | Vários desafios ao mesmo tempo (decisão 0009) |
| Todos os membros participam do desafio | Participação por adesão (decisão 0009) |
| Botão "Editar desafio" | Meta e datas não mudam: só é possível cancelar (decisão 0009) |
| Metas individuais (`weeklyGoal` por membro) | Mesma meta para todos (`arquitetura.md` §7.4) |

## Impacto

- As próximas telas partem do mockup, mas com seus textos e regras conferidos contra esta tabela.
- A dependência `lucide-react` foi adicionada para os ícones, empacotados no bundle.
- O mockup não é versionado aqui. Se ele mudar, esta decisão continua valendo, e novas divergências entram na tabela.
