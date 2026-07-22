; Inno Setup script for FluentFold
; Run with: iscc installer.iss /dPlatform=x64 /dSourceDir=...\publish

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
OutputDir={#SourcePath}bin\{#Platform}\Release
OutputBaseFilename=FluentFold-{#Platform}-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "{#SourcePath}bin\{#Platform}\Release\net10.0-windows10.0.26100.0\win-{#Platform}\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
