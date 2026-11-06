# Gamificação — EasyFit

> **Status:** proposta refinada, ainda sem código. A direção geral está em `decisoes/0023-gamificacao.md`. Cada fase atualiza `funcionalidades.md` quando for entregue.

## 1. Visão

O modo **Jogo** é uma tela inicial alternativa, leve e bem-humorada, para quem gosta de um toque de jogo. Cada pessoa escolhe entre **App** (a tela inicial de hoje) e **Jogo**. Nada muda para quem continua no App.

No Jogo, os membros do grupo aparecem como avatares em pixel-art numa cena. Os treinos verificados rendem **Energia**, que compra roupas e acessórios para o avatar. Missões semanais e interações amistosas completam a experiência.

### 1.1. Princípios

1. **A meta oficial vem primeiro.** O Jogo nunca altera a verificação, as metas, a classificação nem as sequências.
2. **Recompensa constância, não desempenho.** Nada depende de duração, intensidade ou calorias além do que a meta já exige.
3. **Ninguém prejudica ninguém.** Nenhuma interação remove Energia, invalida atividades ou dificulta a meta de outra pessoa.
4. **Simples e discreto.** Nada de missões diárias, apostas ou lembretes: o Jogo não pede atenção fora dos treinos.
5. **Tudo calculado no servidor**, a partir dos dados que já existem sempre que possível.
6. **A classificação continua igual.** A Energia não aparece no ranking e não existe ranking de Energia.

### 1.2. Direção criativa

Pixel-art contemporâneo com estilo de vida urbano: streetwear, tênis, roupa de academia, fones de ouvido, café, apartamento, academia, praça.

Evitar fantasia, medieval, monstros, magia, armas, combate, classes e atributos de RPG.

O avatar é uma versão estilizada de uma pessoa real.

## 2. Modos da tela inicial

| Modo | Tela inicial (`/grupos/:id/inicio`) |
|---|---|
| `App` | A tela atual, sem mudanças. |
| `Jogo` | A cena do grupo (§3). |

- O modo é uma preferência da conta e vale em todos os dispositivos.
- O padrão é `App`, inclusive para contas novas.
- No modo App, a tela inicial mostra **uma vez** um cartão convidando para experimentar o Jogo. A pessoa pode ativar ou fechar o cartão. A dispensa fica num cookie comum, como a do aviso de instalação.
- A troca de modo fica em Configurações.
- O modo muda só a tela. Todos acumulam Energia e têm avatar, mesmo no modo App.

## 3. Cena do grupo

A tela Jogo mostra:

- a cena com o avatar de cada membro ativo do grupo;
- o seu progresso na semana (o mesmo anel da tela App, em versão compacta);
- o saldo de Energia (a partir da fase G3);
- o botão "Registrar atividade";
- o aviso de atividades para avaliar, como hoje.

A pose de cada avatar vem do status semanal no desafio em destaque, com os dados que `useProgresso` já entrega:

| Situação na semana | Pose |
|---|---|
| Meta cumprida (`metaCumprida`) | Comemorando |
| Tem sessões provisórias | Alongando |
| Demais casos | Parado |
| Não conta nesta semana, ou sem desafio | Parado |

Tocar num avatar abre a página do membro. A partir da fase G5, também permite dar um "Toca aqui".

## 4. Avatar

- Cada usuário tem um único avatar, igual em todos os grupos.
- Quem nunca configurou o avatar recebe um avatar padrão, escolhido de forma estável pelo identificador do usuário.
- Camadas: corpo, cabelo, parte de cima, parte de baixo, calçado e um acessório.
- Animações iniciais: parado, alongando e comemorando. Leves, com poucos quadros e reprodução determinística.
- Os sprites vêm de um pacote **CC0**, ficam no repositório com o arquivo de licença e são compostos no navegador. Nada carregado de servidores externos.
- Fora da cena, o componente `Avatar` mostra a **cabeça do avatar** no lugar das iniciais para quem já configurou o seu. Isso vale para todos, inclusive para quem está no modo App.

## 5. Energia

Energia (EN) é a moeda do Jogo. É única por usuário, somando todos os grupos.

### 5.1. Como se ganha

| Evento | EN |
|---|---:|
| Atividade verificada | 20 |
| Semana com a meta cumprida | 40 |
| Sequência de 4 semanas | 50 |
| Sequência de 8 semanas | 100 |
| Sequência de 12 semanas | 200 |
| Desafio concluído (ver §11) | 150 |
| Missão semanal concluída (§7) | 10 a 30 |
| Rival da semana vencido (§8.2) | 30 |
| Desafio extra concluído (§8.3) | 25 para quem aceitou, 10 para quem convidou |

