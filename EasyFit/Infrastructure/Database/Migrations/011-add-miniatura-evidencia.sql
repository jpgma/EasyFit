-- Gerada no navegador e enviada com a evidência. Vazia nas evidências que não têm miniatura.
ALTER TABLE evidencias_atividade ADD COLUMN caminho_miniatura text;
