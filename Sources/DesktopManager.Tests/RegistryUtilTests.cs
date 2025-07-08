using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace DesktopManager.Tests;

[TestClass]
public class RegistryUtilTests
{
    [TestMethod]
    public void OpenSubKey_InvalidPath_ReturnsNull()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }

        using RegistryKey? key = RegistryUtil.OpenSubKey(Registry.CurrentUser, "SOFTWARE\\NonExisting\\Path", "TestInvalidPath");
        Assert.IsNull(key);
    }

    [TestMethod]
    public void CreateSubKey_SetAndGetValue_Works()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }

        const string subKey = "SOFTWARE\\DesktopManager\\TestKey";
        using RegistryKey? key = RegistryUtil.CreateSubKey(Registry.CurrentUser, subKey, "TestCreate");
        Assert.IsNotNull(key);
        RegistryUtil.SetValue(key!, "Sample", "abc");
        Assert.AreEqual("abc", RegistryUtil.GetValue(key!, "Sample")?.ToString());
        Registry.CurrentUser.DeleteSubKeyTree(subKey);
    }
}
