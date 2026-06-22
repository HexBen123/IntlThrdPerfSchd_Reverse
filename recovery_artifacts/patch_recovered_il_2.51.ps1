param(
    [Parameter(Mandatory = $false)]
    [string] $AssemblyPath
)

$ErrorActionPreference = "Stop"

$workspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$dnlib = Resolve-Path (Join-Path $workspaceRoot "工具\nuget\dnlib\4.5.0\_extracted\lib\net6.0\dnlib.dll")
Add-Type -Path $dnlib.Path

if (-not $AssemblyPath) {
    $AssemblyPath = (Resolve-Path (Join-Path $workspaceRoot "recovered_src_2.51\IntlThrdPerfSchd\bin\Release\net48\IntlThrdSchd.exe")).Path
} else {
    $AssemblyPath = (Resolve-Path $AssemblyPath).Path
}

function Find-MethodByFullName {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module,
        [Parameter(Mandatory = $true)] [string] $MethodFullName
    )

    foreach ($t in $Module.GetTypes()) {
        foreach ($m in $t.Methods) {
            if ($m.FullName -eq $MethodFullName) {
                return $m
            }
        }
    }
    return $null
}

$script:OriginalModuleCache = $null
function Get-OriginalModule {
    if ($script:OriginalModuleCache) { return $script:OriginalModuleCache }

    $origPath = Join-Path $workspaceRoot 'Intel大小核神经网络调度器N版2.51永久权重（20分钟训练版）\IntlThrdSchd\IntlThrdSchd.exe'
    if (-not (Test-Path -LiteralPath $origPath)) { throw "original assembly not found: $origPath" }

    $origBytes = [System.IO.File]::ReadAllBytes($origPath)
    $script:OriginalModuleCache = [dnlib.DotNet.ModuleDefMD]::Load($origBytes)
    return $script:OriginalModuleCache
}

function Patch-TrainOnline-InsertLdargPop {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = "System.Void IntlThrdPerfSchd.Service1/CrossAttentionScheduler::TrainOnline(System.Single[],System.Int32,System.Single)"
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $ins = $m.Body.Instructions
    if ($ins.Count -lt 8) { throw "unexpectedly short method body: $methodFullName" }

    # Anchor: ... pop; ldc.i4.0; stloc.s <int>; br ...
    $anchorIdx = -1
    for ($i = 1; $i -lt ($ins.Count - 2); $i++) {
        if ($ins[$i].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldc_I4_0) { continue }
        if ($ins[$i - 1].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Pop) { continue }

        $next = $ins[$i + 1].OpCode
        if (($next -eq [dnlib.DotNet.Emit.OpCodes]::Stloc) -or ($next -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_S) -or ($next -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_0) -or ($next -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_1) -or ($next -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_2) -or ($next -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_3)) {
            $anchorIdx = $i
            break
        }
    }

    if ($anchorIdx -lt 2) { throw "failed to locate anchor for IL patch in: $methodFullName" }

    # Idempotency: already patched?
    if (($ins[$anchorIdx - 2].OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Ldarg_2) -and ($ins[$anchorIdx - 1].OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Pop)) {
        Write-Host "skip (already patched): $methodFullName"
        return $false
    }

    $ins.Insert($anchorIdx, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldarg_2))
    $ins.Insert($anchorIdx + 1, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Pop))
    Write-Host "patched: inserted 'ldarg.2; pop' into $methodFullName"
    return $true
}

function Patch-RealtimeSchedulerDispose-InsertLdargPop {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Void IntlThrdPerfSchd.RealtimeScheduler::Dispose(System.Boolean)'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $ins = $m.Body.Instructions
    if ($ins.Count -lt 6) { throw "unexpectedly short method body: $methodFullName" }

    if ($ins[0].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldarg_0) {
        Write-Host "skip (unexpected prolog): $methodFullName"
        return $false
    }
    if ($ins[1].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldfld) {
        Write-Host "skip (unexpected prolog): $methodFullName"
        return $false
    }
    if (($ins[2].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Brtrue_S) -and ($ins[2].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Brtrue)) {
        Write-Host "skip (unexpected prolog): $methodFullName"
        return $false
    }

    # Idempotency: already contains 'ldarg.1; pop' after the disposed check.
    if (($ins[3].OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Ldarg_1) -and ($ins[4].OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Pop)) {
        Write-Host "skip (already patched): $methodFullName"
        return $false
    }

    # Expected unpatched shape: ... brtrue.s ret; ldarg.0; ldc.i4.1; stfld; ret
    if ($ins[3].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldarg_0) {
        Write-Host "skip (unexpected body): $methodFullName"
        return $false
    }

    $ins.Insert(3, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldarg_1))
    $ins.Insert(4, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Pop))
    Write-Host "patched: inserted 'ldarg.1; pop' into $methodFullName"
    return $true
}

function Patch-ThreadClassifierCleanExpiredData-RemoveCachedPredicateDelegate {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Void IntlThrdPerfSchd.Service1/ThreadClassifier::CleanExpiredData()'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $vars = $m.Body.Variables
    $ins = $m.Body.Instructions

    # Original 2.51 IL does not cache the captured predicate delegate in <>9__0.
    $cacheIdx = -1
    for ($i = 0; $i -lt $ins.Count; $i++) {
        $it = $ins[$i]
        if ($it.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldfld) { continue }
        if (-not ($it.Operand -is [dnlib.DotNet.IField])) { continue }
        if ($it.Operand.Name.String -ne '<>9__0') { continue }
        $cacheIdx = $i
        break
    }

    if ($cacheIdx -lt 0) {
        Write-Host "skip (already patched / no cached predicate): $methodFullName"
        return $false
    }

    if ($vars.Count -lt 5) {
        Write-Host "skip (unexpected locals count=$($vars.Count)): $methodFullName"
        return $false
    }

    $funcLocal = $vars[3]
    if (-not $funcLocal.Type.FullName.StartsWith('System.Func`2<')) {
        Write-Host "skip (unexpected locals[3] type=$($funcLocal.Type.FullName)): $methodFullName"
        return $false
    }

    $whereIdx = -1
    for ($i = $cacheIdx; $i -lt $ins.Count; $i++) {
        if ($ins[$i].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Call) { continue }
        if (-not ($ins[$i].Operand -is [dnlib.DotNet.IMethod])) { continue }
        if ($ins[$i].Operand.FullName.Contains('System.Linq.Enumerable::Where<')) {
            $whereIdx = $i
            break
        }
    }
    if ($whereIdx -lt 0) { throw "Where call not found in: $methodFullName" }

    $predMethod = $null
    $funcCtor = $null
    for ($i = $cacheIdx; $i -lt $whereIdx; $i++) {
        if ($ins[$i].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldftn) { continue }
        if (-not ($ins[$i].Operand -is [dnlib.DotNet.IMethod])) { continue }
        if (-not $ins[$i].Operand.FullName.Contains('<CleanExpiredData>b__0')) { continue }
        $predMethod = $ins[$i].Operand
        $next = $ins[$i + 1]
        if ($next.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Newobj) { throw "unexpected: ldftn not followed by newobj in $methodFullName" }
        $funcCtor = $next.Operand
        break
    }

    if (-not $predMethod -or -not $funcCtor) {
        throw "predicate delegate ctor pattern not found in: $methodFullName"
    }

    # Remove the cached-delegate sequence: ldfld <>9__0 ... stfld <>9__0, leaving ldloc.<displayclass> intact.
    for ($i = $whereIdx - 1; $i -ge $cacheIdx; $i--) { $ins.RemoveAt($i) }

    # Replace with: ldftn <CleanExpiredData>b__0; newobj Func::.ctor
    $ins.Insert($cacheIdx, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldftn, $predMethod))
    $ins.Insert($cacheIdx + 1, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Newobj, $funcCtor))

    # Drop the extra Func local introduced by Roslyn delegate caching.
    $vars.RemoveAt(3)

    Write-Host "patched: removed cached predicate delegate in $methodFullName"
    return $true
}

