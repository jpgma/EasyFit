#!/bin/sh
# Executado no Termux (Android, sem root) por run.bat --ssh com SO "termux". POSIX sh.
# Roda PostgreSQL, app e túnel da Cloudflare como processos nativos, sem VM e sem Docker.
# Ver docs/decisoes/0029-implantacao-nativa-no-termux.md.
#
#   remote-termux.sh implantar <nome-tunel> <id-tunel>   troca o app, migra e (re)inicia tudo
#   remote-termux.sh iniciar                              sobe o que não estiver rodando (Termux:Boot usa este)
#   remote-termux.sh parar
#   remote-termux.sh backup | restore AAAAMMDD-HHMMSS     usa deploy/backup.sh
#   remote-termux.sh status
set -eu

base=$HOME/wrexercise
dados=$base/dados
cd "$base"

# O binário Native AOT é glibc; o Termux é bionic. O grun (pacote glibc-runner) faz a ponte sem proot.
exigir() {
  faltam=
  for c in postgres initdb pg_ctl psql pg_dump cloudflared grun curl; do
    command -v "$c" >/dev/null 2>&1 || faltam="$faltam $c"
  done
  if [ -n "$faltam" ]; then
    echo "ERRO: faltam no Termux:$faltam" >&2
    echo "Instale e rode de novo:" >&2
    echo "  pkg install -y glibc-repo && pkg update -y && pkg install -y postgresql cloudflared glibc-runner curl" >&2
    exit 1
  fi
  # O diretório de dados só abre na mesma versão maior: um "pkg upgrade" para outra versão deixaria o banco ilegível.
  case $(postgres --version) in
    *" 18."*) ;;
    *)
      echo "ERRO: o PostgreSQL do Termux precisa ser o 18 (testado com 18.6), não: $(postgres --version)" >&2
      exit 1 ;;
  esac
  apt-mark showhold 2>/dev/null | grep -qx postgresql     || echo "AVISO: fixe a versão com: apt-mark hold postgresql" >&2
}

# Preserva a senha do banco entre implantações; ela nunca sai do telefone.
ler_env() {
  if [ ! -f .env ]; then
    (
      umask 077
      printf 'DB_PASSWORD=%s\n' "$(tr -dc 'a-f0-9' </dev/urandom | head -c 64)" > .env
    )
  fi
  DB_PASSWORD=$(sed -n 's/^DB_PASSWORD=//p' .env)
}

# Uma tarefa que reinicia sozinha se cair; a pausa evita laço apertado em erro de configuração.
iniciar_tarefa() {
  nome=$1
  shift
  if [ -f "run/$nome.pid" ] && kill -0 "$(cat "run/$nome.pid")" 2>/dev/null; then
    return 0
  fi
  [ -f "log/$nome.log" ] && tail -c 1048576 "log/$nome.log" > "log/$nome.log.tmp" && mv "log/$nome.log.tmp" "log/$nome.log"
  setsid nohup sh -c 'while :; do "$@"; sleep 3; done' sh "$@" >>"log/$nome.log" 2>&1 </dev/null &
  echo $! > "run/$nome.pid"
}

parar_tarefa() {
  nome=$1
  if [ -f "run/$nome.pid" ]; then
    kill "$(cat "run/$nome.pid")" 2>/dev/null || true
    rm -f "run/$nome.pid"
  fi
}

iniciar_banco() {
  if [ ! -f "$dados/postgres/PG_VERSION" ]; then
    mkdir -p "$dados/postgres"
    (umask 077; printf '%s\n' "$DB_PASSWORD" > run/pw)
    initdb -D "$dados/postgres" --auth=scram-sha-256 -U wrexercise --pwfile=run/pw >/dev/null
    rm -f run/pw
  fi
  if ! pg_ctl -D "$dados/postgres" status >/dev/null 2>&1; then
    # synchronous_commit=off: um corte de energia perde no máximo ~0,5 s de commits, sem corromper o banco.
    pg_ctl -D "$dados/postgres" -l log/postgres.log -w -t 120 \
      -o "-c listen_addresses=127.0.0.1 -c max_connections=20 -c shared_buffers=128MB -c effective_cache_size=1GB \
-c work_mem=4MB -c maintenance_work_mem=64MB -c max_wal_size=512MB -c checkpoint_completion_target=0.9 \
-c synchronous_commit=off -c wal_compression=on" start >/dev/null
  fi
  if [ "$(PGPASSWORD=$DB_PASSWORD psql -h 127.0.0.1 -U wrexercise -d postgres -Atc "select 1 from pg_database where datname='wrexercise'")" != 1 ]; then
    PGPASSWORD=$DB_PASSWORD createdb -h 127.0.0.1 -U wrexercise wrexercise
  fi
}

