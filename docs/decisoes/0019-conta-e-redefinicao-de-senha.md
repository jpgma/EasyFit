# 0019 — Dados da conta e redefinição de senha

**Status:** DECIDIDA. Altera a decisão 0006.

## Contexto

O novo mockup (decisão 0014) tem Configurações com edição de perfil e troca de senha. A decisão 0006 deixou a recuperação de senha fora do MVP: o operador alteraria o hash direto no banco, o que obriga a gerar um hash à mão e deixa o operador conhecer a senha temporária.

## Alternativas consideradas

**Senha esquecida**

- **Link por e-mail, como no mockup:** exige um serviço de e-mail, uma dependência externa para um grupo de amigos.
- **Administrador do grupo redefine:** daria a alguém do grupo o poder de assumir uma conta, que pode estar em outros grupos.
- **Comando do operador gera um link de uso único:** sem dependência nova, e o operador nunca conhece a senha.

**Onde fica o código**

- **Na fatia `Usuarios`:** precisaria importar `Senhas` e `Sessoes`, que são internos de `Autenticacao`.
- **Na fatia `Autenticacao`:** é dona de senhas, sessões e validação da conta, mesmo com rotas em `/api/usuarios/eu`.

## Decisão

- `PUT /api/usuarios/eu` altera o nome de exibição. O e-mail não muda, porque é o identificador de entrada e não há como confirmar um endereço novo.
- `POST /api/usuarios/eu/senha` exige a senha atual, usa o limite de tentativas da autenticação e revoga as demais sessões, mantendo a atual. Senha atual errada recebe 400 `SENHA_ATUAL_INCORRETA`.
- `reset-senha <email>` é um subcomando da aplicação, como `migrate`. Encerra as sessões da conta, invalida links anteriores e gera um token de uso único, válido por 1 hora, guardado só como hash.
- O caminho `/redefinir-senha#token` só é escrito no terminal do operador, nunca no log. O token viaja no fragmento, como o do convite.
- `POST /api/autenticacao/redefinir-senha` é público, com limite de tentativas. Link inexistente, usado ou expirado recebe a mesma resposta, `REDEFINICAO_INVALIDA`. Depois de usar o link, a pessoa entra normalmente.
- A tela de entrada não oferece "Esqueci minha senha": ela prometeria algo que depende de falar com o operador.

## Impacto

- Migração 012 cria `redefinicoes_senha`.
- Nova página de Configurações, para onde vai "Sair da conta", e nova página `/redefinir-senha`.
- **Compromisso aceito:** quem pede a redefinição precisa falar com o operador, e rodar o comando desconecta a conta na hora. Por isso ele só deve ser usado a pedido do dono da conta.
