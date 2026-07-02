param(
    [string]$OriginalExe = "$PSScriptRoot\original\IntlThrdSchd.exe",
    [string]$RecoveredExe = "$PSScriptRoot\..\IntlThrdPerfSchd\bin\Release\net48\IntlThrdSchd.exe",
    [int]$MaxDiffLines = 500,
    [string]$OutPath = "$PSScriptRoot\method_il_hash_diff_2.81.txt"
)

$ErrorActionPreference = "Stop"

$sharedScript = Resolve-Path (Join-Path $PSScriptRoot "..\..\recovered_src_2.51\recovery_artifacts\compare_method_il_hashes_2.51.ps1")
& $sharedScript.Path -OriginalExe $OriginalExe -RecoveredExe $RecoveredExe -MaxDiffLines $MaxDiffLines -OutPath $OutPath
