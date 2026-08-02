using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Threading;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
/// <summary>
/// Proves semantic text operations against same-process WPF providers while their dispatcher remains responsive.
/// </summary>
public class DesktopWpfControlObservationTests {
    [TestMethod]
    public void DesktopAutomationService_WpfSelectionPattern_ReturnsSelectedItem() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using var harness = new PumpingWpfHarness();
        var automation = new DesktopAutomationService();
        DesktopControlObservation observation = harness.Invoke(() => automation.ObserveControls(
                CreateWindowQuery(harness.WindowHandle),
                new WindowControlQueryOptions {
                    AutomationIdPattern = PumpingWpfHarness.SelectionAutomationId,
                    ControlTypePattern = "List",
                    UseUiAutomation = true,
                    IncludeUiAutomation = true
                },
                new DesktopControlObservationOptions { IncludeSemanticState = true },
                allWindows: false,
                allControls: false)
            .Single());

        Assert.AreEqual(false, observation.IsPassword);
        CollectionAssert.Contains(observation.Selection.Items.ToArray(), "Beta");
    }

    [TestMethod]
    public void DesktopAutomationService_WpfRichTextSelectionCaretAndPassword_RoundTripSafely() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();
        using var harness = new PumpingWpfHarness();
        var automation = new DesktopAutomationService();
        WindowQueryOptions windowQuery = CreateWindowQuery(harness.WindowHandle);
        var options = new DesktopControlObservationOptions {
            MaxTextLength = 4096,
            IncludeTextRanges = true,
            IncludeSemanticState = true
        };
        var richQuery = new WindowControlQueryOptions {
            AutomationIdPattern = PumpingWpfHarness.RichAutomationId,
            ControlTypePattern = "Document",
            UseUiAutomation = true,
            IncludeUiAutomation = true
        };

        Stopwatch selfThreadStopwatch = Stopwatch.StartNew();
        DesktopControlObservation before = harness.Invoke(() => automation.ObserveControls(
                windowQuery,
                richQuery,
                options,
                allWindows: false,
                allControls: false)
            .Single());
        selfThreadStopwatch.Stop();
        Assert.IsTrue(selfThreadStopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Same-thread observation took {selfThreadStopwatch.Elapsed}.");
        StringAssert.Contains(before.Text.Value, "alpha beta gamma");
        Assert.AreEqual(1, before.Text.SelectedText.Count);
        StringAssert.Contains(before.Text.SelectedText[0], "beta");
        Assert.IsTrue(before.Capabilities.CanReadText);
        CollectionAssert.Contains(before.Capabilities.Patterns.ToArray(), "Text");
        Assert.AreEqual(64, before.Text.EditContextFingerprint.Length);

        IReadOnlyList<DesktopControlObservation> defaultDiscovery = harness.Invoke(() => automation.ObserveControls(
            windowQuery,
            controlOptions: null,
            options,
            allWindows: false,
            allControls: true));
        Assert.IsTrue(defaultDiscovery.Any(observation =>
            string.Equals(observation.Identity.AutomationId, PumpingWpfHarness.RichAutomationId, StringComparison.Ordinal)));

        harness.SelectGamma();
        DesktopTextEditResult movedRange = automation.EditControlText(
            before,
            new DesktopTextEditRequest {
                Text = "must-not-apply",
                Mode = DesktopTextEditMode.ReplaceSelection,
                ExpectedFingerprint = before.Text.ContentFingerprint,
                EnsureForegroundWindow = true,
                AllowForegroundInputFallback = true
            },
            options);
        Assert.IsFalse(movedRange.Success);
        Assert.IsFalse(movedRange.Applied);
        Assert.AreEqual("edit-context-changed", movedRange.FailureCode);

        harness.SelectBeta();
        before = automation.ObserveControls(windowQuery, richQuery, options, false, false).Single();

        DesktopTextEditResult replace = harness.Invoke(() => automation.EditControlText(
                before,
                new DesktopTextEditRequest {
                    Text = "delta",
                    Mode = DesktopTextEditMode.ReplaceSelection,
                    ExpectedFingerprint = before.Text.ContentFingerprint,
                    EnsureForegroundWindow = true,
                    AllowForegroundInputFallback = true
                },
                options));
        Assert.IsTrue(
            replace.Success,
            $"{replace.FailureReason} expectedContext={before.Text.EditContextFingerprint}; actualContext={replace.Before?.Text.EditContextFingerprint}; selection={string.Join(",", replace.Before?.Text.SelectionRanges.Select(range => $"{range.Offset}:{range.Length}:{range.Text}") ?? Array.Empty<string>())}; caret={replace.Before?.Text.CaretOffset}; active={replace.Before?.Text.IsCaretActive}");
        StringAssert.Contains(replace.After!.Text.Value, "alpha delta gamma");

        harness.MoveCaretToDocumentEnd();
        DesktopControlObservation caret = automation.ObserveControls(
            windowQuery,
            richQuery,
            options,
            allWindows: false,
            allControls: false).Single();
        Assert.IsTrue(caret.Text.CaretOffset.HasValue, caret.FailureReason);

        DesktopTextEditResult emptyCollapsedReplacement = automation.EditControlText(
            caret,
            new DesktopTextEditRequest {
                Text = string.Empty,
                Mode = DesktopTextEditMode.ReplaceSelection,
                ExpectedFingerprint = caret.Text.ContentFingerprint,
                EnsureForegroundWindow = true,
                AllowForegroundInputFallback = true
            },
            options);
        Assert.IsTrue(emptyCollapsedReplacement.Success, emptyCollapsedReplacement.FailureReason);
        Assert.AreEqual(caret.Text.Value, emptyCollapsedReplacement.After!.Text.Value);
        caret = emptyCollapsedReplacement.After;

        DesktopTextEditResult insert = automation.EditControlText(
            caret,
            new DesktopTextEditRequest {
                Text = "!",
                Mode = DesktopTextEditMode.InsertAtCaret,
                ExpectedFingerprint = caret.Text.ContentFingerprint,
                EnsureForegroundWindow = true,
                AllowForegroundInputFallback = true
            },
            options);
        Assert.IsTrue(insert.Success, insert.FailureReason);
        StringAssert.Contains(insert.After!.Text.Value, "alpha delta gamma!");

        DesktopControlObservation protectedObservation = automation.ObserveControls(
            windowQuery,
            new WindowControlQueryOptions {
                AutomationIdPattern = PumpingWpfHarness.PasswordAutomationId,
                ControlTypePattern = "Edit",
                UseUiAutomation = true,
                IncludeUiAutomation = true
            },
            options,
            allWindows: false,
            allControls: false).Single();
        Assert.AreEqual(true, protectedObservation.IsPassword);
        Assert.AreEqual(string.Empty, protectedObservation.Text.Value);
        Assert.AreEqual(string.Empty, protectedObservation.Text.ContentFingerprint);
        Assert.IsFalse(protectedObservation.Text.IsComplete);
        Assert.AreEqual(0, protectedObservation.Text.SelectedText.Count);
        Assert.AreEqual(0, protectedObservation.Text.SelectionRanges.Count);

        Stopwatch stopwatch = Stopwatch.StartNew();
        DesktopControlObservation ownerThreadWait = harness.InvokeWithQueuedDocumentText(
            "owner-thread wait changed",
            () => automation.WaitForControlObservation(
                windowQuery,
                richQuery,
                new DesktopControlObservationCondition { ExpectedText = "wait changed" },
                timeoutMilliseconds: 3000,
                intervalMilliseconds: 1000,
                observationOptions: options));
        stopwatch.Stop();

        StringAssert.Contains(ownerThreadWait.Text.Value, "owner-thread wait changed");
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Owner-thread wait took {stopwatch.Elapsed}.");
    }

    private static WindowQueryOptions CreateWindowQuery(IntPtr handle) {
        return new WindowQueryOptions {
            Handle = handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }
}

