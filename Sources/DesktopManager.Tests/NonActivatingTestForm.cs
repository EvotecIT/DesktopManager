using System.Windows.Forms;

namespace DesktopManager.Tests;

/// <summary>Shows owned test windows without taking focus from the user's application.</summary>
internal class NonActivatingTestForm : Form {
    protected override bool ShowWithoutActivation => true;
}
