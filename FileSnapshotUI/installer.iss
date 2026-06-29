[Setup]
AppId={{6748358B-E774-4237-B585-13BAA56E1766}}
AppName=File Snapshot
AppVersion=1.0.0
AppPublisher=ORB International
UninstallDisplayName=File Snapshot
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DefaultDirName={autopf}\File Snapshot
SetupIconFile=Assets\Icon.ico
UninstallDisplayIcon={app}\File Snapshot.exe
OutputDir=.\InstallerOutput
OutputBaseFilename=FileSnapshotSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
Uninstallable=yes

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "startmenu"; Description: "Create a &start menu shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Register the app to launch on Windows Startup
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "FileSnapshot"; ValueData: """{app}\File Snapshot.exe"""; Flags: uninsdeletevalue

[Icons]
Name: "{commonprograms}\File Snapshot"; Filename: "{app}\File Snapshot.exe"; Tasks: startmenu
Name: "{autodesktop}\File Snapshot"; Filename: "{app}\File Snapshot.exe"; Tasks: desktopicon

[Run]
; 1. Install .NET if needed (pointing to the function)
Filename: "{tmp}\dotnet_installer.exe"; Parameters: "/quiet /norestart"; Check: NeedsDotNetCheck; StatusMsg: "Installing .NET Runtime..."; Flags: waituntilterminated
; 2. Install WinAppSDK if needed (pointing to the function)
Filename: "{tmp}\winappsdk_installer.exe"; Parameters: "--quiet"; Check: NeedsWinAppSDKCheck; StatusMsg: "Installing Windows App Runtime..."; Flags: waituntilterminated
; 3. Launch the app
Filename: "{app}\File Snapshot.exe"; Description: "{cm:LaunchProgram,File Snapshot}"; Flags: nowait postinstall skipifsilent

[Code]
var
  NeedsDotNet: Boolean;
  NeedsWinAppSDK: Boolean;

function DownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  Result := True;
end;

function NeedsDotNetCheck(): Boolean;
begin
  Result := NeedsDotNet;
end;

function NeedsWinAppSDKCheck(): Boolean;
begin
  Result := NeedsWinAppSDK;
end;

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  // Kill the app if running
  Exec(ExpandConstant('taskkill.exe'), '/F /IM "File Snapshot.exe"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  
  // Check dependencies
  if not RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost') then
    NeedsDotNet := True;
  
  // Note: Adjust this path if you have a specific registry key for your WinAppSDK version
  if not RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WindowsAppRuntime') then
    NeedsWinAppSDK := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then
  begin
    if NeedsDotNet then
      DownloadTemporaryFile('https://download.visualstudio.microsoft.com/download/pr/dotnet-desktop-runtime-8.0.x-win-x64.exe', 'dotnet_installer.exe', '', @DownloadProgress);
      
    if NeedsWinAppSDK then
      DownloadTemporaryFile('https://aka.ms/windowsappsdk/1.5/1.5.240311000/windowsappruntimeinstall-x64.exe', 'winappsdk_installer.exe', '', @DownloadProgress);
  end;
end;