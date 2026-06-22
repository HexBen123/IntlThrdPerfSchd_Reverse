[CmdletBinding()]
param(
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if ($PSVersionTable.PSEdition -ne "Core") {
    throw "Use PowerShell 7 pwsh to run this script. Windows PowerShell 5.1 can misread UTF-8 paths."
}

$root = $PSScriptRoot
$solution = Join-Path $root "IntlThrdPerfSchd.sln"
$projectDir = Join-Path $root "IntlThrdPerfSchd"
$artifactsDir = Join-Path $root "recovery_artifacts"
$patchedDir = Join-Path $artifactsDir "patched"
$originalExe = Join-Path $artifactsDir "original\IntlThrdSchd.exe"
$builtExe = Join-Path $projectDir "bin\$Configuration\net48\IntlThrdSchd.exe"
$patchedExe = Join-Path $patchedDir "IntlThrdSchd.exe"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [scriptblock] $Body
    )

    Write-Host ""
    Write-Host "== $Name =="
    & $Body
}

function Invoke-PwshFile {
    param(
        [Parameter(Mandatory = $true)] [string] $ScriptPath,
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    & pwsh -NoProfile -ExecutionPolicy Bypass -File $ScriptPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Script failed with exit code ${LASTEXITCODE}: $ScriptPath"
    }
}

function Invoke-PwshFileWithReport {
    param(
        [Parameter(Mandatory = $true)] [string] $ScriptPath,
        [Parameter(Mandatory = $true)] [string[]] $Arguments,
        [Parameter(Mandatory = $true)] [string] $ReportPath
    )

    & pwsh -NoProfile -ExecutionPolicy Bypass -File $ScriptPath @Arguments 2>&1 |
        Tee-Object -FilePath $ReportPath
    if ($LASTEXITCODE -ne 0) {
        throw "Script failed with exit code ${LASTEXITCODE}: $ScriptPath"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Text
    )

    $content = [IO.File]::ReadAllText([IO.Path]::GetFullPath($Path))
    if (-not $content.Contains($Text)) {
        throw "Expected report text not found in ${Path}: $Text"
    }
}

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Expected path not found: $Path"
    }
}

Assert-PathExists -Path $solution
Assert-PathExists -Path $originalExe

[IO.Directory]::CreateDirectory($patchedDir) | Out-Null

Invoke-Step "Build restored source" {
    dotnet build $solution -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
    Assert-PathExists -Path $builtExe
}

Invoke-Step "Patch method bodies for audit output" {
    Invoke-PwshFile `
        -ScriptPath (Join-Path $artifactsDir "patch_method_bodies_1.26.ps1") `
        -Arguments @(
            "-OriginalExe", $originalExe,
            "-RecoveredExe", $builtExe,
            "-OutExe", $patchedExe
        )
    Assert-PathExists -Path $patchedExe
}

Invoke-Step "Compare stable method IL hashes" {
    Invoke-PwshFile `
        -ScriptPath (Join-Path $artifactsDir "compare_method_il_hashes_1.26.ps1") `
        -Arguments @(
            "-OriginalExe", $originalExe,
            "-RecoveredExe", $patchedExe,
            "-OutPath", (Join-Path $patchedDir "method_il_hash_diff_1.26_patched.txt")
        )
}

Invoke-Step "Compare all method IL hashes" {
    Invoke-PwshFile `
        -ScriptPath (Join-Path $artifactsDir "compare_method_il_hashes_1.26.ps1") `
        -Arguments @(
            "-OriginalExe", $originalExe,
            "-RecoveredExe", $patchedExe,
            "-OutPath", (Join-Path $patchedDir "method_il_hash_diff_1.26_patched_all.txt"),
            "-IncludeCompilerGenerated"
        )
}

