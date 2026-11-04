-- As datas são dias do calendário no fuso do grupo. O desafio não guarda fuso próprio porque o do grupo não muda.
-- Só semanas inteiras, de segunda a domingo: assim não existem semanas parciais.
CREATE TABLE desafios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    grupo_id uuid NOT NULL REFERENCES grupos (id),
    criador_id uuid NOT NULL REFERENCES usuarios (id),
    nome text NOT NULL,
    descricao text,
    data_inicio date NOT NULL,
    data_fim date NOT NULL,
    duracao_minima_segundos integer NOT NULL CHECK (duracao_minima_segundos > 0),
    sessoes_por_semana integer NOT NULL CHECK (sessoes_por_semana > 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    cancelled_at timestamptz,
    CHECK (data_fim > data_inicio),
    CHECK (EXTRACT(ISODOW FROM data_inicio) = 1),
    CHECK (EXTRACT(ISODOW FROM data_fim) = 7)
);

CREATE INDEX desafios_grupo_id ON desafios (grupo_id, data_inicio);

-- Sem saída própria: quem deixa o grupo some das listas pelo vínculo em membros_grupo, e a linha fica para o histórico.
CREATE TABLE participantes_desafio (
    desafio_id uuid NOT NULL REFERENCES desafios (id),
    usuario_id uuid NOT NULL REFERENCES usuarios (id),
    joined_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (desafio_id, usuario_id)
);

CREATE INDEX participantes_desafio_usuario_id ON participantes_desafio (usuario_id);