As sequências seguem a mesma regra das conquistas: a melhor sequência num único desafio, sem somar desafios. Cada marco conta uma vez.

### 5.2. Bônus por atividade

| Qualificador | EN |
|---|---:|
| Começou antes das 06:00 | 10 |
| Começou entre 06:00 e 08:00 | 5 |
| Fim de semana | 5 |
| Primeira atividade da semana | 5 |
| Primeira atividade num dia da semana ainda sem treino | 5 |
| Primeiro tipo de atividade novo na semana | 10 |

- Máximo de **15 EN de bônus por atividade**.
- Horários e semanas no fuso do grupo da atividade.
- Várias atividades no mesmo dia rendem o bônus de dia novo só uma vez.

### 5.3. Cálculo na leitura

Não existe carteira nem livro-razão. O saldo é calculado a cada consulta, como as conquistas (`decisoes/0018-perfil-e-pagina-do-membro.md`):

```text
saldo = Energia ganha (calculada) − soma das compras
```

Regras que garantem um saldo estável:

- Só atividades `Verificada` geram Energia. `Verificada` é final, então um crédito nunca desaparece.
- Os qualificadores ("primeira da semana", "dia novo", "tipo novo") são calculados sobre as atividades **não rejeitadas**, em ordem de `started_at`. Uma atividade pendente já ocupa sua posição. Se ela for rejeitada, a seguinte pode ganhar o bônus, mas nenhum crédito é perdido.
- Por isso a Energia ganha **só cresce**, e o saldo nunca fica negativo se cada compra verificar `saldo >= preço`.
- Atividades de grupos que a pessoa deixou continuam valendo, como as conquistas.

A regra fica numa função pura (`CalculoEnergia`), com testes unitários.

## 6. Loja

- O catálogo fica no código: de 15 a 20 itens, nas camadas do avatar (§4).
- Alguns itens básicos de cada camada são gratuitos e não precisam de compra.
- Preços de referência: roupa básica 50, cabelo 80, boné ou óculos 100, fone de ouvido 120, tênis 150, roupa rara 400.
- Uma compra é permanente. Um item não pode ser comprado duas vezes (`UNIQUE (usuario_id, item)`).
- A compra roda numa transação que bloqueia a linha do usuário, recalcula o saldo e só grava se `saldo >= preço`.
- Nada de consumíveis, itens temporários ou itens com data.

## 7. Missões semanais

- Toda semana, cada pessoa recebe de 2 a 3 missões, sorteadas de uma lista fixa no código. O sorteio é determinístico por usuário e semana, então não precisa ser gravado.
- O progresso vem das atividades verificadas, como tudo o mais.
- A mesma atividade pode contar para várias missões.
- Exemplos:
  - Treinar em 3 dias diferentes.
  - Registrar 2 tipos de atividade diferentes.
  - Cumprir a meta antes de sexta-feira.
  - Treinar num fim de semana.
- Missões não exigem mais do que uma participação saudável nem um desempenho físico específico.
- Não há missões diárias.

## 8. Interações amistosas

Todas são voluntárias, inofensivas e não tocam no desafio oficial.

### 8.1. Toca aqui

- Na cena, tocar no avatar de um colega permite dar um "Toca aqui" (um *high five*).
- No máximo um por par de pessoas por dia.
- O colega vê o toque como um pequeno indicador junto do avatar dele na cena. Não cria notificação.
- Não vale Energia.

### 8.2. Rival da semana

- Cada pessoa pode escolher um rival por semana no grupo. O rival não precisa aceitar e não perde nada.
- Critério normalizado: vence quem cumprir a meta semanal primeiro (pelo horário de verificação da sessão que completou a meta). Se só um cumprir, esse vence. Se ninguém cumprir, não há vencedor.
- Prêmio: 30 EN e um selo, só para quem escolheu.
- Não há aposta de Energia.

### 8.3. Desafio extra

- Uma pessoa convida um colega para um treino extra opcional.
- O colega tem 72 horas para aceitar e, depois de aceitar, 72 horas para registrar uma atividade que seja verificada.
- Qualquer atividade normal serve, e ela conta para a meta oficial como sempre. Não existe tipo especial de atividade.
- Quem aceitou recebe 25 EN e quem convidou recebe 10 EN.
- Recusar ou não cumprir não tem consequência.

## 9. Fases de implementação

