#define AppVersion "0.2.0-beta.0.2"
[Setup]
AppId={{D5FB900F-ED44-45B9-90B3-4131FA74F7A8}
AppName=CasterDesk
AppVersion={#AppVersion}
AppPublisher=CasterDesk
AppPublisherURL=https://github.com/moska1m/CasterDesk
DefaultDirName={localappdata}\Programs\CasterDesk
DefaultGroupName=CasterDesk
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=CasterDesk-{#AppVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\casterdesk.ico
UninstallDisplayIcon={app}\CasterDesk.exe
CloseApplications=yes
RestartApplications=no
VersionInfoVersion=0.2.0.2
[Languages]
Name: "brazilianportuguese"; MessagesFile: "Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "Languages\Spanish.isl"
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\assets\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{autoprograms}\CasterDesk"; Filename: "{app}\CasterDesk.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\CasterDesk"; Filename: "{app}\CasterDesk.exe"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\CasterDesk.exe"; Description: "{cm:LaunchProgram,CasterDesk}"; Flags: nowait postinstall skipifsilent
