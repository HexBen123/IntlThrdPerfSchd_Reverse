param(
    [string]$OriginalExe = "$PSScriptRoot\original\IntlThrdSchd.exe",
    [string]$RecoveredExe = "$PSScriptRoot\..\IntlThrdPerfSchd\bin\Release\net48\IntlThrdSchd.exe",
    [int]$MaxDiffLines = 500,
    [string]$OutPath = "$PSScriptRoot\metadata_surface_diff_2.81.txt"
)

$ErrorActionPreference = "Stop"

$sharedScript = Resolve-Path (Join-Path $PSScriptRoot "..\..\recovered_src_1.26\recovery_artifacts\compare_metadata_surface_1.26.ps1")
& $sharedScript.Path -OriginalExe $OriginalExe -RecoveredExe $RecoveredExe -MaxDiffLines $MaxDiffLines -OutPath $OutPath
