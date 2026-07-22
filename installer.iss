; Inno Setup script for FluentFold
; Run with: iscc installer.iss /dPublishDir=...\publish

#define MyAppName "FluentFold"
#define MyAppVersion "1.0.0.0"
#define MyAppPublisher "AppPublisher"
#define MyAppURL "https://github.com/MoHamed-B-M/Fluent-Fold"
#define MyAppExeName "FluentFold.exe"

[Setup]
AppId={{C5A1E627-2AFB-440C-A06A-231E03AB2ED4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#SourcePath}installer
OutputBaseFilename=FluentFold-{#Platform}-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
SetupIconFile={#SourcePath}Assets\AppIcon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; AppUserModelID: "FluentFold"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; AppUserModelID: "FluentFold"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  VCKey = 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\';

function IsVCInstalled: Boolean;
var
  Key: string;
begin
  Key := VCKey + '{#Platform}';
  Result := RegValueExists(HKLM, Key, 'Bld')
         or RegValueExists(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\{#Platform}', 'Bld');
end;

procedure DownloadAndRun(const Url, FileName: string);
var
  TmpFile, Cmd: string;
  Code: Integer;
begin
  TmpFile := ExpandConstant('{tmp}\' + FileName);
  Cmd := '-Command "& {param($u,$f) [Net.ServicePointManager]::SecurityProtocol = 3072; try { Invoke-WebRequest -Uri $u -OutFile $f; exit 0 } catch { exit 1 }}" -u "' + Url + '" -f "' + TmpFile + '"';
  Log('Downloading: ' + Url);
  if Exec('powershell.exe', Cmd, '', SW_HIDE, ewWaitUntilTerminated, Code) and (Code = 0) then
  begin
    Log('Installing VC++ redist...');
    Exec(TmpFile, '/install /quiet /norestart', '', SW_SHOW, ewNoWait, Code);
  end
  else
    Log('Download failed with code: ' + IntToStr(Code));
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Url: string;
begin
  if IsVCInstalled then Exit;
  case '{#Platform}' of
    'x64':   Url := 'https://aka.ms/vs/17/release/vc_redist.x64.exe';
    'x86':   Url := 'https://aka.ms/vs/17/release/vc_redist.x86.exe';
    'arm64': Url := 'https://aka.ms/vs/17/release/vc_redist.arm64.exe';
  end;
  if Url = '' then Exit;
  if SuppressibleMsgBox('This app requires the Microsoft Visual C++ Redistributable.'#13#10#13#10'Download and install it now?', mbConfirmation, MB_YESNO, IDYES) = IDYES then
    DownloadAndRun(Url, 'vc_redist_{#Platform}.exe')
  else
    Result := 'Missing Microsoft Visual C++ Redistributable. Install manually from: ' + Url;
end;
