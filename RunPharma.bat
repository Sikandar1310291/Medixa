@echo off
setlocal
cd /d "%~dp0"

echo [1/3] Looking for MSBuild...

:: Try Visual Studio 2022 first (best C# 10 support)
set MSBUILD_PATH=""
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)
:: Try Visual Studio 2019
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)

:: Try Visual Studio 18 Insiders
if exist "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
    goto :found
)

:: If no VS found, just launch the last compiled exe directly
echo [!] Visual Studio not found. Launching last compiled version...
goto :launch

:found
echo     Found: %MSBUILD_PATH%
echo [2/3] Building Medixa Desktop App...
%MSBUILD_PATH% PharmaBilling.csproj /t:Build /p:Configuration=Debug /v:m /nologo

if errorlevel 1 (
    echo.
    echo [!] Build Failed. Launching last compiled version instead...
)

:launch
echo [3/3] Launching Application...
if exist "bin\Debug\PharmaBilling.exe" (
    start "" "bin\Debug\PharmaBilling.exe"
) else (
    echo Error: No compiled exe found. Please open in Visual Studio and Build first.
    pause
)

endlocal