internal sealed class PumpingWpfHarness : IDisposable {
    internal const string RichAutomationId = "SemanticRichDocument";
    internal const string PasswordAutomationId = "SemanticPassword";
    internal const string SelectionAutomationId = "SemanticSelection";
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly Thread _thread;
    private Dispatcher? _dispatcher;
    private Window? _window;
    private WpfRichTextBox? _richTextBox;
    private Exception? _startupFailure;
    private IntPtr _windowHandle;

    internal PumpingWpfHarness() {
        _thread = new Thread(Run) {
            IsBackground = true,
            Name = "DesktopManager semantic WPF provider"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!_ready.Wait(TimeSpan.FromSeconds(10))) {
            throw new TimeoutException("The WPF provider did not start.");
        }

        if (_startupFailure != null) {
            throw new InvalidOperationException("The WPF provider failed to start.", _startupFailure);
        }

        WindowHandle = _windowHandle;
    }

    internal IntPtr WindowHandle { get; }

    internal void MoveCaretToDocumentEnd() {
        Invoke(() => {
            _richTextBox!.Focus();
            Paragraph paragraph = (Paragraph)_richTextBox.Document.Blocks.FirstBlock!;
            _richTextBox.CaretPosition = paragraph.ContentEnd.GetPositionAtOffset(-1, LogicalDirection.Backward)!;
        });
    }

