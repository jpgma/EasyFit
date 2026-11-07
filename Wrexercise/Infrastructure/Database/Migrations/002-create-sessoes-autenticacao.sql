CREATE TABLE sessoes_autenticacao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    token_hash bytea NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz
);

CREATE UNIQUE INDEX sessoes_autenticacao_token_hash_unique ON sessoes_autenticacao (token_hash);
CREATE INDEX sessoes_autenticacao_usuario_id ON sessoes_autenticacao (usuario_id);
