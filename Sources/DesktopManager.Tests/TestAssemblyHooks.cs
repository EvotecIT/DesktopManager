using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace DesktopManager.Tests;

[TestClass]
public sealed class TestAssemblyHooks {
    [AssemblyInitialize]
    public static void Initialize(TestContext context) {
        TestHelper.KillAllNotepads();
    }

    [AssemblyCleanup]
    public static void Cleanup() {
        DisposeSingletonIfCreated<HotkeyService>("_instance");
        DisposeSingletonIfCreated<WindowKeepAlive>("_instance");
        TestHelper.KillAllNotepads();
    }

    private static void DisposeSingletonIfCreated<T>(string fieldName) where T : class, IDisposable {
        FieldInfo? field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is not Lazy<T> lazy || !lazy.IsValueCreated) {
            return;
        }

        lazy.Value.Dispose();
    }
}