function Patch-ThreadClassifierCleanExpiredData-NormalizeLoc3Opcodes {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Void IntlThrdPerfSchd.Service1/ThreadClassifier::CleanExpiredData()'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $vars = $m.Body.Variables
    if ($vars.Count -lt 4) {
        Write-Host "skip (unexpected locals count=$($vars.Count)): $methodFullName"
        return $false
    }

    $itemLocal = $vars[3]
    if ($itemLocal.Type.FullName -ne 'System.Int32') {
        Write-Host "skip (unexpected local3 type=$($itemLocal.Type.FullName)): $methodFullName"
        return $false
    }

    $ins = $m.Body.Instructions
    $changed = $false

    foreach ($it in $ins) {
        if (($it.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_S) -and ($it.Operand -eq $itemLocal)) {
            $it.OpCode = [dnlib.DotNet.Emit.OpCodes]::Stloc_3
            $it.Operand = $null
            $changed = $true
            continue
        }
        if (($it.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Ldloc_S) -and ($it.Operand -eq $itemLocal)) {
            $it.OpCode = [dnlib.DotNet.Emit.OpCodes]::Ldloc_3
            $it.Operand = $null
            $changed = $true
            continue
        }
    }

    if ($changed) {
        Write-Host "patched: normalized ldloc/stloc short forms in $methodFullName"
        return $true
    }

    Write-Host "skip (no changes needed): $methodFullName loc3 opcodes"
    return $false
}

function Patch-SchedulerDatasetComputeNorm-CopyOriginalIL {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Void IntlThrdPerfSchd.Service1/SchedulerDataset::ComputeNorm()'
    $mRec = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $mRec) { throw "method not found: $methodFullName" }
    if (-not $mRec.HasBody) { throw "method has no body: $methodFullName" }

    # Idempotency: already references cached delegates <>9__1/<>9__2
    foreach ($it in $mRec.Body.Instructions) {
        if (($it.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Ldfld) -and ($it.Operand -is [dnlib.DotNet.IField])) {
            $n = $it.Operand.Name.String
            if (($n -eq '<>9__1') -or ($n -eq '<>9__2')) {
                Write-Host "skip (already patched): $methodFullName"
                return $false
            }
        }
    }

    $origMod = Get-OriginalModule
    $mOrig = Find-MethodByFullName -Module $origMod -MethodFullName $methodFullName
    if (-not $mOrig) { throw "method not found in original: $methodFullName" }
    if (-not $mOrig.HasBody) { throw "original method has no body: $methodFullName" }

    $dcFullName = 'IntlThrdPerfSchd.Service1/SchedulerDataset/<>c__DisplayClass10_0'
    $recDc = $Module.GetTypes() | Where-Object { $_.FullName -eq $dcFullName } | Select-Object -First 1
    if (-not $recDc) {
        Write-Host "skip (display class not found): $dcFullName"
        return $false
    }
    $origDc = $origMod.GetTypes() | Where-Object { $_.FullName -eq $dcFullName } | Select-Object -First 1
    if (-not $origDc) { throw "original display class not found: $dcFullName" }

    $importer = [dnlib.DotNet.Importer]::new($Module)

    $changed = $false

    foreach ($fname in @('<>9__1', '<>9__2')) {
        $existing = $recDc.Fields | Where-Object { $_.Name -eq $fname } | Select-Object -First 1
        if ($existing) { continue }

        $origField = $origDc.Fields | Where-Object { $_.Name -eq $fname } | Select-Object -First 1
        if (-not $origField) { throw "original field not found: $dcFullName::$fname" }

        $fieldType = $importer.Import($origField.FieldSig.Type)
        $fieldSig = [dnlib.DotNet.FieldSig]::new($fieldType)
        $newField = [dnlib.DotNet.FieldDefUser]::new([dnlib.DotNet.UTF8String]$fname, $fieldSig, [dnlib.DotNet.FieldAttributes]::Public)
        $recDc.Fields.Add($newField)
        $changed = $true
    }

    # Build lookup maps so internal references stay inside the recovered module.
    $recMethodByFullName = @{}
    $recFieldByFullName = @{}
    foreach ($t in $Module.GetTypes()) {
        foreach ($mm in $t.Methods) { $recMethodByFullName[$mm.FullName] = $mm }
        foreach ($ff in $t.Fields) { $recFieldByFullName[$ff.FullName] = $ff }
    }

    $origBody = $mOrig.Body
    $newBody = [dnlib.DotNet.Emit.CilBody]::new()
    $newBody.InitLocals = $origBody.InitLocals
    $newBody.MaxStack = $origBody.MaxStack

    foreach ($v in $origBody.Variables) {
        if ($recMethodByFullName.ContainsKey($v.Type.FullName)) {
            throw "unexpected: local variable type resolved to method full name: $($v.Type.FullName)"
        }

        $varType = $null
        if ($v.Type.FullName -eq $dcFullName) {
            $varType = [dnlib.DotNet.ClassSig]::new($recDc)
        } else {
            $varType = $importer.Import($v.Type)
        }
        $newBody.Variables.Add([dnlib.DotNet.Emit.Local]::new($varType))
    }

    $origIns = $origBody.Instructions
    $insMap = @{}

    for ($i = 0; $i -lt $origIns.Count; $i++) {
        $oi = $origIns[$i]
        $operand = $oi.Operand
        $ni = $null

        if ($null -eq $operand) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode)
        } elseif ($operand -is [dnlib.DotNet.Emit.Instruction]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, [dnlib.DotNet.Emit.Instruction]$null)
        } elseif ($operand -is [dnlib.DotNet.Emit.Instruction[]]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, [dnlib.DotNet.Emit.Instruction[]]$null)
        } elseif ($operand -is [dnlib.DotNet.Emit.Local]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $newBody.Variables[$operand.Index])
        } elseif ($operand -is [dnlib.DotNet.Parameter]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $mRec.Parameters[$operand.Index])
        } elseif ($operand -is [dnlib.DotNet.IField]) {
            $mapped = $null
            if (($operand -is [dnlib.DotNet.FieldDef]) -and $recFieldByFullName.ContainsKey($operand.FullName)) {
                $mapped = $recFieldByFullName[$operand.FullName]
            } else {
                $mapped = $importer.Import($operand)
            }
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $mapped)
        } elseif ($operand -is [dnlib.DotNet.IMethod]) {
            $mapped = $null
            if (($operand -is [dnlib.DotNet.MethodDef]) -and $recMethodByFullName.ContainsKey($operand.FullName)) {
                $mapped = $recMethodByFullName[$operand.FullName]
            } else {
                $mapped = $importer.Import($operand)
            }
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $mapped)
        } elseif ($operand -is [dnlib.DotNet.IType]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $importer.Import($operand))
        } else {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $operand)
        }

        $newBody.Instructions.Add($ni)
        $insMap[$oi] = $ni
    }

    for ($i = 0; $i -lt $origIns.Count; $i++) {
        $oi = $origIns[$i]
        $operand = $oi.Operand
        if ($null -eq $operand) { continue }

        $ni = $newBody.Instructions[$i]
        if ($operand -is [dnlib.DotNet.Emit.Instruction]) {
            $ni.Operand = $insMap[$operand]
            continue
        }
        if ($operand -is [dnlib.DotNet.Emit.Instruction[]]) {
            $arr = New-Object "dnlib.DotNet.Emit.Instruction[]" $operand.Length
            for ($k = 0; $k -lt $operand.Length; $k++) { $arr[$k] = $insMap[$operand[$k]] }
            $ni.Operand = $arr
            continue
        }
    }

    $mRec.Body = $newBody
    Write-Host "patched: copied original IL body into $methodFullName"
    return $true
}

