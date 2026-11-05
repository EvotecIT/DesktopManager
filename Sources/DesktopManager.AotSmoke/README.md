# Windows Native AOT smoke consumer

This standalone consumer publishes as a self-contained .NET 8 or .NET 10
Windows x64 executable and references the core project directly. It refuses
to run with dynamic code enabled. Use the repository's SDK and Windows Native
AOT linker prerequisites.

Set `EVOTEC_SCRATCH_ROOT` to a writable directory outside the checkout, then
publish and run from the repository root:

```powershell
$smokeRoot = Join-Path $env:EVOTEC_SCRATCH_ROOT 'DesktopManager-AotSmoke'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:TEMP = Join-Path $smokeRoot 'temp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
dotnet publish Sources/DesktopManager.AotSmoke/DesktopManager.AotSmoke.csproj --configuration Release --framework net10.0-windows10.0.19041.0 --runtime win-x64 --artifacts-path (Join-Path $smokeRoot 'build') --output (Join-Path $smokeRoot 'publish')
& (Join-Path $smokeRoot 'publish/DesktopManager.AotSmoke.exe')
```

Use `--framework net8.0-windows10.0.19041.0` for .NET 8. Ordinary shared NuGet
caches are sufficient for this project reference; local candidate packages
require an isolated cache.

The checks cover:

- Monitor bounds/DPI, wallpaper metadata, slideshow reads, and checkpoint
  capture/disposal. Checkpoints are never restored.
- Shell item arrays with an owned Unicode-named file, including agile retention
  and resolution from another initialized MTA. No slideshow is applied.
- Live clipboard capture/disposal and independent duplication of an owned
  Unicode-text HGLOBAL, plus creation and disposal of a message-only clipboard owner. Neither fixture handle is placed on the clipboard.
- Live Explorer folder capture plus an unregistered native folder view whose
  PIDL must resolve to its owned temporary directory. No Explorer window is opened.
- Experimental desktop enumeration, Core Audio endpoints/default roles, and
  generated audio callback registration/disposal. No desktop or audio setting changes.
- Application audio sessions and cached peaks, default output/microphone level
  snapshots, native IAudioClient activation/release without stream initialization,
  current media metadata/artwork, and microphone/camera usage records.
  No volume, mute, playback or privacy settings are changed.
- Physical window-position and optional application-identity reads on the owned nonactivating window.
- UIA discovery, names, values and runtime IDs for an offscreen, nonactivating
  EDIT/BUTTON harness. Only the owned EDIT value changes to verify generated
  event delivery, followed by subscription disposal. Win32 fallback cannot
  satisfy the UIA checks; no keyboard, mouse or foreground input is sent.

Each area runs in an owned child process with a 30-second deadline. Timeout
cleanup terminates that process tree; native fixtures free their handles and
remove their known files and empty directories. User paths, window titles,
desktop names and clipboard contents are not printed.

Exit `0` means every area passed; `1` means failure or timeout; `2` means no
failures but at least one unavailable area. Full coverage needs an interactive
Windows session with displays, audio, a supported experimental shell build and
an open Explorer filesystem folder. An empty capture result alone does not
prove that no folders are open. The unregistered view fixture does not replace
live `IServiceProvider`/`IShellBrowser` coverage.

Empty live slideshow or clipboard contents still permit the independent native
fixtures. Unsupported checkpoint modes and clipboard sequence changes remain
explicitly unavailable. Synthetic retention does not claim that wallpaper
restoration was tested, and the HGLOBAL fixture does not qualify every clipboard
format. Application-owned or oversized clipboard data is rejected before any
clipboard write. Slideshow fallback metadata alone is not strict HRESULT proof.
