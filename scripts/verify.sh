#!/usr/bin/env bash
# Verificação local do repositório. Não há CI: este script é a única barreira.
#   scripts/verify.sh          rápido, sem Docker
#   scripts/verify.sh --full   também testes de integração e imagem arm64
set -euo pipefail

cd "$(dirname "$0")/.."

full=false
case "${1:-}" in
  "") ;;
  --full) full=true ;;
  *) echo "Uso: scripts/verify.sh [--full]" >&2; exit 2 ;;
esac

step() { printf '\n==> %s\n' "$1"; }

step "Backend: formatação"
dotnet format Wrexercise.slnx --verify-no-changes

step "Backend: build (avisos de AOT são erros)"
dotnet build Wrexercise.slnx -c Release --nologo -v quiet

step "Backend: testes unitários e de arquitetura"
dotnet test --project Wrexercise.UnitTests -c Release --no-build

step "Frontend: dependências"
(cd Wrexercise/webapp && npm ci --no-audit --no-fund)

step "Frontend: Prettier"
(cd Wrexercise/webapp && npm run --silent format:check)

step "Frontend: ESLint"
(cd Wrexercise/webapp && npm run --silent lint)

step "Frontend: tipos"
(cd Wrexercise/webapp && npm run --silent typecheck)

step "Frontend: build"
(cd Wrexercise/webapp && npm run --silent build)

if [ "$full" = true ]; then
  step "Banco de desenvolvimento"
  scripts/dev-db.sh

  step "Backend: testes de integração"
  dotnet test --project Wrexercise.IntegrationTests -c Release --no-build

  step "Compose: pilha com túnel e perfil de memória baixa"
  DOMAIN=http://localhost DB_PASSWORD=x DB_CONNECTION=x TUNNEL_IMAGE=x TUNNEL_NAME=x TUNNEL_DIR=/tmp \
    docker compose -f deploy/compose.yaml -f deploy/compose.tunnel.yaml -f deploy/compose.baixa-memoria.yaml config -q
  sh -n deploy/remote-ssh.sh
  sh -n deploy/remote-termux.sh

  step "Imagem linux/arm64 (publicação Native AOT real)"
  docker buildx build --platform linux/arm64 -f deploy/Dockerfile -t wrexercise:verify-arm64 --load .
fi

printf '\nTudo certo.\n'
