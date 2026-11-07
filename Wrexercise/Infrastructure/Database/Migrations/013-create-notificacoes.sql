CREATE TABLE notificacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL REFERENCES usuarios (id),
    grupo_id uuid NOT NULL REFERENCES grupos (id),
    atividade_id uuid NOT NULL REFERENCES atividades (id) ON DELETE CASCADE,
    tipo text NOT NULL CHECK (tipo IN ('NovaAtividade', 'Reacao', 'Comentario', 'SituacaoAlterada', 'Disputa')),
    ator_id uuid REFERENCES usuarios (id),
    situacao text,
    total integer NOT NULL DEFAULT 1 CHECK (total > 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    read_at timestamptz
);

CREATE INDEX notificacoes_usuario_recentes ON notificacoes (usuario_id, updated_at DESC);
CREATE INDEX notificacoes_atividade_id ON notificacoes (atividade_id);
CREATE INDEX notificacoes_updated_at ON notificacoes (updated_at);

-- Enquanto não forem lidas, as reações e os comentários de uma atividade viram uma linha só.
CREATE UNIQUE INDEX notificacoes_agrupadas_nao_lidas ON notificacoes (usuario_id, atividade_id, tipo)
    WHERE read_at IS NULL AND tipo IN ('Reacao', 'Comentario');
