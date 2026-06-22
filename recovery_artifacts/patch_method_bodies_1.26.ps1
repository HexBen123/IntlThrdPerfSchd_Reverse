param(
    [string] $OriginalExe = "$PSScriptRoot\original\IntlThrdSchd.exe",
    [string] $RecoveredExe = "$PSScriptRoot\..\IntlThrdPerfSchd\bin\Release\net48\IntlThrdSchd.exe",
    [string] $OutExe = "$PSScriptRoot\patched\IntlThrdSchd.exe"
)

$ErrorActionPreference = "Stop"

$dnlib = Resolve-Path (Join-Path $PSScriptRoot "tools\dnlib.dll")
Add-Type -Path $dnlib.Path

$methodsToPatch = @(
    'System.Int32 IntlThrdPerfSchd.Service1::Intval2Limit(System.Int32,System.Int64,System.Int64,System.Int64,System.Int64&,System.Int64&,System.Int32,System.Int64,System.Int64&,System.Int64&,System.Int64,System.Int64&,System.UInt32,System.Int64&,System.Int64&,System.Int64&,System.Int64&,System.Int64&,System.Int64&,System.Int64&)',
    'System.Int32 IntlThrdPerfSchd.Service1::UpdateNode(System.Int32,IntlThrdPerfSchd.Service1/Node&,System.Int32,System.Int32)',
    'System.Int32 IntlThrdPerfSchd.Service1::UpdateNode1(System.Int32,IntlThrdPerfSchd.Service1/Node1&,System.Int32,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.UInt32,System.Int64)',
    'System.Int32 IntlThrdPerfSchd.Service1::UpdateNodeP(System.Int32,IntlThrdPerfSchd.Service1/NodeP&,System.Int32,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64,System.Int64)',
    'System.Void IntlThrdPerfSchd.Service1::OnStart(System.String[])',
    'System.Void IntlThrdPerfSchd.Service1/<>c__DisplayClass484_0::<OnStart>b__2(Microsoft.Diagnostics.Tracing.Parsers.Kernel.CSwitchTraceData)',
    'System.Void IntlThrdPerfSchd.Service1::OnTimedEvent(System.Object,System.Timers.ElapsedEventArgs)',
    'System.Void OpenLibSys.Ols::.ctor()'
)

function Find-MethodByFullName {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module,
        [Parameter(Mandatory = $true)] [string] $FullName
    )

    foreach ($type in $Module.GetTypes()) {
        foreach ($method in $type.Methods) {
            if ($method.FullName -eq $FullName) {
                return $method
            }
        }
    }
    return $null
}

function New-ModuleMemberMaps {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $Module
    )

    $maps = @{
        Types = @{}
        Fields = @{}
        Methods = @{}
    }

    foreach ($type in $Module.GetTypes()) {
        $maps.Types[$type.FullName] = $type
        foreach ($field in $type.Fields) {
            $maps.Fields[$field.FullName] = $field
        }
        foreach ($method in $type.Methods) {
            $maps.Methods[$method.FullName] = $method
        }
    }

    return $maps
}

function Import-TypeSig {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.TypeSig] $TypeSig,
        [Parameter(Mandatory = $true)] [dnlib.DotNet.Importer] $Importer,
        [Parameter(Mandatory = $true)] [hashtable] $TypeMap
    )

    if ($TypeMap.ContainsKey($TypeSig.FullName)) {
        $targetType = $TypeMap[$TypeSig.FullName]
        if ($targetType.IsValueType) {
            return [dnlib.DotNet.ValueTypeSig]::new($targetType)
        }
        return [dnlib.DotNet.ClassSig]::new($targetType)
    }

    return $Importer.Import($TypeSig)
}

