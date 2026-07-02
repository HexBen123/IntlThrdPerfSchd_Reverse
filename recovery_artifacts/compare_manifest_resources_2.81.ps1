param(
    [string]$OriginalExe = "$PSScriptRoot\original\IntlThrdSchd.exe",
    [string]$RecoveredExe = "$PSScriptRoot\..\IntlThrdPerfSchd\bin\Release\net48\IntlThrdSchd.exe",
    [string]$ResourceName = "IntlThrdSchd.ProjectInstaller.resources",
    [string]$OutPath = "$PSScriptRoot\manifest_resource_compare_2.81.txt"
)

$ErrorActionPreference = "Stop"

function Get-ResBytes([string]$assemblyPath, [string]$resName) {
    $asm = [System.Reflection.Assembly]::LoadFile([IO.Path]::GetFullPath($assemblyPath))
    $s = $asm.GetManifestResourceStream($resName)
    if (-not $s) {
        throw "resource not found: $resName in $assemblyPath"
    }

    $ms = New-Object IO.MemoryStream
    try {
        $s.CopyTo($ms)
        return ,$ms.ToArray()
    } finally {
        $s.Dispose()
        $ms.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $OriginalExe)) {
    throw "Original EXE not found: $OriginalExe"
}
if (-not (Test-Path -LiteralPath $RecoveredExe)) {
    throw "Recovered EXE not found: $RecoveredExe"
}

$b1 = Get-ResBytes -assemblyPath $OriginalExe -resName $ResourceName
$b2 = Get-ResBytes -assemblyPath $RecoveredExe -resName $ResourceName

$sha = [Security.Cryptography.SHA256]::Create()
$h1 = [BitConverter]::ToString($sha.ComputeHash($b1)).Replace("-", "")
$h2 = [BitConverter]::ToString($sha.ComputeHash($b2)).Replace("-", "")

$equal = ($h1 -eq $h2) -and ($b1.Length -eq $b2.Length)

$lines = @(
    "resource: $ResourceName",
    "original: len=$($b1.Length) sha256=$h1",
    "recovered: len=$($b2.Length) sha256=$h2",
    "equal: $equal"
)

$lines | Set-Content -Encoding UTF8 $OutPath
$lines
