using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// OnModuleImportAndRemove is a class that implements the IModuleAssemblyInitializer and IModuleAssemblyCleanup interfaces.
/// This class is used to handle the assembly resolve event when the module is imported and removed.
/// </summary>
public class OnModuleImportAndRemove : IModuleAssemblyInitializer, IModuleAssemblyCleanup {
    /// <summary>
    /// OnImport is called when the module is imported.
    /// </summary>
    public void OnImport() {
        if (IsNetFramework()) {
            AppDomain.CurrentDomain.AssemblyResolve += MyResolveEventHandler;
        }
    }

    /// <summary>
    /// Called when the module is removed from the PowerShell session.
    /// </summary>
    /// <param name="module">Module being removed.</param>
    public void OnRemove(PSModuleInfo module) {
        if (IsNetFramework()) {
            AppDomain.CurrentDomain.AssemblyResolve -= MyResolveEventHandler;
        }
    }

    /// <summary>
    /// Handles assembly resolution for dependencies shipped with the module.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">Information about the assembly to resolve.</param>
    /// <returns>The loaded assembly or <c>null</c> if not found.</returns>
    private static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args) {
        var libDirectory = Path.GetDirectoryName(typeof(OnModuleImportAndRemove).Assembly.Location);
        var directoriesToSearch = new List<string> { libDirectory };

        if (Directory.Exists(libDirectory)) {
            directoriesToSearch.AddRange(Directory.GetDirectories(libDirectory, "*", SearchOption.AllDirectories));
        }

        var requestedAssemblyName = new AssemblyName(args.Name).Name + ".dll";

        foreach (var directory in directoriesToSearch) {
            var assemblyPath = Path.Combine(directory, requestedAssemblyName);

            if (File.Exists(assemblyPath)) {
                try {
                    return Assembly.LoadFrom(assemblyPath);
                } catch (Exception ex) {
                    Console.WriteLine($"Failed to load assembly from {assemblyPath}: {ex.Message}");
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether the current runtime is .NET Framework.
    /// </summary>
    /// <returns><c>true</c> if running on .NET Framework; otherwise <c>false</c>.</returns>
    private bool IsNetFramework() {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
    }

    // Determine if the current runtime is .NET Core
    private bool IsNetCore() {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current runtime is .NET 5 or higher.
    /// </summary>
    /// <returns><c>true</c> if running on .NET 5 or newer; otherwise <c>false</c>.</returns>
    private bool IsNet5OrHigher() {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 5", StringComparison.OrdinalIgnoreCase) ||
               System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 6", StringComparison.OrdinalIgnoreCase) ||
               System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 7", StringComparison.OrdinalIgnoreCase) ||
               System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 8", StringComparison.OrdinalIgnoreCase) ||
               System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET 9", StringComparison.OrdinalIgnoreCase);
    }
}