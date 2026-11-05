CREATE TABLE comentarios_revisao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    atividade_id uuid NOT NULL REFERENCES atividades (id) ON DELETE CASCADE,
    usuario_id uuid NOT NULL REFERENCES usuarios (id),
    texto text NOT NULL CHECK (char_length(texto) >= 1 AND char_length(texto) <= 500),
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX comentarios_revisao_atividade_id ON comentarios_revisao (atividade_id, created_at);
