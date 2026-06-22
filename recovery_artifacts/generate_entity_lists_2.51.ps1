param(
    [string]$OriginalExe = "$PSScriptRoot\..\..\Intel大小核神经网络调度器N版2.51永久权重（20分钟训练版）\IntlThrdSchd\IntlThrdSchd.exe",
    [string]$RecoveredExe = "$PSScriptRoot\..\..\recovered_src_2.51\IntlThrdPerfSchd\bin\Debug\net48\IntlThrdSchd.exe"
)

$ErrorActionPreference = "Stop"

$outOriginal = Join-Path $PSScriptRoot "entities_original_2.51.txt"
$outRecovered = Join-Path $PSScriptRoot "entities_recovered_2.51.txt"
$outDiff = Join-Path $PSScriptRoot "entities_diff_2.51.txt"

if (-not (Test-Path -LiteralPath $OriginalExe)) {
    throw "Original EXE not found: $OriginalExe"
}
if (-not (Test-Path -LiteralPath $RecoveredExe)) {
    throw "Recovered build EXE not found: $RecoveredExe"
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

