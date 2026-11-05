# 0017 — Link de convite reutilizável

**Status:** DECIDIDA. Altera a decisão 0008.

## Contexto

A decisão 0008 adotou convites de uso único, com o token só como hash no banco e o link visível uma única vez. Num grupo de até 10 amigos, isso obriga a gerar um link por pessoa e a perder o link se ele não for copiado na hora. O novo mockup (decisão 0014) mostra um link só, sempre visível, que o administrador cola no grupo de mensagens.

## Alternativas consideradas

**Uso do link**

- **Uso único:** cada link entra uma pessoa, mas dá mais trabalho e não combina com colar o link num grupo de mensagens.
- **Reutilizável, com validade:** um link por grupo. A capacidade de 10 membros continua limitando quem entra.

**Armazenamento**

- **Só o hash, link visível uma vez:** protege contra vazamento do banco, mas o link não fica sempre visível.
- **Token criptografado:** protege e permite exibir, ao custo de uma chave para administrar.
- **Token legível:** simples e permite exibir. Um vazamento do banco expõe links por no máximo 7 dias, e com esse acesso o invasor já lê os dados de qualquer grupo.

**Pessoa removida**

- **Manter o link:** quem foi removido poderia voltar com ele.
- **Cancelar o link em toda remoção:** a remoção vale de verdade, e o administrador gera outro se precisar.

## Decisão

- Cada grupo tem no máximo um link ativo, garantido por um índice único parcial. "Gerar novo" cancela o anterior na mesma transação, sob a trava do grupo.
- O link é reutilizável e vale 7 dias. Aceitar não o consome.
- Só proprietários e administradores veem, geram e cancelam o link: `GET`, `POST` e `DELETE /api/grupos/{id}/convite`. A interface mostra o link sempre que ele for válido.
- Remover um membro cancela o link, seja quem for o criador. Sair voluntariamente só cancela o link criado por quem saiu, como na decisão 0008.
- A prévia pública passa a mostrar também o nome do desafio atual ou do próximo e os nomes dos membros. Nunca o e-mail. Quem tem o link já foi convidado para conhecer essas pessoas.
- Continuam valendo: o token de 256 bits, o fragmento da URL, a mesma resposta `CONVITE_INVALIDO` para link inexistente, expirado ou cancelado, e a verificação de capacidade na entrada.

## Impacto

- A migração 010 apaga os convites antigos, cujo token não pode ser recuperado, e troca `token_hash`, `used_at` e `usado_por_id` por `token`. Como a produção ainda não foi implantada (Q001), nenhum convite real se perde.
- **Compromisso aceito:** o token fica legível no banco e nos backups por 7 dias. Ele só permite entrar num grupo que tenha vaga.
- **Compromisso aceito:** qualquer pessoa com o link pode entrar enquanto houver vaga, e não há registro de qual link trouxe cada membro. Se o link circular demais, o administrador gera outro.
