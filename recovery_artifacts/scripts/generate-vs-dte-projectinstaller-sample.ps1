param(
    [string]$OutputRoot = (Join-Path $PSScriptRoot "..\tmp\vs_dte_projectinstaller_sample"),
    [string]$SummaryJson = (Join-Path $PSScriptRoot "..\vs_dte_projectinstaller_sample_2.36.json"),
    [string]$ProjectName = "IntlThrdPerfSchd",
    [string]$RecoveredRoot = "",
    [switch]$RoundTripRecovered
)

$ErrorActionPreference = "Stop"

function Resolve-NormalizedPath {
    param([string]$PathText)

    $resolved = Resolve-Path -LiteralPath $PathText
    return [System.IO.Path]::GetFullPath($resolved.Path)
}

function Ensure-Directory {
    param([string]$PathText)

    if (-not (Test-Path -LiteralPath $PathText)) {
        New-Item -ItemType Directory -Path $PathText | Out-Null
    }
}

function Test-IsChildPath {
    param(
        [string]$ParentPath,
        [string]$ChildPath
    )

    $parent = [System.IO.Path]::GetFullPath($ParentPath).TrimEnd('\') + '\'
    $child = [System.IO.Path]::GetFullPath($ChildPath)
    return $child.StartsWith($parent, [System.StringComparison]::OrdinalIgnoreCase)
}

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [int]$TimeoutMs = 20000,
        [int]$SleepMs = 250,
        [string]$Description = "condition"
    )

    $deadline = [DateTime]::UtcNow.AddMilliseconds($TimeoutMs)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (& $Condition) {
            return
        }
        Start-Sleep -Milliseconds $SleepMs
    }

    throw "Timed out waiting for $Description"
}

function Get-FileSha256 {
    param([string]$PathText)

    return (Get-FileHash -LiteralPath $PathText -Algorithm SHA256).Hash.ToLower()
}

$projectTemplatePath = "D:\Microsoft Visual Studio\18\Community\Common7\IDE\ProjectTemplates\CSharp\Windows\2052\WindowsService\cswindowsservice.vstemplate"
$itemTemplatePath = "D:\Microsoft Visual Studio\18\Community\Common7\IDE\ItemTemplates\CSharp\General\2052\Installer\installer.vstemplate"

if (-not (Test-Path -LiteralPath $projectTemplatePath)) {
    throw "Missing project template: $projectTemplatePath"
}

if (-not (Test-Path -LiteralPath $itemTemplatePath)) {
    throw "Missing item template: $itemTemplatePath"
}

$outputRootFull = [System.IO.Path]::GetFullPath($OutputRoot)
$summaryJsonFull = [System.IO.Path]::GetFullPath($SummaryJson)
$scriptRootFull = Resolve-NormalizedPath $PSScriptRoot
$artifactRootFull = Resolve-NormalizedPath (Join-Path $PSScriptRoot "..")

Ensure-Directory $outputRootFull
Ensure-Directory ([System.IO.Path]::GetDirectoryName($summaryJsonFull))

if (-not (Test-IsChildPath -ParentPath $artifactRootFull -ChildPath $outputRootFull)) {
    throw "OutputRoot must stay under recovery_artifacts subtree: $outputRootFull"
}

if (Test-Path -LiteralPath $outputRootFull) {
    Get-ChildItem -LiteralPath $outputRootFull -Force | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }
}

$solutionRoot = Join-Path $outputRootFull "workspace"
$solutionFile = Join-Path $solutionRoot "$ProjectName.sln"
$projectDir = Join-Path $solutionRoot $ProjectName
$projectFile = Join-Path $projectDir "$ProjectName.csproj"
$installerFile = Join-Path $projectDir "ProjectInstaller.cs"
$installerDesignerFile = Join-Path $projectDir "ProjectInstaller.Designer.cs"

Ensure-Directory $solutionRoot

$dte = $null
$solution = $null
$project = $null
$item = $null
$designerWindow = $null

$result = [ordered]@{
    generated_at_utc = [DateTime]::UtcNow.ToString("o")
    project_template_path = $projectTemplatePath
    item_template_path = $itemTemplatePath
    output_root = $outputRootFull
    solution_root = $solutionRoot
    solution_file = $solutionFile
    project_dir = $projectDir
    project_file = $projectFile
    project_name = $ProjectName
    installer_file = $installerFile
    installer_designer_file = $installerDesignerFile
    recovered_root = $RecoveredRoot
    roundtrip_recovered = [bool]$RoundTripRecovered
    before_roundtrip = @()
    files = @()
    error = $null
}

