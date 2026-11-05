@echo off
rem Sobe localmente a mesma pilha da produção: Native AOT, PostgreSQL e Caddy.
rem Requer Docker Desktop, com as portas 80 e 443 livres. Só para testes: a senha abaixo não é segredo.
rem   run.bat              constrói a imagem, aplica as migrações e sobe os serviços
rem   run.bat --rebuild    igual ao padrão, mas reconstrói a imagem sem cache do Docker
rem                          (frontend wwwroot + Native AOT) e recria o contêiner app
rem   run.bat rebuild      atalho para --rebuild
rem   run.bat --tunnel     igual ao padrão, mas publica em https://fit.jpgmaraujo.com.br por um túnel
rem                          da Cloudflare, com o cadastro sem convite fechado. Pode ser combinado com
rem                          --rebuild. Na primeira vez, pede o login na Cloudflare pelo navegador.
rem   run.bat logs         acompanha os logs da aplicação
rem   run.bat stop         para os serviços e mantém os dados
rem   run.bat reset        para os serviços e APAGA os dados locais
chcp 65001 >nul
setlocal
cd /d "%~dp0"

rem HTTP evita o aviso de certificado. Os navegadores tratam localhost como seguro
rem e aceitam o cookie Secure da sessão; só o Caddy responde a este nome.
set "URL=http://localhost"
set "DATA_ROOT=%LOCALAPPDATA%\EasyFit\local"

rem O compose.yaml exige estas variáveis em qualquer comando, inclusive logs e down.
set "DOMAIN=%URL%"
set "DB_PASSWORD=easyfit_local"
set "DB_CONNECTION=Host=db;Port=5432;Database=easyfit;Username=easyfit;Password=easyfit_local"
set "EASYFIT_IMAGE=easyfit:local"
set "CADASTRO_ABERTO=true"
set "DATA_DIR=%DATA_ROOT:\=/%"

rem Fora de DATA_ROOT: run.bat reset apaga os dados, mas mantém o túnel.
set "TUNNEL_HOST=fit.jpgmaraujo.com.br"
set "TUNNEL_NAME=easyfit-local"
set "TUNNEL_ROOT=%LOCALAPPDATA%\EasyFit\cloudflared"
set "TUNNEL_DIR=%TUNNEL_ROOT:\=/%"
set "TUNNEL_IMAGE=cloudflare/cloudflared:2026.10.3"
set "CLOUDFLARED=docker run --rm -v "%TUNNEL_DIR%:/home/nonroot/.cloudflared" %TUNNEL_IMAGE% tunnel"

rem Projeto próprio: não se mistura com o banco de desenvolvimento (easyfit-dev).
set "COMPOSE=docker compose -p easyfit-local -f deploy/compose.yaml"

set "DOCKER_BUILD_EXTRAS="
set "REBUILD="
set "TUNNEL="

:parse
if "%~1"=="" goto :start
if /i "%~1"=="--rebuild" (
  set "DOCKER_BUILD_EXTRAS=--no-cache"
  set "REBUILD=1"
  shift
  goto :parse
)
if /i "%~1"=="rebuild" (
  set "DOCKER_BUILD_EXTRAS=--no-cache"
  set "REBUILD=1"
  shift
  goto :parse
)
if /i "%~1"=="--tunnel" (
  set "TUNNEL=1"
  shift
  goto :parse
)
if /i "%~1"=="logs" goto :logs
if /i "%~1"=="stop" goto :stop
if /i "%~1"=="reset" goto :reset
echo Uso: run.bat [--rebuild^|rebuild] [--tunnel] ^| logs ^| stop ^| reset
exit /b 2

:start
if defined TUNNEL (
  rem Na internet, novas contas só entram por convite.
  set "CADASTRO_ABERTO=false"
  set "COMPOSE=%COMPOSE% -f deploy/compose.tunnel.yaml"
  call :prepare_tunnel
  if errorlevel 1 goto :fail
)

echo.
if defined REBUILD (
  echo ==^> Construindo %EASYFIT_IMAGE% do zero ^(--no-cache^)
) else (
  echo ==^> Construindo %EASYFIT_IMAGE% para esta máquina
)
docker buildx build %DOCKER_BUILD_EXTRAS% -f deploy/Dockerfile -t %EASYFIT_IMAGE% --load .
if errorlevel 1 goto :fail

echo.
echo ==^> Aplicando migrações
%COMPOSE% run --rm app migrate
if errorlevel 1 goto :fail

echo.
echo ==^> Subindo os serviços
if defined REBUILD (
  rem Proxy também recriado para garantir que nada fique apontando para app antigo.
  %COMPOSE% up -d --force-recreate --remove-orphans
) else (
  rem Sem --tunnel, remove o túnel que tenha ficado de uma execução anterior.
  %COMPOSE% up -d --remove-orphans
)
if errorlevel 1 goto :fail

echo.
echo ==^> Verificando %URL%/ready
curl.exe -fs --retry 30 --retry-all-errors --retry-delay 1 --max-time 5 %URL%/ready
if errorlevel 1 goto :fail

echo.
echo.
echo EasyFit rodando em %URL%
if defined TUNNEL call :check_tunnel
echo Para parar: run.bat stop
exit /b 0

:prepare_tunnel
if not exist "%TUNNEL_ROOT%" mkdir "%TUNNEL_ROOT%"
if errorlevel 1 exit /b 1
if not exist "%TUNNEL_ROOT%\cert.pem" (
  echo.
  echo ==^> Abra o link abaixo e autorize a zona jpgmaraujo.com.br
  %CLOUDFLARED% login
  if errorlevel 1 exit /b 1
)
if not exist "%TUNNEL_ROOT%\%TUNNEL_NAME%.json" (
  echo.
  echo ==^> Criando o túnel %TUNNEL_NAME%
  %CLOUDFLARED% create --cred-file /home/nonroot/.cloudflared/%TUNNEL_NAME%.json %TUNNEL_NAME%
  if errorlevel 1 exit /b 1
)
echo.
echo ==^> Apontando %TUNNEL_HOST% para o túnel
%CLOUDFLARED% route dns --overwrite-dns %TUNNEL_NAME% %TUNNEL_HOST%
exit /b %errorlevel%

:check_tunnel
rem Não é falha: o DNS desta máquina pode demorar a enxergar o registro novo.
echo.
echo ==^> Verificando https://%TUNNEL_HOST%/ready
curl.exe -fs --retry 30 --retry-all-errors --retry-delay 2 --max-time 5 https://%TUNNEL_HOST%/ready
if errorlevel 1 (
  echo Ainda sem resposta. Se persistir, veja: docker logs easyfit-local-tunnel-1
) else (
  echo.
  echo EasyFit publicado em https://%TUNNEL_HOST%
)
exit /b 0

:logs
%COMPOSE% logs -f app
exit /b %errorlevel%

:stop
%COMPOSE% down --remove-orphans
exit /b %errorlevel%

:reset
%COMPOSE% down -v --remove-orphans
if errorlevel 1 goto :fail
if exist "%DATA_ROOT%" rmdir /s /q "%DATA_ROOT%"
if exist "%DATA_ROOT%" goto :fail
echo Dados locais apagados.
exit /b 0

:fail
echo.
echo run.bat FALHOU na etapa acima.
exit /b 1
