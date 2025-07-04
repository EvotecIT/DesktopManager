using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowManagerWildcardTests.
/// </summary>
public class WindowManagerWildcardTests {
    private static bool InvokeMatches(string text, string pattern) {
        var manager = (WindowManager)FormatterServices.GetUninitializedObject(typeof(WindowManager));
        var method = typeof(WindowManager).GetMethod("MatchesWildcard", BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool)method.Invoke(manager, new object[] { text, pattern });
    }

    [TestMethod]
    /// <summary>
    /// Test for MatchesWildcard_BasicPatterns.
    /// </summary>
    public void MatchesWildcard_BasicPatterns() {
        Assert.IsTrue(InvokeMatches("hello", "*"));
        Assert.IsTrue(InvokeMatches("hello", "h*"));
        Assert.IsTrue(InvokeMatches("hello", "*lo"));
        Assert.IsTrue(InvokeMatches("hello", "he*lo"));
        Assert.IsTrue(InvokeMatches("hello", "ell"));
        Assert.IsFalse(InvokeMatches("hello", "abc*"));
    }

    [TestMethod]
    /// <summary>
    /// Test for MatchesWildcard_ExtendedPatterns.
    /// </summary>
    public void MatchesWildcard_ExtendedPatterns() {
        Assert.IsTrue(InvokeMatches("hello", "h*o"));
        Assert.IsTrue(InvokeMatches("hello", "h*l?"));
        Assert.IsTrue(InvokeMatches("hello", "h?l*"));
        Assert.IsTrue(InvokeMatches("hello", "*e??o"));
        Assert.IsTrue(InvokeMatches("hello", "h??lo"));
        Assert.IsFalse(InvokeMatches("hello", "h?x?o"));
    }
}

