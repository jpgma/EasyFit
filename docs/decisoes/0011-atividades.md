# 0011 — Atividades, evidências e feed

**Status:** DECIDIDA. Complementada pelas decisões 0014 (formulário) e pela seção Miniaturas abaixo.

## Contexto

A Fase 5 precisa registrar atividades, anexar evidências, exibir feed e detalhes, e alimentar o progresso dos desafios. `arquitetura.md` §10.4 prevê criação em JSON e upload em rota separada; §11 define armazenamento em disco. A Q009 (semana da atividade) e a Q019 (quantas evidências) estavam abertas. A página Início precisa de atividades recentes em todos os grupos do usuário, o que §10.4 original não listava. §10.4 também não previa leitura autenticada de arquivos.

## Alternativas consideradas

**Criação e upload**

- **Um POST multipart com metadados e arquivos:** menos idas à rede no mobile, mas mistura limite de corpo JSON (1 MB) com uploads de 5 MB e exige mudar §10.4.
- **Dois endpoints (§10.4):** `POST` JSON para metadados e `POST` multipart só para evidências; limites e rate limit independentes.

**Feed no Início**

- **Só feed na página do grupo:** Início continua só com grupos.
- **Agregação no cliente (N+1):** sem endpoint novo, pior em mobile.
- **`GET /api/atividades/recentes`:** uma consulta paginada por cursor.

**Evidência em `RegistroManual`**

- **Ignorar upload silenciosamente:** confuso para o cliente.
- **409 `COMPROVACAO_NAO_APLICAVEL`:** contrato explícito.

## Decisão

**API**

| Operação | Quem | Contrato |
|---|---|---|
| Listar feed do grupo | Membro ativo | `GET /api/grupos/{id}/atividades?limite=&cursor=` |
| Criar atividade | Membro ativo | `POST /api/grupos/{id}/atividades` — **somente** `application/json` |
| Detalhe | Membro do grupo da atividade | `GET /api/atividades/{id}` |
| Anexar evidência | Autor da atividade | `POST /api/atividades/{id}/evidencias` — `multipart/form-data`, um arquivo por requisição |
| Ler evidência | Membro do grupo | `GET /api/atividades/{id}/evidencias/{evidenciaId}` — stream, sem expor `/dados/evidencias` |
| Recentes (Início) | Autenticado | `GET /api/atividades/recentes?limite=&cursor=` — todos os grupos em que é membro |

Quem não é membro recebe 404 `GRUPO_NAO_ENCONTRADO` nas rotas do grupo e 404 `ATIVIDADE_NAO_ENCONTRADA` nas da atividade (mesmo padrão da decisão 0008).

**Comprovação**

- Tipos: `RegistroManual`, `Fotografia`, `CapturaDeTela` (§7.5). Não há configuração por grupo no MVP: os três valem em todo grupo.
- `RegistroManual`: upload retorna 409 `COMPROVACAO_NAO_APLICAVEL`.
- `Fotografia` / `CapturaDeTela`: até **3** evidências por atividade (Q019); ordem por coluna `ordem`. Só o autor anexa, enquanto `situacao_verificacao = AguardandoFeedback`.

**Domínio**

- Semana da atividade (Q009): a semana é a do **`started_at`** convertido para o fuso IANA do grupo; uma atividade não é dividida entre semanas.
- `situacao_verificacao` inicia em `AguardandoFeedback`; transições e `revisores_atividade` ficam na Fase 6.
- `sha256` de cada arquivo é persistido; hash igual **não** impede nova evidência (Q020). Idempotência na criação fica fora desta fase.

**Progresso do desafio**

- Na semana calendário atual do desafio, contar atividades do participante com `duracao_segundos >= duracao_minima_segundos` do desafio.
- Definitivo: `Verificada`. Provisório: elegíveis em `AguardandoFeedback`, `EmRevisao` ou `Contestada` (§8.5).

**Paginação**

- Feed e recentes: cursor opaco (`started_at` + `id`), limite padrão 20, máximo 50.

## Miniaturas e formulário (acréscimo)

O novo mockup mostra uma imagem em cada cartão do feed, e baixar a evidência inteira em cada cartão custaria caro em conexões móveis.

- **Formulário:** uma área de envio, até 3 evidências, sem escolher o tipo. Sem arquivo é `RegistroManual`; com arquivo, `Fotografia`.
- **Geração no navegador:** um JPEG de 240 px, com qualidade 0,6, gerado com a foto reduzida. O servidor continua sem bibliotecas de imagem, e o canvas descarta metadados, inclusive a localização (Q030).
- **Envio:** parte opcional `miniatura` em `POST /api/atividades/{id}/evidencias`, na mesma requisição. Até 100 KB, somente JPEG. Uma miniatura inválida recusa o envio inteiro.
- **Leitura:** `GET /api/atividades/{id}/evidencias/{evidenciaId}/miniatura`, com a mesma verificação de grupo da evidência e `Cache-Control: private, max-age=604800, immutable`. Evidências não podem ser trocadas, então a miniatura não muda.
- **Sem miniatura:** o cartão mostra o ícone de câmera.
- **Grupo sem revisores:** a primeira evidência verifica a atividade, e as seguintes continuam sendo aceitas.
- **Compromisso aceito:** a miniatura vem do aparelho do autor, então um cliente alterado poderia enviar uma que não corresponde à foto. Quem avalia sempre vê a evidência completa, por isso a verificação não é afetada.
- **Compromisso aceito:** quem sai do grupo pode continuar vendo por até 7 dias uma miniatura que já esteja no cache do próprio navegador.
- **Service Worker:** quando existir (Fase 8), não pode guardar esta rota (§6.3).

## Impacto

- Migração `006-create-atividades.sql` com `atividades` e `evidencias_atividade`.
- Nova fatia `Features/Atividades/` e `Infrastructure/Files/`.
- `ObterProgresso` passa a consultar `atividades`.
- Frontend: registro em duas etapas na mesma tela (JSON, depois uploads sequenciais).
- Q009 e Q019 marcadas como decididas neste ADR.
