param(
    [string]$OriginalExe = "$PSScriptRoot\original\IntlThrdSchd.exe",
    [string]$RecoveredExe = "$PSScriptRoot\..\IntlThrdPerfSchd\bin\Release\net48\IntlThrdSchd.exe",
    [Parameter(Mandatory = $true)]
    [string]$MethodFullName,
    [int]$Context = 10
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

function Find-MethodByFullName {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module,
        [Parameter(Mandatory = $true)] [string] $FullName
    )

    foreach ($t in $Module.GetTypes()) {
        foreach ($m in $t.Methods) {
            if ($m.FullName -eq $FullName) { return $m }
        }
    }

    return $null
}

function Dump-MethodILNormalized {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.MethodDef] $Method
    )

    $lines = New-Object "System.Collections.Generic.List[string]"

    if (-not $Method.HasBody) {
        $lines.Add("<no_body>")
        return ,$lines.ToArray()
    }

    $body = $Method.Body
    $insts = $body.Instructions

    if ($body.HasVariables) {
        for ($i = 0; $i -lt $body.Variables.Count; $i++) {
            $v = $body.Variables[$i]
            $lines.Add(("local:{0}:{1}" -f $i, $v.Type.FullName))
        }
    }

    $index = New-Object "System.Collections.Generic.Dictionary[object,int]"
    for ($i = 0; $i -lt $insts.Count; $i++) { $index[$insts[$i]] = $i }

    for ($i = 0; $i -lt $insts.Count; $i++) {
        $ins = $insts[$i]
        $op = $ins.OpCode.Name
        if ($null -ne $ins.Operand) {
            $op = $op + " " + (Normalize-Operand -Operand $ins.Operand -InstrIndex $index)
        }
        $lines.Add(("{0:D4}: {1}" -f $i, $op))
    }

    if ($body.HasExceptionHandlers) {
        foreach ($eh in $body.ExceptionHandlers) {
            $tryStart = if ($null -ne $eh.TryStart -and $index.ContainsKey($eh.TryStart)) { $index[$eh.TryStart] } else { "?" }
            $tryEnd = if ($null -ne $eh.TryEnd -and $index.ContainsKey($eh.TryEnd)) { $index[$eh.TryEnd] } else { "?" }
            $handlerStart = if ($null -ne $eh.HandlerStart -and $index.ContainsKey($eh.HandlerStart)) { $index[$eh.HandlerStart] } else { "?" }
            $handlerEnd = if ($null -ne $eh.HandlerEnd -and $index.ContainsKey($eh.HandlerEnd)) { $index[$eh.HandlerEnd] } else { "?" }
            $catchType = if ($null -ne $eh.CatchType) { $eh.CatchType.FullName } else { "" }
            $lines.Add(("eh:{0} try:{1}-{2} handler:{3}-{4} catch:{5}" -f $eh.HandlerType, $tryStart, $tryEnd, $handlerStart, $handlerEnd, $catchType))
        }
    }

    return ,$lines.ToArray()
}

if (-not (Test-Path -LiteralPath $OriginalExe)) { throw "Original EXE not found: $OriginalExe" }
if (-not (Test-Path -LiteralPath $RecoveredExe)) { throw "Recovered EXE not found: $RecoveredExe" }

$orig = [dnlib.DotNet.ModuleDefMD]::Load([IO.Path]::GetFullPath($OriginalExe))
$rec = [dnlib.DotNet.ModuleDefMD]::Load([IO.Path]::GetFullPath($RecoveredExe))

$origMethod = Find-MethodByFullName -Module $orig -FullName $MethodFullName
$recMethod = Find-MethodByFullName -Module $rec -FullName $MethodFullName

if ($null -eq $origMethod) { throw "Method not found in OriginalExe: $MethodFullName" }
if ($null -eq $recMethod) { throw "Method not found in RecoveredExe: $MethodFullName" }

$origDump = Dump-MethodILNormalized -Method $origMethod
$recDump = Dump-MethodILNormalized -Method $recMethod

$max = [Math]::Max($origDump.Length, $recDump.Length)
$first = -1
for ($i = 0; $i -lt $max; $i++) {
    $a = if ($i -lt $origDump.Length) { $origDump[$i] } else { "" }
    $b = if ($i -lt $recDump.Length) { $recDump[$i] } else { "" }
    if ($a -ne $b) { $first = $i; break }
}

if ($first -lt 0) {
    Write-Host "IL dump match for: $MethodFullName"
    exit 0
}

$start = [Math]::Max(0, $first - $Context)
$end = [Math]::Min($max - 1, $first + $Context)

Write-Host ("first_mismatch_index: {0}" -f $first)
Write-Host ""
Write-Host "---- original ----"
for ($i = $start; $i -le $end; $i++) {
    if ($i -lt $origDump.Length) { Write-Host $origDump[$i] } else { Write-Host "<eof>" }
}
Write-Host ""
Write-Host "---- recovered ----"
for ($i = $start; $i -le $end; $i++) {
    if ($i -lt $recDump.Length) { Write-Host $recDump[$i] } else { Write-Host "<eof>" }
}
