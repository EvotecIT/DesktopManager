using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;

namespace DesktopManager.App;

public sealed partial class MainWindow {
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int GWLP_WNDPROC = -4;
    private const int WM_APP = 0x8000;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;
    private const int TrayCallbackMessage = WM_APP + 42;
    private const int TrayIconId = 1;
    private const int OpenMenuCommand = 1001;
    private const int ExitMenuCommand = 1002;
    private const int ApplyRulesMenuCommand = 1003;
    private const int ToggleHotkeysMenuCommand = 1004;
    private const int ReloadProfileMenuCommand = 1005;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint MF_STRING = 0x00000000;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;

    private IntPtr _windowHandle;
    private IntPtr _originalWndProc;
    private WindowProc? _trayWndProc;
    private bool _trayIconAdded;
    private bool _exitRequested;

    private void InitializeTray() {
        _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _trayWndProc = TrayWindowProc;
        _originalWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_trayWndProc));
        AppWindow.Closing += AppWindow_Closing;
        AddTrayIcon();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args) {
        if (_exitRequested || !_profile.MinimizeToTray || !_trayIconAdded) {
            return;
        }

        args.Cancel = true;
        HideToTray();
    }

    internal void HideToTrayAfterLaunch() {
        if (!_profile.MinimizeToTray || !_trayIconAdded) {
            return;
        }

        HideToTray();
    }

    private void HideToTray() {
        ShowWindow(_windowHandle, SW_HIDE);
        AddLog("Window hidden to tray.");
    }

    private void ShowFromTray() {
        ShowWindow(_windowHandle, SW_SHOW);
        SetForegroundWindow(_windowHandle);
    }

    private void ExitFromTray() {
        _exitRequested = true;
        Close();
    }

    private IntPtr TrayWindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam) {
        if (message == TrayCallbackMessage) {
            int eventId = lParam.ToInt32();
            if (eventId == WM_LBUTTONDBLCLK) {
                ShowFromTray();
                return IntPtr.Zero;
            }

            if (eventId == WM_RBUTTONUP) {
                ShowTrayMenu();
                return IntPtr.Zero;
            }
        }

        return CallWindowProc(_originalWndProc, hWnd, message, wParam, lParam);
    }

    private void AddTrayIcon() {
        NOTIFYICONDATA data = CreateNotifyIconData();
        _trayIconAdded = Shell_NotifyIcon(NIM_ADD, ref data);
        if (!_trayIconAdded) {
            AddLog("Tray icon unavailable; close-to-tray disabled until the icon can be added.");
        } else {
            UpdateTrayTooltip();
        }
    }

    private void ShowTrayMenu() {
        GetCursorPos(out POINT point);
        IntPtr menu = CreatePopupMenu();
        try {
            AppendMenu(menu, MF_STRING, OpenMenuCommand, "Open");
            AppendMenu(menu, MF_STRING, ToggleHotkeysMenuCommand, _profile.Enabled ? "Disable Hotkeys" : "Enable Hotkeys");
            AppendMenu(menu, MF_STRING, ApplyRulesMenuCommand, "Apply Rules");
            AppendMenu(menu, MF_STRING, ReloadProfileMenuCommand, "Reload Profile");
            AppendMenu(menu, MF_STRING, ExitMenuCommand, "Exit");
            SetForegroundWindow(_windowHandle);
            int command = TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, point.X, point.Y, 0, _windowHandle, IntPtr.Zero);
            if (command == OpenMenuCommand) {
                ShowFromTray();
            } else if (command == ToggleHotkeysMenuCommand) {
                ToggleHotkeysFromTray();
            } else if (command == ApplyRulesMenuCommand) {
                ApplyLayoutRules(addDetailsToLog: true);
            } else if (command == ReloadProfileMenuCommand) {
                ReloadProfileFromTray();
            } else if (command == ExitMenuCommand) {
                ExitFromTray();
            }
        } finally {
            DestroyMenu(menu);
        }
    }

    private NOTIFYICONDATA CreateNotifyIconData() {
        return CreateNotifyIconData("DesktopManager");
    }

    private NOTIFYICONDATA CreateNotifyIconData(string tooltip) {
        return new NOTIFYICONDATA {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = TrayCallbackMessage,
            hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)),
            szTip = tooltip.Length <= 127 ? tooltip : tooltip.Substring(0, 127)
        };
    }

    private void UpdateTrayTooltip(string tooltip) {
        if (!_trayIconAdded) {
            return;
        }

        NOTIFYICONDATA data = CreateNotifyIconData(tooltip);
        Shell_NotifyIcon(NIM_MODIFY, ref data);
    }

    private void DisposeTray() {
        if (_trayIconAdded) {
            NOTIFYICONDATA data = CreateNotifyIconData();
            Shell_NotifyIcon(NIM_DELETE, ref data);
            _trayIconAdded = false;
        }

        if (_originalWndProc != IntPtr.Zero) {
            SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, _originalWndProc);
            _originalWndProc = IntPtr.Zero;
        }
    }

    private delegate IntPtr WindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}
