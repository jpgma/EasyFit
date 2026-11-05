# 0016 — Um desafio por vez, com participação automática

**Status:** DECIDIDA. Altera as decisões 0009 e 0013.

## Contexto

O novo mockup (decisão 0014) trata o desafio como a meta do grupo: há um por vez, todo mundo está nele, e não existe botão de participar. A decisão 0009 permitia desafios simultâneos com adesão manual, e a 0013 contava como não cumpridas as semanas anteriores à adesão. Com participação automática, essa contagem puniria quem acabou de entrar no grupo.

## Alternativas consideradas

**Desafios no grupo**

- **Vários simultâneos:** mais flexível, mas toda tela precisaria escolher qual desafio mostrar.
- **Um por vez, sem sobreposição:** a tela é direta, e um novo desafio pode ser agendado para depois do atual.

**Participação**

- **Adesão manual:** dá para ficar de fora, mas cria um passo que todos precisam lembrar de dar.
- **Automática, sem saída:** quem está no grupo está no desafio. Quem não quer participar sai do grupo.

**Quem entra depois do início**

- **Semanas anteriores como não cumpridas:** todos no mesmo calendário, mas a pessoa já entra perdendo sequência e histórico.
- **Ranking proporcional:** mais justo, porém fora de `arquitetura.md` §8.5.
- **Contar a partir da primeira semana inteira:** as semanas anteriores aparecem como "não participou", e o ranking continua igual.

**Volta ao grupo**

- **`joined_at` recomeça a cada entrada:** simples, mas a pessoa perderia as semanas que já cumpriu, e a decisão 0009 prometia devolver a participação.
- **`joined_at` guarda a primeira entrada:** as semanas cumpridas continuam valendo. As semanas em que a pessoa esteve fora ficam zeradas, sem o rótulo "não participou".

## Decisão

- Um grupo não tem dois desafios com datas sobrepostas. Criar um assim recebe 409 `DESAFIO_SOBREPOSTO`, verificado sob a trava do grupo. Desafios cancelados não ocupam o período.
- `participantes_desafio` e `POST /api/desafios/{id}/participar` deixam de existir. Participa todo membro ativo que entrou até o início da última semana do desafio.
- Uma semana conta para o membro se começou no dia da entrada dele ou depois, no fuso do grupo. Quem entra numa segunda conta nessa semana; quem entra numa terça, a partir da segunda seguinte. Treinos anteriores continuam registrados e verificados, mas não contam neste desafio.
- As respostas informam `ContaNestaSemana` (progresso e resumo) e `Participou` (histórico).
- O ranking não muda de fórmula: semanas sem participação não somam nada.
- Voltar ao grupo mantém o `joined_at` original.
- `PUT /api/desafios/{id}` altera nome e descrição, para proprietários e administradores, enquanto o desafio estiver `Agendado` ou `Ativo`. Meta e datas continuam imutáveis, porque valem para todos desde a criação.

## Impacto

- Migração 009 remove `participantes_desafio`; `DesafioResponse` mantém `Participando` e `TotalParticipantes`, agora calculados a partir de `membros_grupo`.
- `ContagensAtividadeDesafio` filtra as semanas pela entrada do membro.
- A interface perde o botão "Participar" e ganha a edição, o aviso de quem entrou na semana e o "não participou" no histórico.
- Como a meta do grupo é somada só entre quem conta na semana, um novo membro não atrapalha o total coletivo.