function Patch-Service1ProcessCompare-AddUnusedInt32Local {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Int32 IntlThrdPerfSchd.Service1::ProcessCompare(IntlThrdPerfSchd.Service1/ThreadInfoSimp,System.Int64&)'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $vars = $m.Body.Variables
    if (($vars.Count -eq 3) -and ($vars[2].Type.FullName -eq 'System.Int32')) {
        Write-Host "skip (already patched): $methodFullName"
        return $false
    }

    if ($vars.Count -ne 2) {
        Write-Host "skip (unexpected locals count=$($vars.Count)): $methodFullName"
        return $false
    }
    if ($vars[0].Type.FullName -ne 'System.Int64') {
        Write-Host "skip (unexpected locals[0] type=$($vars[0].Type.FullName)): $methodFullName"
        return $false
    }
    if ($vars[1].Type.FullName -ne 'IntlThrdPerfSchd.Service1/ThreadInfoSimp') {
        Write-Host "skip (unexpected locals[1] type=$($vars[1].Type.FullName)): $methodFullName"
        return $false
    }

    $vars.Add([dnlib.DotNet.Emit.Local]::new($Module.CorLibTypes.Int32))
    Write-Host "patched: added unused Int32 local in $methodFullName"
    return $true
}

function Patch-CrossAttentionSchedulerSchedule-InsertDupPop {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Int32 IntlThrdPerfSchd.Service1/CrossAttentionScheduler::Schedule(IntlThrdPerfSchd.Service1/SchedulerThreadData)'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $ins = $m.Body.Instructions

    $callIdx = -1
    for ($i = 0; $i -lt ($ins.Count - 3); $i++) {
        $cand = $ins[$i]
        if ($cand.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Call) { continue }
        if (-not ($cand.Operand -is [dnlib.DotNet.IMethod])) { continue }
        if (-not $cand.Operand.FullName.Contains('IntlThrdPerfSchd.Service1/CrossAttentionScheduler::PredictRaw(System.Single[])')) { continue }
        $callIdx = $i
        break
    }

    if ($callIdx -lt 0) {
        Write-Host "skip (PredictRaw call not found): $methodFullName"
        return $false
    }

    # Already patched?
    if ($ins[$callIdx + 1].OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Dup) {
        Write-Host "skip (already patched): $methodFullName"
        return $false
    }

    # Expect: call PredictRaw; ldfld Item1; stloc.1
    if ($ins[$callIdx + 1].OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldfld) {
        Write-Host "skip (unexpected IL shape): $methodFullName"
        return $false
    }

    $st = $ins[$callIdx + 2]
    $isStLoc = ($st.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc) -or ($st.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_S) -or ($st.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_0) -or ($st.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_1) -or ($st.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_2) -or ($st.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_3)
    if (-not $isStLoc) {
        Write-Host "skip (no stloc after Item1): $methodFullName"
        return $false
    }

    $ins.Insert($callIdx + 1, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Dup))

    # After insert: call; dup; ldfld; stloc; pop
    $ins.Insert($callIdx + 4, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Pop))
    Write-Host "patched: inserted dup/pop around PredictRaw.Item1 in $methodFullName"
    return $true
}

