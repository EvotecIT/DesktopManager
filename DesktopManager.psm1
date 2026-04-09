# Get public and private function definition files.
$Public = @( Get-ChildItem -Path $PSScriptRoot\Public\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Private = @( Get-ChildItem -Path $PSScriptRoot\Private\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Classes = @( Get-ChildItem -Path $PSScriptRoot\Classes\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Enums = @( Get-ChildItem -Path $PSScriptRoot\Enums\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
# Get all assemblies
$AssemblyFolders = Get-ChildItem -Path $PSScriptRoot\Lib -Directory -ErrorAction SilentlyContinue

# to speed up development adding direct path to binaries, instead of the the Lib folder
$Development = $false
$DevelopmentEnv = $env:DESKTOPMANAGER_DEVELOPMENT
if ($DevelopmentEnv) {
    $Development = $DevelopmentEnv.ToString().ToLowerInvariant() -in @('1', 'true', 'yes', 'on')
}
$DevelopmentPath = "$PSScriptRoot\Sources\DesktopManager.PowerShell\bin\Debug"
$DevelopmentFolderCore = "net8.0-windows10.0.19041.0"
$DevelopmentFolderDefault = "net472"
$DevelopmentCoreFolder = Get-ChildItem -Path $DevelopmentPath -Directory -Filter 'net8.0-windows*' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $DevelopmentCoreFolder) {
    $DevelopmentCoreFolder = Get-ChildItem -Path $DevelopmentPath -Directory -Filter 'net8.*' -ErrorAction SilentlyContinue | Select-Object -First 1
}
if ($DevelopmentCoreFolder) {
    $DevelopmentFolderCore = $DevelopmentCoreFolder.Name
}
$BinaryModules = @(
    "DesktopManager.PowerShell.dll"
)

# Lets find which libraries we need to load
$Default = $false
$Core = $false
$Standard = $false
foreach ($A in $AssemblyFolders.Name) {
    if ($A -eq 'Default') {
        $Default = $true
    } elseif ($A -eq 'Core') {
        $Core = $true
    } elseif ($A -eq 'Standard') {
        $Standard = $true
    }
}
if ($Standard -and $Core -and $Default) {
    $FrameworkNet = 'Default'
    $Framework = 'Standard'
} elseif ($Standard -and $Core) {
    $Framework = 'Standard'
    $FrameworkNet = 'Standard'
} elseif ($Core -and $Default) {
    $Framework = 'Core'
    $FrameworkNet = 'Default'
} elseif ($Standard -and $Default) {
    $Framework = 'Standard'
    $FrameworkNet = 'Default'
} elseif ($Standard) {
    $Framework = 'Standard'
    $FrameworkNet = 'Standard'
} elseif ($Core) {
    $Framework = 'Core'
    $FrameworkNet = ''
} elseif ($Default) {
    $Framework = ''
    $FrameworkNet = 'Default'
} else {
    #Write-Error -Message 'No assemblies found'
}

$Assembly = @(
    if ($Development) {
        if ($PSEdition -eq 'Core') {
            Get-ChildItem -Path $DevelopmentPath\$DevelopmentFolderCore\*.dll -ErrorAction SilentlyContinue -Recurse
        } else {
            Get-ChildItem -Path $DevelopmentPath\$DevelopmentFolderDefault\*.dll -ErrorAction SilentlyContinue -Recurse
        }
    } else {
        if ($Framework -and $PSEdition -eq 'Core') {
            Get-ChildItem -Path $PSScriptRoot\Lib\$Framework\*.dll -ErrorAction SilentlyContinue -Recurse
        }
        if ($FrameworkNet -and $PSEdition -ne 'Core') {
            Get-ChildItem -Path $PSScriptRoot\Lib\$FrameworkNet\*.dll -ErrorAction SilentlyContinue -Recurse
        }
    }
)

$BinaryDev = @()
if ($Development) {
    $BinaryDev = @(
        foreach ($BinaryModule in $BinaryModules) {
            if ($PSEdition -eq 'Core') {
                $Variable = Resolve-Path "$DevelopmentPath\$DevelopmentFolderCore\$BinaryModule"
            } else {
                $Variable = Resolve-Path "$DevelopmentPath\$DevelopmentFolderDefault\$BinaryModule"
            }
            $Variable
            Write-Warning "Development mode: Using binaries from $Variable"
        }
    )
}

$ImportedBinaryModules = New-Object 'System.Collections.Generic.List[object]'
$FoundErrors = New-Object 'System.Collections.Generic.List[bool]'
if ($Development) {
    foreach ($BinaryModule in $BinaryDev) {
        try {
            $ImportedModule = Import-Module -Name $BinaryModule -Force -ErrorAction Stop -PassThru -Global
            if ($ImportedModule) {
                [void] $ImportedBinaryModules.Add($ImportedModule)
            }
        } catch {
            Write-Warning "Failed to import module $($BinaryModule): $($_.Exception.Message)"
            [void] $FoundErrors.Add($true)
        }
    }
} else {
    foreach ($BinaryModule in $BinaryModules) {
        try {
            if ($Framework -and $PSEdition -eq 'Core') {
                $ImportedModule = Import-Module -Name "$PSScriptRoot\Lib\$Framework\$BinaryModule" -Force -ErrorAction Stop -PassThru -Global
                if ($ImportedModule) {
                    [void] $ImportedBinaryModules.Add($ImportedModule)
                }
            }
            if ($FrameworkNet -and $PSEdition -ne 'Core') {
                $ImportedModule = Import-Module -Name "$PSScriptRoot\Lib\$FrameworkNet\$BinaryModule" -Force -ErrorAction Stop -PassThru -Global
                if ($ImportedModule) {
                    [void] $ImportedBinaryModules.Add($ImportedModule)
                }
            }
        } catch {
            Write-Warning "Failed to import module $($BinaryModule): $($_.Exception.Message)"
            [void] $FoundErrors.Add($true)
        }
    }
}
foreach ($Import in @($Assembly)) {
    try {
        Write-Verbose -Message $Import.FullName
        Add-Type -Path $Import.Fullname -ErrorAction Stop
        #  }
    } catch [System.Reflection.ReflectionTypeLoadException] {
        Write-Warning "Processing $($Import.Name) Exception: $($_.Exception.Message)"
        $LoaderExceptions = $($_.Exception.LoaderExceptions) | Sort-Object -Unique
        foreach ($E in $LoaderExceptions) {
            Write-Warning "Processing $($Import.Name) LoaderExceptions: $($E.Message)"
        }
        [void] $FoundErrors.Add($true)
        #Write-Error -Message "StackTrace: $($_.Exception.StackTrace)"
    } catch {
        Write-Warning "Processing $($Import.Name) Exception: $($_.Exception.Message)"
        $LoaderExceptions = $($_.Exception.LoaderExceptions) | Sort-Object -Unique
        foreach ($E in $LoaderExceptions) {
            Write-Warning "Processing $($Import.Name) LoaderExceptions: $($E.Message)"
        }
        [void] $FoundErrors.Add($true)
        #Write-Error -Message "StackTrace: $($_.Exception.StackTrace)"
    }
}
#Dot source the files
foreach ($Import in @($Classes + $Enums + $Private + $Public)) {
    try {
        . $Import.Fullname
    } catch {
        Write-Error -Message "Failed to import functions from $($import.Fullname): $_"
        [void] $FoundErrors.Add($true)
    }
}

if ($FoundErrors.Count -gt 0) {
    $ModuleName = (Get-ChildItem $PSScriptRoot\*.psd1).BaseName
    Write-Warning "Importing module $ModuleName failed. Fix errors before continuing."
    break
}

$CmdletsToExport = New-Object 'System.Collections.Generic.List[string]'
foreach ($Module in $ImportedBinaryModules) {
    if ($Module -and $Module.ExportedCmdlets) {
        foreach ($Key in $Module.ExportedCmdlets.Keys) {
            [void] $CmdletsToExport.Add($Key)
        }
    }
}
if ($CmdletsToExport.Count -eq 0) {
    foreach ($BinaryModule in $BinaryModules) {
        $ModuleName = [System.IO.Path]::GetFileNameWithoutExtension($BinaryModule)
        $ModuleCmdlets = Get-Command -Module $ModuleName -CommandType Cmdlet -ErrorAction SilentlyContinue
        foreach ($Cmdlet in $ModuleCmdlets) {
            [void] $CmdletsToExport.Add($Cmdlet.Name)
        }
    }
}
$CmdletsToExport = $CmdletsToExport.ToArray() | Sort-Object -Unique

Export-ModuleMember -Function '*' -Alias '*' -Cmdlet $CmdletsToExport
