@echo off
rem Atalho para o Windows: executa scripts/gerar-icones.sh com o Bash do Git for Windows.
rem   scripts\gerar-icones.bat caminho\novo-icone.png
rem   scripts\gerar-icones.bat
setlocal
rem O Bash do Git fica ao lado do git.exe. O bash.exe do System32 é o WSL, que não enxerga o Windows do mesmo jeito.
set "BASH="
for /f "delims=" %%G in ('where git 2^>nul') do if not defined BASH if exist "%%~dpG..\bin\bash.exe" set "BASH=%%~dpG..\bin\bash.exe"
if not defined BASH if exist "%ProgramFiles%\Git\bin\bash.exe" set "BASH=%ProgramFiles%\Git\bin\bash.exe"
if not defined BASH (
  echo Git for Windows nao encontrado. Instale o Git ou rode scripts/gerar-icones.sh no Git Bash. 1>&2
  exit /b 1
)
rem O Bash não abre o script com barras invertidas no caminho.
set "SCRIPT=%~dp0gerar-icones.sh"
"%BASH%" "%SCRIPT:\=/%" %*
exit /b %ERRORLEVEL%
