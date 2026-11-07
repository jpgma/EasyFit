#!/bin/sh
# Executado na máquina remota por run.bat --ssh. POSIX sh: o guest Alpine não tem bash.
# Uso: remote-ssh.sh <imagem> <perfil-ram> <arquitetura> <nome-tunel> <imagem-tunel> <id-tunel>
#   perfil-ram: baixa | padrao        arquitetura: arm64 | amd64
set -eu

image=$1
perfil=$2
arch=$3
tunnel_name=$4
tunnel_image=$5
tunnel_id=$6

base=/opt/wrexercise
data=/srv/wrexercise

cd "$base"

# Preserva a senha do banco entre implantações; ela nunca sai desta máquina.
password=
if [ -f .env ]; then
  password=$(sed -n 's/^DB_PASSWORD=//p' .env)
fi
if [ -z "$password" ]; then
  password=$(tr -dc 'a-f0-9' </dev/urandom | head -c 64)
fi

pool=
protocol=auto
edge_ip=auto
if [ "$perfil" = baixa ]; then
  pool=";Maximum Pool Size=5"
fi
if [ "$arch" = arm64 ]; then
  # QEMU com SLIRP: QUIC/UDP e IPv6 atrasam o túnel.
  protocol=http2
  edge_ip=4
fi

(
  umask 077
  cat > .env <<EOF
DOMAIN=http://localhost
DB_PASSWORD=$password
DB_CONNECTION=Host=db;Port=5432;Database=wrexercise;Username=wrexercise;Password=$password$pool
WREXERCISE_IMAGE=$image
CADASTRO_ABERTO=false
DATA_DIR=$data
TUNNEL_IMAGE=$tunnel_image
TUNNEL_NAME=$tunnel_name
TUNNEL_ID=$tunnel_id
TUNNEL_DIR=$base/tunnel
TUNNEL_PROTOCOL=$protocol
TUNNEL_EDGE_IP=$edge_ip
EOF
)

mkdir -p "$data/postgres" "$data/backups" "$data/evidencias"
# 1654 é o usuário "app" da imagem chiseled da aplicação.
chown 1654:1654 "$data/evidencias"
chmod 750 "$data/evidencias"
chmod 700 "$data/backups"
# 65532 é o usuário "nonroot" da imagem do cloudflared; só ele lê a credencial.
chown 65532:65532 "$base/tunnel/$tunnel_name.json"
chmod 600 "$base/tunnel/$tunnel_name.json"

files="-f deploy/compose.yaml -f deploy/compose.tunnel.yaml"
if [ "$perfil" = baixa ]; then
  files="$files -f deploy/compose.baixa-memoria.yaml"
fi

compose() {
  # shellcheck disable=SC2086
  docker compose -p wrexercise-ssh --env-file .env $files "$@"
}

compose up -d --wait db
compose run --rm app migrate
# Sem Caddy: o túnel fala direto com o app, e o Android não abre as portas 80 e 443.
compose up -d --remove-orphans app tunnel

# Mantém as 5 imagens mais recentes do app para permitir reverter.
docker images wrexercise --format '{{.Repository}}:{{.Tag}}' | tail -n +6 | xargs -r docker rmi || true
docker image prune -f >/dev/null
