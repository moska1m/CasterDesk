#ifndef AppVersion
#define AppVersion "0.2.0-beta.0.8"
#endif
#ifndef AppFileVersion
#define AppFileVersion "0.2.0.8"
#endif
#ifndef PublishFolder
#define PublishFolder "publish"
#endif
[Setup]
SetupArchitecture=x64
AppId={{D5FB900F-ED44-45B9-90B3-4131FA74F7A8}
AppName=CasterDesk
AppVersion={#AppVersion}
AppPublisher=CasterDesk
AppPublisherURL=https://github.com/moska1m/CasterDesk
DefaultDirName={localappdata}\Programs\CasterDesk
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
[CustomMessages]
brazilianportuguese.DataKept=Seus dados foram mantidos em Documentos\CasterDesk. Partidas, imagens, textos, configurações e backups continuam disponíveis para uma futura instalação. Arquivos antigos em AppData também foram preservados.
english.DataKept=Your data was kept in Documents\CasterDesk. Matches, images, text files, settings and backups remain available for a future installation. Older AppData files were also preserved.
spanish.DataKept=Tus datos se conservaron en Documentos\CasterDesk. Las partidas, imágenes, textos, configuraciones y copias de seguridad siguen disponibles para una futura instalación. También se conservaron los archivos antiguos de AppData.
[Files]
Source: "..\{#PublishFolder}\*"; DestDir: "{app}"; Excludes: "*.pdb,CasterDesk.Engine.exe,CasterDesk.Engine.runtimeconfig.json,CasterDesk.Engine.deps.json"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\assets\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{autoprograms}\CasterDesk"; Filename: "{app}\CasterDesk.exe"; Parameters: "--language {code:AppLanguage}"; WorkingDir: "{app}"
Name: "{autodesktop}\CasterDesk"; Filename: "{app}\CasterDesk.exe"; Parameters: "--language {code:AppLanguage}"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\CasterDesk.exe"; Parameters: "--language {code:AppLanguage}"; Description: "{cm:LaunchProgram,CasterDesk}"; Flags: nowait postinstall skipifsilent

[Code]
function AppLanguage(Param: String): String;
begin
  if ActiveLanguage = 'spanish' then Result := 'es'
  else if ActiveLanguage = 'english' then Result := 'en'
  else Result := 'pt-BR';
end;
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then
    MsgBox(ExpandConstant('{cm:DataKept}'), mbInformation, MB_OK);
end;
