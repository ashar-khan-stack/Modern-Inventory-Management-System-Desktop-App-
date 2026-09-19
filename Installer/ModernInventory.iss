; Modern Inventory Management System - Inno Setup Script
; Self-contained Windows x64 Installer
; Generates a professional Windows Setup with desktop & start menu shortcuts,
; uninstaller registration, and automatic application closing during upgrades.

#define MyAppName "Modern Inventory Management System"
#define MyAppShortName "ModernInventory"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Modern Inventory"
#define MyAppExeName "ModernInventory.Desktop.exe"
#define MyAppId "{{8B49E71F-2C5E-494E-8D2A-93F16CA21E42}"

[Setup]
; Unique application GUID for install/upgrade recognition
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=ModernInventory_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Support both Per-User (AppData) and Per-Machine (Program Files) installation
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline

; Auto-close running app before upgrade/reinstall to avoid locked DLLs
CloseApplications=yes
CloseApplicationsFilter=*.exe,{#MyAppExeName}
RestartApplications=no

; Clean Uninstall entry in Windows Settings / Control Panel
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Source directory populated by dotnet publish -r win-x64 --self-contained true
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Note: User data and SQLite database files are stored in %LocalAppData%\ModernInventoryDesktop\Data,
; ensuring updates never overwrite user transactions or inventory records.

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// Retain user data on uninstall: remind the user that their local database remains secure.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // User data is safely preserved in LocalAppData
  end;
end;
