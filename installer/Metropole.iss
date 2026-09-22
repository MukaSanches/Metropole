#define MyAppName "METRÓPOLE ∞"
#define MyAppVersion "1.9.0"
#define MyAppPublisher "METRÓPOLE"
#define MyAppExeName "Metropole.exe"

[Setup]
AppId={{2A31C457-5B50-4B5E-86D7-6AF6C587C6E9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Metropole
DefaultGroupName=METRÓPOLE
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=Metropole-1.9.0-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
SetupLogging=yes
VersionInfoVersion=1.9.0.0
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoDescription=Simulador sistêmico de sociedade e economia

[Files]
Source: "..\build\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\METRÓPOLE ∞"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\METRÓPOLE ∞"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Executar METRÓPOLE ∞"; Flags: nowait postinstall skipifsilent
