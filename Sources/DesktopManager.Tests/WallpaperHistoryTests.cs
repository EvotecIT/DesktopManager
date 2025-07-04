using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WallpaperHistoryTests.
/// </summary>
public class WallpaperHistoryTests
{
    [TestMethod]
    /// <summary>
    /// Test for AddEntry_WritesFile.
    /// </summary>
    public void AddEntry_WritesFile()
    {
        var temp = Path.GetTempFileName();
        Environment.SetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH", temp);
        try
        {
            WallpaperHistory.SetHistory(Array.Empty<string>());
            WallpaperHistory.AddEntry("wall1");
            var history = WallpaperHistory.GetHistory();
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual("wall1", history.First());
        }
        finally
        {
            Environment.SetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH", null);
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Test for AddEntry_EnforcesMaximum.
    /// </summary>
    public void AddEntry_EnforcesMaximum()
    {
        var temp = Path.GetTempFileName();
        Environment.SetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH", temp);
        try
        {
            WallpaperHistory.SetHistory(Enumerable.Range(0, 60).Select(i => $"wall{i}"));
            WallpaperHistory.AddEntry("new");
            var history = WallpaperHistory.GetHistory();
            Assert.AreEqual(50, history.Count);
            Assert.AreEqual("new", history.First());
            Assert.AreEqual("wall48", history.Last());
        }
        finally
        {
            Environment.SetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH", null);
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
}
