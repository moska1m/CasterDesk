#ifndef AppVersion
#error Build using scripts/Package-Avalonia.ps1.
#endif
#ifndef PublishFolder
#error A verified publication directory is required.
#endif
[Setup]
SetupArchitecture=x64
AppId={{5BB534BC-6E39-447F-BB82-4A86199BAC31}
AppName=CasterDesk Avalonia Preview
AppVersion={#AppVersion}
AppPublisher=CasterDesk
DefaultDirName={localappdata}\Programs\CasterDesk-Avalonia
DefaultGroupName=CasterDesk Avalonia Preview
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=CasterDesk-Avalonia-{#AppVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\casterdesk.ico
UninstallDisplayIcon={app}\CasterDesk.Avalonia.exe
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#AppFileVersion}
[Languages]
Name: "brazilianportuguese"; MessagesFile: "Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "Languages\Spanish.isl"
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
[Files]
Source: "..\{#PublishFolder}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\assets\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\AVALONIA-PROGRESSO.md"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{autoprograms}\CasterDesk Avalonia Preview"; Filename: "{app}\CasterDesk.Avalonia.exe"; Parameters: "--language {code:AppLanguage}"; WorkingDir: "{app}"
Name: "{autodesktop}\CasterDesk Avalonia Preview"; Filename: "{app}\CasterDesk.Avalonia.exe"; Parameters: "--language {code:AppLanguage}"; WorkingDir: "{app}"; Tasks: desktopicon
[Code]
function AppLanguage(Param: String): String;
begin
  if ActiveLanguage = 'spanish' then Result := 'es'
  else if ActiveLanguage = 'english' then Result := 'en'
  else Result := 'pt-BR';
end;
