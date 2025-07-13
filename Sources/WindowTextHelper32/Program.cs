using System;
using DesktopManager;

namespace WindowTextHelper32;

class Program {
    static int Main(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine(string.Empty);
            return 1;
        }

        if (!long.TryParse(args[0], out var handleValue)) {
            Console.WriteLine(string.Empty);
            return 1;
        }

        var handle = new IntPtr(handleValue);
        string text = WindowTextHelper.GetWindowText(handle);
        Console.WriteLine(text);
        return 0;
    }
}
