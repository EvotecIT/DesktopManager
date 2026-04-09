using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows.Forms.Integration;
using Microsoft.Web.WebView2.Core;
using WpfWebView2 = Microsoft.Web.WebView2.Wpf.WebView2;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsTextBox = System.Windows.Forms.TextBox;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace DesktopManager.TestApp;

internal sealed class MainForm : Form {
    private const int ForegroundHistoryLimit = 12;
    private const string DragPayloadText = "desktopmanager-drag-payload";
    private const string DefaultWebViewStatus = "WebView2 surface initializing.";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private readonly string _baseTitle;
    private readonly bool _useCommandBarSurface;
    private readonly bool _useWebViewSurface;
    private readonly string? _statusFilePath;
    private readonly string? _commandFilePath;
    private readonly WinFormsTextBox _editorTextBox;
    private readonly ElementHost _commandBarHost;
    private readonly WpfTextBox _commandBarTextBox;
    private readonly ElementHost _webViewHost;
    private readonly WpfWebView2 _webViewControl;
    private readonly WinFormsLabel _statusLabel;
    private readonly CheckBox _automationCheckBox;
    private readonly ComboBox _optionsComboBox;
    private readonly Button _applyButton;
    private readonly WinFormsLabel _basicControlsStatusLabel;
    private readonly ListBox _scrollListBox;
    private readonly WinFormsLabel _scrollStatusLabel;
    private readonly Panel _dragSourcePanel;
    private readonly WinFormsLabel _dragSourceLabel;
    private readonly Panel _dropTargetPanel;
    private readonly WinFormsLabel _dropTargetLabel;
    private SecondaryFocusForm? _secondaryForm;
    private System.Windows.Forms.Timer? _statusTimer;
    private DateTime _foregroundHoldUntilUtc;
    private string _foregroundHoldSurfaceName = "editor";
    private int _foregroundHoldRequestCount;
    private int _foregroundHoldRecoveryCount;
    private long _lastObservedForegroundHandle;
    private string _lastObservedForegroundTitle = string.Empty;
    private string _lastObservedForegroundClass = string.Empty;
    private string _lastObservedForegroundChangedUtc = string.Empty;
    private string _lastCommand = string.Empty;
    private Point _dragSourceMouseDownLocation = Point.Empty;
    private string _droppedText = string.Empty;
    private string _dragDropStatus = "Drag source ready.";
    private int _dragDropCount;
    private bool _webViewReady;
    private string _webViewStatusText = DefaultWebViewStatus;
    private string _webViewPromptText = string.Empty;
    private string _webViewDomStatusText = string.Empty;
    private string _webViewLastEvent = string.Empty;
    private readonly List<string> _foregroundHistory = [];

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public MainForm(TestAppOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        _baseTitle = options.Title;
        _useCommandBarSurface = string.Equals(options.Surface, "commandbar", StringComparison.OrdinalIgnoreCase);
        _useWebViewSurface = string.Equals(options.Surface, "webview", StringComparison.OrdinalIgnoreCase);
        _statusFilePath = options.StatusFilePath;
        _commandFilePath = options.CommandFilePath;
        Text = options.Title;
        Name = "DesktopManagerMcpTestApp";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 960;
        Height = 720;
        MinimumSize = new Size(640, 480);

        var titleLabel = new WinFormsLabel {
            AutoSize = true,
            Text = "DesktopManager MCP Test App",
            Font = new Font(SystemFonts.MessageBoxFont ?? Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };

        var hintLabel = new WinFormsLabel {
            AutoSize = true,
            Text = "This window is used by automated end-to-end tests.",
            Margin = new Padding(0, 0, 0, 12)
        };

        _statusLabel = new WinFormsLabel {
            AutoSize = true,
            Name = "StatusLabel",
            AccessibleName = "StatusLabel",
            Text = GetInitialStatusText(),
            Margin = new Padding(0, 0, 0, 12)
        };

        _editorTextBox = new WinFormsTextBox {
            Name = "EditorTextBox",
            AccessibleName = "Editor",
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            Text = options.InitialText
        };

        _commandBarTextBox = new WpfTextBox {
            Name = "CommandBarTextBox",
            Text = options.InitialText,
            MinWidth = 480,
            MinHeight = 30
        };
        _commandBarTextBox.KeyDown += CommandBarTextBox_KeyDown;

        var commandBarPanel = new System.Windows.Controls.StackPanel {
            Orientation = System.Windows.Controls.Orientation.Horizontal
        };
        commandBarPanel.Children.Add(new System.Windows.Controls.Label {
            Content = "Command",
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Padding = new System.Windows.Thickness(0, 0, 8, 0)
        });
        commandBarPanel.Children.Add(_commandBarTextBox);

        _commandBarHost = new ElementHost {
            Name = "CommandBarHost",
            Dock = DockStyle.Top,
            Height = 42,
            Child = commandBarPanel,
            Visible = _useCommandBarSurface
        };

        _webViewControl = new WpfWebView2 {
            Name = "WebViewSurface"
        };
        _webViewHost = new ElementHost {
            Name = "WebViewHost",
            Dock = DockStyle.Fill,
            Child = _webViewControl,
            Visible = _useWebViewSurface
        };

        _automationCheckBox = new CheckBox {
            Name = "AutomationCheckBox",
            AccessibleName = "AutomationCheckBox",
            AutoSize = true,
            Text = "Enable automation option",
            Checked = true,
            Margin = new Padding(0, 0, 16, 0)
        };
        _automationCheckBox.CheckedChanged += (_, _) => UpdateBasicControlsStatus(
            _automationCheckBox.Checked ? "Checkbox enabled." : "Checkbox disabled.");

        _optionsComboBox = new ComboBox {
            Name = "OptionsComboBox",
            AccessibleName = "OptionsComboBox",
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220,
            Margin = new Padding(0, 0, 16, 0)
        };
        _optionsComboBox.Items.AddRange([
            "Alpha",
            "Beta",
            "Gamma"
        ]);
        _optionsComboBox.SelectedIndex = 0;
        _optionsComboBox.SelectedIndexChanged += (_, _) => UpdateBasicControlsStatus(
            "Selected option: " + _optionsComboBox.Text);

        _applyButton = new Button {
            Name = "ApplyButton",
            AccessibleName = "ApplyButton",
            AutoSize = true,
            Text = "Apply",
            Margin = new Padding(0)
        };
        _applyButton.Click += (_, _) => {
            string mode = _automationCheckBox.Checked ? "enabled" : "disabled";
            string option = string.IsNullOrWhiteSpace(_optionsComboBox.Text) ? "<none>" : _optionsComboBox.Text;
            UpdateBasicControlsStatus("Applied option '" + option + "' with checkbox " + mode + ".");
        };

        _basicControlsStatusLabel = new WinFormsLabel {
            Name = "BasicControlsStatusLabel",
            AccessibleName = "BasicControlsStatusLabel",
            AutoSize = true,
            Text = "Basic controls ready.",
            Margin = new Padding(0, 8, 0, 12)
        };

        var basicControlsPanel = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        basicControlsPanel.Controls.Add(_automationCheckBox);
        basicControlsPanel.Controls.Add(_optionsComboBox);
        basicControlsPanel.Controls.Add(_applyButton);

        var scrollSurfaceLabel = new WinFormsLabel {
            Name = "ScrollSurfaceLabel",
            AccessibleName = "ScrollSurfaceLabel",
            AutoSize = true,
            Text = "Scroll Surface",
            Margin = new Padding(0, 0, 0, 6)
        };

        _scrollListBox = new ListBox {
            Name = "ScrollSurfaceListBox",
            AccessibleName = "ScrollSurface",
            IntegralHeight = false,
            Width = 452,
            Height = 132,
            Font = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Point),
            HorizontalScrollbar = false,
            Margin = new Padding(0, 0, 0, 6)
        };
        for (int index = 1; index <= 24; index++) {
            _scrollListBox.Items.Add($"Scroll Item {index:00} - generic verification lane");
        }

        _scrollStatusLabel = new WinFormsLabel {
            Name = "ScrollStatusLabel",
            AccessibleName = "ScrollStatusLabel",
            AutoSize = true,
            Text = string.Empty,
            Margin = new Padding(0, 0, 0, 12)
        };
        _scrollListBox.MouseWheel += (_, _) => BeginInvoke((Action)UpdateScrollStatus);
        _scrollListBox.SelectedIndexChanged += (_, _) => UpdateScrollStatus();
        UpdateScrollStatus();

        var scrollPanel = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12)
        };
        scrollPanel.Controls.Add(scrollSurfaceLabel);
        scrollPanel.Controls.Add(_scrollListBox);
        scrollPanel.Controls.Add(_scrollStatusLabel);

        var closeButton = new Button {
            Name = "CloseButton",
            Text = "Close",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        closeButton.Click += (_, _) => Close();

        _dragSourceLabel = new WinFormsLabel {
            Name = "DragSourceLabel",
            AccessibleName = "DragSource",
            AutoSize = false,
            Text = "Drag Source\r\nHold and drag me",
            TextAlign = ContentAlignment.MiddleCenter,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(229, 240, 255),
            Dock = DockStyle.Fill,
            Enabled = false
        };

        _dragSourcePanel = new Panel {
            Name = "DragSourcePanel",
            AccessibleName = "DragSource",
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(229, 240, 255),
            Margin = new Padding(0, 0, 12, 0),
            Width = 220,
            Height = 88
        };
        _dragSourcePanel.Controls.Add(_dragSourceLabel);
        _dragSourcePanel.MouseDown += DragSourceLabel_MouseDown;
        _dragSourcePanel.MouseMove += DragSourceLabel_MouseMove;
        _dragSourcePanel.MouseUp += DragSourceLabel_MouseUp;

        _dropTargetLabel = new WinFormsLabel {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Drop Target\r\nAwaiting payload",
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(239, 245, 231),
            Enabled = false
        };

        _dropTargetPanel = new Panel {
            Name = "DropTargetPanel",
            AccessibleName = "DropTarget",
            AllowDrop = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(239, 245, 231),
            Margin = new Padding(0),
            Width = 220,
            Height = 88
        };
        _dropTargetPanel.Controls.Add(_dropTargetLabel);
        _dropTargetPanel.DragEnter += DropTargetPanel_DragEnter;
        _dropTargetPanel.DragLeave += DropTargetPanel_DragLeave;
        _dropTargetPanel.DragDrop += DropTargetPanel_DragDrop;

        var dragDropPanel = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12)
        };
        dragDropPanel.Controls.Add(_dragSourcePanel);
        dragDropPanel.Controls.Add(_dropTargetPanel);

        var buttonPanel = new FlowLayoutPanel {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };
        buttonPanel.Controls.Add(closeButton);

        var surfacePanel = new Panel {
            Dock = DockStyle.Fill
        };
        surfacePanel.Controls.Add(_editorTextBox);
        surfacePanel.Controls.Add(_webViewHost);
        _editorTextBox.Visible = !_useWebViewSurface;
        _webViewHost.Visible = _useWebViewSurface;

        var contentPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(16)
        };
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        contentPanel.Controls.Add(titleLabel, 0, 0);
        contentPanel.Controls.Add(hintLabel, 0, 1);
        contentPanel.Controls.Add(_statusLabel, 0, 2);
        contentPanel.Controls.Add(basicControlsPanel, 0, 3);
        contentPanel.Controls.Add(_basicControlsStatusLabel, 0, 4);
        contentPanel.Controls.Add(dragDropPanel, 0, 5);
        contentPanel.Controls.Add(scrollPanel, 0, 6);
        contentPanel.Controls.Add(surfacePanel, 0, 7);

        Controls.Add(_commandBarHost);
        Controls.Add(contentPanel);
        Controls.Add(buttonPanel);

        _editorTextBox.TextChanged += (_, _) => WriteStatusSnapshot();
        _commandBarTextBox.TextChanged += (_, _) => WriteStatusSnapshot();
        Activated += (_, _) => WriteStatusSnapshot();
        Deactivate += (_, _) => WriteStatusSnapshot();
        Shown += async (_, _) => {
            await InitializeOptionalWebViewSurfaceAsync(options.InitialText);
            FocusSurface(GetRequestedSurfaceName());

            var activationTimer = new System.Windows.Forms.Timer {
                Interval = 250
            };
            int activationAttempts = 0;
            activationTimer.Tick += (_, _) => {
                activationAttempts++;
                FocusSurface(GetRequestedSurfaceName());
                if (ContainsFocus || activationAttempts >= 4) {
                    activationTimer.Stop();
                    activationTimer.Dispose();
                }
            };
            activationTimer.Start();
            StartStatusChannel();
            WriteStatusSnapshot();
        };
        FormClosed += (_, _) => {
            _statusTimer?.Stop();
            _statusTimer?.Dispose();
            _statusTimer = null;
            WriteStatusSnapshot();
        };
    }

    private void CommandBarTextBox_KeyDown(object? sender, WpfKeyEventArgs e) {
        if (e.Key != WpfKey.Return) {
            return;
        }

        string command = _commandBarTextBox.Text.Trim();
        _statusLabel.Text = string.IsNullOrWhiteSpace(command)
            ? "Accepted empty command."
            : "Accepted command: " + command;
        Text = string.IsNullOrWhiteSpace(command)
            ? _baseTitle + " - Accepted"
            : _baseTitle + " - Accepted - " + command;
        e.Handled = true;
    }

    private void FocusSurface(string surfaceName) {
        TopMost = true;
        BringToFront();
        Activate();
        BringWindowToTop(Handle);
        SetForegroundWindow(Handle);
        if (!IsForegroundHoldActive()) {
            TopMost = false;
        }

        if (string.Equals(surfaceName, "webview", StringComparison.OrdinalIgnoreCase)) {
            _webViewHost.Focus();
            _webViewControl.Focus();
            WriteStatusSnapshot();
            return;
        }

        if (string.Equals(surfaceName, "commandbar", StringComparison.OrdinalIgnoreCase)) {
            _commandBarTextBox.Focus();
            _commandBarTextBox.Select(_commandBarTextBox.Text.Length, 0);
            WriteStatusSnapshot();
            return;
        }

        _editorTextBox.Focus();
        _editorTextBox.SelectionStart = _editorTextBox.TextLength;
        _editorTextBox.SelectionLength = 0;
        WriteStatusSnapshot();
    }

    private void StartStatusChannel() {
        if (string.IsNullOrWhiteSpace(_statusFilePath) && string.IsNullOrWhiteSpace(_commandFilePath)) {
            return;
        }

        _statusTimer = new System.Windows.Forms.Timer {
            Interval = 100
        };
        _statusTimer.Tick += (_, _) => {
            ProcessCommandFile();
            MaintainForegroundHold();
            WriteStatusSnapshot();
        };
        _statusTimer.Start();
    }

    private void ProcessCommandFile() {
        if (string.IsNullOrWhiteSpace(_commandFilePath) || !File.Exists(_commandFilePath)) {
            return;
        }

        string command;
        try {
            command = File.ReadAllText(_commandFilePath).Trim();
            File.Delete(_commandFilePath);
        } catch {
            return;
        }

        _lastCommand = command;
        AddForegroundHistoryEntry("command", command);

        if (string.Equals(command, "focus-editor", StringComparison.OrdinalIgnoreCase)) {
            FocusSurface("editor");
            return;
        }

        if (string.Equals(command, "focus-commandbar", StringComparison.OrdinalIgnoreCase)) {
            FocusSurface("commandbar");
            return;
        }

        if (string.Equals(command, "focus-webview", StringComparison.OrdinalIgnoreCase)) {
            FocusSurface("webview");
            return;
        }

        if (string.Equals(command, "focus-secondary", StringComparison.OrdinalIgnoreCase)) {
            EnsureSecondaryWindow();
            _secondaryForm?.FocusSecondaryWindow();
            return;
        }

        if (command.StartsWith("hold-editor-foreground:", StringComparison.OrdinalIgnoreCase)) {
            if (TryParseDuration(command, "hold-editor-foreground:", out int editorDurationMilliseconds)) {
                StartForegroundHold("editor", editorDurationMilliseconds);
            }

            return;
        }

        if (command.StartsWith("hold-commandbar-foreground:", StringComparison.OrdinalIgnoreCase)) {
            if (TryParseDuration(command, "hold-commandbar-foreground:", out int commandBarDurationMilliseconds)) {
                StartForegroundHold("commandbar", commandBarDurationMilliseconds);
            }

            return;
        }

        if (command.StartsWith("hold-webview-foreground:", StringComparison.OrdinalIgnoreCase)) {
            if (TryParseDuration(command, "hold-webview-foreground:", out int webViewDurationMilliseconds)) {
                StartForegroundHold("webview", webViewDurationMilliseconds);
            }

            return;
        }

        if (string.Equals(command, "stop-foreground-hold", StringComparison.OrdinalIgnoreCase)) {
            StopForegroundHold();
        }
    }

    private void WriteStatusSnapshot() {
        if (string.IsNullOrWhiteSpace(_statusFilePath)) {
            return;
        }

        try {
            UpdateForegroundDiagnostics();
            string? directory = Path.GetDirectoryName(_statusFilePath);
            if (!string.IsNullOrWhiteSpace(directory)) {
                Directory.CreateDirectory(directory);
            }

            var snapshot = new TestAppStatusSnapshot {
                ProcessId = Environment.ProcessId,
                WindowHandle = Handle.ToInt64(),
                EditorHandle = _editorTextBox.IsHandleCreated ? _editorTextBox.Handle.ToInt64() : 0,
                SecondaryWindowHandle = _secondaryForm != null && !_secondaryForm.IsDisposed && _secondaryForm.IsHandleCreated ? _secondaryForm.Handle.ToInt64() : 0,
                WindowTitle = Text,
                ActiveSurface = GetActiveSurface(),
                ContainsFocus = ContainsFocus,
                IsForegroundWindow = GetForegroundWindow() == Handle,
                SecondaryIsForegroundWindow = _secondaryForm != null && !_secondaryForm.IsDisposed && _secondaryForm.IsHandleCreated && GetForegroundWindow() == _secondaryForm.Handle,
                ForegroundHoldActive = IsForegroundHoldActive(),
                ForegroundHoldSurface = _foregroundHoldSurfaceName,
                ForegroundHoldRequestCount = _foregroundHoldRequestCount,
                ForegroundHoldRecoveryCount = _foregroundHoldRecoveryCount,
                LastObservedForegroundHandle = _lastObservedForegroundHandle,
                LastObservedForegroundTitle = _lastObservedForegroundTitle,
                LastObservedForegroundClass = _lastObservedForegroundClass,
                LastObservedForegroundChangedUtc = _lastObservedForegroundChangedUtc,
                LastCommand = _lastCommand,
                ForegroundHistory = new List<string>(_foregroundHistory),
                EditorText = _editorTextBox.Text,
                SecondaryText = _secondaryForm != null && !_secondaryForm.IsDisposed ? _secondaryForm.CurrentText : string.Empty,
                CommandBarText = _commandBarTextBox.Text,
                CommandBarHostHandle = _commandBarHost.IsHandleCreated ? _commandBarHost.Handle.ToInt64() : 0,
                WebViewReady = _webViewReady,
                WebViewStatusText = _webViewStatusText,
                WebViewPromptText = _webViewPromptText,
                WebViewDomStatusText = _webViewDomStatusText,
                WebViewLastEvent = _webViewLastEvent,
                WebViewHostHandle = _webViewHost.IsHandleCreated ? _webViewHost.Handle.ToInt64() : 0,
                StatusText = _statusLabel.Text,
                AutomationCheckBoxChecked = _automationCheckBox.Checked,
                AutomationCheckBoxHandle = _automationCheckBox.IsHandleCreated ? _automationCheckBox.Handle.ToInt64() : 0,
                SelectedOption = _optionsComboBox.Text,
                OptionsComboBoxHandle = _optionsComboBox.IsHandleCreated ? _optionsComboBox.Handle.ToInt64() : 0,
                BasicActionStatus = _basicControlsStatusLabel.Text,
                ApplyButtonHandle = _applyButton.IsHandleCreated ? _applyButton.Handle.ToInt64() : 0,
                ScrollListHandle = _scrollListBox.IsHandleCreated ? _scrollListBox.Handle.ToInt64() : 0,
                ScrollTopIndex = GetScrollTopIndex(),
                ScrollTopItemText = GetScrollTopItemText(),
                ScrollStatusText = _scrollStatusLabel.Text,
                DragPayload = DragPayloadText,
                DroppedText = _droppedText,
                DragDropCount = _dragDropCount,
                DragDropStatus = _dragDropStatus,
                EditorBounds = GetScreenBounds(_editorTextBox),
                CommandBarHostBounds = GetScreenBounds(_commandBarHost),
                WebViewHostBounds = GetScreenBounds(_webViewHost),
                WebViewClientBounds = GetClientRelativeBounds(_webViewHost),
                AutomationCheckBoxBounds = GetScreenBounds(_automationCheckBox),
                OptionsComboBoxBounds = GetScreenBounds(_optionsComboBox),
                ApplyButtonBounds = GetScreenBounds(_applyButton),
                ScrollListBounds = GetScreenBounds(_scrollListBox),
                DragSourceBounds = GetScreenBounds(_dragSourcePanel),
                DropTargetBounds = GetScreenBounds(_dropTargetPanel)
            };

            string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions {
                WriteIndented = true
            });
            File.WriteAllText(_statusFilePath, json);
        } catch {
            // Best-effort diagnostics only.
        }
    }

    private string GetActiveSurface() {
        if (_secondaryForm != null && !_secondaryForm.IsDisposed && _secondaryForm.ContainsFocus) {
            return "secondary";
        }

        if (_webViewHost.Visible && (_webViewControl.IsKeyboardFocusWithin || _webViewHost.Focused)) {
            return "webview";
        }

        if (_commandBarHost.Visible && _commandBarTextBox.IsKeyboardFocused) {
            return "commandbar";
        }

        if (_editorTextBox.Focused) {
            return "editor";
        }

        return GetRequestedSurfaceName();
    }

    private int GetScrollTopIndex() {
        return _scrollListBox.Items.Count == 0 ? -1 : _scrollListBox.TopIndex;
    }

    private string GetScrollTopItemText() {
        int topIndex = GetScrollTopIndex();
        if (topIndex < 0 || topIndex >= _scrollListBox.Items.Count) {
            return string.Empty;
        }

        return Convert.ToString(_scrollListBox.Items[topIndex]) ?? string.Empty;
    }

    private void UpdateScrollStatus() {
        int topIndex = GetScrollTopIndex();
        string topItem = GetScrollTopItemText();
        _scrollStatusLabel.Text = topIndex < 0
            ? "Scroll surface ready."
            : $"Scroll surface ready. Top item: {topItem} (index {topIndex}).";
    }

    private void EnsureSecondaryWindow() {
        if (_secondaryForm != null && !_secondaryForm.IsDisposed) {
            return;
        }

        _secondaryForm = new SecondaryFocusForm(_baseTitle, WriteStatusSnapshot);
        _secondaryForm.Show(this);
    }

    private void MaintainForegroundHold() {
        if (!IsForegroundHoldActive()) {
            if (TopMost) {
                StopForegroundHold();
            }

            return;
        }

        bool needsFocus = !IsRequestedSurfaceFocused(_foregroundHoldSurfaceName);
        if (!needsFocus) {
            return;
        }

        _foregroundHoldRecoveryCount++;
        AddForegroundHistoryEntry("hold-recover", _foregroundHoldSurfaceName);
        FocusSurface(_foregroundHoldSurfaceName);
    }

    private void StartForegroundHold(string surfaceName, int durationMilliseconds) {
        if (durationMilliseconds <= 0) {
            return;
        }

        _foregroundHoldSurfaceName = surfaceName;
        _foregroundHoldUntilUtc = DateTime.UtcNow.AddMilliseconds(durationMilliseconds);
        _foregroundHoldRequestCount++;
        AddForegroundHistoryEntry("hold-start", _foregroundHoldSurfaceName + " durationMs=" + durationMilliseconds);
        TopMost = true;
        FocusSurface(surfaceName);
    }

    private bool IsForegroundHoldActive() {
        return _foregroundHoldUntilUtc > DateTime.UtcNow;
    }

    private void StopForegroundHold() {
        if (_foregroundHoldUntilUtc != DateTime.MinValue) {
            AddForegroundHistoryEntry("hold-stop", _foregroundHoldSurfaceName);
        }

        _foregroundHoldUntilUtc = DateTime.MinValue;
        TopMost = false;
    }

    private string GetInitialStatusText() {
        if (_useWebViewSurface) {
            return DefaultWebViewStatus;
        }

        return _useCommandBarSurface
            ? "Type a value into the command bar and press Enter."
            : "Editor surface ready.";
    }

    private string GetRequestedSurfaceName() {
        if (_useWebViewSurface) {
            return "webview";
        }

        return _useCommandBarSurface ? "commandbar" : "editor";
    }

    private bool IsRequestedSurfaceFocused(string surfaceName) {
        if (GetForegroundWindow() != Handle) {
            return false;
        }

        if (string.Equals(surfaceName, "webview", StringComparison.OrdinalIgnoreCase)) {
            return _webViewHost.Visible && (_webViewControl.IsKeyboardFocusWithin || _webViewHost.Focused);
        }

        if (string.Equals(surfaceName, "commandbar", StringComparison.OrdinalIgnoreCase)) {
            return _commandBarHost.Visible && _commandBarTextBox.IsKeyboardFocused;
        }

        return _editorTextBox.Focused;
    }

    private async Task InitializeOptionalWebViewSurfaceAsync(string initialText) {
        if (!_useWebViewSurface) {
            return;
        }

        try {
            await _webViewControl.EnsureCoreWebView2Async();
            _webViewControl.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            _webViewControl.NavigationCompleted += (_, _) => {
                _webViewReady = true;
                _webViewStatusText = "WebView2 surface ready.";
                _statusLabel.Text = _webViewStatusText;
                WriteStatusSnapshot();
            };
            _webViewControl.CoreWebView2.NavigateToString(BuildWebViewMarkup(initialText));
        } catch (Exception ex) {
            _webViewReady = false;
            _webViewStatusText = "WebView2 initialization failed: " + ex.GetType().Name;
            _statusLabel.Text = _webViewStatusText;
            AddForegroundHistoryEntry("webview-error", ex.Message);
            WriteStatusSnapshot();
        }
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
        try {
            using JsonDocument document = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) {
                return;
            }

            _webViewPromptText = ReadJsonString(root, "prompt");
            _webViewDomStatusText = ReadJsonString(root, "status");
            _webViewLastEvent = ReadJsonString(root, "reason");
            WriteStatusSnapshot();
        } catch {
            // Best-effort diagnostics only.
        }
    }

    private static string ReadJsonString(JsonElement element, string propertyName) {
        return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string BuildWebViewMarkup(string initialText) {
        string encodedInitialText = System.Net.WebUtility.HtmlEncode(initialText);
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <title>DesktopManager WebView Surface</title>
  <style>
    body {{
      margin: 0;
      font-family: Segoe UI, Arial, sans-serif;
      background: linear-gradient(180deg, #ffffff 0%, #f3f7fb 100%);
      color: #142033;
    }}
    .shell {{
      padding: 18px;
    }}
    h1 {{
      margin: 0 0 10px 0;
      font-size: 22px;
    }}
    p {{
      margin: 0 0 12px 0;
      color: #405266;
    }}
    textarea {{
      width: 100%;
      min-height: 140px;
      padding: 12px;
      border: 1px solid #7d97b4;
      border-radius: 8px;
      font: 15px/1.4 Consolas, monospace;
      box-sizing: border-box;
      resize: vertical;
      background: #ffffff;
      color: #102030;
    }}
    .toolbar {{
      margin-top: 12px;
      display: flex;
      gap: 12px;
      align-items: center;
    }}
    button {{
      border: 0;
      border-radius: 999px;
      padding: 10px 16px;
      background: #1155cc;
      color: white;
      font-weight: 600;
      cursor: pointer;
    }}
    #status {{
      font-size: 14px;
      color: #21425f;
    }}
  </style>
</head>
<body>
  <div class=""shell"">
    <h1>WebView2 Surface</h1>
    <p>This hosted browser surface is used for DesktopManager capture and automation certification.</p>
    <textarea id=""prompt"">{encodedInitialText}</textarea>
    <div class=""toolbar"">
      <button id=""send"">Send</button>
      <div id=""status"">WebView2 content ready.</div>
    </div>
  </div>
  <script>
    const prompt = document.getElementById('prompt');
    const status = document.getElementById('status');
    const publishState = (reason) => {{
      if (window.chrome && window.chrome.webview) {{
        window.chrome.webview.postMessage({{
          reason,
          prompt: prompt.value,
          status: status.textContent || ''
        }});
      }}
    }};
    prompt.addEventListener('input', () => publishState('prompt-input'));
    document.getElementById('send').addEventListener('click', () => {{
      const value = prompt.value.trim();
      status.textContent = value.length === 0 ? 'Sent empty prompt.' : 'Sent prompt: ' + value;
      publishState('send-click');
    }});
    window.addEventListener('load', () => publishState('page-load'));
    setTimeout(() => publishState('initial-state'), 0);
  </script>
</body>
</html>";
    }

    private void UpdateForegroundDiagnostics() {
        IntPtr foregroundHandle = GetForegroundWindow();
        long handleValue = foregroundHandle.ToInt64();
        if (_lastObservedForegroundHandle == handleValue) {
            return;
        }

        _lastObservedForegroundHandle = handleValue;
        _lastObservedForegroundTitle = ReadWindowText(foregroundHandle);
        _lastObservedForegroundClass = ReadWindowClassName(foregroundHandle);
        _lastObservedForegroundChangedUtc = DateTime.UtcNow.ToString("O");
        AddForegroundHistoryEntry(
            "foreground",
            $"0x{_lastObservedForegroundHandle:X} '{_lastObservedForegroundTitle}' class='{_lastObservedForegroundClass}'");
    }

    private static string ReadWindowText(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        var builder = new StringBuilder(512);
        return GetWindowText(handle, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
    }

    private static string ReadWindowClassName(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        var builder = new StringBuilder(256);
        return GetClassName(handle, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
    }

    private static bool TryParseDuration(string command, string prefix, out int durationMilliseconds) {
        durationMilliseconds = 0;
        if (!command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        string rawDuration = command.Substring(prefix.Length).Trim();
        return int.TryParse(rawDuration, out durationMilliseconds) && durationMilliseconds > 0;
    }

    private void AddForegroundHistoryEntry(string category, string detail) {
        string entry = DateTime.UtcNow.ToString("O") + " [" + category + "] " + detail;
        _foregroundHistory.Add(entry);
        if (_foregroundHistory.Count > ForegroundHistoryLimit) {
            _foregroundHistory.RemoveAt(0);
        }
    }

    private void UpdateBasicControlsStatus(string text) {
        _basicControlsStatusLabel.Text = text;
        WriteStatusSnapshot();
    }

    private void DragSourceLabel_MouseDown(object? sender, MouseEventArgs e) {
        if (e.Button != MouseButtons.Left) {
            return;
        }

        _dragSourceMouseDownLocation = e.Location;
        _dragSourcePanel.Capture = true;
        _dragDropStatus = "Drag armed from source.";
        WriteStatusSnapshot();
    }

    private void DragSourceLabel_MouseMove(object? sender, MouseEventArgs e) {
        if ((e.Button & MouseButtons.Left) != MouseButtons.Left) {
            return;
        }

        Size dragSize = SystemInformation.DragSize;
        Rectangle dragBounds = new(
            _dragSourceMouseDownLocation.X - dragSize.Width / 2,
            _dragSourceMouseDownLocation.Y - dragSize.Height / 2,
            dragSize.Width,
            dragSize.Height);
        if (dragBounds.Contains(e.Location)) {
            return;
        }

        _dragDropStatus = "Dragging payload.";
        WriteStatusSnapshot();
        _dragSourcePanel.Capture = false;
        DragDropEffects effect = _dragSourcePanel.DoDragDrop(DragPayloadText, DragDropEffects.Copy);
        _dragDropStatus = effect == DragDropEffects.None ? "Drag canceled." : "Drag completed.";
        WriteStatusSnapshot();
    }

    private void DragSourceLabel_MouseUp(object? sender, MouseEventArgs e) {
        _dragSourcePanel.Capture = false;
    }

    private void DropTargetPanel_DragEnter(object? sender, DragEventArgs e) {
        if (e.Data?.GetDataPresent(DataFormats.UnicodeText) == true || e.Data?.GetDataPresent(DataFormats.Text) == true) {
            e.Effect = DragDropEffects.Copy;
            _dragDropStatus = "Drop target armed.";
            _dropTargetPanel.BackColor = Color.FromArgb(214, 235, 204);
            _dropTargetLabel.BackColor = _dropTargetPanel.BackColor;
            WriteStatusSnapshot();
            return;
        }

        e.Effect = DragDropEffects.None;
    }

    private void DropTargetPanel_DragLeave(object? sender, EventArgs e) {
        _dropTargetPanel.BackColor = Color.FromArgb(239, 245, 231);
        _dropTargetLabel.BackColor = _dropTargetPanel.BackColor;
        _dragDropStatus = "Drag left drop target.";
        WriteStatusSnapshot();
    }

    private void DropTargetPanel_DragDrop(object? sender, DragEventArgs e) {
        string droppedText = e.Data?.GetData(DataFormats.UnicodeText)?.ToString()
            ?? e.Data?.GetData(DataFormats.Text)?.ToString()
            ?? string.Empty;
        _droppedText = droppedText;
        _dragDropCount++;
        _dragDropStatus = string.IsNullOrWhiteSpace(droppedText) ? "Drop completed with empty payload." : "Drop completed.";
        _dropTargetPanel.BackColor = Color.FromArgb(198, 227, 184);
        _dropTargetLabel.BackColor = _dropTargetPanel.BackColor;
        _dropTargetLabel.Text = string.IsNullOrWhiteSpace(droppedText)
            ? "Drop Target\r\nReceived empty payload"
            : "Drop Target\r\nReceived: " + droppedText;
        _statusLabel.Text = string.IsNullOrWhiteSpace(droppedText)
            ? "Drop completed with empty payload."
            : "Dropped payload: " + droppedText;
        WriteStatusSnapshot();
    }

    private static TestAppControlBounds GetScreenBounds(Control control) {
        if (!control.IsHandleCreated) {
            return new TestAppControlBounds();
        }

        if (control is ElementHost && control.Parent != null) {
            Rectangle hostedBounds = control.Parent.RectangleToScreen(control.Bounds);
            return new TestAppControlBounds {
                Left = hostedBounds.Left,
                Top = hostedBounds.Top,
                Width = hostedBounds.Width,
                Height = hostedBounds.Height
            };
        }

        if (GetWindowRect(control.Handle, out RECT rect)) {
            return new TestAppControlBounds {
                Left = rect.Left,
                Top = rect.Top,
                Width = Math.Max(0, rect.Right - rect.Left),
                Height = Math.Max(0, rect.Bottom - rect.Top)
            };
        }

        Rectangle screenBounds = control.RectangleToScreen(control.ClientRectangle);
        return new TestAppControlBounds {
            Left = screenBounds.Left,
            Top = screenBounds.Top,
            Width = screenBounds.Width,
            Height = screenBounds.Height
        };
    }

    private static TestAppControlBounds GetClientRelativeBounds(Control control) {
        if (!control.IsHandleCreated) {
            return new TestAppControlBounds();
        }

        int left = 0;
        int top = 0;
        Control? current = control;
        while (current != null && current.Parent != null) {
            left += current.Left;
            top += current.Top;
            current = current.Parent;
            if (current is Form) {
                break;
            }
        }

        return new TestAppControlBounds {
            Left = left,
            Top = top,
            Width = control.Width,
            Height = control.Height
        };
    }
}