Cada fase entrega algo visível e pode ser a última. Todas ficam na fatia `Jogo` (backend `Features/Jogo`, frontend `features/jogo`), exceto onde indicado.

### G1 — Modo Jogo e cena

- Migração: `usuarios.modo_inicio text NOT NULL DEFAULT 'App'`, com `CHECK` para `App` e `Jogo`.
- `PUT /api/usuarios/eu/modo-inicio` e o campo `modoInicio` na sessão (fatias `Usuarios` e `Autenticacao`).
- Opção na página Configurações e cartão de convite único na tela App.
- `InicioPage` escolhe entre a visão App atual e a cena, na mesma rota.
- Escolha do pacote CC0, com o arquivo de licença junto dos sprites.
- Cena com avatares padrão e poses pelo progresso. Ainda sem Energia nem personalização.

### G2 — Avatar

- Tabela `avatares`: uma linha por usuário, uma coluna por camada.
- Editor em `/perfil/avatar`, só com os itens gratuitos.
- `GET /api/grupos/{id}/avatares` devolve os avatares dos membros, usados pela cena e pelo `Avatar`, que passa a desenhar a cabeça.

### G3 — Energia e Loja

- Regra pura `CalculoEnergia` e a consulta SQL que a alimenta.
- Tabela `compras`.
- `GET /api/jogo/loja` (catálogo, itens comprados e saldo) e `POST /api/jogo/compras`.
- Saldo na cena e no Perfil.

### G4 — Missões semanais

- Lista fixa, sorteio determinístico e progresso derivado.
- A recompensa entra na Energia ganha.

### G5 — Toca aqui

- Tabela `toques`, com `UNIQUE` por par e dia.
- Indicador na cena.

### G6 — Rival da semana e Desafio extra

- Tabelas `rivais` e `desafios_extras`.
- Resultados e recompensas derivados das atividades verificadas.

## 10. Adiado e descartado

**Adiado**, sem fase definida:

| Ideia | Motivo |
|---|---|
| Eventos do grupo (festival de fim de semana, bingo, 100 sessões) | Precisam de datas, regras e itens próprios. Reavaliar depois da G6. |
| Marcos coletivos do grupo | Idem. |
| Itens de edição limitada | Exigem catálogo com datas. |
| Animações especiais, poses extras, fundos de cena | Dependem de mais arte. Podem entrar na loja depois. |

**Descartado:**

| Ideia | Motivo |
|---|---|
| Carteira e livro-razão de transações | A Energia calculada na leitura atende sem sincronização nem idempotência. |
| Consumíveis (Proteção, Boost, Modo Invisível) | Só fazem sentido com pegadinhas e efeitos temporários, que não queremos. |
| Pegadinhas e efeitos sobre outros jogadores | Contrariam "ninguém prejudica ninguém". |
| Duelos com aposta e compromissos (*bounties*) | Apostar Energia traz perda e pressão, o contrário de leve. |
| Missões diárias | Pedem abrir o aplicativo todo dia e distraem do objetivo. |
| Ranking de Energia | Mistura jogo com desempenho e cria uma segunda classificação. |
| Distância e eventos baseados em distância | Fora do escopo (`arquitetura.md` §19). |

## 11. Questões em aberto

1. **O que é "desafio concluído" (150 EN)?** Recomendação: desafio `Encerrado` com todas as semanas que contavam para a pessoa cumpridas.
2. **Semana das missões para quem está em vários grupos.** Recomendação: avaliar cada atividade na semana do fuso do seu grupo e usar como referência a semana do grupo em destaque.
3. **Qual pacote CC0?** Critérios: estilo urbano, quadros pequenos (por exemplo 32×32), poucos quadros por animação, camadas compatíveis entre si e no máximo 200 KB no total.
4. **Bônus antes das 06:00.** Fica por enquanto. Revisar se ele estimular treino de madrugada em vez de constância.
5. **Cena com 10 membros em tela pequena.** Definir o layout (fileiras, rolagem lateral) e o limite de animações simultâneas.

## 12. Critérios de sucesso

1. Quem usa o Jogo registra treinos pelo menos com a mesma regularidade de antes.
2. Personalizar o avatar é divertido e não toma tempo do treino.
3. Pessoas com condicionamentos diferentes têm as mesmas chances.
4. Nenhuma mecânica dificulta a meta de outra pessoa.
5. O desafio oficial continua compreensível e confiável. Quem prefere o App não percebe diferença, salvo pelos avatares no lugar das iniciais.
