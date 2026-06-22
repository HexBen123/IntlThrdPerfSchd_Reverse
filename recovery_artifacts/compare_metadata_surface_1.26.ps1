param(
    [string]$OriginalExe = "$PSScriptRoot\original\IntlThrdSchd.exe",
    [string]$RecoveredExe = "$PSScriptRoot\patched\IntlThrdSchd.exe",
    [int]$MaxDiffLines = 300,
    [string]$OutPath = "$PSScriptRoot\patched\metadata_surface_diff_1.26_patched.txt"
)

$ErrorActionPreference = "Stop"

$dnlib = Resolve-Path "$PSScriptRoot\tools\dnlib.dll"
Add-Type -Path $dnlib.Path

function Hash-Bytes([byte[]]$Bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    return [BitConverter]::ToString($sha.ComputeHash($Bytes)).Replace("-", "")
}

function Safe-FullName($Value) {
    if ($null -eq $Value) { return "" }
    if ($Value.PSObject.Properties.Name -contains "FullName") { return $Value.FullName }
    return $Value.ToString()
}

function Format-CAValue($Arg) {
    $typeName = Safe-FullName $Arg.Type
    $value = $Arg.Value
    if ($null -eq $value) {
        return "$typeName=<null>"
    }
    if ($value -is [System.Array]) {
        return "$typeName=[" + (($value | ForEach-Object { $_.ToString() }) -join ",") + "]"
    }
    return "$typeName=$value"
}

function Format-CustomAttribute($Owner, $CustomAttribute) {
    $ctorArgs = ($CustomAttribute.ConstructorArguments | ForEach-Object { Format-CAValue $_ }) -join ","
    $namedArgs = ($CustomAttribute.NamedArguments | ForEach-Object {
        "$($_.Name)=" + (Format-CAValue $_.Argument)
    }) -join ","
    return "customattr|$Owner|type:$($CustomAttribute.AttributeType.FullName)|ctor:$($CustomAttribute.Constructor.FullName)|args:$ctorArgs|named:$namedArgs"
}

function Add-CustomAttributes($Lines, $Owner, $Provider) {
    if ($null -eq $Provider) { return }
    foreach ($ca in $Provider.CustomAttributes) {
        [void]$Lines.Add((Format-CustomAttribute -Owner $Owner -CustomAttribute $ca))
    }
}

function Get-EmbeddedResourceInfo($Resource) {
    if (-not ($Resource -is [dnlib.DotNet.EmbeddedResource])) {
        return "resource|$($Resource.ResourceType)|$($Resource.Name)"
    }

    $stream = $Resource.CreateReader().AsStream()
    $ms = New-Object IO.MemoryStream
    try {
        $stream.CopyTo($ms)
        $bytes = $ms.ToArray()
        return "resource|$($Resource.ResourceType)|$($Resource.Name)|len:$($bytes.Length)|sha256:$(Hash-Bytes $bytes)"
    } finally {
        $stream.Dispose()
        $ms.Dispose()
    }
}

