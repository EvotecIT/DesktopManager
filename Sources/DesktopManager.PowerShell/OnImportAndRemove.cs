using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using System.Runtime.Loader;
#endif

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
#if NET5_0_OR_GREATER
        else {
            if (IsLoadedInDefaultContext()) {
                AssemblyLoadContext.Default.Resolving += ResolveAlc;
            }
        }
#endif
    }

    /// <summary>
    /// Called when the module is removed from the PowerShell session.
    /// </summary>
    /// <param name="module">Module being removed.</param>
    public void OnRemove(PSModuleInfo module) {
        if (IsNetFramework()) {
            AppDomain.CurrentDomain.AssemblyResolve -= MyResolveEventHandler;
        }
#if NET5_0_OR_GREATER
        else {
            if (IsLoadedInDefaultContext()) {
                AssemblyLoadContext.Default.Resolving -= ResolveAlc;
            }
        }
#endif
    }

    /// <summary>
    /// Handles assembly resolution for dependencies shipped with the module.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">Information about the assembly to resolve.</param>
    /// <returns>The loaded assembly or <c>null</c> if not found.</returns>
    private static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args) {
        var libDirectory = Path.GetDirectoryName(typeof(OnModuleImportAndRemove).Assembly.Location);
        var directoriesToSearch = new List<string>();

        if (!string.IsNullOrEmpty(libDirectory)) {
            directoriesToSearch.Add(libDirectory);
            if (Directory.Exists(libDirectory)) {
                directoriesToSearch.AddRange(Directory.GetDirectories(libDirectory, "*", SearchOption.AllDirectories));
            }
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

#if NET5_0_OR_GREATER
    private sealed class LoadContext : AssemblyLoadContext {
        private readonly string _assemblyDir;

        public LoadContext(string assemblyDir)
            : base(name: "DesktopManager", isCollectible: false) {
            _assemblyDir = assemblyDir;
        }

        protected override Assembly Load(AssemblyName assemblyName) {
            string asmPath = Path.Combine(_assemblyDir, $"{assemblyName.Name}.dll");
            if (File.Exists(asmPath)) {
                return LoadFromAssemblyPath(asmPath);
            }

            return null;
        }
    }

    private static readonly string _assemblyDir = Path.GetDirectoryName(typeof(OnModuleImportAndRemove).Assembly.Location) ?? string.Empty;

    private static readonly LoadContext _alc = new LoadContext(_assemblyDir);

    private static bool IsLoadedInDefaultContext() {
        return AssemblyLoadContext.GetLoadContext(typeof(OnModuleImportAndRemove).Assembly) == AssemblyLoadContext.Default;
    }

    private static Assembly ResolveAlc(AssemblyLoadContext defaultAlc, AssemblyName assemblyToResolve) {
        string asmPath = Path.Combine(_assemblyDir, $"{assemblyToResolve.Name}.dll");
        if (IsSatisfyingAssembly(assemblyToResolve, asmPath)) {
            return _alc.LoadFromAssemblyName(assemblyToResolve);
        }

        return null;
    }

    private static bool IsSatisfyingAssembly(AssemblyName requiredAssemblyName, string assemblyPath) {
        if (requiredAssemblyName.Name == "DesktopManager.PowerShell" || !File.Exists(assemblyPath)) {
            return false;
        }

        AssemblyName asmToLoadName = AssemblyName.GetAssemblyName(assemblyPath);
        if (!string.Equals(asmToLoadName.Name, requiredAssemblyName.Name, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (requiredAssemblyName.Version != null && asmToLoadName.Version != null) {
            return asmToLoadName.Version >= requiredAssemblyName.Version;
        }

        return true;
    }
#endif

    /// <summary>
    /// Determines whether the current runtime is .NET Framework.
    /// </summary>
    /// <returns><c>true</c> if running on .NET Framework; otherwise <c>false</c>.</returns>
    private bool IsNetFramework() {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
    }

}