    internal void SelectBeta() {
        SelectRange(7, 11);
    }

    internal void SelectGamma() {
        SelectRange(12, 17);
    }

    public void Dispose() {
        Dispatcher? dispatcher = _dispatcher;
        if (dispatcher == null) {
            return;
        }

        try {
            dispatcher.BeginInvoke(new Action(() => System.Windows.Application.Current.Shutdown()));
            _thread.Join(TimeSpan.FromSeconds(10));
        } finally {
            _dispatcher = null;
            _ready.Dispose();
        }
    }

    private void Run() {
        try {
            _dispatcher = Dispatcher.CurrentDispatcher;
            var application = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var panel = new StackPanel();
            _richTextBox = new WpfRichTextBox {
                Height = 160,
                Margin = new Thickness(8),
                Document = new FlowDocument(new Paragraph(new Run("alpha beta gamma")))
            };
            AutomationProperties.SetAutomationId(_richTextBox, RichAutomationId);
            var passwordBox = new PasswordBox {
                Password = "wpf-secret-never-return",
                Margin = new Thickness(8)
            };
            AutomationProperties.SetAutomationId(passwordBox, PasswordAutomationId);
            var selection = new System.Windows.Controls.ListBox {
                Height = 80,
                Margin = new Thickness(8),
                ItemsSource = new[] { "Alpha", "Beta", "Gamma" },
                SelectedIndex = 1
            };
            AutomationProperties.SetAutomationId(selection, SelectionAutomationId);
            panel.Children.Add(_richTextBox);
            panel.Children.Add(passwordBox);
            panel.Children.Add(selection);
            _window = new Window {
                Title = "DesktopManager WPF Semantic Proof",
                Width = 480,
                Height = 400,
                Left = 80,
                Top = 80,
                Content = panel
            };
            _window.ContentRendered += (_, _) => {
                _richTextBox.Focus();
                Paragraph paragraph = (Paragraph)_richTextBox.Document.Blocks.FirstBlock!;
                TextPointer start = paragraph.ContentStart.GetPositionAtOffset(7, LogicalDirection.Forward)!;
                TextPointer end = paragraph.ContentStart.GetPositionAtOffset(11, LogicalDirection.Forward)!;
                _richTextBox.Selection.Select(start, end);
                _windowHandle = new WindowInteropHelper(_window).Handle;
                _ready.Set();
            };
            application.Run(_window);
        } catch (Exception ex) {
            _startupFailure = ex;
            _ready.Set();
        }
    }

    private void Invoke(Action action) {
        Invoke(() => {
            action();
            return true;
        });
    }

    internal T Invoke<T>(Func<T> action) {
        Dispatcher dispatcher = _dispatcher ?? throw new ObjectDisposedException(nameof(PumpingWpfHarness));
        return dispatcher.Invoke(action, DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromSeconds(10));
    }

    internal T InvokeWithQueuedDocumentText<T>(string text, Func<T> action) {
        return Invoke(() => {
            Dispatcher dispatcher = _dispatcher!;
            dispatcher.BeginInvoke(new Action(() => {
                var range = new TextRange(_richTextBox!.Document.ContentStart, _richTextBox.Document.ContentEnd);
                range.Text = text;
            }));
            return action();
        });
    }

    private void SelectRange(int startOffset, int endOffset) {
        Invoke(() => {
            _richTextBox!.Focus();
            Paragraph paragraph = (Paragraph)_richTextBox.Document.Blocks.FirstBlock!;
            TextPointer start = paragraph.ContentStart.GetPositionAtOffset(startOffset, LogicalDirection.Forward)!;
            TextPointer end = paragraph.ContentStart.GetPositionAtOffset(endOffset, LogicalDirection.Forward)!;
            _richTextBox.Selection.Select(start, end);
        });
    }
}