function Get-MetadataSurface {
    param(
        [Parameter(Mandatory = $true)] [string] $AssemblyPath
    )

    $module = [dnlib.DotNet.ModuleDefMD]::Load([IO.Path]::GetFullPath($AssemblyPath))
    $lines = New-Object "System.Collections.Generic.List[string]"

    [void]$lines.Add("assembly|$($module.Assembly.FullName)")
    [void]$lines.Add("module|kind:$($module.Kind)|runtime:$($module.RuntimeVersion)|name:$($module.Name)")
    [void]$lines.Add("entrypoint|$(Safe-FullName $module.EntryPoint)")

    Add-CustomAttributes -Lines $lines -Owner "assembly:$($module.Assembly.FullName)" -Provider $module.Assembly
    Add-CustomAttributes -Lines $lines -Owner "module:$($module.Name)" -Provider $module

    foreach ($asmRef in ($module.GetAssemblyRefs() | Sort-Object FullName)) {
        [void]$lines.Add("assemblyref|$($asmRef.FullName)")
    }
    foreach ($modRef in ($module.GetModuleRefs() | Sort-Object Name)) {
        [void]$lines.Add("moduleref|$($modRef.Name)")
    }
    foreach ($resource in ($module.Resources | Sort-Object Name)) {
        [void]$lines.Add((Get-EmbeddedResourceInfo -Resource $resource))
    }

    foreach ($type in ($module.GetTypes() | Sort-Object FullName)) {
        $interfaces = ($type.Interfaces | ForEach-Object { $_.Interface.FullName } | Sort-Object) -join ","
        [void]$lines.Add("type|$($type.FullName)|attrs:$($type.Attributes)|base:$(Safe-FullName $type.BaseType)|interfaces:$interfaces")
        Add-CustomAttributes -Lines $lines -Owner "type:$($type.FullName)" -Provider $type

        foreach ($field in ($type.Fields | Sort-Object FullName)) {
            $initial = ""
            if ($field.HasFieldRVA -and $null -ne $field.InitialValue) {
                $initial = "|rva_len:$($field.InitialValue.Length)|rva_sha256:$(Hash-Bytes $field.InitialValue)"
            }
            $constant = if ($field.HasConstant) { "|constant:$($field.Constant.Value)" } else { "" }
            [void]$lines.Add("field|$($field.FullName)|attrs:$($field.Attributes)|type:$(Safe-FullName $field.FieldType)$constant$initial")
            Add-CustomAttributes -Lines $lines -Owner "field:$($field.FullName)" -Provider $field
        }

        foreach ($method in ($type.Methods | Sort-Object FullName)) {
            $pinvoke = ""
            if ($method.HasImplMap) {
                $pinvoke = "|implmap:$($method.ImplMap.Module.Name)!$($method.ImplMap.Name)|attrs:$($method.ImplMap.Attributes)"
            }
            [void]$lines.Add("method|$($method.FullName)|attrs:$($method.Attributes)|implattrs:$($method.ImplAttributes)|hasbody:$($method.HasBody)$pinvoke")
            Add-CustomAttributes -Lines $lines -Owner "method:$($method.FullName)" -Provider $method

            foreach ($param in ($method.Parameters | Sort-Object Index)) {
                [void]$lines.Add("param|$($method.FullName)|index:$($param.Index)|name:$($param.Name)|type:$(Safe-FullName $param.Type)|attrs:$($param.ParamDef.Attributes)")
                Add-CustomAttributes -Lines $lines -Owner "param:$($method.FullName):$($param.Index)" -Provider $param.ParamDef
            }
        }

        foreach ($property in ($type.Properties | Sort-Object FullName)) {
            [void]$lines.Add("property|$($property.FullName)|attrs:$($property.Attributes)|type:$(Safe-FullName $property.PropertySig.RetType)")
            Add-CustomAttributes -Lines $lines -Owner "property:$($property.FullName)" -Provider $property
        }

        foreach ($event in ($type.Events | Sort-Object FullName)) {
            [void]$lines.Add("event|$($event.FullName)|attrs:$($event.Attributes)|type:$(Safe-FullName $event.EventType)")
            Add-CustomAttributes -Lines $lines -Owner "event:$($event.FullName)" -Provider $event
        }
    }

    return ,($lines | Sort-Object)
}

if (-not (Test-Path -LiteralPath $OriginalExe)) {
    throw "Original EXE not found: $OriginalExe"
}
if (-not (Test-Path -LiteralPath $RecoveredExe)) {
    throw "Recovered EXE not found: $RecoveredExe"
}

$orig = Get-MetadataSurface -AssemblyPath $OriginalExe
$rec = Get-MetadataSurface -AssemblyPath $RecoveredExe

$diff = Compare-Object -ReferenceObject $orig -DifferenceObject $rec
$missing = @($diff | Where-Object { $_.SideIndicator -eq "<=" } | Select-Object -ExpandProperty InputObject)
$extra = @($diff | Where-Object { $_.SideIndicator -eq "=>" } | Select-Object -ExpandProperty InputObject)

$lines = New-Object "System.Collections.Generic.List[string]"
[void]$lines.Add("OriginalExe: $([IO.Path]::GetFullPath($OriginalExe))")
[void]$lines.Add("RecoveredExe: $([IO.Path]::GetFullPath($RecoveredExe))")
[void]$lines.Add("")
[void]$lines.Add("metadata_surface_count_original: $($orig.Count)")
[void]$lines.Add("metadata_surface_count_recovered: $($rec.Count)")
[void]$lines.Add("metadata_surface_missing_in_recovered_count: $($missing.Count)")
[void]$lines.Add("metadata_surface_extra_in_recovered_count: $($extra.Count)")

function Add-LimitedList([string]$Title, [object[]]$Items) {
    [void]$lines.Add("")
    [void]$lines.Add($Title)
    $take = if ($Items.Count -gt $MaxDiffLines) { $Items[0..($MaxDiffLines - 1)] } else { $Items }
    foreach ($item in $take) {
        [void]$lines.Add("  $item")
    }
    if ($Items.Count -gt $MaxDiffLines) {
        [void]$lines.Add("  ... truncated ...")
    }
}

Add-LimitedList -Title "missing_in_recovered:" -Items $missing
Add-LimitedList -Title "extra_in_recovered:" -Items $extra

[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($OutPath))) | Out-Null
[IO.File]::WriteAllLines([IO.Path]::GetFullPath($OutPath), $lines, [Text.Encoding]::UTF8)

Write-Host "wrote: $OutPath"
