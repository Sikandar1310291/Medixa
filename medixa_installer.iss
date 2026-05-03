; ============================================================
;  Medixa Pharmacy Software  -  Professional Installer Script
;  Created by Apexora AI  |  Inno Setup 6
; ============================================================

#define AppName      "Medixa Pharmacy Software"
#define AppVersion   "1.8"
#define AppPublisher "Apexora AI Solutions"
#define AppURL       "https://apexoraai.com"
#define AppExeName   "PharmaBilling.exe"
#define AppId        "{{F8A3C2D1-4B7E-4F9A-8C3D-2E5F6A7B8C9D}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\Medixa Pharmacy
DefaultGroupName={#AppName}
AllowNoIcons=no
LicenseFile=
OutputDir=.\Installer_Output
OutputBaseFilename=Medixa_Setup_v1.8
SetupIconFile=logo.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=110
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
; Splash / header branding
WizardImageFile=compiler:WizClassicImage.bmp
WizardSmallImageFile=compiler:WizClassicSmallImage.bmp

; ---- .NET 4.8 check (most Windows 10/11 PCs already have it) ----
[Code]
function NetFrameworkInstalled(): Boolean;
var
  Version: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Version) then
    Result := (Version >= 528040); // .NET 4.8
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not NetFrameworkInstalled() then
  begin
    MsgBox(
      'Medixa Pharmacy requires Microsoft .NET Framework 4.8 or higher.' + #13#10 +
      'Please download it from:' + #13#10 +
      'https://dotnet.microsoft.com/download/dotnet-framework/net48' + #13#10#10 +
      'After installing .NET 4.8, run this setup again.',
      mbError, MB_OK
    );
    Result := False;
  end;
end;

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a Desktop Shortcut"; GroupDescription: "Additional icons:"; Flags: checkedonce
Name: "startupicon"; Description: "Start Medixa automatically when Windows starts"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; ---- Main Executable ----
Source: "bin\Debug\PharmaBilling.exe";        DestDir: "{app}";               Flags: ignoreversion
; ---- SQLite Library ----
Source: "bin\Debug\System.Data.SQLite.dll";   DestDir: "{app}";               Flags: ignoreversion
; ---- SQLite Native x64 ----
Source: "bin\Debug\x64\e_sqlite3.dll";        DestDir: "{app}\x64";           Flags: ignoreversion
; ---- SQLite Native x86 ----
Source: "bin\Debug\x86\e_sqlite3.dll";        DestDir: "{app}\x86";           Flags: ignoreversion
; ---- Pre-filled Database (Contains 24k Medicines) ----
Source: "bin\Debug\PharmaDB.sqlite";   DestDir: "{app}";   Flags: ignoreversion
; ---- App Config ----
Source: "config.json";                         DestDir: "{app}";               Flags: ignoreversion
; ---- Logo / Icon ----
Source: "logo.ico";                            DestDir: "{app}";               Flags: ignoreversion
[Dirs]
; App install folder — executables only
Name: "{app}"; Permissions: users-modify
Name: "{app}\Backups"; Permissions: users-modify
; ProgramData\Medixa — safe DB location, never synced by OneDrive or Dropbox
Name: "{commonappdata}\Medixa"; Permissions: users-modify

[Icons]
; Start Menu shortcut
Name: "{group}\Medixa Pharmacy";          Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\logo.ico"
Name: "{group}\Uninstall Medixa";         Filename: "{uninstallexe}"
; Desktop shortcut (only if user ticked the task)
Name: "{autodesktop}\Medixa Pharmacy";    Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\logo.ico"; Tasks: desktopicon
; Startup shortcut (Windows boots → Medixa auto opens)
Name: "{userstartup}\Medixa Pharmacy";    Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\logo.ico"; Tasks: startupicon

[Run]
; Launch Medixa right after install finishes
Filename: "{app}\{#AppExeName}"; Description: "Launch Medixa Pharmacy"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\license_key.txt"
Type: files; Name: "{app}\license_check.txt"
Type: files; Name: "{app}\config.json"
Type: dirifempty; Name: "{app}\Backups"
Type: dirifempty; Name: "{app}"
