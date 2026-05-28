Describe 'Packaged AssemblyLoadContext isolation' {
    It 'loads DesktopManager from the packaged module ALC without unapproved verb warnings' {
        $packagedModuleRoot = Join-Path $PSScriptRoot '..\Artefacts\Unpacked\Modules'
        $packagedModule = Join-Path $packagedModuleRoot 'DesktopManager'
        $packagedLoader = Join-Path $packagedModule 'Lib\Core\DesktopManager.ModuleLoadContext.dll'
        if ($PSVersionTable.PSEdition -ne 'Core' -or -not (Test-Path -LiteralPath $packagedLoader)) {
            Set-ItResult -Skipped -Because 'packaged Core artifact is required'
            return
        }

        $moduleRootLiteral = $packagedModuleRoot.Replace("'", "''")
        $script = @"
`$ErrorActionPreference = 'Stop'
`$moduleRoot = '$moduleRootLiteral'
`$env:PSModulePath = `$moduleRoot + [IO.Path]::PathSeparator + [IO.Path]::Combine(`$PSHOME, 'Modules')

`$warnings = @()
`$module = Import-Module DesktopManager -Force -WarningVariable warnings -PassThru
`$command = `$module.ExportedCmdlets['Get-DesktopBackgroundColor']
`$stepCommand = `$module.ExportedCmdlets['Step-DesktopSlideshow']
`$commandAssembly = `$command.ImplementingType.Assembly
`$commandAlc = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext(`$commandAssembly)
`$loadedAssemblies = [System.Runtime.Loader.AssemblyLoadContext]::All |
    ForEach-Object {
        `$alc = `$_
        foreach (`$assembly in `$alc.Assemblies) {
            if (`$assembly.GetName().Name -in @('DesktopManager.PowerShell', 'DesktopManager', 'DesktopManager.ModuleLoadContext')) {
                [pscustomobject]@{
                    Assembly = `$assembly.GetName().Name
                    Version = `$assembly.GetName().Version.ToString()
                    ALC = `$alc.Name
                    IsDefault = [object]::ReferenceEquals(`$alc, [System.Runtime.Loader.AssemblyLoadContext]::Default)
                    Location = `$assembly.Location
                }
            }
        }
    }

[pscustomobject]@{
    WarningCount = @(`$warnings).Count
    Warnings = @(`$warnings | ForEach-Object ToString)
    CommandSource = `$command.Source
    CommandModuleName = `$command.ModuleName
    CommandAssembly = `$commandAssembly.Location
    CommandALC = `$commandAlc.Name
    CommandALCIsDefault = [object]::ReferenceEquals(`$commandAlc, [System.Runtime.Loader.AssemblyLoadContext]::Default)
    StepCommandName = `$stepCommand.Name
    LoadedAssemblies = @(`$loadedAssemblies)
} | ConvertTo-Json -Depth 6 -Compress
"@
        $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
        $output = pwsh -NoProfile -ExecutionPolicy Bypass -EncodedCommand $encoded 2>&1
        $LASTEXITCODE | Should -Be 0 -Because ($output -join [Environment]::NewLine)

        $json = $output | Where-Object { $_ -is [string] -and $_.TrimStart().StartsWith('{') } | Select-Object -Last 1
        $json | Should -Not -BeNullOrEmpty -Because ($output -join [Environment]::NewLine)
        $result = $json | ConvertFrom-Json

        $result.WarningCount | Should -Be 0 -Because ($result.Warnings -join [Environment]::NewLine)
        $result.CommandSource | Should -Be 'DesktopManager'
        $result.CommandModuleName | Should -Be 'DesktopManager'
        $result.CommandAssembly | Should -BeLike '*\Artefacts\Unpacked\Modules\DesktopManager\Lib\Core\DesktopManager.PowerShell.dll'
        $result.CommandALC | Should -Be 'DesktopManager'
        $result.CommandALCIsDefault | Should -BeFalse
        $result.StepCommandName | Should -Be 'Step-DesktopSlideshow'

        $loadedAssemblies = @($result.LoadedAssemblies)
        $powerShellAssembly = $loadedAssemblies | Where-Object Assembly -eq 'DesktopManager.PowerShell' | Select-Object -First 1
        $coreAssembly = $loadedAssemblies | Where-Object Assembly -eq 'DesktopManager' | Select-Object -First 1
        $loaderAssembly = $loadedAssemblies | Where-Object Assembly -eq 'DesktopManager.ModuleLoadContext' | Select-Object -First 1

        $powerShellAssembly.ALC | Should -Be 'DesktopManager'
        $powerShellAssembly.IsDefault | Should -BeFalse
        $coreAssembly.ALC | Should -Be 'DesktopManager'
        $coreAssembly.IsDefault | Should -BeFalse
        $loaderAssembly.IsDefault | Should -BeTrue
    }
}
