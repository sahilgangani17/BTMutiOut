@echo off
setlocal enabledelayedexpansion
title BT MultiOut - Build Installer

echo.
echo ============================================================
echo   BT MultiOut - Installer Builder
echo ============================================================
echo.

:: ── Check prerequisites ────────────────────────────────────────────────────

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK not found. Install from https://dotnet.microsoft.com/download
    pause & exit /b 1
)

where makensis >nul 2>&1
if errorlevel 1 (
    echo [ERROR] NSIS not found. Install from https://nsis.sourceforge.io
    echo         Then add its install folder to your PATH.
    pause & exit /b 1
)

:: ── Locate source files ────────────────────────────────────────────────────

set "SCRIPT_DIR=%~dp0"
set "SRC_DIR=%SCRIPT_DIR%..\BTMultiOut"
set "OUT_DIR=%SCRIPT_DIR%dist"

if not exist "%SRC_DIR%\BTMultiOut.csproj" (
    echo [ERROR] Cannot find BTMultiOut.csproj at: %SRC_DIR%
    echo         Make sure BTMultiOut\ and BTMultiOut-Installer\ are sibling folders.
    pause & exit /b 1
)

if not exist "%SCRIPT_DIR%VBCABLE_Driver_Pack45\VBCABLE_Setup_x64.exe" (
    echo [ERROR] Missing: VBCABLE_Driver_Pack45 folder
    echo.
    echo  Extract the VB-CABLE zip into this folder so you have:
    echo  %SCRIPT_DIR%VBCABLE_Driver_Pack45\VBCABLE_Setup_x64.exe
    pause & exit /b 1
)

:: ── Build BTMultiOut (single-file exe) ────────────────────────────────────

echo [1/3] Building BTMultiOut...
mkdir "%OUT_DIR%" 2>nul

dotnet publish "%SRC_DIR%\BTMultiOut.csproj" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUT_DIR%\app"

if errorlevel 1 (
    echo [ERROR] dotnet publish failed.
    pause & exit /b 1
)

echo [OK] Build successful.

:: ── Copy files for NSIS ───────────────────────────────────────────────────

echo [2/3] Preparing installer files...

copy /y "%OUT_DIR%\app\BTMultiOut.exe" "%SCRIPT_DIR%BTMultiOut.exe" >nul

:: Copy optional LICENSE if it exists
if exist "%SRC_DIR%\LICENSE.txt" (
    copy /y "%SRC_DIR%\LICENSE.txt" "%SCRIPT_DIR%LICENSE.txt" >nul
) else (
    :: Create a minimal placeholder so NSIS doesn't error on the !File directive
    echo BT MultiOut - Open Source Software > "%SCRIPT_DIR%LICENSE.txt"
)

echo [OK] Files ready.

:: ── Run NSIS ──────────────────────────────────────────────────────────────

echo [3/3] Compiling installer with NSIS...

makensis "%SCRIPT_DIR%installer.nsi"

if errorlevel 1 (
    echo [ERROR] NSIS compilation failed.
    pause & exit /b 1
)

echo.
echo ============================================================
echo   SUCCESS!
echo   Installer: %SCRIPT_DIR%BTMultiOut-Setup.exe
echo ============================================================
echo.

:: Cleanup temp copies (keep originals)
del /f /q "%SCRIPT_DIR%BTMultiOut.exe" 2>nul

pause
