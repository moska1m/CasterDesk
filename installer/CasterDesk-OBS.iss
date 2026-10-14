#ifndef PluginPackage
#error Build using scripts/Package-Avalonia.ps1.
#endif
[Setup]
SetupArchitecture=x64
AppId={{09AA78A4-8DC8-4D9E-A266-8CFE1B89F088}
AppName=CasterDesk — Plugin OBS
AppVersion=0.1.0
AppPublisher=CasterDesk
DefaultDirName={commonappdata}\obs-studio\plugins\casterdesk-obs
PrivilegesRequired=admin
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=CasterDesk-OBS-0.1.0-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\casterdesk.ico
CloseApplications=no
RestartApplications=no
LicenseFile=..\obs-plugin\LICENSE
[Languages]
Name: "brazilianportuguese"; MessagesFile: "Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "Languages\Spanish.isl"
[CustomMessages]
brazilianportuguese.CloseOBS=Feche o OBS antes de instalar ou atualizar o plugin CasterDesk. O instalador não encerra transmissões. Depois de fechar, execute a instalação novamente.
english.CloseOBS=Close OBS before installing or updating the CasterDesk plugin. This installer does not stop streams. Run setup again after closing OBS.
spanish.CloseOBS=Cierra OBS antes de instalar o actualizar el plugin CasterDesk. El instalador no detiene transmisiones. Ejecuta la instalación nuevamente después de cerrar OBS.
[Files]
Source: "..\{#PluginPackage}\bin\64bit\casterdesk-obs.dll"; DestDir: "{app}\bin\64bit"; Flags: ignoreversion
Source: "..\obs-plugin\LICENSE"; DestDir: "{app}\data"; Flags: ignoreversion
Source: "..\obs-plugin\data\Rajdhani-OFL.txt"; DestDir: "{app}\data"; Flags: ignoreversion
Source: "..\{#PluginPackage}\source.zip"; DestDir: "{app}\data"; Flags: ignoreversion
[Code]
function OBSRunning(): Boolean;
var Locator, Service, Processes: Variant;
begin
  Result := True;
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('', 'root\CIMV2');
    Processes := Service.ExecQuery('SELECT Name FROM Win32_Process WHERE Name = ''obs64.exe''');
    Result := Processes.Count > 0;
  except
    { Fail closed if process enumeration is unavailable. }
  end;
end;
function InitializeSetup(): Boolean;
begin
  Result := not OBSRunning();
  if not Result then MsgBox(CustomMessage('CloseOBS'), mbError, MB_OK);
end;
function InitializeUninstall(): Boolean;
begin
  Result := not OBSRunning();
  if not Result then MsgBox(CustomMessage('CloseOBS'), mbError, MB_OK);
end;
