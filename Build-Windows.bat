@echo off
setlocal EnableDelayedExpansion
set "MODVERSION=1.5.3"
cd /d "%~dp0"
rem ==========================================================================
rem  Viking's Feast - Build ohne Eingaben
rem  Pfade werden automatisch gesucht und in build-paths.txt gemerkt.
rem  Nach dem Build wird die DLL direkt ins Testprofil kopiert (TESTPROFILE).
rem ==========================================================================
where dotnet >nul 2>&1 || (echo FEHLER: .NET SDK fehlt. https://dotnet.microsoft.com/download & pause & exit /b 1)

set "CFG=%~dp0build-paths.txt"
set "VALHEIM="
set "JOTUNN="
set "TESTPROFILE=TestMods"
if exist "%CFG%" for /f "usebackq tokens=1,* delims==" %%a in ("%CFG%") do set "%%a=%%b"
if not "%~1"=="" set "VALHEIM=%~1"

rem ---- Valheim suchen -------------------------------------------------------
if defined VALHEIM if not exist "!VALHEIM!\valheim_Data\Managed\assembly_valheim.dll" set "VALHEIM="
if not defined VALHEIM (
  for %%d in ("C:\Program Files (x86)\Steam\steamapps\common\Valheim" "C:\Program Files\Steam\steamapps\common\Valheim" "D:\SteamLibrary\steamapps\common\Valheim" "E:\SteamLibrary\steamapps\common\Valheim" "F:\SteamLibrary\steamapps\common\Valheim" "D:\Steam\steamapps\common\Valheim" "E:\Steam\steamapps\common\Valheim") do (
    if not defined VALHEIM if exist "%%~d\valheim_Data\Managed\assembly_valheim.dll" set "VALHEIM=%%~d"
  )
)
if not defined VALHEIM (
  for /f "tokens=2,*" %%a in ('reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul ^| find "SteamPath"') do set "STEAM=%%b"
  if defined STEAM (
    set "STEAM=!STEAM:/=\!"
    if exist "!STEAM!\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll" set "VALHEIM=!STEAM!\steamapps\common\Valheim"
  )
)
if not defined VALHEIM set /p "VALHEIM=Valheim-Ordner nicht gefunden. Bitte einmal eingeben: "
if not exist "!VALHEIM!\valheim_Data\Managed\assembly_valheim.dll" (echo FEHLER: Valheim-Ordner nicht gefunden & pause & exit /b 1)

rem ---- Jotunn suchen (Mod-Manager-Profile, dann Valheim-Ordner) ------------
if defined JOTUNN if not exist "!JOTUNN!" set "JOTUNN="
set "TSM=%APPDATA%\Thunderstore Mod Manager\DataFolder\Valheim\profiles"
set "R2M=%APPDATA%\r2modmanPlus-local\Valheim\profiles"
if not defined JOTUNN if exist "%TSM%\%TESTPROFILE%\BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll" set "JOTUNN=%TSM%\%TESTPROFILE%\BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll"
if not defined JOTUNN (
  for /d %%p in ("%TSM%\*" "%R2M%\*") do (
    if not defined JOTUNN if exist "%%~p\BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll" set "JOTUNN=%%~p\BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll"
  )
)
if not defined JOTUNN if exist "!VALHEIM!\BepInEx\plugins\Jotunn\Jotunn.dll" set "JOTUNN=!VALHEIM!\BepInEx\plugins\Jotunn\Jotunn.dll"
if not defined JOTUNN set /p "JOTUNN=Jotunn.dll nicht gefunden. Bitte einmal den vollen Pfad eingeben: "
if not exist "!JOTUNN!" (echo FEHLER: Jotunn.dll nicht gefunden & pause & exit /b 1)

rem ---- Pfade merken ----------------------------------------------------------
> "%CFG%" echo VALHEIM=!VALHEIM!
>> "%CFG%" echo JOTUNN=!JOTUNN!
>> "%CFG%" echo TESTPROFILE=!TESTPROFILE!
echo Valheim: !VALHEIM!
echo Jotunn:  !JOTUNN!

rem ---- Bauen ------------------------------------------------------------------
for %%i in ("!JOTUNN!\..\..\..") do set "BEPROOT=%%~fi"
set "BEPINEX=!BEPROOT!\core\BepInEx.dll"
if not exist "!BEPINEX!" set "BEPINEX=!VALHEIM!\BepInEx\core\BepInEx.dll"
if not exist "!BEPINEX!" (echo FEHLER: BepInEx.dll nicht gefunden: !BEPINEX! & pause & exit /b 1)
echo BepInEx: !BEPINEX!
dotnet build VikingFoodPlus.csproj -c Release -p:ValheimDir="!VALHEIM!" -p:JotunnDll="!JOTUNN!" -p:BepInExDll="!BEPINEX!"
if errorlevel 1 (echo BUILD FEHLGESCHLAGEN - Fehlermeldungen oben beachten & pause & exit /b 1)
set "DLL=bin\Release\netstandard2.1\VikingFoodPlus.dll"
echo DLL erstellt: %DLL%

rem ---- Direkt ins Testprofil kopieren (direkt in BepInEx\plugins) ------------
set "DEPLOY="
if exist "%TSM%\!TESTPROFILE!\BepInEx\plugins" set "DEPLOY=%TSM%\!TESTPROFILE!\BepInEx\plugins"
if not defined DEPLOY if exist "%R2M%\!TESTPROFILE!\BepInEx\plugins" set "DEPLOY=%R2M%\!TESTPROFILE!\BepInEx\plugins"
if defined DEPLOY (
  copy /y "%DLL%" "!DEPLOY!\VikingFoodPlus.dll" >nul && echo In Profil "!TESTPROFILE!" kopiert: !DEPLOY!\VikingFoodPlus.dll
  rem alte Kopie im Mod-Manager-Ordner stilllegen, sonst laedt der Mod doppelt
  if exist "!DEPLOY!\Pscht-VikingsFeast\VikingFoodPlus.dll" (
    move /y "!DEPLOY!\Pscht-VikingsFeast\VikingFoodPlus.dll" "!DEPLOY!\Pscht-VikingsFeast\VikingFoodPlus.dll.old" >nul
    echo Hinweis: alte DLL in Pscht-VikingsFeast wurde in VikingFoodPlus.dll.old umbenannt.
  )
) else (
  echo Hinweis: Profil "!TESTPROFILE!" nicht gefunden, DLL wurde nicht kopiert. Name in build-paths.txt aendern.
)

rem ---- Mod-Paket -------------------------------------------------------------
echo Erstelle Mod-Paket...
set "PKG=%~dp0dist\pkg"
if exist "%~dp0dist" rmdir /s /q "%~dp0dist"
mkdir "%PKG%\plugins" "%PKG%\Translations"
copy /y "%DLL%" "%PKG%\plugins\" >nul
copy /y manifest.json "%PKG%\" >nul
copy /y icon.png "%PKG%\" >nul
copy /y README.md "%PKG%\" >nul
copy /y CHANGELOG.md "%PKG%\" >nul
xcopy /e /i /y Translations "%PKG%\Translations" >nul
powershell -NoProfile -Command "Compress-Archive -Path '%PKG%\*' -DestinationPath '%~dp0dist\VikingsFeast_%MODVERSION%.zip' -Force"
if errorlevel 1 (echo PAKET FEHLGESCHLAGEN & pause & exit /b 1)
echo FERTIG: %~dp0dist\VikingsFeast_%MODVERSION%.zip
timeout /t 8
