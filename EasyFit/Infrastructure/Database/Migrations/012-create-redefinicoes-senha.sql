-- Criadas só pelo operador (comando reset-senha). Como o token da sessão, fica apenas o hash.
CREATE TABLE redefinicoes_senha (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    token_hash bytea NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    used_at timestamptz
);

CREATE UNIQUE INDEX redefinicoes_senha_token_hash_unique ON redefinicoes_senha (token_hash);
CREATE INDEX redefinicoes_senha_usuario_id ON redefinicoes_senha (usuario_id);
