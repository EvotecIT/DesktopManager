$ModuleName = (Get-ChildItem $PSScriptRoot\*.psd1).BaseName
if (-not (Get-Variable -Name IsWindows -Scope Global -ErrorAction SilentlyContinue)) {
    $global:IsWindows = $env:OS -eq 'Windows_NT'
}
$PrimaryModule = Get-ChildItem -Path $PSScriptRoot -Filter '*.psd1' -Recurse -ErrorAction SilentlyContinue -Depth 1
if (-not $PrimaryModule) {
    throw "Path $PSScriptRoot doesn't contain PSD1 files. Failing tests."
}
if ($PrimaryModule.Count -ne 1) {
    throw 'More than one PSD1 files detected. Failing tests.'
}
$LibPath = Join-Path -Path $PSScriptRoot -ChildPath 'Lib'
if (-not (Test-Path -Path $LibPath)) {
    $env:DESKTOPMANAGER_DEVELOPMENT = 'true'
}
$PSDInformation = Import-PowerShellDataFile -Path $PrimaryModule.FullName
$RequiredModules = @(
    'Pester'
    'PSWriteColor'
    'DesktopManager'
    if ($PSDInformation.RequiredModules) {
        $PSDInformation.RequiredModules
    }
)
foreach ($Module in $RequiredModules) {
    if ($Module -eq 'DesktopManager') {
        continue
    }
    if ($Module -is [System.Collections.IDictionary]) {
        $Exists = Get-Module -ListAvailable -Name $Module.ModuleName
        if (-not $Exists) {
            Write-Warning "$ModuleName - Downloading $($Module.ModuleName) from PSGallery"
            Install-Module -Name $Module.ModuleName -Force -SkipPublisherCheck
        }
    } else {
        $Exists = Get-Module -ListAvailable $Module -ErrorAction SilentlyContinue
        if (-not $Exists) {
            Install-Module -Name $Module -Force -SkipPublisherCheck
        }
    }
}

Write-Color 'ModuleName: ', $ModuleName, ' Version: ', $PSDInformation.ModuleVersion -Color Yellow, Green, Yellow, Green -LinesBefore 2
Write-Color 'PowerShell Version: ', $PSVersionTable.PSVersion -Color Yellow, Green
Write-Color 'PowerShell Edition: ', $PSVersionTable.PSEdition -Color Yellow, Green
Write-Color 'Required modules: ' -Color Yellow
foreach ($Module in $PSDInformation.RequiredModules) {
    if ($Module -is [System.Collections.IDictionary]) {
        Write-Color '   [>] ', $Module.ModuleName, ' Version: ', $Module.ModuleVersion -Color Yellow, Green, Yellow, Green
    } else {
        Write-Color '   [>] ', $Module -Color Yellow, Green
    }
}
Write-Color

try {
    Import-Module $PSScriptRoot\*.psd1 -Force -ErrorAction Stop
    Import-Module Pester -Force -ErrorAction Stop
} catch {
    throw "Failed to import module $ModuleName"
}

$Configuration = [PesterConfiguration]::Default
$Configuration.Run.Path = "$PSScriptRoot\Tests"
$Configuration.Run.Exit = $true
$Configuration.Should.ErrorAction = 'Continue'
$Configuration.CodeCoverage.Enabled = $false
$Configuration.Output.Verbosity = 'Detailed'
$RunInteractive = $false
if ($env:RUN_UI_TESTS -eq 'true' -or $env:DESKTOPMANAGER_RUN_UI_TESTS -eq 'true') {
    $RunInteractive = $true
}
if (-not $RunInteractive) {
    $Configuration.Filter.ExcludeTag = @('Interactive')
}
$Result = Invoke-Pester -Configuration $Configuration
#$result = Invoke-Pester -Script $PSScriptRoot\Tests -Verbose -Output Detailed #-EnableExit

if ($Result.FailedCount -gt 0) {
    throw "$($Result.FailedCount) tests failed."
}
