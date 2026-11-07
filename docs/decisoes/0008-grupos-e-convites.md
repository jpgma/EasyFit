# 0008 — Grupos, convites e permissões

**Status:** DECIDIDA. Parcialmente substituída pela decisão 0017 (convite reutilizável).

## Contexto

A Fase 3 precisa de grupos, convites, entrada em grupo e controle de permissões. A decisão 0006 definiu que o cadastro passaria a exigir convite nesta fase. A Q028 recomendava convites de uso único, mas o mockup mostra um link compartilhável e também um código reutilizável. A API de `arquitetura.md` §10.2 não previa nenhuma forma de alterar papéis, o que deixaria o papel de Administrador sem uso. A saída de membros dependia da Q011, ainda aberta.

## Alternativas consideradas

**Tipo de convite**

- **Link reutilizável, com validade:** um único link para colar no grupo de mensagens, mas quem o obtiver pode entrar até ele expirar.
- **Limite de usos escolhido a cada convite:** flexível, mas com mais colunas, regras e telas para um grupo de 10 pessoas.
- **Uso único, com validade de 7 dias:** um link por pessoa. Um link vazado admite no máximo um estranho e pode ser cancelado.

**Cadastro por convite**

- **Em duas etapas, como no mockup:** o convite seria consumido no cadastro, deixando uma etapa sem função, ou continuaria válido e poderia criar várias contas até ser aceito.
- **Em uma etapa:** conta, sessão, consumo do convite e entrada no grupo na mesma transação.

**Saída do Proprietário**

- **Sucessão automática:** alguém poderia virar proprietário sem ter escolhido.
- **Nunca poder sair:** a pessoa que criou o grupo ficaria presa a ele.
- **Transferência explícita antes de sair:** uma operação a mais, mas sem surpresas.

## Decisão

**Convites**

- Token de 32 bytes aleatórios, com só o SHA-256 no banco, igual ao da sessão. O link é mostrado uma única vez, ao ser gerado.
- Uso único e validade de 7 dias. Podem ser cancelados.
- O token viaja no fragmento do link (`/convite#token`) e no corpo de requisições `POST`, nunca no caminho de uma URL. O navegador não envia o fragmento ao servidor, então ele fica fora de logs de acesso e do `Referer`.
- A prévia (`POST /api/convites/consultar`) é pública, porque quem foi convidado pode ainda não ter conta. Mostra apenas o nome do grupo, o nome de quem convidou e a contagem de membros.
- Convite inexistente, expirado, usado ou cancelado recebem a mesma resposta, `CONVITE_INVALIDO`. Com 256 bits de entropia, adivinhar um token é inviável, por isso a prévia não tem limite de tentativas próprio (Q027 continua aberta).
- Uma tentativa recusada por `JA_E_MEMBRO`, `GRUPO_CHEIO` ou `EMAIL_JA_CADASTRADO` não consome o convite.

**Cadastro**

- O convite é o único caminho de cadastro. A primeira conta nasce na configuração inicial (decisão 0031), que substituiu `Autenticacao:CadastroAberto`.
- Convite recusado desfaz toda a transação: nenhuma conta fica para trás.

**Grupos**

- Um usuário pode participar de vários grupos. O grupo atual vem da URL (`/grupos/:id`), sem estado de "grupo ativo".
- Qualquer pessoa autenticada pode criar um grupo e se torna sua Proprietária.
- Capacidade de 10 membros ativos, verificada na entrada. Convites pendentes não reservam vaga.
- O fuso horário é validado pelo PostgreSQL (`pg_timezone_names`). Com `InvariantGlobalization`, o .NET não reconhece nomes IANA no Windows.
- Nome e fuso não podem ser alterados nesta fase, e não existe exclusão de grupo (Q008 e Q029).

**Permissões**

| Operação | Proprietário | Administrador | Membro |
|---|---|---|---|
| Ver grupo e membros | Sim | Sim | Sim |
| Gerar, listar e cancelar convites | Sim | Sim | Não |
| Alterar papel entre Administrador e Membro | Sim | Não | Não |
| Remover membro | Qualquer outro | Apenas `Membro` | Não |
| Transferir a propriedade | Sim | Não | Não |
| Sair do grupo | Só depois de transferir | Sim | Sim |

- Quem não é membro, ou já saiu, recebe 404 `GRUPO_NAO_ENCONTRADO`: a existência do grupo não é revelada. Membro sem permissão recebe 403 `ACESSO_NEGADO`.
- Na transferência, o proprietário anterior se torna Administrador.
- Colegas de grupo veem nome e papel uns dos outros, nunca o e-mail.

**Entrada e saída de membros (Q011)**

- A saída é uma exclusão lógica (`removed_at`). O vínculo continua existindo para o histórico.
- O acesso termina imediatamente. Os convites pendentes criados por quem saiu são cancelados na mesma transação.
- Só é possível voltar com um novo convite. O vínculo é reaproveitado, sempre como `Membro`.
- Para as próximas fases: as atividades e revisões de quem saiu são preservadas, e a saída nunca invalida retroativamente uma atividade. As Fases 4 e 6 definem a participação em desafios e as revisões pendentes a partir desse princípio.

**Concorrência**

Entrada, remoção, saída, transferência, alteração de papel e criação de convite travam a linha do grupo (`FOR NO KEY UPDATE`) antes de verificar qualquer regra. O grupo é sempre travado antes dos convites, para evitar deadlock. O papel é lido em um comando separado, depois da trava, porque um único comando que trave e leia pode enxergar dados anteriores à espera.

## Impacto

- Novas tabelas `grupos`, `membros_grupo` e `convites_grupo`, sem `ON DELETE CASCADE`, porque ainda não existe exclusão de conta nem de grupo.
- Novos contratos entre fatias: `Wrexercise.Features.Grupos.Contracts` (`GrupoAcesso`, `GrupoEntrada`, `PapelMembro`), que as próximas fatias usarão para verificar o pertencimento, e `Wrexercise.Features.Convites.Contracts` (`ConviteAceite`), usado pelo cadastro.
- `SessaoToken` passou a se chamar `OpaqueToken`, porque também gera os convites.
- **Compromisso aceito:** um Proprietário sozinho no grupo não consegue sair, porque o grupo não pode ser excluído nem transferido. Até a Q029 ser decidida, o operador resolve isso diretamente no banco.
- **Compromisso aceito:** o link não pode ser recuperado depois de gerado. Quem perder o link cancela o convite e gera outro.
- **Compromisso aceito:** alterar alguém de Administrador para Membro não cancela os convites que essa pessoa já criou. O Proprietário pode cancelá-los na lista de pendentes.
