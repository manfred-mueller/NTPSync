#define MyAppName "NTPSync"
#define MyAppVersion "1.0.0"
#define MyAppExeName MyAppName + ".exe"
#define MyAppPublisher "NASS e.K."
#define MyAppURL "https://www.nass-ek.de"
#define ProgramFiles GetEnv("ProgramFiles")

[Setup]
AppId={{424CD576-1191-4760-A6CD-D6E2E5534E8E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=D:\Dokumente\gpl_de.txt
PrivilegesRequired=admin
OutputDir=bin/Release
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}
SetupIconFile=icons\logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableWelcomePage=False
WizardImageFile=D:\Bilder\wz_nass-ek.bmp
WizardSmallImageFile=D:\Bilder\wz_leer_small.bmp
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ChangesAssociations = yes
SignTool=Certum
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}";
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon;

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
Source: "{#MyAppName}.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\System.Runtime.CompilerServices.Unsafe.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\System.Numerics.Vectors.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\System.Memory.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\System.Diagnostics.DiagnosticSource.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\System.Buffers.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\Open.Nat.dll"; DestDir: "{app}"; Flags: ignoreversion

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  UninstallString: string;
begin
  Result := True;
  if RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{424CD576-1191-4760-A6CD-D6E2E5534E8E}_is1',
    'UninstallString', UninstallString) then
  begin
    MsgBox('Eine frühere Version wird nun entfernt.', mbInformation, MB_OK);
    if ShellExec('', UninstallString, '/SILENT', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
    begin
      // Alles gut
    end
    else
    begin
      MsgBox('Fehler bei der Deinstallation der alten Version. Installation wird abgebrochen.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

[Run]
Filename: "schtasks.exe"; \
Parameters: "/Create /TN ""NTPSync"" /TR ""powershell.exe -ExecutionPolicy Bypass -File \""{app}\NTPSync.ps1\"" >> C:\Windows\Temp\NTPSync-Log.txt 2>&1"" /SC ONEVENT /EC System /MO ""*[System[Provider[@Name='Microsoft-Windows-Winlogon'] and (EventID=7001)]]"" /RL HIGHEST /RU SYSTEM /F"; \
Flags: runhidden

[UninstallRun]
Filename: "schtasks.exe"; \
Parameters: "/Delete /TN ""NTPSync"" /F"; \
Flags: runhidden
