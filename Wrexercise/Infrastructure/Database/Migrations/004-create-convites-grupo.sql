CREATE TABLE convites_grupo (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    grupo_id uuid NOT NULL REFERENCES grupos (id),
    criador_id uuid NOT NULL REFERENCES usuarios (id),
    token_hash bytea NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    used_at timestamptz,
    usado_por_id uuid REFERENCES usuarios (id),
    revoked_at timestamptz
);

CREATE UNIQUE INDEX convites_grupo_token_hash_unique ON convites_grupo (token_hash);
CREATE INDEX convites_grupo_grupo_id ON convites_grupo (grupo_id);