try {
    $dte = New-Object -ComObject "VisualStudio.DTE.18.0"
    $dte.SuppressUI = $true
    $dte.UserControl = $false
    $dte.MainWindow.Visible = $false

    Wait-Until -Description "DTE solution object" -Condition {
        try {
            return $null -ne $dte.Solution
        } catch {
            return $false
        }
    }

    $solution = $dte.Solution
    $solution.Create($solutionRoot, $ProjectName)
    $solution.AddFromTemplate($projectTemplatePath, $projectDir, $ProjectName, $false)

    Wait-Until -Description "project file creation" -Condition {
        Test-Path -LiteralPath $projectFile
    }

    $solution.SaveAs($solutionFile)

    $project = $solution.Projects.Item(1)
    $item = $project.ProjectItems.AddFromTemplate($itemTemplatePath, "ProjectInstaller.cs")
    if ($item -eq $null) {
        try {
            $item = $project.ProjectItems.Item("ProjectInstaller.cs")
        } catch {
            $item = $null
        }
    }

    Wait-Until -Description "ProjectInstaller item generation" -Condition {
        (Test-Path -LiteralPath $installerFile) -and (Test-Path -LiteralPath $installerDesignerFile)
    }

    try {
        $dte.ExecuteCommand("File.SaveAll")
    } catch {
        # Some DTE instances return before command routing is fully ready; explicit SaveAs above is enough.
    }

    if ($RoundTripRecovered) {
        if ([string]::IsNullOrWhiteSpace($RecoveredRoot)) {
            throw "RecoveredRoot is required when RoundTripRecovered is set."
        }

        $recoveredRootFull = [System.IO.Path]::GetFullPath($RecoveredRoot)
        $recoveredInstaller = Join-Path $recoveredRootFull "ProjectInstaller.cs"
        $recoveredInstallerDesigner = Join-Path $recoveredRootFull "ProjectInstaller.Designer.cs"

        foreach ($path in @($recoveredInstaller, $recoveredInstallerDesigner)) {
            if (-not (Test-Path -LiteralPath $path)) {
                throw "Missing recovered file for round-trip: $path"
            }
        }

        foreach ($path in @($installerFile, $installerDesignerFile)) {
            $result.before_roundtrip += [ordered]@{
                path = $path
                exists = (Test-Path -LiteralPath $path)
                sha256 = (Get-FileSha256 $path)
                length = (Get-Item -LiteralPath $path).Length
            }
        }

        [System.IO.File]::Copy($recoveredInstaller, $installerFile, $true)
        [System.IO.File]::Copy($recoveredInstallerDesigner, $installerDesignerFile, $true)

        $designerViewKind = "{7651a702-06e5-11d1-8ebd-00a0c90f26ea}"
        if ($item -eq $null) {
            $item = $project.ProjectItems.Item("ProjectInstaller.cs")
        }
        $designerWindow = $item.Open($designerViewKind)
        $designerWindow.Activate()
        Start-Sleep -Milliseconds 1500

        try {
            $dte.ExecuteCommand("File.SaveAll")
        } catch {
        }

        Start-Sleep -Milliseconds 1500
    }

    foreach ($path in @($installerFile, $installerDesignerFile)) {
        $result.files += [ordered]@{
            path = $path
            exists = (Test-Path -LiteralPath $path)
            sha256 = (Get-FileSha256 $path)
            length = (Get-Item -LiteralPath $path).Length
        }
    }
} catch {
    $result.error = $_.Exception.Message
    if ($_.ScriptStackTrace) {
        $result.error += "`n" + $_.ScriptStackTrace
    }
} finally {
    if ($solution -ne $null) {
        try {
            $solution.Close($true)
        } catch {
        }
    }

    if ($designerWindow -ne $null) {
        try {
            $designerWindow.Close(0)
        } catch {
        }
    }

    if ($dte -ne $null) {
        try {
            $dte.Quit()
        } catch {
        }
    }

    foreach ($comObject in @($designerWindow, $item, $project, $solution, $dte)) {
        if ($null -ne $comObject) {
            try {
                [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($comObject)
            } catch {
            }
        }
    }

    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}

$json = $result | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($summaryJsonFull, $json, [System.Text.UTF8Encoding]::new($true))
Write-Output $json
