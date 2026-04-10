using System.Text.Json;

namespace DesktopManager.WinUiHarness;

public sealed class MainPage : ContentPage {
    private readonly WinUiHarnessOptions _options;
    private readonly Label _statusLabel;
    private readonly Editor _editorInput;
    private readonly CheckBox _automationCheckBox;
    private readonly Picker _optionsPicker;
    private readonly Button _applyButton;
    private readonly Label _actionStatusLabel;
    private bool _statusLoopStarted;

    public MainPage(WinUiHarnessOptions options) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Title = "DesktopManager WinUI Harness";

        _statusLabel = new Label {
            AutomationId = "ModernStatusLabel",
            Text = "Modern WinUI harness ready."
        };
        _editorInput = new Editor {
            AutomationId = "ModernEditor",
            HeightRequest = 140,
            Placeholder = "Type a message",
            Text = _options.InitialText
        };
        _automationCheckBox = new CheckBox {
            AutomationId = "ModernCheckBox",
            IsChecked = true
        };
        _optionsPicker = new Picker {
            AutomationId = "ModernPicker",
            Title = "Choose an option"
        };
        _optionsPicker.Items.Add("Alpha");
        _optionsPicker.Items.Add("Beta");
        _optionsPicker.Items.Add("Gamma");
        _optionsPicker.SelectedIndex = 0;
        _applyButton = new Button {
            AutomationId = "ModernApplyButton",
            Text = "Apply"
        };
        _actionStatusLabel = new Label {
            AutomationId = "ModernActionStatusLabel",
            Text = "Modern controls ready."
        };

        _editorInput.TextChanged += EditorInput_TextChanged;
        _automationCheckBox.CheckedChanged += AutomationCheckBox_CheckedChanged;
        _optionsPicker.SelectedIndexChanged += OptionsPicker_SelectedIndexChanged;
        _applyButton.Clicked += ApplyButton_Clicked;
        Loaded += MainPage_Loaded;

        Content = new ScrollView {
            Content = new VerticalStackLayout {
                Padding = 24,
                Spacing = 16,
                Children = {
                    new Label {
                        AutomationId = "ModernHeaderLabel",
                        Text = "DesktopManager WinUI Harness",
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 24
                    },
                    _statusLabel,
                    _editorInput,
                    new HorizontalStackLayout {
                        Spacing = 12,
                        Children = {
                            _automationCheckBox,
                            new Label {
                                Text = "Enable modern automation option",
                                VerticalTextAlignment = TextAlignment.Center
                            }
                        }
                    },
                    _optionsPicker,
                    _applyButton,
                    _actionStatusLabel
                }
            }
        };
    }

    private void MainPage_Loaded(object? sender, EventArgs e) {
        if (_statusLoopStarted) {
            return;
        }

        _statusLoopStarted = true;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () => {
            WriteStatusSnapshot();
            return true;
        });
        WriteStatusSnapshot();
    }

    private void EditorInput_TextChanged(object? sender, TextChangedEventArgs e) {
        _statusLabel.Text = string.IsNullOrWhiteSpace(_editorInput.Text)
            ? "Modern editor cleared."
            : "Modern editor updated.";
        WriteStatusSnapshot();
    }

    private void AutomationCheckBox_CheckedChanged(object? sender, CheckedChangedEventArgs e) {
        _statusLabel.Text = e.Value ? "Modern checkbox enabled." : "Modern checkbox disabled.";
        WriteStatusSnapshot();
    }

    private void OptionsPicker_SelectedIndexChanged(object? sender, EventArgs e) {
        _statusLabel.Text = "Selected option: " + SelectedOption;
        WriteStatusSnapshot();
    }

    private void ApplyButton_Clicked(object? sender, EventArgs e) {
        string mode = _automationCheckBox.IsChecked ? "enabled" : "disabled";
        _actionStatusLabel.Text = "Applied option '" + SelectedOption + "' with checkbox " + mode + ".";
        _statusLabel.Text = "Modern apply button invoked.";
        WriteStatusSnapshot();
    }

    private string SelectedOption => _optionsPicker.SelectedIndex >= 0 && _optionsPicker.SelectedIndex < _optionsPicker.Items.Count
        ? _optionsPicker.Items[_optionsPicker.SelectedIndex]
        : string.Empty;

    private void WriteStatusSnapshot() {
        if (string.IsNullOrWhiteSpace(_options.StatusFilePath)) {
            return;
        }

        try {
            string? directory = Path.GetDirectoryName(_options.StatusFilePath);
            if (!string.IsNullOrWhiteSpace(directory)) {
                Directory.CreateDirectory(directory);
            }

            var snapshot = new WinUiHarnessStatusSnapshot {
                ProcessId = Environment.ProcessId,
                WindowTitle = Window?.Title ?? _options.Title,
                StatusText = _statusLabel.Text ?? string.Empty,
                EditorText = _editorInput.Text ?? string.Empty,
                AutomationCheckBoxChecked = _automationCheckBox.IsChecked,
                SelectedOption = SelectedOption,
                ActionStatus = _actionStatusLabel.Text ?? string.Empty
            };

            string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions {
                WriteIndented = true
            });
            string tempPath = _options.StatusFilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Copy(tempPath, _options.StatusFilePath, overwrite: true);
            File.Delete(tempPath);
        } catch {
            // Best-effort diagnostics only.
        }
    }
}
