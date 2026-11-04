CREATE TABLE atividades (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    grupo_id uuid NOT NULL REFERENCES grupos (id),
    usuario_id uuid NOT NULL REFERENCES usuarios (id),
    tipo text NOT NULL CHECK (tipo IN (
        'Musculacao',
        'Corrida',
        'Caminhada',
        'Ciclismo',
        'Natacao',
        'Esporte',
        'Yoga',
        'TreinoEmCasa',
        'Outro'
    )),
    duracao_segundos integer NOT NULL CHECK (duracao_segundos > 0),
    started_at timestamptz NOT NULL,
    descricao text,
    tipo_comprovacao text NOT NULL CHECK (tipo_comprovacao IN (
        'RegistroManual',
        'Fotografia',
        'CapturaDeTela'
    )),
    situacao_verificacao text NOT NULL DEFAULT 'AguardandoFeedback' CHECK (situacao_verificacao IN (
        'AguardandoFeedback',
        'Verificada',
        'EmRevisao',
        'Contestada',
        'Rejeitada'
    )),
    submitted_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX atividades_grupo_feed ON atividades (grupo_id, started_at DESC, id DESC);

CREATE INDEX atividades_usuario_started ON atividades (usuario_id, started_at DESC);

CREATE INDEX atividades_situacao ON atividades (grupo_id, situacao_verificacao);

CREATE TABLE evidencias_atividade (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    atividade_id uuid NOT NULL REFERENCES atividades (id),
    caminho_relativo text NOT NULL,
    tipo_mime text NOT NULL,
    tamanho_bytes integer NOT NULL CHECK (tamanho_bytes > 0),
    sha256 text NOT NULL,
    ordem smallint NOT NULL CHECK (ordem >= 1 AND ordem <= 3),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (atividade_id, ordem)
);

CREATE INDEX evidencias_atividade_atividade_id ON evidencias_atividade (atividade_id, ordem);
