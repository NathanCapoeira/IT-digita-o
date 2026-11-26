@echo off
setlocal
where msbuild >nul 2>nul
if errorlevel 1 (
  echo MSBuild nao encontrado. Instale Visual Studio Build Tools com .NET desktop build tools.
  exit /b 1
)
msbuild ITDigitacao.sln /m /p:Configuration=Release /p:Platform="Any CPU" /verbosity:minimal
if errorlevel 1 exit /b %errorlevel%
msbuild src\ITDigitacao.App\ITDigitacao.App.csproj /p:Configuration=Release /p:Platform=x86 /verbosity:minimal
exit /b %errorlevel%
