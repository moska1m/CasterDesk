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
WizardSizePercent=115
WizardImageFile=branding/wizard.bmp
WizardSmallImageFile=branding/wizard-small.bmp
DisableWelcomePage=no
AppPublisherURL=https://github.com/moska1m/CasterDesk
AppSupportURL=https://github.com/moska1m/CasterDesk/issues
SetupIconFile=..\assets\casterdesk.ico
UninstallDisplayIcon={app}\CasterDesk.exe
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#AppFileVersion}
[Languages]
Name: "brazilianportuguese"; MessagesFile: "Languages\BrazilianPortuguese.isl"; LicenseFile: "..\docs\TERMOS-PT.txt"
Name: "english"; MessagesFile: "compiler:Default.isl"; LicenseFile: "..\docs\TERMOS-EN.txt"
Name: "spanish"; MessagesFile: "Languages\Spanish.isl"; LicenseFile: "..\docs\TERMOS-ES.txt"
[Messages]
brazilianportuguese.WizardLicense=Condições de uso e direitos
brazilianportuguese.LicenseAccepted=Estou em &acordo
brazilianportuguese.LicenseNotAccepted=&Não estou em acordo
english.WizardLicense=Terms of use and rights
english.LicenseAccepted=I &agree
spanish.WizardLicense=Condiciones de uso y derechos
spanish.LicenseAccepted=Estoy de &acuerdo
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "obsplugin"; Description: "{cm:InstallOBSPlugin}"; Flags: unchecked
[CustomMessages]
brazilianportuguese.InstallOBSPlugin=Instalar o painel CasterDesk no OBS (OBS 31.1 ou mais recente; feche o OBS; exige permissão de administrador)
english.InstallOBSPlugin=Install the CasterDesk dock in OBS (OBS 31.1 or newer; close OBS; administrator permission required)
spanish.InstallOBSPlugin=Instalar el panel CasterDesk en OBS (OBS 31.1 o posterior; cierra OBS; requiere permisos de administrador)
brazilianportuguese.AppStage=Instalando o CasterDesk e os recursos da interface…
english.AppStage=Installing CasterDesk and interface resources…
spanish.AppStage=Instalando CasterDesk y recursos de la interfaz…
brazilianportuguese.PluginStage=Instalando o painel opcional do OBS…
english.PluginStage=Installing the optional OBS dock…
spanish.PluginStage=Instalando el panel opcional de OBS…
brazilianportuguese.LaunchApp=Abrir o CasterDesk
english.LaunchApp=Open CasterDesk
spanish.LaunchApp=Abrir CasterDesk
[Files]
Source: "..\{#PublishFolder}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs; BeforeInstall: AppStage
Source: "..\assets\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PROGRESSO.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\CasterDesk-OBS-1.0.0-win-x64-Setup.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\TERMOS-*.txt"; DestDir: "{app}\termos"; Flags: ignoreversion
[Run]
Filename: "{app}\CasterDesk-OBS-1.0.0-win-x64-Setup.exe"; Parameters: "/SILENT /NORESTART /LANG={language}"; Tasks: obsplugin; Flags: shellexec waituntilterminated; BeforeInstall: PluginStage
Filename: "{app}\CasterDesk.exe"; Parameters: "--language {code:AppLanguage}"; Description: "{cm:LaunchApp}"; WorkingDir: "{app}"; Flags: postinstall nowait skipifsilent unchecked
[InstallDelete]
Type: files; Name: "{app}\AVALONIA-PROGRESSO.md"
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
function InitializeSetup(): Boolean;
begin
  Result := (not WizardSilent) or (Pos(' /ACCEPTTERMS ', ' ' + Uppercase(GetCmdTail) + ' ') > 0);
  if not Result then Log('Silent installation requires explicit /ACCEPTTERMS.');
end;
procedure AppStage;
begin
  WizardForm.StatusLabel.Caption := CustomMessage('AppStage');
end;
procedure PluginStage;
begin
  WizardForm.StatusLabel.Caption := CustomMessage('PluginStage');
end;
function AppLanguage(Param: String): String;
begin
  if ActiveLanguage = 'spanish' then Result := 'es'
  else if ActiveLanguage = 'english' then Result := 'en'
  else Result := 'pt-BR';
end;
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssDone) and WizardIsTaskSelected('obsplugin') and
    FileExists(ExpandConstant('{commonappdata}\obs-studio\plugins\casterdesk-obs\bin\64bit\casterdesk-obs.dll')) then
    SaveStringToFile(ExpandConstant('{app}\obs-plugin.optin'), 'CasterDesk OBS plugin selected', False);
end;
