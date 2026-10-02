using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

/// <summary>
/// Creates an explicit WinForms window for mutation-focused tests.
/// </summary>
internal sealed class WinFormsWindowHarness : IDisposable {
    private WinFormsWindowHarness(Form form, WindowInfo window) {
        Form = form;
        Window = window;
    }

    public Form Form { get; }

    public WindowInfo Window { get; }

    public static WinFormsWindowHarness Create(string title, int width = 320, int height = 240) {
        Form form = new NonActivatingTestForm() {
            Text = title,
            Width = width,
            Height = height,
            Left = 40,
            Top = 40,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false
        };

        form.Show();
        form.CreateControl();
        Application.DoEvents();
        Thread.Sleep(100);

        WindowInfo? window = new WindowManager()
            .GetWindows(includeHidden: true)
            .FirstOrDefault(candidate => candidate.Handle == form.Handle);
        if (window == null) {
            form.Dispose();
            throw new InvalidOperationException("Failed to resolve the WinForms harness window.");
        }

        return new WinFormsWindowHarness(form, window);
    }

    public static WinFormsWindowHarness Create(string title, Action<Form> configure, int width = 320, int height = 240) {
        if (configure == null) {
            throw new ArgumentNullException(nameof(configure));
        }

        Form form = new NonActivatingTestForm() {
            Text = title,
            Width = width,
            Height = height,
            Left = 40,
            Top = 40,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false
        };

        configure(form);
        form.Show();
        form.CreateControl();
        Application.DoEvents();
        Thread.Sleep(100);

        WindowInfo? window = new WindowManager()
            .GetWindows(includeHidden: true)
            .FirstOrDefault(candidate => candidate.Handle == form.Handle);
        if (window == null) {
            form.Dispose();
            throw new InvalidOperationException("Failed to resolve the WinForms harness window.");
        }

        return new WinFormsWindowHarness(form, window);
    }

    public void Dispose() {
        try {
            Form.Close();
            Application.DoEvents();
        } catch {
            // Ignore close failures during test cleanup.
        } finally {
            Form.Dispose();
        }
    }
}
