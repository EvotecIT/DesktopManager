using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Sends text to a desktop window.</summary>
    /// <para type="synopsis">Sends text to a desktop window using either typing or paste method.</para>
    /// <para type="description">Sends text to a window on the desktop. You can identify the window by its title (supports wildcards). You can choose between typing the text or pasting it.</para>
    /// <example>
    ///   <para>Type text into a specific window</para>
    ///   <code>Send-DesktopText -Name "Notepad" -Text "Hello World"</code>
    /// </example>
    /// <example>
    ///   <para>Paste text into a specific window</para>
    ///   <code>Send-DesktopText -Name "Notepad" -Text "Hello World" -Method Paste</code>
    /// </example>
    /// <example>
    ///   <para>Send text from clipboard to a window</para>
    ///   <code>Get-Clipboard | Send-DesktopText -Name "Notepad"</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Send, "DesktopText", SupportsShouldProcess = true)]
    public class CmdletSendDesktopText : PSCmdlet {
        /// <summary>
        /// <para type="description">The title of the window to send text to. Supports wildcards.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The text to send to the window. If not specified, text will be read from the pipeline.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 1, ValueFromPipeline = true)]
        public string Text { get; set; }

        /// <summary>
        /// <para type="description">The method to use for sending text (Type or Paste). Default is Type.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public TextSendMethod Method { get; set; } = TextSendMethod.Type;

        private string _pipelineText;

        /// <summary>
        /// Begin processing
        /// </summary>
        protected override void BeginProcessing() {
            _pipelineText = string.Empty;
        }

        /// <summary>
        /// Process each record from the pipeline
        /// </summary>
        protected override void ProcessRecord() {
            if (Text != null) {
                _pipelineText += Text;
            }
        }

        /// <summary>
        /// End processing
        /// </summary>
        protected override void EndProcessing() {
            var manager = new WindowManager();
            var windows = manager.GetWindows(Name);

            WriteDebug($"Found {windows.Count} windows matching pattern '{Name}'");
            foreach (var window in windows) {
                WriteDebug($"Window: Title='{window.Title}', Handle={window.Handle}");
            }

            // If no text was specified in parameters, use pipeline text
            if (string.IsNullOrEmpty(Text)) {
                Text = _pipelineText;
            }

            if (string.IsNullOrEmpty(Text)) {
                WriteWarning("No text was provided to send to the window.");
                return;
            }

            foreach (var window in windows) {
                if (ShouldProcess($"Window '{window.Title}'", $"Send text using {Method} method")) {
                    try {
                        WriteDebug($"Attempting to send text to window '{window.Title}' using {Method} method");
                        manager.SendText(window, Text, Method);
                        WriteDebug("Text sent successfully");
                    } catch (Exception ex) {
                        WriteWarning($"Failed to send text to window '{window.Title}': {ex.Message}");
                        WriteDebug($"Exception details: {ex}");
                    }
                }
            }
        }
    }
}