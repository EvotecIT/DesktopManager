Describe 'Desktop background color cmdlets' {
    BeforeAll {
        . "$PSScriptRoot/TestBootstrap.ps1"
    }

    It 'Supports WhatIf for Set-DesktopBackgroundColor' {
        { Set-DesktopBackgroundColor -Color 0x123456 -WhatIf } | Should -Not -Throw
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        It 'Returns a UInt32 from Get-DesktopBackgroundColor' {
            $color = Get-DesktopBackgroundColor
            $color.GetType().FullName | Should -Be 'System.UInt32'
        }
    } else {
        It 'Get-DesktopBackgroundColor throws on non-Windows' {
            { Get-DesktopBackgroundColor } | Should -Throw -ExceptionType System.PlatformNotSupportedException
        }
    }
}
