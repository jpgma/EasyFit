#!/usr/bin/env bash
# Executado na VM por deploy.bat. Uso: remote.sh <imagem> <dominio>
set -euo pipefail

image=$1
domain=$2

# Antes do nome Wrexercise a pilha ficava em /opt/easyfit: sem migrar, um .env novo apontaria para um banco vazio.
if [ -d /opt/easyfit ]; then
  echo "ERRO: /opt/easyfit ainda existe. Rode deploy/migrar-nome.sh antes (decisão 0026)." >&2
  exit 1
fi

cd /opt/wrexercise

# Na primeira implantação, a senha do banco é gerada aqui e nunca sai da VM.
if [ ! -f .env ]; then
  password=$(openssl rand -hex 32)
  (
    umask 077
    cat > .env <<EOF
DOMAIN=$domain
DB_PASSWORD=$password
DB_CONNECTION=Host=db;Port=5432;Database=wrexercise;Username=wrexercise;Password=$password
WREXERCISE_IMAGE=$image
DATA_DIR=/srv/wrexercise
EOF
  )
fi

sed -i "s|^WREXERCISE_IMAGE=.*|WREXERCISE_IMAGE=$image|; s|^DOMAIN=.*|DOMAIN=$domain|" .env

compose() { docker compose --env-file .env -f deploy/compose.yaml "$@"; }

compose up -d --wait db
compose run --rm app migrate
compose up -d --remove-orphans
compose exec -T proxy caddy reload --config /etc/caddy/Caddyfile

# Mantém as 5 imagens mais recentes para permitir reverter.
docker images wrexercise --format '{{.Repository}}:{{.Tag}}' | tail -n +6 | xargs -r docker rmi || true
docker image prune -f >/dev/null
