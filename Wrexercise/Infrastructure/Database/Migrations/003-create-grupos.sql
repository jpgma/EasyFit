CREATE TABLE grupos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome text NOT NULL,
    criador_id uuid NOT NULL REFERENCES usuarios (id),
    fuso_horario text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

-- A saída é uma exclusão lógica: o histórico do grupo continua apontando para quem saiu.
CREATE TABLE membros_grupo (
    grupo_id uuid NOT NULL REFERENCES grupos (id),
    usuario_id uuid NOT NULL REFERENCES usuarios (id),
    papel text NOT NULL CHECK (papel IN ('Proprietario', 'Administrador', 'Membro')),
    joined_at timestamptz NOT NULL DEFAULT now(),
    removed_at timestamptz,
    PRIMARY KEY (grupo_id, usuario_id)
);

CREATE UNIQUE INDEX membros_grupo_proprietario_unique
    ON membros_grupo (grupo_id)
    WHERE papel = 'Proprietario' AND removed_at IS NULL;

CREATE INDEX membros_grupo_usuario_id
    ON membros_grupo (usuario_id)
    WHERE removed_at IS NULL;