function Import-Operand {
    param(
        [Parameter(Mandatory = $false)] $Operand,
        [Parameter(Mandatory = $true)] [dnlib.DotNet.Importer] $Importer,
        [Parameter(Mandatory = $true)] [dnlib.DotNet.MethodDef] $TargetMethod,
        [Parameter(Mandatory = $true)] [hashtable] $InstructionMap,
        [Parameter(Mandatory = $true)] [hashtable] $LocalMap,
        [Parameter(Mandatory = $true)] [hashtable] $TypeMap,
        [Parameter(Mandatory = $true)] [hashtable] $FieldMap,
        [Parameter(Mandatory = $true)] [hashtable] $MethodMap
    )

    if ($null -eq $Operand) { return $null }

    if ($Operand -is [dnlib.DotNet.Emit.Instruction]) {
        return $InstructionMap[$Operand]
    }

    if ($Operand -is [dnlib.DotNet.Emit.Instruction[]]) {
        $targets = New-Object 'dnlib.DotNet.Emit.Instruction[]' $Operand.Length
        for ($i = 0; $i -lt $Operand.Length; $i++) {
            $targets[$i] = $InstructionMap[$Operand[$i]]
        }
        return $targets
    }

    if ($Operand -is [dnlib.DotNet.Emit.Local]) {
        return $LocalMap[$Operand]
    }

    if ($Operand -is [dnlib.DotNet.Parameter]) {
        return $TargetMethod.Parameters[$Operand.Index]
    }

    if ($Operand -is [dnlib.DotNet.MemberRef]) {
        if ($Operand.IsFieldRef) {
            return $Importer.Import([dnlib.DotNet.IField]$Operand)
        }
        if ($Operand.IsMethodRef) {
            return $Importer.Import([dnlib.DotNet.IMethod]$Operand)
        }
    }

    if ($Operand -is [dnlib.DotNet.IField]) {
        if (($Operand -is [dnlib.DotNet.FieldDef]) -and $FieldMap.ContainsKey($Operand.FullName)) {
            return $FieldMap[$Operand.FullName]
        }
        return $Importer.Import($Operand)
    }

    if ($Operand -is [dnlib.DotNet.IMethod]) {
        if (($Operand -is [dnlib.DotNet.MethodDef]) -and $MethodMap.ContainsKey($Operand.FullName)) {
            return $MethodMap[$Operand.FullName]
        }
        return $Importer.Import($Operand)
    }

    if ($Operand -is [dnlib.DotNet.ITypeDefOrRef]) {
        if (($Operand -is [dnlib.DotNet.TypeDef]) -and $TypeMap.ContainsKey($Operand.FullName)) {
            return $TypeMap[$Operand.FullName]
        }
        return $Importer.Import($Operand)
    }

    if ($Operand -is [dnlib.DotNet.TypeSig]) {
        return Import-TypeSig -TypeSig $Operand -Importer $Importer -TypeMap $TypeMap
    }

    return $Operand
}

