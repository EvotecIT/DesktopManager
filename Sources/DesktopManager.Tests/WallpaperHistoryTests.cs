using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace DesktopManager.Tests;

[TestClass]
public class WallpaperHistoryTests
{
    [TestMethod]
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
}
