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
dotnet format EasyFit.slnx --verify-no-changes

step "Backend: build (avisos de AOT são erros)"
dotnet build EasyFit.slnx -c Release --nologo -v quiet

step "Backend: testes unitários e de arquitetura"
dotnet test --project EasyFit.UnitTests -c Release --no-build

step "Frontend: dependências"
(cd EasyFit/webapp && npm ci --no-audit --no-fund)

step "Frontend: Prettier"
(cd EasyFit/webapp && npm run --silent format:check)

step "Frontend: ESLint"
(cd EasyFit/webapp && npm run --silent lint)

step "Frontend: tipos"
(cd EasyFit/webapp && npm run --silent typecheck)

step "Frontend: build"
(cd EasyFit/webapp && npm run --silent build)

if [ "$full" = true ]; then
  step "Banco de desenvolvimento"
  scripts/dev-db.sh

  step "Backend: testes de integração"
  dotnet test --project EasyFit.IntegrationTests -c Release --no-build

  step "Imagem linux/arm64 (publicação Native AOT real)"
  docker buildx build --platform linux/arm64 -f deploy/Dockerfile -t easyfit:verify-arm64 --load .
fi

printf '\nTudo certo.\n'
