@echo off
setlocal
cd /d "%~dp0.."

echo Building IT Digitacao 3.0 WPF application for Windows...
where msbuild >nul 2>nul
if errorlevel 1 goto no_msbuild

echo Restoring project assets...
msbuild src\ITDigitacao.App\ITDigitacao.App.csproj /t:Restore /p:Configuration=Release /p:Platform=x86 /verbosity:minimal
if errorlevel 1 goto restore_failed
msbuild src\ITDigitacao.Wpf\ITDigitacao.Wpf.csproj /t:Restore /p:Configuration=Release /p:Platform=x86 /verbosity:minimal
if errorlevel 1 goto restore_failed

echo Building the existing application and the WPF application...
msbuild src\ITDigitacao.App\ITDigitacao.App.csproj /m /p:Configuration=Release /p:Platform=x86 /verbosity:minimal
if errorlevel 1 goto build_failed
msbuild src\ITDigitacao.Wpf\ITDigitacao.Wpf.csproj /m /p:Configuration=Release /p:Platform=x86 /verbosity:minimal
if errorlevel 1 goto build_failed

where dotnet >nul 2>nul
if errorlevel 1 goto tests_skipped
echo Running Core regression tests...
dotnet test tests\ITDigitacao.Core.Tests\ITDigitacao.Core.Tests.csproj --configuration Release --verbosity minimal
if errorlevel 1 goto tests_failed
goto tests_done

:tests_skipped
echo .NET SDK not found. Core regression tests were skipped.

:tests_done

echo.
echo Existing application: src\ITDigitacao.App\bin\x86\Release\net48\IT-Digitacao.exe
echo WPF application: src\ITDigitacao.Wpf\bin\x86\Release\net48\IT-Digitacao-3.0.exe
if exist src\ITDigitacao.Wpf\bin\x86\Release\net48\IT-Digitacao-3.0.exe start "" "src\ITDigitacao.Wpf\bin\x86\Release\net48\IT-Digitacao-3.0.exe"
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

:restore_failed
echo.
echo ERROR: Package restore failed. Check network access and confirm the .NET Framework 4.8 Developer Pack is installed.
echo.
pause
exit /b 1

:tests_failed
echo.
echo ERROR: Core regression tests failed.
echo.
pause
exit /b 1
