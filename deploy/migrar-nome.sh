#!/usr/bin/env bash
# Migração única da VM de EasyFit para Wrexercise. Ver docs/decisoes/0026-renomeacao-para-wrexercise.md.
# Rode na VM, como ubuntu, ANTES do primeiro deploy.bat com o nome novo:
#   scp deploy/migrar-nome.sh ubuntu@<vm>:/tmp/ && ssh ubuntu@<vm> bash /tmp/migrar-nome.sh
# Faz backup, renomeia o banco e o usuário do PostgreSQL, move /opt e /srv e reescreve o .env.
# Pode ser repetido: cada passo confere se já foi feito.
set -euo pipefail

antigo=/opt/easyfit
novo=/opt/wrexercise
dados_antigo=/srv/easyfit
dados_novo=/srv/wrexercise
temporario=migracao_wrexercise

falhar() {
  echo "ERRO: $1" >&2
  exit 1
}

if [ -f "$novo/.env" ] && [ ! -d "$antigo" ]; then
  echo "Migração já concluída. Próximo passo: deploy.bat."
  exit 0
fi

[ -f "$antigo/.env" ] || falhar "$antigo/.env não existe: nada a migrar."
[ ! -e "$novo/.env" ] || falhar "$novo/.env já existe junto com $antigo. Confira à mão antes de continuar."

cd "$antigo"
# Lê a senha sem executar o .env.
senha=$(sed -n 's/^DB_PASSWORD=//p' .env)
[ -n "$senha" ] || falhar "DB_PASSWORD não encontrado em $antigo/.env."

compose() { docker compose --env-file .env -f deploy/compose.yaml "$@"; }

echo "==> Backup antes de migrar"
compose up -d --wait db
compose run --rm backup backup

echo "==> Parando a aplicação"
compose stop app proxy

echo "==> Renomeando o banco e o usuário do PostgreSQL"
# O PostgreSQL não renomeia o usuário da própria sessão, então um superusuário temporário faz isso.
# Pelo socket local, a imagem oficial autentica sem senha.
psql_como() { compose exec -T db psql -v ON_ERROR_STOP=1 -U "$1" -d "$2" -c "$3"; }

if compose exec -T db psql -U easyfit -d postgres -tAc "select 1" >/dev/null 2>&1; then
  psql_como easyfit postgres "drop role if exists $temporario; create role $temporario superuser login;"
  psql_como "$temporario" postgres "alter database easyfit rename to wrexercise;"
  psql_como "$temporario" postgres "alter role easyfit rename to wrexercise;"
  # A senha já é SCRAM e sobrevive à troca de nome; redefinir garante que continue valendo.
  printf "alter role wrexercise password '%s';\n" "$senha" |
    compose exec -T db psql -v ON_ERROR_STOP=1 -U "$temporario" -d postgres >/dev/null
  psql_como wrexercise postgres "drop role $temporario;"
else
  echo "    Usuário easyfit não existe mais: banco já renomeado."
fi

echo "==> Parando todos os serviços"
compose down

echo "==> Movendo diretórios"
if [ -d "$dados_antigo" ]; then
  [ ! -e "$dados_novo" ] || falhar "$dados_novo já existe junto com $dados_antigo."
  sudo mv "$dados_antigo" "$dados_novo"
fi
cd /
# Um deploy.bat rodado antes da migração cria $novo/deploy e para no aviso de remote.sh.
if [ -d "$novo" ]; then
  mv "$novo" "$novo.antes-da-migracao-$(date +%Y%m%d-%H%M%S)"
fi
mv "$antigo" "$novo"

echo "==> Reescrevendo o .env"
sed -i \
  -e 's|^EASYFIT_IMAGE=|WREXERCISE_IMAGE=|' \
  -e 's|Database=easyfit;Username=easyfit;|Database=wrexercise;Username=wrexercise;|' \
  -e 's|/srv/easyfit|/srv/wrexercise|g' \
  "$novo/.env"
grep -q '^WREXERCISE_IMAGE=' "$novo/.env" || falhar "WREXERCISE_IMAGE não ficou no .env."
grep -q 'Database=wrexercise;Username=wrexercise;' "$novo/.env" || falhar "DB_CONNECTION não foi atualizado."

echo
echo "Migração concluída. Próximo passo, na sua máquina: deploy.bat"
echo "As imagens antigas continuam na VM; depois de validar, remova com:"
echo "  docker images easyfit -q | xargs -r docker rmi"
