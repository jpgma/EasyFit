# 0009 — Desafios, participação e semanas

**Status:** DECIDIDA. Parcialmente substituída pela decisão 0016 (desafios simultâneos, adesão e edição).

## Contexto

A Fase 4 precisa criar desafios, definir a meta e o período e exibir o progresso. A Q008 exige um comportamento definido para semanas parciais, e a decisão 0008 deixou para esta fase a participação em desafios de quem entra ou sai do grupo (Q011). `arquitetura.md` §10.3 não prevê como alguém passa a participar nem como corrigir um desafio criado com erro. Além disso, as atividades só chegam na Fase 5, então ainda não há o que contar no progresso.

## Alternativas consideradas

**Participação**

- **Automática, para todos os membros:** como no mockup, mas a entrada no grupo teria de escrever em desafios, e ninguém poderia recusar.
- **Automática, com saída:** a mais flexível, e a que traz mais regras e telas.
- **Por adesão:** um toque em "Participar", e a fatia de grupos não precisa conhecer desafios.

**Semanas parciais**

- **Datas livres, sem contar semanas parciais:** o desafio pode começar hoje, mas a tela precisa explicar por que uma semana não conta.
- **Datas livres, com meta proporcional:** a mais justa, mas exige uma regra de arredondamento e é difícil de explicar.
- **Só semanas inteiras:** semanas parciais deixam de existir. Um desafio criado na quarta-feira começa na segunda seguinte.

**Correções**

- **Editar até o início:** mais amigável, com mais um modo de formulário, um endpoint e regras.
- **Nenhuma correção:** um erro de digitação ficaria para sempre, ou exigiria o operador no banco.
- **Só cancelar:** meta e datas nunca mudam, então o progresso calculado continua confiável.

**Progresso antes das atividades**

- **Só meta e semana:** nada parece incompleto, mas o item da fase fica em aberto.
- **Antecipar parte da Fase 5:** contaria sessões sem evidência nem verificação.
- **Endpoint definitivo, com contagens zeradas:** a tela e o contrato ficam prontos, e a Fase 5 só acrescenta a contagem.

## Decisão

**Permissões**

| Operação | Proprietário | Administrador | Membro |
|---|---|---|---|
| Ver desafios e progresso | Sim | Sim | Sim |
| Participar | Sim | Sim | Sim |
| Criar e cancelar | Sim | Sim | Não |

- Quem não é membro, ou já saiu, recebe 404: `GRUPO_NAO_ENCONTRADO` nas rotas do grupo e `DESAFIO_NAO_ENCONTRADO` nas do desafio. Membro sem permissão recebe 403 `ACESSO_NEGADO`.
- Criar e cancelar travam o grupo antes de verificar o papel, como definido na decisão 0008.

**Período (Q008)**

- As datas são dias do calendário (`date`), no fuso do grupo. O início é uma segunda-feira, o fim é um domingo, e a última data é incluída. O banco também garante isso.
- O início não pode estar no passado, e um desafio tem no máximo 52 semanas.
- O desafio não guarda fuso próprio, ao contrário do previsto em `arquitetura.md` §7.4: o fuso do grupo não pode ser alterado (decisão 0008), então uma cópia só poderia divergir.
- O "hoje" vem do PostgreSQL (`now() AT TIME ZONE`), porque com `InvariantGlobalization` o .NET não reconhece nomes IANA no Windows.

**Meta**

- De 1 a 7 sessões por semana e duração mínima de 1 minuto a 24 horas. A meta nunca muda depois da criação.
- Vários desafios podem existir ao mesmo tempo no mesmo grupo (Q010).

**Situação**

- `Agendado`, `Ativo`, `Encerrado` e `Cancelado` são calculados a partir das datas e de `cancelled_at`, sem coluna de situação: assim nenhuma tarefa agendada precisa atualizar linhas quando o dia muda.
- Só é possível participar de um desafio ou cancelá-lo enquanto ele estiver `Agendado` ou `Ativo`. Fora disso, a resposta é 409 `DESAFIO_ENCERRADO` ou `DESAFIO_CANCELADO`.
- Cancelar é uma exclusão lógica. O desafio continua visível no grupo e não pode ser reativado.

**Participação (Q011)**

- Quem cria o desafio já participa. Os demais membros aderem quando quiserem, mesmo com o desafio em andamento, e não há como sair do desafio.
- Quem sai do grupo deixa de aparecer entre os participantes porque a consulta cruza com `membros_grupo`, sem alterar `participantes_desafio`. Quem volta ao grupo recupera a participação. A fatia `Grupos` não foi alterada.

**Progresso**

- `GET /api/desafios/{id}/progresso` devolve a meta, o total de semanas, a semana atual e o progresso de cada participante, separando o definitivo do provisório (`arquitetura.md` §8.5).
- Até a Fase 5, todas as contagens são zero. A regra de cálculo (`CalculateProgresso`) já está pronta e testada.

## Impacto

- Novas tabelas `desafios` e `participantes_desafio`, sem `ON DELETE CASCADE`, como na decisão 0008.
- Novas rotas `POST /api/desafios/{id}/participar` e `POST /api/desafios/{id}/cancelar`.
- A Q013 (grupos pequenos) não bloqueia a criação: um grupo costuma criar o desafio antes de convidar as pessoas. A regra de revisores mínimos fica para a Fase 6, quando as revisões existirem.
- **Compromisso aceito:** um desafio criado na quarta-feira só começa na segunda seguinte.
- **Compromisso aceito:** uma correção na meta ou nas datas exige cancelar e criar outro desafio, perdendo as adesões.
- **Compromisso aceito:** a lista de desafios de um grupo não é paginada. Só administradores criam desafios, e os grupos têm até 10 pessoas.
- **Compromisso aceito:** quem adere com o desafio em andamento é tratado como os demais. Se isso afetar a classificação, a Fase 7 decide como considerar as semanas anteriores à adesão, usando `joined_at`.
