@echo off
:: ===========================================================================
:: Medixa Pharmacy - Windows Task Scheduler Auto-Setup
:: ===========================================================================
:: Runs daily at 02:00 AM silently in background.
:: Run this script as Administrator (one-time setup).
:: ===========================================================================

TITLE Medixa Backup Scheduler Setup

SET "PYTHON=python"
SET "SCRIPT_PATH=c:\Users\ma516\OneDrive\Desktop\Pharma\backup_system\medixa_backup.py"
SET "TASK_NAME=MedixaPharmacyAutoBackup"
SET "TASK_TIME=02:00"

echo.
echo ============================================================
echo    MEDIXA PHARMACY - BACKUP SCHEDULER SETUP
echo ============================================================
echo.
echo [1/3] Checking Python availability...
%PYTHON% --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Python not found in PATH. Please install Python 3.8+
    pause
    exit /b 1
)
echo       Python OK.

echo.
echo [2/3] Installing dependencies...
%PYTHON% -m pip install -r "%~dp0requirements.txt" --quiet
if %errorlevel% neq 0 (
    echo ERROR: Failed to install dependencies.
    pause
    exit /b 1
)
echo       Dependencies installed.

echo.
echo [3/3] Registering Windows Task Scheduler job...
echo       Task Name : %TASK_NAME%
echo       Schedule  : Daily at %TASK_TIME%
echo       Script    : %SCRIPT_PATH%
echo.

:: Delete old task if exists
schtasks /delete /tn "%TASK_NAME%" /f >nul 2>&1

:: Create new scheduled task
schtasks /create ^
  /tn "%TASK_NAME%" ^
  /tr "\"%PYTHON%\" \"%SCRIPT_PATH%\"" ^
  /sc DAILY ^
  /st %TASK_TIME% ^
  /ru SYSTEM ^
  /rl HIGHEST ^
  /f ^
  /it

if %errorlevel% equ 0 (
    echo.
    echo ============================================================
    echo    SUCCESS! Backup Scheduler Registered.
    echo ============================================================
    echo.
    echo    The backup will run daily at %TASK_TIME%.
    echo    Logs saved to: backup_system\logs\
    echo    To verify: Open Task Scheduler and look for "%TASK_NAME%"
    echo.
    echo    Running a TEST backup now to verify system is working...
    echo.
    %PYTHON% "%SCRIPT_PATH%" --layer 1
    echo.
    echo    Setup complete. Press any key to exit.
) else (
    echo.
    echo ERROR: Task Scheduler registration failed.
    echo        Try running this script as Administrator.
)

pause
