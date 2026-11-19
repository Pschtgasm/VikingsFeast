@echo off
setlocal
set "MODVERSION=1.5.0"
cd /d "%~dp0"
where dotnet >nul 2>&1 || (echo FEHLER: .NET SDK fehlt. https://dotnet.microsoft.com/download & pause & exit /b 1)
if "%~1"=="" (set /p "VALHEIM=Valheim Installationsordner eingeben: ") else (set "VALHEIM=%~1")
if not exist "%VALHEIM%\valheim_Data\Managed\assembly_valheim.dll" (echo FEHLER: Valheim-Ordner nicht gefunden & pause & exit /b 1)
set "JOTUNN=%VALHEIM%\BepInEx\plugins\Jotunn\Jotunn.dll"
if not exist "%JOTUNN%" (set /p "JOTUNN=Vollstaendigen Pfad zur Jotunn.dll eingeben: ")
if not exist "%JOTUNN%" (echo FEHLER: Jotunn.dll nicht gefunden & pause & exit /b 1)
dotnet build VikingFoodPlus.csproj -c Release -p:ValheimDir="%VALHEIM%" -p:JotunnDll="%JOTUNN%"
if errorlevel 1 (echo BUILD FEHLGESCHLAGEN - Fehlermeldungen oben beachten & pause & exit /b 1)
echo DLL erstellt unter bin\Release\netstandard2.1\VikingFoodPlus.dll
echo.
echo Erstelle Mod-Paket...
set "PKG=%~dp0dist\pkg"
if exist "%~dp0dist" rmdir /s /q "%~dp0dist"
mkdir "%PKG%\plugins" "%PKG%\Translations"
copy /y "bin\Release\netstandard2.1\VikingFoodPlus.dll" "%PKG%\plugins\" >nul
copy /y manifest.json "%PKG%\" >nul
copy /y icon.png "%PKG%\" >nul
copy /y README.md "%PKG%\" >nul
copy /y CHANGELOG.md "%PKG%\" >nul
xcopy /e /i /y Translations "%PKG%\Translations" >nul
powershell -NoProfile -Command "Compress-Archive -Path '%PKG%\*' -DestinationPath '%~dp0dist\VikingsFeast_%MODVERSION%.zip' -Force"
if errorlevel 1 (echo PAKET FEHLGESCHLAGEN & pause & exit /b 1)
echo FERTIG: %~dp0dist\VikingsFeast_%MODVERSION%.zip  (im Thunderstore Mod Manager: Import local mod)
pause
