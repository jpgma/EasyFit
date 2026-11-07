# 0024 — Cartas como destino da Energia e avatar importado

**Status:** DECIDIDA (direção). Substitui partes de `0023-gamificacao.md`. As recompensas das Jogadas e as notificações foram revistas em `0033-regras-de-energia-e-jogadas.md`. Detalhes em `../gamificacao.md` e `../avatar-formato.md`.

## Contexto

Na proposta da decisão 0023, a Energia comprava roupas e acessórios permanentes. Depois que a pessoa compra o que quer, a Energia deixa de importar, e o modo Jogo perde o motivo para voltar.

Uma proposta seguinte trouxe cartas consumíveis de provocação, resposta e defesa entre colegas, com livro-razão e sem loja de cosméticos. Ela dá à Energia um uso que se renova toda semana, mas trazia mecânicas redundantes (defesas que fazem o mesmo que recusar), um livro-razão e objetivos de duração.

A arte também mudou: o projeto vai derivar os sprites do template CC0 *2D Platformer Character Template* (quadros de 16 × 16, personagem de lado), e as pessoas querem poder desenhar o próprio avatar.

## Alternativas consideradas

**Destino da Energia**

- **Só cosméticos (0023):** simples, mas o gasto acaba quando a coleção fica completa.
- **Cosméticos e cartas:** dois catálogos para balancear e o dobro de arte.
- **Só cartas, com cosméticos liberados por conquistas:** o gasto se renova e a conquista continua visível na cena.

**Saldo**

- **Livro-razão com chaves de idempotência:** flexível, mas exige uma escrita a cada verificação e reabre a decisão 0023.
- **Cálculo na leitura, com o preço guardado em cada carta:** reaproveita o modelo da 0023. Usar uma carta não muda o saldo, e as recompensas das Jogadas são derivadas das atividades verificadas.

**Defesas**

- **Manter BLOQUEIO e ESCUDO:** fazem o mesmo que recusar e sair do modo Jogo, que são grátis.
- **Tirar as defesas:** a coleção diminui e toda carta faz algo que não dá para fazer de graça.

**Avatar**

- **Camadas de roupa compradas na loja (0023):** a arte cresce com quadros × itens.
- **Máscara de cores sobre o template:** pouca arte, mas pouca expressão.
- **Folha de sprites importada, num formato público:** cada pessoa desenha o que quiser. Exige upload validado e uma forma de remover imagens impróprias.

**Cena**

- **Fileira com rolagem lateral:** simples, mas as interações ficam sem palco.
- **Círculo em 2,5D, virado para o centro:** as interações acontecem no centro. O andar de lado na vertical desliza, o que é aceito como estilo.

## Decisão

- **A Energia só compra cartas.** Não existe loja de cosméticos. As conquistas liberam Enfeites de cena, calculados na leitura.
- **Saldo calculado na leitura:** `Energia ganha − soma de cartas.preco`. A Energia ganha inclui as Jogadas concluídas. O livro-razão continua descartado.
- **Cartas consumíveis**, no catálogo do código: cinco `Provocacao` (+10 MIN, +20 MIN, VARIEDADE, FIM DE SEMANA, ANTES DAS 8) e quatro `Resposta` (DESAFIO DUPLO, APOSTA, PRAZO EXTRA, MUDANÇA DE PLANOS). Isso substitui o item "nada de consumíveis" da 0023.
- **Recusar é sempre grátis.** Não existem cartas de defesa.
- **Os dois ganham com uma Jogada concluída:** cerca de 1,5 × o preço para quem recebeu e 0,5 × para quem enviou.
- **Cartas de duração** (+10 MIN e +20 MIN) ficam, relativas à duração mínima do desafio do grupo. É uma exceção consciente ao princípio "recompensa constância, não desempenho", aceita porque a carta é opcional e pode ser recusada.
- **APOSTA** é a única aposta: a pessoa arrisca só a própria carta para ganhar mais. Isso substitui o item "nada de apostas" da 0023. Apostas entre pessoas continuam descartadas.
- **Só quem está no modo Jogo** envia e recebe cartas. O modo App continua igual.
- **A carta recebida gera uma `Notificacao`** no aplicativo, como as da decisão 0020.
- **Mão por pessoa**, com no máximo 5 cartas, e limites contra excesso de cartas por par e por pessoa.
- **Toca aqui e Desafio extra saem.** Ficam as Missões semanais e o Rival da semana.
- **Avatar por folha de sprites** de 256 × 384 (células de 32 × 32), num formato público (`avatar-formato.md`). Folhas padrão CC0 no bundle e importação validada no navegador e no servidor. O `Proprietario` e os `Administrador` podem redefinir o avatar de um membro para o padrão. Isso substitui "só sprites CC0 do bundle" e a tabela de camadas da 0023.
- **Cena em círculo 2,5D**, com encenações no centro, vistas na próxima visita.

## Impacto

- `gamificacao.md` foi reescrito. Ele tem as fases G1 a G7.
- Tabelas planejadas: `cartas`, `jogadas`, `avatares` (caminho da folha importada) e `rivais`. Saem `compras`, `toques` e `desafios_extras`.
- As folhas importadas ficam no disco, em `/dados/avatares/`, com as mesmas regras das evidências.
- O glossário de `AGENTS.md` troca `Item`, `Compra`, `TocaAqui` e `DesafioExtra` por `Carta`, `Provocacao`, `Resposta`, `Jogada`, `Mao` e `Enfeite`.
- Como na 0023, cada fase atualiza `funcionalidades.md`, `modelo-dados.md` e as migrações quando for implementada.
