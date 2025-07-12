describe 'Invoke-DesktopMouseDrag' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Invoke-DesktopMouseDrag -Button Left -StartX 0 -StartY 0 -EndX 10 -EndY 10 -StepDelay 0 -WhatIf } | Should -Not -Throw
    }
}
