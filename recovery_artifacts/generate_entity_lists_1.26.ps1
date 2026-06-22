param(
    [string]$OriginalExe = "$PSScriptRoot\original\IntlThrdSchd.exe",
    [string]$RecoveredExe = "$PSScriptRoot\patched\IntlThrdSchd.exe"
)

$ErrorActionPreference = "Stop"

$outOriginal = Join-Path $PSScriptRoot "entities_original_1.26.txt"
$outRecovered = Join-Path $PSScriptRoot "entities_patched_1.26.txt"
$outDiff = Join-Path $PSScriptRoot "entities_diff_patched_1.26.txt"

if (-not (Test-Path -LiteralPath $OriginalExe)) {
    throw "Original EXE not found: $OriginalExe"
}
if (-not (Test-Path -LiteralPath $RecoveredExe)) {
    throw "Recovered EXE not found: $RecoveredExe"
}

$orig = ilspycmd --disable-updatecheck -l cisde $OriginalExe | Sort-Object
$rec = ilspycmd --disable-updatecheck -l cisde $RecoveredExe | Sort-Object

$orig | Set-Content -Encoding UTF8 $outOriginal
$rec | Set-Content -Encoding UTF8 $outRecovered

Compare-Object -ReferenceObject $orig -DifferenceObject $rec |
    Sort-Object SideIndicator, InputObject |
    Format-Table -AutoSize |
    Out-String |
    Set-Content -Encoding UTF8 $outDiff

Write-Host "wrote:"
Write-Host "  $outOriginal"
Write-Host "  $outRecovered"
Write-Host "  $outDiff"
