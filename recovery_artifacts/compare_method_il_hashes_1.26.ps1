param(
    [string]$OriginalExe = "$PSScriptRoot\original\IntlThrdSchd.exe",
    [string]$RecoveredExe = "$PSScriptRoot\..\IntlThrdPerfSchd\bin\Release\net48\IntlThrdSchd.exe",
    [int]$MaxDiffLines = 200,
    [string]$OutPath = "$PSScriptRoot\method_il_hash_diff_1.26.txt",
    [switch]$IncludeCompilerGenerated
)

$ErrorActionPreference = "Stop"

$dnlib = Resolve-Path "$PSScriptRoot\tools\dnlib.dll"
Add-Type -Path $dnlib.Path

function Normalize-Operand {
    param(
        [Parameter(Mandatory = $true)] $Operand,
        [Parameter(Mandatory = $true)] [System.Collections.Generic.Dictionary[object, int]] $InstrIndex
    )

    if ($null -eq $Operand) { return "" }

    if ($Operand -is [dnlib.DotNet.MemberRef]) {
        if ($Operand.IsFieldRef) { return "field:" + $Operand.FullName }
        if ($Operand.IsMethodRef) { return "method:" + $Operand.FullName }
    }
    if ($Operand -is [dnlib.DotNet.IField]) { return "field:" + $Operand.FullName }
    if ($Operand -is [dnlib.DotNet.IType]) { return "type:" + $Operand.FullName }
    if ($Operand -is [dnlib.DotNet.IMethod]) { return "method:" + $Operand.FullName }

    if ($Operand -is [dnlib.DotNet.Emit.Instruction]) {
        if ($InstrIndex.ContainsKey($Operand)) { return "br:" + $InstrIndex[$Operand] }
        return "br:?"
    }

    if ($Operand -is [dnlib.DotNet.Emit.Instruction[]]) {
        $parts = foreach ($t in $Operand) {
            if ($InstrIndex.ContainsKey($t)) { $InstrIndex[$t] } else { "?" }
        }
        return "switch:" + ($parts -join ",")
    }

    if ($Operand -is [dnlib.DotNet.Emit.Local]) {
        return ("loc:{0}:{1}" -f $Operand.Index, $Operand.Type.FullName)
    }

    if ($Operand -is [dnlib.DotNet.Parameter]) {
        return ("arg:{0}:{1}" -f $Operand.Index, $Operand.Type.FullName)
    }

    if ($Operand -is [string]) { return "str:" + $Operand.Replace("`r", "\r").Replace("`n", "\n") }
    if ($Operand -is [byte[]]) { return "blob:" + ([BitConverter]::ToString($Operand).Replace("-", "")) }

    return "op:" + $Operand.ToString()
}

function Get-MethodIlHash {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.MethodDef] $Method
    )

    if (-not $Method.HasBody) { return $null }

    $sha = [System.Security.Cryptography.SHA256]::Create()
    $enc = [System.Text.Encoding]::UTF8

    $body = $Method.Body
    $insts = $body.Instructions
    $index = New-Object "System.Collections.Generic.Dictionary[object,int]"
    for ($i = 0; $i -lt $insts.Count; $i++) {
        $index[$insts[$i]] = $i
    }

    # Include locals and EH table so two identical instruction streams with different
    # exception regions do not get treated as equal.
    if ($body.HasVariables) {
        foreach ($v in $body.Variables) {
            $line = "local:" + $v.Type.FullName
            $bytes = $enc.GetBytes($line + "`n")
            [void]$sha.TransformBlock($bytes, 0, $bytes.Length, $null, 0)
        }
    }

    foreach ($ins in $insts) {
        $op = $ins.OpCode.Name
        if ($null -ne $ins.Operand) {
            $op = $op + " " + (Normalize-Operand -Operand $ins.Operand -InstrIndex $index)
        }
        $bytes = $enc.GetBytes($op + "`n")
        [void]$sha.TransformBlock($bytes, 0, $bytes.Length, $null, 0)
    }

    if ($body.HasExceptionHandlers) {
        foreach ($eh in $body.ExceptionHandlers) {
            $tryStart = if ($null -ne $eh.TryStart -and $index.ContainsKey($eh.TryStart)) { $index[$eh.TryStart] } else { "?" }
            $tryEnd = if ($null -ne $eh.TryEnd -and $index.ContainsKey($eh.TryEnd)) { $index[$eh.TryEnd] } else { "?" }
            $handlerStart = if ($null -ne $eh.HandlerStart -and $index.ContainsKey($eh.HandlerStart)) { $index[$eh.HandlerStart] } else { "?" }
            $handlerEnd = if ($null -ne $eh.HandlerEnd -and $index.ContainsKey($eh.HandlerEnd)) { $index[$eh.HandlerEnd] } else { "?" }
            $catchType = if ($null -ne $eh.CatchType) { $eh.CatchType.FullName } else { "" }
            $line = ("eh:{0} try:{1}-{2} handler:{3}-{4} catch:{5}" -f $eh.HandlerType, $tryStart, $tryEnd, $handlerStart, $handlerEnd, $catchType)
            $bytes = $enc.GetBytes($line + "`n")
            [void]$sha.TransformBlock($bytes, 0, $bytes.Length, $null, 0)
        }
    }

    [void]$sha.TransformFinalBlock([byte[]]::new(0), 0, 0)
    return [BitConverter]::ToString($sha.Hash).Replace("-", "")
}