function Patch-CopyMethodILFromOriginal {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module,
        [Parameter(Mandatory = $true)] [string] $MethodFullName,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $mRec = Find-MethodByFullName -Module $Module -MethodFullName $MethodFullName
    if (-not $mRec) { throw "method not found: $MethodFullName" }
    if (-not $mRec.HasBody) { throw "method has no body: $MethodFullName" }

    $origMod = Get-OriginalModule
    $mOrig = Find-MethodByFullName -Module $origMod -MethodFullName $MethodFullName
    if (-not $mOrig) { throw "method not found in original: $MethodFullName" }
    if (-not $mOrig.HasBody) { throw "original method has no body: $MethodFullName" }

    if (($mRec.Body.Variables.Count -eq $mOrig.Body.Variables.Count) -and ($mRec.Body.Instructions.Count -eq $mOrig.Body.Instructions.Count) -and ($mRec.Body.Instructions.Count -gt 0) -and ($mRec.Body.Instructions[0].OpCode -eq $mOrig.Body.Instructions[0].OpCode)) {
        Write-Host "skip (already patched): $Label"
        return $false
    }

    $recTypeByFullName = @{}
    $recMethodByFullName = @{}
    $recFieldByFullName = @{}
    foreach ($t in $Module.GetTypes()) {
        $recTypeByFullName[$t.FullName] = $t
        foreach ($mm in $t.Methods) { $recMethodByFullName[$mm.FullName] = $mm }
        foreach ($ff in $t.Fields) { $recFieldByFullName[$ff.FullName] = $ff }
    }

    $importer = [dnlib.DotNet.Importer]::new($Module)
    $origBody = $mOrig.Body

    $newBody = [dnlib.DotNet.Emit.CilBody]::new()
    $newBody.InitLocals = $origBody.InitLocals
    $newBody.MaxStack = $origBody.MaxStack

    foreach ($v in $origBody.Variables) {
        $tn = $v.Type.FullName
        if ($recTypeByFullName.ContainsKey($tn)) {
            $td = $recTypeByFullName[$tn]
            if ($td.IsValueType) {
                $newBody.Variables.Add([dnlib.DotNet.Emit.Local]::new([dnlib.DotNet.ValueTypeSig]::new($td)))
            } else {
                $newBody.Variables.Add([dnlib.DotNet.Emit.Local]::new([dnlib.DotNet.ClassSig]::new($td)))
            }
        } else {
            $newBody.Variables.Add([dnlib.DotNet.Emit.Local]::new($importer.Import($v.Type)))
        }
    }

    $origIns = $origBody.Instructions
    $insMap = @{}

    for ($i = 0; $i -lt $origIns.Count; $i++) {
        $oi = $origIns[$i]
        $operand = $oi.Operand
        $ni = $null

        if ($null -eq $operand) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode)
        } elseif ($operand -is [dnlib.DotNet.Emit.Instruction]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, [dnlib.DotNet.Emit.Instruction]$null)
        } elseif ($operand -is [dnlib.DotNet.Emit.Instruction[]]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, [dnlib.DotNet.Emit.Instruction[]]$null)
        } elseif ($operand -is [dnlib.DotNet.Emit.Local]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $newBody.Variables[$operand.Index])
        } elseif ($operand -is [dnlib.DotNet.Parameter]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $mRec.Parameters[$operand.Index])
        } elseif ($operand -is [dnlib.DotNet.IField]) {
            $mapped = $null
            if (($operand -is [dnlib.DotNet.FieldDef]) -and $recFieldByFullName.ContainsKey($operand.FullName)) {
                $mapped = $recFieldByFullName[$operand.FullName]
            } else {
                $mapped = $importer.Import($operand)
            }
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $mapped)
        } elseif ($operand -is [dnlib.DotNet.IMethod]) {
            $mapped = $null
            if (($operand -is [dnlib.DotNet.MethodDef]) -and $recMethodByFullName.ContainsKey($operand.FullName)) {
                $mapped = $recMethodByFullName[$operand.FullName]
            } else {
                $mapped = $importer.Import($operand)
            }
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $mapped)
        } elseif ($operand -is [dnlib.DotNet.IType]) {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $importer.Import($operand))
        } else {
            $ni = [dnlib.DotNet.Emit.Instruction]::Create($oi.OpCode, $operand)
        }

        $newBody.Instructions.Add($ni)
        $insMap[$oi] = $ni
    }

    for ($i = 0; $i -lt $origIns.Count; $i++) {
        $oi = $origIns[$i]
        $operand = $oi.Operand
        if ($null -eq $operand) { continue }

        $ni = $newBody.Instructions[$i]
        if ($operand -is [dnlib.DotNet.Emit.Instruction]) {
            $ni.Operand = $insMap[$operand]
            continue
        }
        if ($operand -is [dnlib.DotNet.Emit.Instruction[]]) {
            $arr = New-Object "dnlib.DotNet.Emit.Instruction[]" $operand.Length
            for ($k = 0; $k -lt $operand.Length; $k++) { $arr[$k] = $insMap[$operand[$k]] }
            $ni.Operand = $arr
            continue
        }
    }

    if ($origBody.HasExceptionHandlers) {
        foreach ($eh in $origBody.ExceptionHandlers) {
            $neh = [dnlib.DotNet.Emit.ExceptionHandler]::new($eh.HandlerType)
            $neh.TryStart = if ($eh.TryStart) { $insMap[$eh.TryStart] } else { $null }
            $neh.TryEnd = if ($eh.TryEnd) { $insMap[$eh.TryEnd] } else { $null }
            $neh.HandlerStart = if ($eh.HandlerStart) { $insMap[$eh.HandlerStart] } else { $null }
            $neh.HandlerEnd = if ($eh.HandlerEnd) { $insMap[$eh.HandlerEnd] } else { $null }
            $neh.FilterStart = if ($eh.FilterStart) { $insMap[$eh.FilterStart] } else { $null }
            if ($eh.CatchType) {
                $ct = $eh.CatchType.FullName
                if ($recTypeByFullName.ContainsKey($ct)) {
                    $neh.CatchType = $recTypeByFullName[$ct]
                } else {
                    $neh.CatchType = $importer.Import($eh.CatchType)
                }
            }
            $newBody.ExceptionHandlers.Add($neh)
        }
    }

    $mRec.Body = $newBody
    Write-Host "patched: copied original IL body into $Label"
    return $true
}

function Patch-LayerNormLayerBackward-SwapSpanLocals {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Void IntlThrdPerfSchd.LayerNormLayer::Backward(System.ReadOnlySpan`1<System.Single>,System.Single)'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $vars = $m.Body.Variables
    if ($vars.Count -lt 6) { throw "unexpected locals count in: $methodFullName (vars=$($vars.Count))" }

    $loc4 = $vars[4]
    $loc5 = $vars[5]
    if ($loc4.Type.FullName -ne 'System.ReadOnlySpan`1<System.Single>') { throw "unexpected local4 type: $($loc4.Type.FullName) in $methodFullName" }
    if ($loc5.Type.FullName -ne 'System.ReadOnlySpan`1<System.Single>') { throw "unexpected local5 type: $($loc5.Type.FullName) in $methodFullName" }

    $ins = $m.Body.Instructions
    $changed = $false

    # 1) gradOutput slice temp: stloc.* loc4; ldloca.* loc4; ldloc.3; call CopyTo(...)
    for ($i = 1; $i -lt ($ins.Count - 4); $i++) {
        $st = $ins[$i]
        $ldloca = $ins[$i + 1]
        $ldloc3 = $ins[$i + 2]
        $copyCall = $ins[$i + 3]

        $isStLoc4 = (($st.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc) -or ($st.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_S)) -and ($st.Operand -eq $loc4)
        $isLdLoca4 = ($ldloca.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Ldloca_S) -and ($ldloca.Operand -eq $loc4)
        $isLdLoc3 = ($ldloc3.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Ldloc_3)
        $isCopyTo = ($copyCall.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Call) -and ($copyCall.Operand -is [dnlib.DotNet.IMethod]) -and ($copyCall.Operand.FullName.Contains('System.ReadOnlySpan`1<System.Single>::CopyTo'))

        if ($isStLoc4 -and $isLdLoca4 -and $isLdLoc3 -and $isCopyTo) {
            $st.Operand = $loc5
            $ldloca.Operand = $loc5
            $changed = $true
            break
        }
    }

    # 2) xNorm temp: ldloca.s loc5; ldarg.0; ldfld _cachedXNorm; ...; call ReadOnlySpan::.ctor(...)
    for ($i = 0; $i -lt ($ins.Count - 6); $i++) {
        $ldloca = $ins[$i]
        if (($ldloca.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldloca_S) -or ($ldloca.Operand -ne $loc5)) { continue }

        # Expect next few ops to load _cachedXNorm then call the ReadOnlySpan ctor.
        $looksLikeCtor = $false
        for ($j = $i + 1; $j -lt [Math]::Min($i + 10, $ins.Count); $j++) {
            $cand = $ins[$j]
            if (($cand.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Call) -and ($cand.Operand -is [dnlib.DotNet.IMethod]) -and ($cand.Operand.FullName.Contains('System.ReadOnlySpan`1<System.Single>::.ctor'))) {
                $looksLikeCtor = $true
                break
            }
        }
        if (-not $looksLikeCtor) { continue }

        $ldloca.Operand = $loc4
        $changed = $true
        break
    }

    # 3) call ComputeLayerNormInputGrad: the xNorm local should be loc4, not loc5.
    for ($i = 0; $i -lt $ins.Count; $i++) {
        $cand = $ins[$i]
        if (($cand.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Call) -or (-not ($cand.Operand -is [dnlib.DotNet.IMethod]))) { continue }
        if ($cand.Operand.FullName -notlike "*SimdLibrary.VectorMathNew::ComputeLayerNormInputGrad*") { continue }

        for ($k = $i - 1; $k -ge [Math]::Max(0, $i - 10); $k--) {
            $prev = $ins[$k]
            if (($prev.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Ldloc_S) -and ($prev.Operand -eq $loc5)) {
                $prev.Operand = $loc4
                $changed = $true
                break
            }
        }
        break
    }

    if ($changed) {
        Write-Host "patched: swapped LayerNormLayer.Backward span locals ($methodFullName)"
        return $true
    }

    Write-Host "skip (no match / already patched): $methodFullName"
    return $false
}

function New-LdlocForStloc {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.Emit.Instruction] $StlocIns
    )

    $op = $StlocIns.OpCode
    if ($op -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_0) { return [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldloc_0) }
    if ($op -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_1) { return [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldloc_1) }
    if ($op -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_2) { return [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldloc_2) }
    if ($op -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_3) { return [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldloc_3) }

    if (($op -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_S) -and ($StlocIns.Operand -is [dnlib.DotNet.Emit.Local])) {
        return [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldloc_S, $StlocIns.Operand)
    }
    if (($op -eq [dnlib.DotNet.Emit.OpCodes]::Stloc) -and ($StlocIns.Operand -is [dnlib.DotNet.Emit.Local])) {
        return [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldloc, $StlocIns.Operand)
    }

    throw "unsupported stloc opcode for ldloc synthesis: $($op.Name)"
}

