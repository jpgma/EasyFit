@echo off
rem Implanta o Wrexercise na VM de produção via SSH, sem registry nem CI.
rem Requer Docker Desktop e a chave SSH carregada no ssh-agent do Windows.
rem Ver docs/decisoes/0005-implantacao-via-ssh.md.
chcp 65001 >nul
setlocal
cd /d "%~dp0"

set "DOMAIN=fit.jpgmaraujo.com.br"
if not defined WREXERCISE_SSH_HOST set "WREXERCISE_SSH_HOST=%DOMAIN%"
set "TARGET=ubuntu@%WREXERCISE_SSH_HOST%"
set "SSH_OPTS=-i "%USERPROFILE%\.ssh\wrexercise_oracle" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new"
rem Caminhos explícitos: o ssh do Git não enxerga o ssh-agent do Windows.
set "SSH=%SystemRoot%\System32\OpenSSH\ssh.exe"
set "SCP=%SystemRoot%\System32\OpenSSH\scp.exe"

set "DIRTY="
for /f %%i in ('git status --porcelain') do set "DIRTY=-dirty"
for /f %%i in ('git rev-parse --short HEAD') do set "IMAGE=wrexercise:%%i%DIRTY%"
if not defined IMAGE (
  echo Não foi possível obter o commit atual.
  exit /b 1
)
if defined DIRTY echo AVISO: há alterações sem commit; a imagem será marcada como %IMAGE%.

echo.
echo ==^> Construindo %IMAGE% para linux/arm64
docker buildx build --platform linux/arm64 -f deploy/Dockerfile -t %IMAGE% --load .
if errorlevel 1 goto :fail

echo.
echo ==^> Aguardando a preparação da VM e copiando a configuração
"%SSH%" %SSH_OPTS% %TARGET% "cloud-init status --wait >/dev/null; mkdir -p /opt/wrexercise/deploy"
if errorlevel 1 goto :fail
"%SCP%" %SSH_OPTS% deploy/compose.yaml deploy/Caddyfile deploy/remote.sh deploy/backup.sh %TARGET%:/opt/wrexercise/deploy/
if errorlevel 1 goto :fail

echo.
echo ==^> Enviando a imagem
docker save --platform linux/arm64 %IMAGE% | "%SSH%" -C %SSH_OPTS% %TARGET% "docker load"
if errorlevel 1 goto :fail

echo.
echo ==^> Aplicando migrações e reiniciando os serviços
"%SSH%" %SSH_OPTS% %TARGET% "bash /opt/wrexercise/deploy/remote.sh %IMAGE% %DOMAIN%"
if errorlevel 1 goto :fail

echo.
echo ==^> Verificando https://%DOMAIN%/ready
curl.exe -fsS --retry 30 --retry-all-errors --retry-delay 5 --max-time 10 https://%DOMAIN%/ready
if errorlevel 1 goto :fail

echo.
echo.
echo Implantação concluída: https://%DOMAIN% (%IMAGE%)
exit /b 0

:fail
echo.
echo Implantação FALHOU na etapa acima.
exit /b 1
