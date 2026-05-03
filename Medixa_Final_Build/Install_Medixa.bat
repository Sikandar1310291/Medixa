@echo off
TITLE Medixa Pharmacy - Professional Installer
COLOR 0B
SET "APP_NAME=Medixa Pharmacy"
SET "INSTALL_DIR=C:\MedixaPharmacy"
SET "EXE_NAME=Medixa.exe"

echo ----------------------------------------------------
echo         MEDIXA PHARMACY SYSTEM INSTALLER
echo         (Senior Google Engineer Deployment)
echo ----------------------------------------------------
echo.
echo [1/3] Creating Installation Directory: %INSTALL_DIR%...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [2/3] Copying Medixa files and initializing Backup Environment...
xcopy /E /Y /I * "%INSTALL_DIR%\" >nul
if not exist "%INSTALL_DIR%\Backups" mkdir "%INSTALL_DIR%\Backups"

echo [3/3] Generating Desktop Shortcut for Easy Access...
set SCRIPT="%TEMP%\%RANDOM%-%RANDOM%-%RANDOM%-%RANDOM%.vbs"
echo Set oWS = WScript.CreateObject("WScript.Shell") >> %SCRIPT%
echo sLinkFile = oWS.SpecialFolders("Desktop") ^& "\%APP_NAME%.lnk" >> %SCRIPT%
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> %SCRIPT%
echo oLink.TargetPath = "%INSTALL_DIR%\%EXE_NAME%" >> %SCRIPT%
echo oLink.WorkingDirectory = "%INSTALL_DIR%\" >> %SCRIPT%
echo oLink.Description = "Medixa Pharmacy POS & Reports" >> %SCRIPT%
echo oLink.Save >> %SCRIPT%
cscript /nologo %SCRIPT%
del %SCRIPT%

echo.
echo ----------------------------------------------------
echo        INSTALLATION COMPLETE SUCCESSFULLY!
echo ----------------------------------------------------
echo.
echo - Software Location: %INSTALL_DIR%
echo - Desktop Shortcut Created: "%APP_NAME%"
echo.
echo Press any key to finish installation...
pause >nul
