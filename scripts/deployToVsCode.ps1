param
(   
    [string]$TargetPath
)

Stop-Process -Name "Microsoft.Dynamics.Nav.EditorServices.Host" -Force -ErrorAction Ignore
$vscodeALAnalyzerFolder = Get-Item "$env:USERPROFILE\.vscode\extensions\ms-dynamics-smb.al-*\bin\Analyzers" | Select -Last 1
xcopy.exe /y /d $TargetPath $vscodeALAnalyzerFolder