function Patch-MultiHeadAttentionSelfForward-ReorderSpanImplicit {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Void IntlThrdPerfSchd.MultiHeadAttention::SelfForward(System.ReadOnlySpan`1<System.Single>,System.Span`1<System.Single>)'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $ins = $m.Body.Instructions

    $dotIdx = -1
    for ($i = 0; $i -lt $ins.Count; $i++) {
        $cand = $ins[$i]
        if (($cand.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Call) -or (-not ($cand.Operand -is [dnlib.DotNet.IMethod]))) { continue }
        if (-not $cand.Operand.FullName.Contains('SimdLibrary.VectorMathNew::DotProduct(System.ReadOnlySpan`1<System.Single>,System.ReadOnlySpan`1<System.Single>)')) { continue }
        $dotIdx = $i
        break
    }

    if ($dotIdx -lt 4) {
        Write-Host "skip (unexpected IL shape): $methodFullName"
        return $false
    }

    $kImplicitIns = $ins[$dotIdx - 1]
    if (($kImplicitIns.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Call) -or (-not ($kImplicitIns.Operand -is [dnlib.DotNet.IMethod])) -or (-not $kImplicitIns.Operand.FullName.Contains('System.Span`1<System.Single>::op_Implicit'))) {
        Write-Host "skip (no trailing Span->ReadOnlySpan implicit): $methodFullName"
        return $false
    }

    $stlocIns = $ins[$dotIdx - 2]
    $dupIns = $ins[$dotIdx - 3]
    $kAsSpanIns = $ins[$dotIdx - 4]

    $isStLoc = ($stlocIns.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc) -or ($stlocIns.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_S) -or ($stlocIns.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_0) -or ($stlocIns.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_1) -or ($stlocIns.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_2) -or ($stlocIns.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Stloc_3)
    if (-not $isStLoc) {
        Write-Host "skip (unexpected stloc before DotProduct): $methodFullName"
        return $false
    }

    if ($dupIns.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Dup) {
        Write-Host "skip (already patched / no dup found): $methodFullName"
        return $false
    }

    if (($kAsSpanIns.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Call) -or (-not ($kAsSpanIns.Operand -is [dnlib.DotNet.IMethod])) -or (-not $kAsSpanIns.Operand.FullName.Contains('System.MemoryExtensions::AsSpan<System.Single>(System.Single[],System.Int32,System.Int32)'))) {
        Write-Host "skip (unexpected kProj AsSpan shape): $methodFullName"
        return $false
    }

    # Find the earlier qProj AsSpan immediately followed by op_Implicit. We want to delete that op_Implicit
    # and re-insert it after storing the kProj span, matching shipped IL.
    $kAsSpanIdx = $dotIdx - 4
    $qImplicitIns = $null
    for ($i = $kAsSpanIdx - 1; $i -ge 0; $i--) {
        $a = $ins[$i]
        if (($a.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Call) -or (-not ($a.Operand -is [dnlib.DotNet.IMethod])) -or (-not $a.Operand.FullName.Contains('System.MemoryExtensions::AsSpan<System.Single>(System.Single[],System.Int32,System.Int32)'))) { continue }

        if (($i + 1) -ge $ins.Count) { continue }
        $b = $ins[$i + 1]
        if (($b.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Call) -and ($b.Operand -is [dnlib.DotNet.IMethod]) -and $b.Operand.FullName.Contains('System.Span`1<System.Single>::op_Implicit')) {
            $qImplicitIns = $b
            break
        }
    }

    if (-not $qImplicitIns) {
        Write-Host "skip (already patched / no leading implicit found): $methodFullName"
        return $false
    }

    $opImplicit = $kImplicitIns.Operand

    # Remove the early implicit conversion on qProj span.
    $qImplicitIdx = $ins.IndexOf($qImplicitIns)
    if ($qImplicitIdx -ge 0) { $ins.RemoveAt($qImplicitIdx) }

    # Remove dup before stloc (we want: AsSpan; stloc; call op_Implicit; ldloc; call op_Implicit; DotProduct).
    $dupIdx2 = $ins.IndexOf($dupIns)
    if ($dupIdx2 -ge 0) { $ins.RemoveAt($dupIdx2) }

    $stIdx = $ins.IndexOf($stlocIns)
    if ($stIdx -lt 0) { throw "failed to re-find stloc after edits: $methodFullName" }

    $ins.Insert($stIdx + 1, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Call, $opImplicit))

    $kImplicitIdx2 = $ins.IndexOf($kImplicitIns)
    if ($kImplicitIdx2 -lt 0) { throw "failed to re-find kImplicit after edits: $methodFullName" }

    $ins.Insert($kImplicitIdx2, (New-LdlocForStloc -StlocIns $stlocIns))

    Write-Host "patched: reordered Span->ReadOnlySpan implicit conversions in $methodFullName"
    return $true
}

