#nullable enable
using System.Management.Automation;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;

namespace DesktopManager.PowerShell;

/// <summary>Types text, a password or a script into a remote or locked-down session.</summary>
/// <para type="synopsis">Types text into an RDP, Citrix or VNC session, or a password field, where pasting is blocked.</para>
/// <para type="description">Each character is sent as the key presses that produce it on the session's keyboard layout, so the remote side sees ordinary typing. The cmdlet never takes the focus: it waits until you click into the target window, then types. Typing stops when another window or control takes the focus, when you press a key, click or scroll, or when you press Ctrl+C, and every key it pressed is released.</para>
/// <para type="description">Pass secrets with -SecureString or -Credential so they never appear on the command line or in history. The text is never written to output, verbose messages or errors.</para>
/// <example>
///   <summary>Type a password into a Remote Desktop Manager RDP session</summary>
///   <code>Send-DesktopSessionText -Name '*Remote Desktop Manager*' -SecureString (Read-Host -AsSecureString)</code>
///   <para>After the command starts, click into the session; typing begins once its window is in the foreground.</para>
/// </example>
/// <example>
///   <summary>Type a script into a German remote session, letting the remote editor indent</summary>
///   <code>Send-DesktopSessionText -Name '*Remote Desktop Manager*' -Text (Get-Content .\Setup.ps1 -Raw) -Layout de-DE -StripIndentation</code>
/// </example>
/// <example>
///   <summary>Type into a macOS session over VNC</summary>
///   <code>Send-DesktopSessionText -Name '*Remote Desktop Manager*' -Credential $credential -Profile Vnc</code>
///   <para>Only the password of the credential is typed.</para>
/// </example>
[Cmdlet(VerbsCommunications.Send, "DesktopSessionText", DefaultParameterSetName = "Name", SupportsShouldProcess = true)]
[OutputType(typeof(SessionTypingResult))]
[SupportedOSPlatform("windows")]
public sealed class CmdletSendDesktopSessionText : PSCmdlet {
    private readonly object _cancellationSync = new();
    private CancellationTokenSource? _cancellation;

    /// <summary>
    /// <para type="description">Title of the window to type into, such as the remote desktop client. Supports wildcards and must match exactly one visible window.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Window to type into, for example from Get-DesktopWindow.</para>
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Window")]
    public WindowInfo Window { get; set; } = null!;

    /// <summary>
    /// <para type="description">Text to type. For secrets use -SecureString or -Credential instead.</para>
    /// </summary>
    [Parameter(Position = 1)]
    public string? Text { get; set; }

    /// <summary>
    /// <para type="description">Secret text to type, such as a password.</para>
    /// </summary>
    [Parameter]
    public SecureString? SecureString { get; set; }

    /// <summary>
    /// <para type="description">Credential whose password is typed. The user name is not typed.</para>
    /// </summary>
    [Parameter]
    [Credential]
    public PSCredential? Credential { get; set; }

    /// <summary>
    /// <para type="description">Kind of session: Rdp (default), Citrix, Vnc for VNC and Apple Remote Desktop viewers, or Local for an ordinary local window.</para>
    /// </summary>
    [Parameter]
    public SessionTypingProfile Profile { get; set; } = SessionTypingProfile.Rdp;

    /// <summary>
    /// <para type="description">Keyboard layout of the remote session, as a layout identifier such as 00000407 or a culture name such as de-DE. Defaults to the layout of the target window. Ignored by the Local profile.</para>
    /// </summary>
    [Parameter]
    public string? Layout { get; set; }

    /// <summary>
    /// <para type="description">How line breaks are typed: Enter (default), ShiftEnter, or None to leave them out.</para>
    /// </summary>
    [Parameter]
    public SessionLineBreakMode LineBreak { get; set; } = SessionLineBreakMode.Enter;

    /// <summary>
    /// <para type="description">Leave out leading spaces and tabs of every line, for editors that indent on their own.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter StripIndentation { get; set; }

    /// <summary>
    /// <para type="description">What to do with characters the layout cannot produce: Fail (default), Unicode to send them as Unicode packets, or Skip.</para>
    /// </summary>
    [Parameter]
    public UnmappedCharacterPolicy UnmappedCharacters { get; set; } = UnmappedCharacterPolicy.Fail;

    /// <summary>
    /// <para type="description">What to do when Caps Lock is on: Fail (default), TurnOff while typing, or Compensate.</para>
    /// </summary>
    [Parameter]
    public CapsLockPolicy CapsLock { get; set; } = CapsLockPolicy.Fail;

    /// <summary>
    /// <para type="description">Seconds to wait for you to click into the target window. Zero requires it to be in the foreground already.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, 3600)]
    public int ForegroundTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// <para type="description">Bring the target window to the foreground first. Only allowed with the Local profile.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Activate { get; set; }

    /// <summary>
    /// <para type="description">Delay after each character in milliseconds. Defaults to the profile's value.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, 10000)]
    public int? KeyDelayMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">How long Shift or AltGr is held around the key it modifies, in milliseconds. Defaults to the profile's value.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, 10000)]
    public int? ModifierHoldMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Delay after each line break in milliseconds. Defaults to the profile's value.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, 60000)]
    public int? LineDelayMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Number of characters typed before each pause. Zero types without pausing. Defaults to the profile's value.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int? ChunkSize { get; set; }

