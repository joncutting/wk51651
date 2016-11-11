[Setup]
AppName=ASTM E55 WK51651 eData Tools
AppVersion=0.1
DefaultDirName={pf}\ASTM E55 WK51651 eData Tools
OutputDir=.
DefaultGroupName=ASTM E55 WK51651 eData
UninstallDisplayIcon={app}\eDataSigTool.exe
;Require Windows 7 SP1 
MinVersion=6.1.7601

[Files]
;Files for eData Signature Tool
Source: "..\eDataSigTool\bin\Debug\eDataSigTool.exe"; DestDir: "{app}"
Source: "..\eDataSigTool\bin\Debug\eDataSigTool.exe.config"; DestDir: "{app}"
Source: "..\eDataSigTool\bin\Debug\eDataSigTool.exe.manifest"; DestDir: "{app}"
Source: "..\eDataSigTool\bin\Debug\eData.xsd"; DestDir: "{app}"
Source: "..\eDataSigTool\bin\Debug\xmldsig-core-schema.xsd"; DestDir: "{app}"
Source: "readme.txt"; DestDir: "{app}"; Flags: "isreadme" 
[Icons]
Name: "{group}\eData Signature Tool"; Filename: "{app}\eDataSigTool.exe"

