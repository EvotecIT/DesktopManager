namespace DesktopManager.Tests;

[TestClass]
public sealed class WorkspaceExplorerFoldersTests {
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void ReadWindows_PartialShellViewDoesNotHideOtherReadyFolders(int incompleteStage) {
        string path = Path.GetTempPath();
        object? item = incompleteStage == 2 ? null : new ShellItem { Path = incompleteStage == 3 ? null : path };
        object? folder = incompleteStage == 1 ? null : new ShellFolder { Self = item };
        object? document = incompleteStage == 0 ? null : new ShellDocument { Folder = folder };
        var partial = new ShellWindow { HWND = 1, Document = document };
        var ready = new ShellWindow {
            HWND = 2, Document = new ShellDocument { Folder = new ShellFolder { Self = new ShellItem { Path = path } } }
        };

        Dictionary<IntPtr, string> result = WorkspaceExplorerFolders.ReadWindows(new object[] { partial, ready });

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(path, result[new IntPtr(2)]);
        Assert.IsFalse(result.ContainsKey(new IntPtr(1)));
    }

    public sealed class ShellWindow {
        public string FullName => @"C:\Windows\explorer.exe";
        public long HWND { get; set; }
        public object? Document { get; set; }
    }
    public sealed class ShellDocument { public object? Folder { get; set; } }
    public sealed class ShellFolder { public object? Self { get; set; } }
    public sealed class ShellItem { public object? Path { get; set; } }
}