Invoke-Step "Compare manifest resources" {
    Invoke-PwshFileWithReport `
        -ScriptPath (Join-Path $artifactsDir "compare_manifest_resources_1.26.ps1") `
        -Arguments @(
            "-OriginalExe", $originalExe,
            "-RecoveredExe", $patchedExe
        ) `
        -ReportPath (Join-Path $patchedDir "manifest_resource_compare_1.26_patched.txt")
}

Invoke-Step "Generate entity lists" {
    Invoke-PwshFile `
        -ScriptPath (Join-Path $artifactsDir "generate_entity_lists_1.26.ps1") `
        -Arguments @(
            "-OriginalExe", $originalExe,
            "-RecoveredExe", $patchedExe
        )
}

Invoke-Step "Compare metadata surface" {
    Invoke-PwshFile `
        -ScriptPath (Join-Path $artifactsDir "compare_metadata_surface_1.26.ps1") `
        -Arguments @(
            "-OriginalExe", $originalExe,
            "-RecoveredExe", $patchedExe,
            "-OutPath", (Join-Path $patchedDir "metadata_surface_diff_1.26_patched.txt")
        )
}

Invoke-Step "Verify reports and output payload" {
    $stableReport = Join-Path $patchedDir "method_il_hash_diff_1.26_patched.txt"
    $allReport = Join-Path $patchedDir "method_il_hash_diff_1.26_patched_all.txt"
    $manifestReport = Join-Path $patchedDir "manifest_resource_compare_1.26_patched.txt"
    $metadataReport = Join-Path $patchedDir "metadata_surface_diff_1.26_patched.txt"
    $entityDiff = Join-Path $artifactsDir "entities_diff_patched_1.26.txt"

    Assert-FileContains -Path $stableReport -Text "stable_method_count_original: 167"
    Assert-FileContains -Path $stableReport -Text "stable_method_hash_match_count: 167"
    Assert-FileContains -Path $stableReport -Text "stable_method_hash_match_percent: 100"
    Assert-FileContains -Path $stableReport -Text "missing_in_recovered_count: 0"
    Assert-FileContains -Path $stableReport -Text "extra_in_recovered_count: 0"
    Assert-FileContains -Path $stableReport -Text "il_mismatch_count: 0"

    Assert-FileContains -Path $allReport -Text "include_compiler_generated: True"
    Assert-FileContains -Path $allReport -Text "stable_method_count_original: 171"
    Assert-FileContains -Path $allReport -Text "stable_method_hash_match_count: 171"
    Assert-FileContains -Path $allReport -Text "stable_method_hash_match_percent: 100"
    Assert-FileContains -Path $allReport -Text "missing_in_recovered_count: 0"
    Assert-FileContains -Path $allReport -Text "extra_in_recovered_count: 0"
    Assert-FileContains -Path $allReport -Text "il_mismatch_count: 0"

    Assert-FileContains -Path $manifestReport -Text "equal: True"
    Assert-FileContains -Path $metadataReport -Text "metadata_surface_count_original: 2658"
    Assert-FileContains -Path $metadataReport -Text "metadata_surface_count_recovered: 2658"
    Assert-FileContains -Path $metadataReport -Text "metadata_surface_missing_in_recovered_count: 0"
    Assert-FileContains -Path $metadataReport -Text "metadata_surface_extra_in_recovered_count: 0"

    $entityDiffText = [IO.File]::ReadAllText([IO.Path]::GetFullPath($entityDiff))
    if ($entityDiffText.Trim().Length -ne 0) {
        throw "Entity diff is not empty: $entityDiff"
    }

    $originalItem = Get-Item -LiteralPath $originalExe
    $patchedItem = Get-Item -LiteralPath $patchedExe
    if ($originalItem.Length -ne 73216 -or $patchedItem.Length -ne 73216) {
        throw "Unexpected EXE size. original=$($originalItem.Length), patched=$($patchedItem.Length)"
    }

    $originalVersion = $originalItem.VersionInfo
    $patchedVersion = $patchedItem.VersionInfo
    foreach ($field in @("FileVersion", "ProductVersion", "FileDescription", "ProductName")) {
        if ($originalVersion.$field -ne $patchedVersion.$field) {
            throw "Version field mismatch for $field. original=$($originalVersion.$field), patched=$($patchedVersion.$field)"
        }
    }

    $fileReport = Join-Path $patchedDir "file_properties_compare_1.26_patched.txt"
    [IO.File]::WriteAllLines(
        [IO.Path]::GetFullPath($fileReport),
        @(
            "OriginalExe: $([IO.Path]::GetFullPath($originalExe))",
            "RecoveredExe: $([IO.Path]::GetFullPath($patchedExe))",
            "",
            "original_size: $($originalItem.Length)",
            "recovered_size: $($patchedItem.Length)",
            "size_equal: $($originalItem.Length -eq $patchedItem.Length)",
            "original_file_version: $($originalVersion.FileVersion)",
            "recovered_file_version: $($patchedVersion.FileVersion)",
            "original_product_version: $($originalVersion.ProductVersion)",
            "recovered_product_version: $($patchedVersion.ProductVersion)",
            "original_file_description: $($originalVersion.FileDescription)",
            "recovered_file_description: $($patchedVersion.FileDescription)",
            "original_product_name: $($originalVersion.ProductName)",
            "recovered_product_name: $($patchedVersion.ProductName)"
        ),
        [Text.Encoding]::UTF8
    )

    $outputDir = Split-Path -Parent $builtExe
    foreach ($relativePath in @(
        "Microsoft.Diagnostics.Tracing.TraceEvent.dll",
        "Microsoft.Diagnostics.FastSerialization.dll",
        "System.Runtime.CompilerServices.Unsafe.dll",
        "System.Security.AccessControl.dll",
        "System.Security.Principal.Windows.dll",
        "WinRing0.dll",
        "WinRing0.sys",
        "WinRing0x64.dll",
        "WinRing0x64.sys",
        "InstallUtil.exe",
        "IntlThrdschd.reg",
        "powerreg.reg",
        "amd64\KernelTraceControl.dll",
        "arm64\KernelTraceControl.dll",
        "x86\KernelTraceControl.Win61.dll",
        "安装服务.bat",
        "卸载服务.bat"
    )) {
        Assert-PathExists -Path (Join-Path $outputDir $relativePath)
    }
}

Write-Host ""
Write-Host "High-fidelity audit build verified:"
Write-Host "  audit patched exe: $patchedExe"
Write-Host "  audit reports: $patchedDir"
Write-Host "  note: normal source development should use dotnet build output before this IL patch step"