function Patch-TransformerSchedulerUpdateTATInternal-InsertNoopPops {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.Void IntlThrdPerfSchd.TransformerScheduler::UpdateTATInternal(System.Single,System.Single)'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $ins = $m.Body.Instructions
    $patched = $false

    for ($i = 0; $i -lt ($ins.Count - 10); $i++) {
        $ldloc1 = $ins[$i]
        $brtrue = $ins[$i + 1]
        $ldarg1 = $ins[$i + 2]
        $ldc1 = $ins[$i + 3]
        $blt = $ins[$i + 4]
        $ldloc2 = $ins[$i + 5]
        $pop = $ins[$i + 6]
        $next = $ins[$i + 7]
        $next2 = $ins[$i + 8]

        if ($pop.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Pop) { continue }
        if ($ldarg1.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldarg_1) { continue }
        if (($ldc1.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldc_R4) -or ([float]$ldc1.Operand -ne 1.0)) { continue }
        if (($blt.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Blt_S) -and ($blt.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Blt)) { continue }
        if (($brtrue.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Brtrue_S) -and ($brtrue.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Brtrue)) { continue }

        # This sequence should be: ldloc.s flag; brtrue.s <...>; ldarg.1; ldc.r4 1; blt.s <L>; ldloc.s flag; pop; ldarg.0; ldfld _decisionsInCurrentSecond
        if (($ldloc1.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldloc_S) -or (-not ($ldloc1.Operand -is [dnlib.DotNet.Emit.Local]))) { continue }
        if (($ldloc2.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldloc_S) -or ($ldloc2.Operand -ne $ldloc1.Operand)) { continue }
        if ($next.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldarg_0) { continue }
        if (($next2.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldfld) -or (-not ($next2.Operand -is [dnlib.DotNet.IField])) -or (-not $next2.Operand.FullName.Contains('IntlThrdPerfSchd.TransformerScheduler::_decisionsInCurrentSecond'))) { continue }

        $target = $blt.Operand
        if (-not ($target -is [dnlib.DotNet.Emit.Instruction])) { continue }

        # Idempotency: already patched if we see brtrue + (ldarg.1; ldc.r4 2; pop; pop) before the same ldarg.0.
        $already = $false
        if (($pop.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Brtrue_S) -or ($pop.OpCode -eq [dnlib.DotNet.Emit.OpCodes]::Brtrue)) {
            $already = $true
        }
        if ($already) {
            Write-Host "skip (already patched): $methodFullName"
            return $false
        }

        # Replace pop with: brtrue.s <L>; ldarg.1; ldc.r4 2; pop; pop
        $ins.RemoveAt($i + 6)
        $ins.Insert($i + 6, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Brtrue_S, $target))
        $ins.Insert($i + 7, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldarg_1))
        $ins.Insert($i + 8, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ldc_R4, [float]2.0))
        $ins.Insert($i + 9, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Pop))
        $ins.Insert($i + 10, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Pop))

        Write-Host "patched: inserted noop pops in $methodFullName"
        $patched = $true
        break
    }

    if ($patched) { return $true }
    Write-Host "skip (pattern not found): $methodFullName"
    return $false
}

function Patch-TransformerSchedulerGetAttentionHeadReport-ReplaceBeqWithCeq {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $methodFullName = 'System.String IntlThrdPerfSchd.TransformerScheduler::GetAttentionHeadReport(System.Int32)'
    $m = Find-MethodByFullName -Module $Module -MethodFullName $methodFullName
    if (-not $m) { throw "method not found: $methodFullName" }
    if (-not $m.HasBody) { throw "method has no body: $methodFullName" }

    $ins = $m.Body.Instructions

    for ($i = 2; $i -lt ($ins.Count - 4); $i++) {
        $cand = $ins[$i]
        if (($cand.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Beq_S) -and ($cand.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Beq)) { continue }
        if (-not ($cand.Operand -is [dnlib.DotNet.Emit.Instruction])) { continue }

        $prev = $ins[$i - 1]
        $next = $ins[$i + 1]
        if ($prev.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldelem_I4) { continue }
        if (($next.OpCode -ne [dnlib.DotNet.Emit.OpCodes]::Ldstr) -or (-not ($next.Operand -is [string])) -or (-not $next.Operand.StartsWith(' {0:F4}'))) { continue }

        $target = $cand.Operand

        # Idempotency: if we've already patched, the beq will be gone.
        $ins.RemoveAt($i)
        $ins.Insert($i, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Ceq))
        $ins.Insert($i + 1, [dnlib.DotNet.Emit.Instruction]::Create([dnlib.DotNet.Emit.OpCodes]::Brtrue_S, $target))

        Write-Host "patched: replaced beq with ceq+brtrue in $methodFullName"
        return $true
    }

    Write-Host "skip (pattern not found / already patched): $methodFullName"
    return $false
}

function Patch-OnlineLearningManagerGetStats-RenameLambdaOrdinal {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $typeFullName = 'IntlThrdPerfSchd.OnlineLearningManager/<>c'
    $t = $Module.GetTypes() | Where-Object { $_.FullName -eq $typeFullName } | Select-Object -First 1
    if (-not $t) {
        Write-Host "skip (type not found): $typeFullName"
        return $false
    }

    $fieldOld = $t.Fields | Where-Object { $_.Name.String -eq '<>9__142_0' } | Select-Object -First 1
    $methodOld = $t.Methods | Where-Object { $_.Name.String -eq '<GetStats>b__142_0' } | Select-Object -First 1
    $fieldNew = $t.Fields | Where-Object { $_.Name.String -eq '<>9__141_0' } | Select-Object -First 1
    $methodNew = $t.Methods | Where-Object { $_.Name.String -eq '<GetStats>b__141_0' } | Select-Object -First 1

    if ($fieldNew -and $methodNew) {
        Write-Host "skip (already patched): $typeFullName GetStats lambda ordinal"
        return $false
    }

    if (-not $fieldOld -or -not $methodOld) {
        Write-Host "skip (expected GetStats lambda not found): $typeFullName"
        return $false
    }

    # Avoid accidental collisions if something unexpected is present.
    if ($fieldNew -or $methodNew) {
        throw "unexpected partial rename state in $typeFullName"
    }

    $fieldOld.Name = '<>9__141_0'
    $methodOld.Name = '<GetStats>b__141_0'
    Write-Host "patched: renamed OnlineLearningManager.GetStats cached lambda from 142->141"
    return $true
}

function Patch-OnlineLearningManagerUpdateModel-RenameDisplayClassOrdinal {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $oldFullName = 'IntlThrdPerfSchd.OnlineLearningManager/<>c__DisplayClass153_0'
    $newName = '<>c__DisplayClass152_0'

    $tOld = $Module.GetTypes() | Where-Object { $_.FullName -eq $oldFullName } | Select-Object -First 1
    if ($tOld) {
        $decl = $tOld.DeclaringType
        if (-not $decl) { throw "unexpected: displayclass has no declaring type: $oldFullName" }

        $conflict = $decl.NestedTypes | Where-Object { $_.Name.String -eq $newName } | Select-Object -First 1
        if ($conflict) { throw "displayclass rename conflict: $newName already exists under $($decl.FullName)" }

        $tOld.Name = $newName
        Write-Host "patched: renamed UpdateModel display class from 153->152"
        return $true
    }

    # Idempotency: if the new name already exists, treat as already patched.
    $tNew = $Module.GetTypes() | Where-Object { $_.FullName -eq ('IntlThrdPerfSchd.OnlineLearningManager/' + $newName) } | Select-Object -First 1
    if ($tNew) {
        Write-Host "skip (already patched): OnlineLearningManager display class ordinal"
        return $false
    }

    Write-Host "skip (display class not found): $oldFullName"
    return $false
}

function Patch-OnlineLearningManagerUpdateModel-RenameLambdaOrdinal {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $typeFullName = 'IntlThrdPerfSchd.OnlineLearningManager/<>c'
    $t = $Module.GetTypes() | Where-Object { $_.FullName -eq $typeFullName } | Select-Object -First 1
    if (-not $t) {
        Write-Host "skip (type not found): $typeFullName"
        return $false
    }

    $fieldOld = $t.Fields | Where-Object { $_.Name.String -eq '<>9__153_0' } | Select-Object -First 1
    $methodOld = $t.Methods | Where-Object { $_.Name.String -eq '<UpdateModel>b__153_0' } | Select-Object -First 1
    $fieldNew = $t.Fields | Where-Object { $_.Name.String -eq '<>9__152_0' } | Select-Object -First 1
    $methodNew = $t.Methods | Where-Object { $_.Name.String -eq '<UpdateModel>b__152_0' } | Select-Object -First 1

    if ($fieldNew -and $methodNew) {
        Write-Host "skip (already patched): $typeFullName UpdateModel lambda ordinal"
        return $false
    }

    if (-not $fieldOld -or -not $methodOld) {
        Write-Host "skip (expected UpdateModel lambda not found): $typeFullName"
        return $false
    }

    if ($fieldNew -or $methodNew) {
        throw "unexpected partial rename state in $typeFullName (UpdateModel)"
    }

    $fieldOld.Name = '<>9__152_0'
    $methodOld.Name = '<UpdateModel>b__152_0'
    Write-Host "patched: renamed OnlineLearningManager.UpdateModel cached lambda from 153->152"
    return $true
}

