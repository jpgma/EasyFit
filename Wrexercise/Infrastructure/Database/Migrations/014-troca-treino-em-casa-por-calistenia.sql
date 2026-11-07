-- "Treino em casa" dizia onde, não o quê. Calistenia, o treino com o peso do corpo, ocupa o lugar.
-- As atividades já registradas mudam junto: era quase sempre esse o treino feito em casa.
ALTER TABLE atividades DROP CONSTRAINT atividades_tipo_check;

UPDATE atividades SET tipo = 'Calistenia' WHERE tipo = 'TreinoEmCasa';

ALTER TABLE atividades ADD CONSTRAINT atividades_tipo_check CHECK (tipo IN (
    'Musculacao',
    'Corrida',
    'Caminhada',
    'Ciclismo',
    'Natacao',
    'Esporte',
    'Yoga',
    'Calistenia',
    'Outro'
));
