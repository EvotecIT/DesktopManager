BeforeAll {
    . "$PSScriptRoot/TestBootstrap.ps1"
}

describe 'Get-DesktopWindow' {
    it 'handles IncludeHidden parameter' -Skip:(-not $IsWindows) {
        { Get-DesktopWindow -Name '__DesktopManager_NoMatch__' -IncludeHidden } | Should -Not -Throw
    }
}