function Patch-TrackerRecalculateMaxValue-RenameLambdaOrdinal {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $typeFullName = 'IntlThrdPerfSchd.Tracker/<>c'
    $t = $Module.GetTypes() | Where-Object { $_.FullName -eq $typeFullName } | Select-Object -First 1
    if (-not $t) {
        Write-Host "skip (type not found): $typeFullName"
        return $false
    }

    $fieldOld = $t.Fields | Where-Object { $_.Name.String -eq '<>9__15_0' } | Select-Object -First 1
    $methodOld = $t.Methods | Where-Object { $_.Name.String -eq '<RecalculateMaxValue>b__15_0' } | Select-Object -First 1
    $fieldNew = $t.Fields | Where-Object { $_.Name.String -eq '<>9__11_0' } | Select-Object -First 1
    $methodNew = $t.Methods | Where-Object { $_.Name.String -eq '<RecalculateMaxValue>b__11_0' } | Select-Object -First 1

    if ($fieldNew -and $methodNew) {
        Write-Host "skip (already patched): $typeFullName RecalculateMaxValue lambda ordinal"
        return $false
    }

    if (-not $fieldOld -or -not $methodOld) {
        Write-Host "skip (expected RecalculateMaxValue lambda not found): $typeFullName"
        return $false
    }

    if ($fieldNew -or $methodNew) {
        throw "unexpected partial rename state in $typeFullName (RecalculateMaxValue)"
    }

    $fieldOld.Name = '<>9__11_0'
    $methodOld.Name = '<RecalculateMaxValue>b__11_0'
    Write-Host "patched: renamed Tracker.RecalculateMaxValue cached lambda from 15->11"
    return $true
}

function Patch-Tracker4latRecalculateMaxValue-RenameLambdaOrdinal {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $typeFullName = 'IntlThrdPerfSchd.Tracker4lat/<>c'
    $t = $Module.GetTypes() | Where-Object { $_.FullName -eq $typeFullName } | Select-Object -First 1
    if (-not $t) {
        Write-Host "skip (type not found): $typeFullName"
        return $false
    }

    $fieldOld = $t.Fields | Where-Object { $_.Name.String -eq '<>9__15_0' } | Select-Object -First 1
    $methodOld = $t.Methods | Where-Object { $_.Name.String -eq '<RecalculateMaxValue>b__15_0' } | Select-Object -First 1
    $fieldNew = $t.Fields | Where-Object { $_.Name.String -eq '<>9__11_0' } | Select-Object -First 1
    $methodNew = $t.Methods | Where-Object { $_.Name.String -eq '<RecalculateMaxValue>b__11_0' } | Select-Object -First 1

    if ($fieldNew -and $methodNew) {
        Write-Host "skip (already patched): $typeFullName RecalculateMaxValue lambda ordinal"
        return $false
    }

    if (-not $fieldOld -or -not $methodOld) {
        Write-Host "skip (expected RecalculateMaxValue lambda not found): $typeFullName"
        return $false
    }

    if ($fieldNew -or $methodNew) {
        throw "unexpected partial rename state in $typeFullName (RecalculateMaxValue)"
    }

    $fieldOld.Name = '<>9__11_0'
    $methodOld.Name = '<RecalculateMaxValue>b__11_0'
    Write-Host "patched: renamed Tracker4lat.RecalculateMaxValue cached lambda from 15->11"
    return $true
}

