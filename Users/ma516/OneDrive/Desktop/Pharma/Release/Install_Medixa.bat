@echo off
color 0B
echo.
echo ==============================================================
echo             MEDIXA - PHARMACY BILLING SYSTEM
echo                      INSTALLER
echo ==============================================================
echo.
echo This will install Medixa on your computer.
echo.
pause

echo.
echo [1/3] Closing running Medixa and Server...
taskkill /f /im Medixa.exe /t 2>nul
taskkill /f /im PharmaServer.exe /t 2>nul
taskkill /f /im PharmaBilling.exe /t 2>nul
echo Done.

echo.
echo [2/3] Cleaning and Updating folder C:\Medixa...
if not exist "C:\Medixa" mkdir "C:\Medixa"
xcopy "%~dp0*.*" "C:\Medixa\" /e /i /h /y >nul

echo.
echo [3/3] Creating Desktop Shortcut...
powershell -Command "$wshell = New-Object -ComObject WScript.Shell; $shortcut = $wshell.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\Medixa Pharmacy.lnk'); $shortcut.TargetPath = 'C:\Medixa\Medixa.exe'; $shortcut.WorkingDirectory = 'C:\Medixa'; $shortcut.IconLocation = 'C:\Medixa\logo.ico'; $shortcut.Save()"

echo.
echo ==============================================================
echo             INSTALLATION COMPLETE!
echo ==============================================================
echo You can now run "Medixa Pharmacy" from your Desktop.
echo.
pause
