CREATE TABLE revisores_atividade (
    atividade_id uuid NOT NULL REFERENCES atividades (id) ON DELETE CASCADE,
    usuario_id uuid NOT NULL REFERENCES usuarios (id),
    PRIMARY KEY (atividade_id, usuario_id)
);

CREATE INDEX revisores_atividade_atividade_id ON revisores_atividade (atividade_id);

CREATE TABLE reacoes_atividade (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    atividade_id uuid NOT NULL REFERENCES atividades (id) ON DELETE CASCADE,
    usuario_id uuid NOT NULL REFERENCES usuarios (id),
    tipo text NOT NULL CHECK (tipo IN ('Kudos', 'Suspeito', 'Rejeitar')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (atividade_id, usuario_id)
);

CREATE INDEX reacoes_atividade_atividade_id ON reacoes_atividade (atividade_id);

CREATE TABLE historico_revisao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    atividade_id uuid NOT NULL REFERENCES atividades (id) ON DELETE CASCADE,
    responsavel_id uuid NOT NULL REFERENCES usuarios (id),
    decisao text NOT NULL CHECK (decisao IN ('Verificada', 'Rejeitada')),
    justificativa text NOT NULL CHECK (char_length(justificativa) >= 1 AND char_length(justificativa) <= 500),
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX historico_revisao_atividade_id ON historico_revisao (atividade_id, created_at DESC);

-- Backfill: revisores = membros ativos do grupo, exceto o autor.
INSERT INTO revisores_atividade (atividade_id, usuario_id)
SELECT a.id, m.usuario_id
FROM atividades a
JOIN membros_grupo m ON m.grupo_id = a.grupo_id AND m.removed_at IS NULL AND m.usuario_id <> a.usuario_id;

-- Grupo solo (sem revisores): já considerada verificada.
UPDATE atividades a
SET situacao_verificacao = 'Verificada'
WHERE situacao_verificacao = 'AguardandoFeedback'
  AND tipo_comprovacao = 'RegistroManual'
  AND NOT EXISTS (
      SELECT 1 FROM revisores_atividade r WHERE r.atividade_id = a.id);