function Rename-CachedLambdaOrdinal {
    param(
        [Parameter(Mandatory = $true)] $TypeDef,
        [Parameter(Mandatory = $true)] [string] $FieldOldName,
        [Parameter(Mandatory = $true)] [string] $MethodOldName,
        [Parameter(Mandatory = $true)] [string] $FieldNewName,
        [Parameter(Mandatory = $true)] [string] $MethodNewName,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $fieldOld = $TypeDef.Fields | Where-Object { $_.Name.String -eq $FieldOldName } | Select-Object -First 1
    $methodOld = $TypeDef.Methods | Where-Object { $_.Name.String -eq $MethodOldName } | Select-Object -First 1
    $fieldNew = $TypeDef.Fields | Where-Object { $_.Name.String -eq $FieldNewName } | Select-Object -First 1
    $methodNew = $TypeDef.Methods | Where-Object { $_.Name.String -eq $MethodNewName } | Select-Object -First 1

    if ($fieldNew -and $methodNew) {
        Write-Host "skip (already patched): $Label"
        return $false
    }

    if (-not $fieldOld -or -not $methodOld) {
        Write-Host "skip (expected lambda not found): $Label"
        return $false
    }

    if ($fieldNew -or $methodNew) {
        throw "unexpected partial rename state: $Label"
    }

    $fieldOld.Name = $FieldNewName
    $methodOld.Name = $MethodNewName
    Write-Host "patched: $Label"
    return $true
}

function Patch-ThreadLoadManager4b-RenameLambdaOrdinals {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $typeFullName = 'IntlThrdPerfSchd.Service1/ThreadLoadManager4b/<>c'
    $t = $Module.GetTypes() | Where-Object { $_.FullName -eq $typeFullName } | Select-Object -First 1
    if (-not $t) {
        Write-Host "skip (type not found): $typeFullName"
        return $false
    }

    $changed = $false
    $changed = (Rename-CachedLambdaOrdinal -TypeDef $t -FieldOldName '<>9__7_0' -MethodOldName '<TakeTopN>b__7_0' -FieldNewName '<>9__5_0' -MethodNewName '<TakeTopN>b__5_0' -Label "$typeFullName TakeTopN lambda ordinal 7->5") -or $changed
    $changed = (Rename-CachedLambdaOrdinal -TypeDef $t -FieldOldName '<>9__8_0' -MethodOldName '<TakeBottomN>b__8_0' -FieldNewName '<>9__6_0' -MethodNewName '<TakeBottomN>b__6_0' -Label "$typeFullName TakeBottomN lambda ordinal 8->6") -or $changed
    return $changed
}

function Patch-ThreadLoadManager4l-RenameLambdaOrdinals {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $typeFullName = 'IntlThrdPerfSchd.Service1/ThreadLoadManager4l/<>c'
    $t = $Module.GetTypes() | Where-Object { $_.FullName -eq $typeFullName } | Select-Object -First 1
    if (-not $t) {
        Write-Host "skip (type not found): $typeFullName"
        return $false
    }

    $changed = $false
    $changed = (Rename-CachedLambdaOrdinal -TypeDef $t -FieldOldName '<>9__7_0' -MethodOldName '<TakeTopN>b__7_0' -FieldNewName '<>9__5_0' -MethodNewName '<TakeTopN>b__5_0' -Label "$typeFullName TakeTopN lambda ordinal 7->5") -or $changed
    $changed = (Rename-CachedLambdaOrdinal -TypeDef $t -FieldOldName '<>9__8_0' -MethodOldName '<TakeBottomN>b__8_0' -FieldNewName '<>9__6_0' -MethodNewName '<TakeBottomN>b__6_0' -Label "$typeFullName TakeBottomN lambda ordinal 8->6") -or $changed
    return $changed
}

Write-Host "patching assembly: $AssemblyPath"
$modBytes = [System.IO.File]::ReadAllBytes($AssemblyPath)
$mod = [dnlib.DotNet.ModuleDefMD]::Load($modBytes)

$changed = $false
$changed = (Patch-TrainOnline-InsertLdargPop -Module $mod) -or $changed
$changed = (Patch-RealtimeSchedulerDispose-InsertLdargPop -Module $mod) -or $changed
$changed = (Patch-ThreadClassifierCleanExpiredData-RemoveCachedPredicateDelegate -Module $mod) -or $changed
$changed = (Patch-ThreadClassifierCleanExpiredData-NormalizeLoc3Opcodes -Module $mod) -or $changed
$changed = (Patch-SchedulerDatasetComputeNorm-CopyOriginalIL -Module $mod) -or $changed
$changed = (Patch-Service1ProcessCompare-AddUnusedInt32Local -Module $mod) -or $changed
$changed = (Patch-CrossAttentionSchedulerSchedule-InsertDupPop -Module $mod) -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'System.Int32 IntlThrdPerfSchd.Service1::ProcessCompare1(IntlThrdPerfSchd.Service1/ThreadInfoSimp,System.Int64&)' -Label 'Service1.ProcessCompare1') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'System.Int32 IntlThrdPerfSchd.Service1::ProcessCompare2(IntlThrdPerfSchd.Service1/ThreadInfoSimp,System.Int64&)' -Label 'Service1.ProcessCompare2') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'System.Int32 IntlThrdPerfSchd.Service1::ProcessCompare3(IntlThrdPerfSchd.Service1/ThreadInfoSimp,System.Int64&)' -Label 'Service1.ProcessCompare3') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'IntlThrdPerfSchd.Service1/GroupInfo IntlThrdPerfSchd.Service1::UpdateGroupInfo(System.Int32,IntlThrdPerfSchd.Service1/GroupInfo&,IntlThrdPerfSchd.Service1/GroupInfo)' -Label 'Service1.UpdateGroupInfo') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'IntlThrdPerfSchd.Service1/ThreadInfoSimp IntlThrdPerfSchd.Service1::UpdateThreadInfoSimp_ascend(System.Int32,IntlThrdPerfSchd.Service1/ThreadInfoSimp&,IntlThrdPerfSchd.Service1/ThreadInfoSimp)' -Label 'Service1.UpdateThreadInfoSimp_ascend') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'IntlThrdPerfSchd.Service1/ThreadInfoSimp IntlThrdPerfSchd.Service1::UpdateThreadInfoSimp(System.Int32,IntlThrdPerfSchd.Service1/ThreadInfoSimp&,IntlThrdPerfSchd.Service1/ThreadInfoSimp)' -Label 'Service1.UpdateThreadInfoSimp') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'IntlThrdPerfSchd.Service1/ThreadInfoSimp IntlThrdPerfSchd.Service1::UpdateThreadInfoSimp1(System.Int32,IntlThrdPerfSchd.Service1/ThreadInfoSimp&,IntlThrdPerfSchd.Service1/ThreadInfoSimp)' -Label 'Service1.UpdateThreadInfoSimp1') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'IntlThrdPerfSchd.Service1/ThreadInfoSimp IntlThrdPerfSchd.Service1::UpdateThreadInfoSimp2(System.Int32,IntlThrdPerfSchd.Service1/ThreadInfoSimp&,IntlThrdPerfSchd.Service1/ThreadInfoSimp)' -Label 'Service1.UpdateThreadInfoSimp2') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'IntlThrdPerfSchd.Service1/ThreadInfoSimp IntlThrdPerfSchd.Service1::UpdateThreadInfoSimp3(System.Int32,IntlThrdPerfSchd.Service1/ThreadInfoSimp&,IntlThrdPerfSchd.Service1/ThreadInfoSimp)' -Label 'Service1.UpdateThreadInfoSimp3') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'System.Int32 IntlThrdPerfSchd.Service1::Intval2Limit(System.Int32,System.Int64,System.Int64,System.Int64,System.Int64&,System.Int64&,System.Int32,System.Int64,System.Int64&,System.Int64&,System.Int64,System.Int64&,System.UInt32,System.Int64&,System.Int64&,System.Int64&,System.Int64&,System.Int64&,System.Int64&,System.Int64&)' -Label 'Service1.Intval2Limit') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'System.Int32 IntlThrdPerfSchd.Service1::UpdateNode(System.Int32,IntlThrdPerfSchd.Service1/Node&,System.Int32,System.Int32)' -Label 'Service1.UpdateNode') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'System.Int32 IntlThrdPerfSchd.Service1::UpdateNode1(System.Int32,IntlThrdPerfSchd.Service1/Node1&,System.Int32,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.UInt32,System.Int64)' -Label 'Service1.UpdateNode1') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'System.Int32 IntlThrdPerfSchd.Service1::UpdateNodeP(System.Int32,IntlThrdPerfSchd.Service1/NodeP&,System.Int32,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,IntlThrdPerfSchd.Service1/Node2,IntlThrdPerfSchd.Service1/Node2)' -Label 'Service1.UpdateNodeP') -or $changed
$changed = (Patch-CopyMethodILFromOriginal -Module $mod -MethodFullName 'System.Void IntlThrdPerfSchd.Service1::OnStart(System.String[])' -Label 'Service1.OnStart') -or $changed
$changed = (Patch-LayerNormLayerBackward-SwapSpanLocals -Module $mod) -or $changed
$changed = (Patch-MultiHeadAttentionSelfForward-ReorderSpanImplicit -Module $mod) -or $changed
$changed = (Patch-TransformerSchedulerUpdateTATInternal-InsertNoopPops -Module $mod) -or $changed
$changed = (Patch-TransformerSchedulerGetAttentionHeadReport-ReplaceBeqWithCeq -Module $mod) -or $changed
$changed = (Patch-OnlineLearningManagerGetStats-RenameLambdaOrdinal -Module $mod) -or $changed
$changed = (Patch-OnlineLearningManagerUpdateModel-RenameDisplayClassOrdinal -Module $mod) -or $changed
$changed = (Patch-OnlineLearningManagerUpdateModel-RenameLambdaOrdinal -Module $mod) -or $changed
$changed = (Patch-ThreadLoadManager4b-RenameLambdaOrdinals -Module $mod) -or $changed
$changed = (Patch-ThreadLoadManager4l-RenameLambdaOrdinals -Module $mod) -or $changed
$changed = (Patch-TrackerRecalculateMaxValue-RenameLambdaOrdinal -Module $mod) -or $changed
$changed = (Patch-Tracker4latRecalculateMaxValue-RenameLambdaOrdinal -Module $mod) -or $changed

if ($changed) {
    $mod.Write($AssemblyPath)
    Write-Host "write ok: $AssemblyPath"
} else {
    Write-Host "no changes needed"
}