    /// <summary>
    /// <para type="description">Pause after each chunk in milliseconds. Defaults to the profile's value.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, 60000)]
    public int? ChunkPauseMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Keep typing when the focused control inside the target window changes.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AllowFocusChange { get; set; }

    /// <summary>
    /// <para type="description">Keep typing when you press a key, click or scroll.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IgnoreUserInput { get; set; }

    /// <summary>
    /// <para type="description">Return the typing result, which reports how many characters were typed and never contains the text.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        int sources = (Text != null ? 1 : 0) + (SecureString != null ? 1 : 0) + (Credential != null ? 1 : 0);
        if (sources != 1) {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException("Specify exactly one of -Text, -SecureString or -Credential."),
                "DesktopSessionTextSourceRequired",
                ErrorCategory.InvalidArgument,
                null));
        }

        WindowInfo? target = ParameterSetName == "Window" ? Window : ResolveSingleWindow();
        if (target == null || !ShouldProcess(target.Title, "Type text")) {
            return;
        }

        char[] text = ReadText();
        var cancellation = new CancellationTokenSource();
        lock (_cancellationSync) {
            _cancellation = cancellation;
        }

        try {
            if (!Activate.IsPresent) {
                WarnWhenNotForeground(target);
            }

            SessionTypingResult result = SessionTypingService.Type(target, text, CreateOptions(), cancellation.Token);
            if (!result.Completed) {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"Typing into '{target.Title}' stopped after {result.CharactersSent} of {result.CharactersTotal} characters: {result.AbortReason}."),
                    "DesktopSessionTextStopped",
                    ErrorCategory.OperationStopped,
                    target));
            }

            if (PassThru.IsPresent) {
                WriteObject(result);
            }
        } catch (Exception ex) when (ex is InvalidOperationException || ex is TimeoutException || ex is ArgumentException || ex is OperationCanceledException) {
            WriteError(new ErrorRecord(ex, "DesktopSessionTextFailed", ErrorCategory.InvalidOperation, target));
        } finally {
            Array.Clear(text, 0, text.Length);
            lock (_cancellationSync) {
                _cancellation = null;
            }

            cancellation.Dispose();
        }
    }

    /// <summary>Stops waiting or typing.</summary>
    protected override void StopProcessing() {
        lock (_cancellationSync) {
            _cancellation?.Cancel();
        }

        base.StopProcessing();
    }

    private SessionTypingOptions CreateOptions() {
        return new SessionTypingOptions {
            Profile = Profile,
            KeyboardLayout = Layout,
            LineBreak = LineBreak,
            StripLeadingIndentation = StripIndentation.IsPresent,
            UnmappedCharacters = UnmappedCharacters,
            CapsLock = CapsLock,
            ForegroundTimeout = TimeSpan.FromSeconds(ForegroundTimeoutSeconds),
            ActivateWindow = Activate.IsPresent,
            StopWhenFocusChanges = !AllowFocusChange.IsPresent,
            StopOnUserInput = !IgnoreUserInput.IsPresent,
            KeyDelayMilliseconds = KeyDelayMilliseconds,
            ModifierHoldMilliseconds = ModifierHoldMilliseconds,
            LineDelayMilliseconds = LineDelayMilliseconds,
            ChunkSize = ChunkSize,
            ChunkPauseMilliseconds = ChunkPauseMilliseconds
        };
    }

    private WindowInfo? ResolveSingleWindow() {
        IReadOnlyList<WindowInfo> windows = new DesktopAutomationService().GetWindows(new WindowQueryOptions {
            TitlePattern = Name,
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = false
        });
        if (windows.Count == 1) {
            return windows[0];
        }

        string problem = windows.Count == 0 ? "No visible window matches" : $"{windows.Count} visible windows match";
        WriteError(new ErrorRecord(
            new InvalidOperationException($"{problem} '{Name}'. Text is typed into exactly one window; narrow the title or pipe the window from Get-DesktopWindow."),
            "DesktopSessionTextWindowNotUnique",
            ErrorCategory.InvalidArgument,
            Name));
        return null;
    }

    private char[] ReadText() {
        if (Text != null) {
            return Text.ToCharArray();
        }

        SecureString secret = SecureString ?? Credential!.Password;
        IntPtr unmanaged = Marshal.SecureStringToGlobalAllocUnicode(secret);
        try {
            var characters = new char[secret.Length];
            Marshal.Copy(unmanaged, characters, 0, characters.Length);
            return characters;
        } finally {
            Marshal.ZeroFreeGlobalAllocUnicode(unmanaged);
        }
    }

    private void WarnWhenNotForeground(WindowInfo target) {
        IntPtr root = MonitorNativeMethods.GetAncestor(target.Handle, MonitorNativeMethods.GA_ROOT);
        IntPtr expected = root == IntPtr.Zero ? target.Handle : root;
        if (MonitorNativeMethods.GetForegroundWindow() != expected && ForegroundTimeoutSeconds > 0) {
            WriteWarning($"Click into '{target.Title}' within {ForegroundTimeoutSeconds} seconds; typing starts when it is the foreground window.");
        }
    }
}