# O app e o túnel leem a configuração do ambiente, como nos contêineres de compose.yaml.
ambiente_app() {
  export ConnectionStrings__Database="Host=127.0.0.1;Port=5432;Database=wrexercise;Username=wrexercise;Password=$DB_PASSWORD;Maximum Pool Size=10"
  export Autenticacao__CadastroAberto=false
  export Evidencias__Diretorio=$dados/evidencias
  export ASPNETCORE_URLS=http://127.0.0.1:8080
}

iniciar() {
  exigir
  mkdir -p run log "$dados/evidencias" "$dados/backups"
  ler_env
  # Sem o wake lock, o Android suspende a CPU com a tela apagada.
  command -v termux-wake-lock >/dev/null 2>&1 && termux-wake-lock || true
  iniciar_banco
  ambiente_app
  iniciar_tarefa app grun "$base/app/Wrexercise"
  tunel=$(cat tunel.txt)
  iniciar_tarefa tunel cloudflared tunnel --no-autoupdate --url http://127.0.0.1:8080 run \
    --cred-file "$base/tunnel/${tunel%% *}.json" "${tunel##* }"
}

parar() {
  parar_tarefa tunel
  parar_tarefa app
  pkill -f "$base/app/Wrexercise" 2>/dev/null || true
  pkill -f "cloudflared tunnel" 2>/dev/null || true
  pg_ctl -D "$dados/postgres" -m fast -w stop >/dev/null 2>&1 || true
}

implantar() {
  exigir
  mkdir -p run log "$dados"
  echo "$1 $2" > tunel.txt
  chmod 600 tunel.txt "tunnel/$1.json"
  ler_env
  parar
  # run.bat envia o app novo em app.novo; o anterior fica em app.antigo para reverter.
  rm -rf app.antigo
  [ -d app ] && mv app app.antigo
  mv app.novo app
  chmod +x app/Wrexercise
  iniciar_banco
  ambiente_app
  mkdir -p "$dados/evidencias"
  grun "$base/app/Wrexercise" migrate
  iniciar
  # Termux:Boot roda tudo em ~/.termux/boot/ ao ligar o telefone.
  mkdir -p "$HOME/.termux/boot"
  printf '#!/data/data/com.termux/files/usr/bin/sh\nexec sh %s iniciar\n' "$base/deploy/remote-termux.sh" > "$HOME/.termux/boot/wrexercise.sh"
  chmod 700 "$HOME/.termux/boot/wrexercise.sh"
}

status() {
  curl -fs --max-time 5 http://127.0.0.1:8080/health || echo "app sem resposta"
  echo
  pg_ctl -D "$dados/postgres" status | head -1 || true
  for p in app tunel; do
    if [ -f "run/$p.pid" ] && kill -0 "$(cat "run/$p.pid")" 2>/dev/null; then echo "$p: rodando"; else echo "$p: parado"; fi
  done
  free -m | head -2
}

backup_sh() {
  ler_env
  PGHOST=127.0.0.1 PGUSER=wrexercise PGDATABASE=wrexercise PGPASSWORD=$DB_PASSWORD \
    BACKUP_DESTINO=$dados/backups EVIDENCIAS_DIR=$dados/evidencias \
    sh "$base/deploy/backup.sh" "$@"
}

case ${1:-} in
  implantar) implantar "$2" "$3" ;;
  iniciar) iniciar ;;
  parar) parar ;;
  status) status ;;
  backup) backup_sh "$@" ;;
  restore)
    # A restauração exige o app parado e o banco no ar.
    parar_tarefa app
    pkill -f "$base/app/Wrexercise" 2>/dev/null || true
    ler_env
    iniciar_banco
    backup_sh "$@"
    echo "Rode 'remote-termux.sh iniciar' para subir o app de novo." ;;
  *) echo "Uso: remote-termux.sh implantar|iniciar|parar|status|backup|restore" >&2; exit 2 ;;
esac
