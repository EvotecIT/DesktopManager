namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Clicks a window control using the specified mouse button.
    /// </summary>
    /// <param name="control">The control to click.</param>
    /// <param name="button">Mouse button to use.</param>
    public void ClickControl(WindowControlInfo control, MouseButton button) {
        WindowControlService.ControlClick(control, button);
    }

    /// <summary>
    /// Gets the check state of a button control.
    /// </summary>
    /// <param name="control">The control to query.</param>
    /// <returns><c>true</c> if checked; otherwise <c>false</c>.</returns>
    public bool GetControlCheckState(WindowControlInfo control) {
        return WindowControlService.GetCheckState(control);
    }

    /// <summary>
    /// Sets the check state of a button control.
    /// </summary>
    /// <param name="control">The control to modify.</param>
    /// <param name="check">Desired check state.</param>
    public void SetControlCheckState(WindowControlInfo control, bool check) {
        WindowControlService.SetCheckState(control, check);
    }

    /// <summary>
    /// Gets the text of a control.
    /// </summary>
    /// <param name="control">Control to query.</param>
    /// <returns>Control text.</returns>
    public string GetControlText(WindowControlInfo control) {
        return WindowControlService.GetControlText(control);
    }

    /// <summary>
    /// Sets the text of a control.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="text">Text to set.</param>
    public void SetControlText(WindowControlInfo control, string text) {
        WindowControlService.SetControlText(control, text);
    }
}
