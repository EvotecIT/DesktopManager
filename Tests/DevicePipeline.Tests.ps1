Describe 'DesktopManager device pipeline input' {
    BeforeAll {
        . "$PSScriptRoot/TestBootstrap.ps1"
    }

    It 'Processes each property-bound device instance' {
        $InputDevices = @(Get-DesktopDevice -Present | Select-Object -First 2)
        if ($InputDevices.Count -eq 0) {
            Set-ItResult -Skipped -Because 'No present Plug and Play device was available.'
        } else {
            $ExpectedIds = @($InputDevices.InstanceId)
            $Actual = @($InputDevices | Get-DesktopDevice)

            $Actual.Count | Should -Be $ExpectedIds.Count
            @($Actual.InstanceId) | Should -Be $ExpectedIds
        }
    }

    It 'Processes each property-bound Driver Store package name' {
        $InputPackages = @(Get-DesktopDriverPackage | Select-Object -First 2)
        if ($InputPackages.Count -eq 0) {
            Set-ItResult -Skipped -Because 'No third-party Driver Store package was available.'
        } else {
            $ExpectedNames = @($InputPackages.PublishedInfName)
            $Actual = @($InputPackages | Get-DesktopDriverPackage)

            $Actual.Count | Should -Be $ExpectedNames.Count
            @($Actual.PublishedInfName) | Should -Be $ExpectedNames
        }
    }
}
