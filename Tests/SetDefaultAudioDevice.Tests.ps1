describe 'Default audio device cmdlet' {
    it 'exports Set-DefaultAudioDevice' {
        Get-Command Set-DefaultAudioDevice | Should -Not -BeNullOrEmpty
    }
    it 'supports WhatIf' -Skip:(-not $IsWindows) {
        { Set-DefaultAudioDevice -DeviceId 'test' -WhatIf } | Should -Not -Throw
    }
}
