#define MyAppName "Operator AI"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Operator AI"
#define MyAppExeName "Operator.Desktop.exe"

[Setup]
AppId={{62E57F35-A249-4F89-9CE6-3ACF61968258}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Operator AI
DefaultGroupName=Operator AI
OutputDir=output
OutputBaseFilename=OperatorAI-1.0-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
WizardStyle=modern

[Files]
Source: "..\Operator.Desktop\bin\publish\OperatorAI-1.0-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Operator AI"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Operator AI"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Operator AI"; Flags: nowait postinstall skipifsilent
