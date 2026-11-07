-- O convite vira um link reutilizável, visível para os administradores: o token passa a ser
-- legível, e não há mais registro de uso. Os convites antigos não têm token recuperável.
DELETE FROM convites_grupo;

ALTER TABLE convites_grupo
    DROP COLUMN token_hash,
    DROP COLUMN used_at,
    DROP COLUMN usado_por_id,
    ADD COLUMN token text NOT NULL;

CREATE UNIQUE INDEX convites_grupo_token_unique ON convites_grupo (token);

-- Um link ativo por grupo. Um link expirado continua aqui até ser substituído.
CREATE UNIQUE INDEX convites_grupo_um_ativo ON convites_grupo (grupo_id) WHERE revoked_at IS NULL;