function Get-MethodMap {
    param(
        [Parameter(Mandatory = $true)] [string] $AssemblyPath,
        [Parameter(Mandatory = $true)] [bool] $IncludeCompilerGenerated
    )

    $mod = [dnlib.DotNet.ModuleDefMD]::Load([IO.Path]::GetFullPath($AssemblyPath))
    $map = @{}

    foreach ($t in $mod.GetTypes()) {
        if ((-not $IncludeCompilerGenerated) -and ($t.FullName -match "[<>]")) { continue }

        foreach ($m in $t.Methods) {
            if (-not $m.HasBody) { continue }
            if ((-not $IncludeCompilerGenerated) -and ($m.Name -match "[<>]")) { continue }

            $key = $m.FullName
            $hash = Get-MethodIlHash -Method $m
            if ($null -eq $hash) { continue }
            $map[$key] = $hash
        }
    }

    return $map
}

if (-not (Test-Path -LiteralPath $OriginalExe)) {
    throw "Original EXE not found: $OriginalExe"
}
if (-not (Test-Path -LiteralPath $RecoveredExe)) {
    throw "Recovered EXE not found: $RecoveredExe"
}

Write-Host "loading modules and hashing stable methods..."
$origMap = Get-MethodMap -AssemblyPath $OriginalExe -IncludeCompilerGenerated $IncludeCompilerGenerated.IsPresent
$recMap = Get-MethodMap -AssemblyPath $RecoveredExe -IncludeCompilerGenerated $IncludeCompilerGenerated.IsPresent

$origKeys = $origMap.Keys | Sort-Object
$recKeys = $recMap.Keys | Sort-Object

$missing = Compare-Object -ReferenceObject $origKeys -DifferenceObject $recKeys -PassThru | Where-Object { $_ -in $origKeys }
$extra = Compare-Object -ReferenceObject $origKeys -DifferenceObject $recKeys -PassThru | Where-Object { $_ -in $recKeys }

$mismatch = New-Object System.Collections.Generic.List[string]
$matchCount = 0
foreach ($k in $origKeys) {
    if (-not $recMap.ContainsKey($k)) { continue }
    if ($origMap[$k] -eq $recMap[$k]) { $matchCount++; continue }
    $mismatch.Add($k)
}

$compared = ($origKeys | Where-Object { $recMap.ContainsKey($_) }).Count
$totalOrig = $origKeys.Count
$totalRec = $recKeys.Count

$pct = if ($compared -eq 0) { 0 } else { [Math]::Round(($matchCount * 100.0) / $compared, 4) }

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("OriginalExe: $([IO.Path]::GetFullPath($OriginalExe))")
$lines.Add("RecoveredExe: $([IO.Path]::GetFullPath($RecoveredExe))")
$lines.Add("include_compiler_generated: $($IncludeCompilerGenerated.IsPresent)")
$lines.Add("")
$lines.Add("stable_method_count_original: $totalOrig")
$lines.Add("stable_method_count_recovered: $totalRec")
$lines.Add("stable_method_count_compared(intersection): $compared")
$lines.Add("stable_method_hash_match_count: $matchCount")
$lines.Add("stable_method_hash_match_percent: $pct")
$lines.Add("")
$lines.Add("missing_in_recovered_count: $($missing.Count)")
$lines.Add("extra_in_recovered_count: $($extra.Count)")
$lines.Add("il_mismatch_count: $($mismatch.Count)")

function Add-LimitedList([string]$title, [object[]]$items) {
    $lines.Add("")
    $lines.Add($title)
    $max = [Math]::Max(0, $MaxDiffLines)
    $take = if ($items.Count -gt $max) { $items[0..($max-1)] } else { $items }
    foreach ($x in $take) { $lines.Add("  " + $x) }
    if ($items.Count -gt $max) { $lines.Add("  ... truncated ...") }
}

Add-LimitedList -title "missing_in_recovered:" -items ($missing | Sort-Object)
Add-LimitedList -title "extra_in_recovered:" -items ($extra | Sort-Object)
Add-LimitedList -title "il_hash_mismatch_methods:" -items ($mismatch | Sort-Object)

[IO.File]::WriteAllLines([IO.Path]::GetFullPath($OutPath), $lines, [Text.Encoding]::UTF8)
Write-Host "wrote: $OutPath"
