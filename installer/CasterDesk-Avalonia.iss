#ifndef AppVersion
#error Build using scripts/Package-Avalonia.ps1.
#endif
#ifndef PublishFolder
#error A verified publication directory is required.
#endif
[Setup]
SetupArchitecture=x64
AppId={{5BB534BC-6E39-447F-BB82-4A86199BAC31}
AppName=CasterDesk
AppVersion={#AppVersion}
AppPublisher=CasterDesk
DefaultDirName={localappdata}\Programs\CasterDesk-Avalonia
DefaultGroupName=CasterDesk
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
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
[InstallDelete]
Type: files; Name: "{autoprograms}\CasterDesk Avalonia Preview.lnk"
Type: files; Name: "{autodesktop}\CasterDesk Avalonia Preview.lnk"
Type: files; Name: "{app}\CasterDesk.Avalonia.exe"
Type: files; Name: "{app}\CasterDesk.Avalonia.dll"
Type: files; Name: "{app}\CasterDesk.Avalonia.deps.json"
Type: files; Name: "{app}\CasterDesk.Avalonia.runtimeconfig.json"
[Icons]
Name: "{autoprograms}\CasterDesk"; Filename: "{app}\CasterDesk.exe"; Parameters: "--language {code:AppLanguage}"; WorkingDir: "{app}"
Name: "{autodesktop}\CasterDesk"; Filename: "{app}\CasterDesk.exe"; Parameters: "--language {code:AppLanguage}"; WorkingDir: "{app}"; Tasks: desktopicon
[Code]
function AppLanguage(Param: String): String;
begin
  if ActiveLanguage = 'spanish' then Result := 'es'
  else if ActiveLanguage = 'english' then Result := 'en'
  else Result := 'pt-BR';
end;
