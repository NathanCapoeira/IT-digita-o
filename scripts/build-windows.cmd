@echo off
setlocal
cd /d "%~dp0.."

echo Building IT Digitacao for Windows...
where msbuild >nul 2>nul
if errorlevel 1 goto no_msbuild

msbuild src\ITDigitacao.App\ITDigitacao.App.csproj /m /p:Configuration=Release /p:Platform=x86 /verbosity:minimal
if errorlevel 1 goto build_failed

echo.
echo Build completed. Output files are in:
echo src\ITDigitacao.App\bin\Release\
echo.
pause
exit /b 0

:no_msbuild
echo.
echo ERROR: MSBuild was not found.
echo Install Visual Studio Build Tools with the .NET desktop build tools,
echo then run this script from the project folder again.
echo.
pause
exit /b 1

:build_failed
echo.
echo ERROR: The build failed. Read the messages above and send a screenshot.
echo.
pause
exit /b 1