function Clone-MethodBody {
    param(
        [Parameter(Mandatory = $true)] [dnlib.DotNet.MethodDef] $SourceMethod,
        [Parameter(Mandatory = $true)] [dnlib.DotNet.MethodDef] $TargetMethod,
        [Parameter(Mandatory = $true)] [dnlib.DotNet.ModuleDefMD] $TargetModule
    )

    if (-not $SourceMethod.HasBody) {
        throw "Source method has no body: $($SourceMethod.FullName)"
    }

    $importer = [dnlib.DotNet.Importer]::new($TargetModule)
    $memberMaps = New-ModuleMemberMaps -Module $TargetModule
    $typeMap = $memberMaps.Types
    $fieldMap = $memberMaps.Fields
    $methodMap = $memberMaps.Methods
    $sourceBody = $SourceMethod.Body
    $newBody = [dnlib.DotNet.Emit.CilBody]::new()
    $newBody.InitLocals = $sourceBody.InitLocals
    $newBody.MaxStack = $sourceBody.MaxStack

    $localMap = @{}
    foreach ($sourceLocal in $sourceBody.Variables) {
        $newLocal = [dnlib.DotNet.Emit.Local]::new((Import-TypeSig -TypeSig $sourceLocal.Type -Importer $importer -TypeMap $typeMap))
        [void]$newBody.Variables.Add($newLocal)
        $localMap[$sourceLocal] = $newLocal
    }

    $instructionMap = @{}
    foreach ($sourceInstruction in $sourceBody.Instructions) {
        $operand = $sourceInstruction.Operand
        $newInstruction = $null

        if ($null -eq $operand) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode)
        } elseif ($operand -is [dnlib.DotNet.Emit.Instruction]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, [dnlib.DotNet.Emit.Instruction]$null)
        } elseif ($operand -is [dnlib.DotNet.Emit.Instruction[]]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, [dnlib.DotNet.Emit.Instruction[]]$null)
        } elseif ($operand -is [dnlib.DotNet.Emit.Local]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, $localMap[$operand])
        } elseif ($operand -is [dnlib.DotNet.Parameter]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, $TargetMethod.Parameters[$operand.Index])
        } elseif ($operand -is [dnlib.DotNet.MemberRef]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, (Import-Operand -Operand $operand -Importer $importer -TargetMethod $TargetMethod -InstructionMap $instructionMap -LocalMap $localMap -TypeMap $typeMap -FieldMap $fieldMap -MethodMap $methodMap))
        } elseif ($operand -is [dnlib.DotNet.IField]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, (Import-Operand -Operand $operand -Importer $importer -TargetMethod $TargetMethod -InstructionMap $instructionMap -LocalMap $localMap -TypeMap $typeMap -FieldMap $fieldMap -MethodMap $methodMap))
        } elseif ($operand -is [dnlib.DotNet.IMethod]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, (Import-Operand -Operand $operand -Importer $importer -TargetMethod $TargetMethod -InstructionMap $instructionMap -LocalMap $localMap -TypeMap $typeMap -FieldMap $fieldMap -MethodMap $methodMap))
        } elseif ($operand -is [dnlib.DotNet.ITypeDefOrRef]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, (Import-Operand -Operand $operand -Importer $importer -TargetMethod $TargetMethod -InstructionMap $instructionMap -LocalMap $localMap -TypeMap $typeMap -FieldMap $fieldMap -MethodMap $methodMap))
        } elseif ($operand -is [dnlib.DotNet.TypeSig]) {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, (Import-Operand -Operand $operand -Importer $importer -TargetMethod $TargetMethod -InstructionMap $instructionMap -LocalMap $localMap -TypeMap $typeMap -FieldMap $fieldMap -MethodMap $methodMap))
        } else {
            $newInstruction = [dnlib.DotNet.Emit.Instruction]::Create($sourceInstruction.OpCode, $operand)
        }

        $newBody.Instructions.Add($newInstruction)
        $instructionMap[$sourceInstruction] = $newInstruction
    }

    for ($i = 0; $i -lt $sourceBody.Instructions.Count; $i++) {
        $sourceInstruction = $sourceBody.Instructions[$i]
        $newInstruction = $newBody.Instructions[$i]
        $newInstruction.Operand = Import-Operand `
            -Operand $sourceInstruction.Operand `
            -Importer $importer `
            -TargetMethod $TargetMethod `
            -InstructionMap $instructionMap `
            -LocalMap $localMap `
            -TypeMap $typeMap `
            -FieldMap $fieldMap `
            -MethodMap $methodMap
    }

    foreach ($sourceHandler in $sourceBody.ExceptionHandlers) {
        $newHandler = [dnlib.DotNet.Emit.ExceptionHandler]::new($sourceHandler.HandlerType)
        if ($null -ne $sourceHandler.TryStart) { $newHandler.TryStart = $instructionMap[$sourceHandler.TryStart] }
        if ($null -ne $sourceHandler.TryEnd) { $newHandler.TryEnd = $instructionMap[$sourceHandler.TryEnd] }
        if ($null -ne $sourceHandler.FilterStart) { $newHandler.FilterStart = $instructionMap[$sourceHandler.FilterStart] }
        if ($null -ne $sourceHandler.HandlerStart) { $newHandler.HandlerStart = $instructionMap[$sourceHandler.HandlerStart] }
        if ($null -ne $sourceHandler.HandlerEnd) { $newHandler.HandlerEnd = $instructionMap[$sourceHandler.HandlerEnd] }
        if ($null -ne $sourceHandler.CatchType) {
            if ($typeMap.ContainsKey($sourceHandler.CatchType.FullName)) {
                $newHandler.CatchType = $typeMap[$sourceHandler.CatchType.FullName]
            } else {
                $newHandler.CatchType = $importer.Import($sourceHandler.CatchType)
            }
        }
        $newBody.ExceptionHandlers.Add($newHandler)
    }

    $TargetMethod.Body = $newBody
}

if (-not (Test-Path -LiteralPath $OriginalExe)) { throw "Original EXE not found: $OriginalExe" }
if (-not (Test-Path -LiteralPath $RecoveredExe)) { throw "Recovered EXE not found: $RecoveredExe" }

$originalModule = [dnlib.DotNet.ModuleDefMD]::Load([IO.Path]::GetFullPath($OriginalExe))
$recoveredModule = [dnlib.DotNet.ModuleDefMD]::Load([IO.Path]::GetFullPath($RecoveredExe))

foreach ($methodFullName in $methodsToPatch) {
    $sourceMethod = Find-MethodByFullName -Module $originalModule -FullName $methodFullName
    $targetMethod = Find-MethodByFullName -Module $recoveredModule -FullName $methodFullName
    if ($null -eq $sourceMethod) { throw "Source method not found: $methodFullName" }
    if ($null -eq $targetMethod) { throw "Target method not found: $methodFullName" }

    Clone-MethodBody -SourceMethod $sourceMethod -TargetMethod $targetMethod -TargetModule $recoveredModule
    Write-Host "patched body: $methodFullName"
}

$outPathFull = [IO.Path]::GetFullPath($OutExe)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outPathFull)) | Out-Null
$recoveredModule.Write($outPathFull)
Write-Host "wrote patched assembly: $outPathFull"
