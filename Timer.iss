; Inno Setup 安装程序脚本
; 用于创建 Timer 应用的 Windows 安装程序

#define AppName "比赛计时系统"
#define AppVersion "1.0.0"
#define AppPublisher "Timer Team"
#define AppURL "https://example.com"
#define AppExeName "Timer.exe"
#define OutputDir "Output"
#define SourceDir "Timer\Timer\bin\Publish\x64\Release"

[Setup]
; 基本信息
AppId={{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
LicenseFile=
OutputDir={#OutputDir}
OutputBaseFilename=Timer-Setup
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

; 安装程序外观
WizardImageFile=
WizardSmallImageFile=

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; 主程序文件（单文件发布，只需要 exe）
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; 如果还有其他文件（如配置文件、数据库模板等），可以在这里添加
; Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// 检查 .NET 运行时（如果需要）
function InitializeSetup(): Boolean;
begin
  Result := True;
  // 如果应用需要特定版本的 .NET，可以在这里检查
  // 例如：检查 .NET 10.0 Runtime
end;

