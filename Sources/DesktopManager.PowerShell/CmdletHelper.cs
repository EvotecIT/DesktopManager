namespace DesktopManager.PowerShell;

/// <summary>
/// Helper class for cmdlets.
/// </summary>
public static class CmdletHelper {
    /// <summary>
    /// Gets the effective <see cref="ActionPreference"/> value for the provided
    /// cmdlet.
    /// </summary>
    /// <param name="cmdlet">Cmdlet for which the error action should be
    /// resolved.</param>
    /// <returns>The resolved error action preference.</returns>
    public static ActionPreference GetErrorAction(PSCmdlet cmdlet) {
        // Get the error action preference as user requested
        // It first sets the error action to the default error action preference
        // If the user has specified the error action, it will set the error action to the user specified error action
        ActionPreference errorAction = (ActionPreference)cmdlet.SessionState.PSVariable.GetValue("ErrorActionPreference");
        if (cmdlet.MyInvocation.BoundParameters.ContainsKey("ErrorAction")) {
            string errorActionString = cmdlet.MyInvocation.BoundParameters["ErrorAction"].ToString();
            if (Enum.TryParse(errorActionString, true, out ActionPreference actionPreference)) {
                errorAction = actionPreference;
            }
        }
        return errorAction;
    }
}
