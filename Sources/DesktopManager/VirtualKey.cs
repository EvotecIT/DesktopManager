namespace DesktopManager;

/// <summary>
/// Virtual key codes used for keyboard input.
/// </summary>
public enum VirtualKey : ushort {
    /// <summary>Left mouse button.</summary>
    VK_LBUTTON = 0x01,
    /// <summary>Right mouse button.</summary>
    VK_RBUTTON = 0x02,
    /// <summary>Cancel key.</summary>
    VK_CANCEL = 0x03,
    /// <summary>Middle mouse button.</summary>
    VK_MBUTTON = 0x04,
    /// <summary>Backspace key.</summary>
    VK_BACK = 0x08,
    /// <summary>Tab key.</summary>
    VK_TAB = 0x09,
    /// <summary>Enter key.</summary>
    VK_RETURN = 0x0D,
    /// <summary>Shift key.</summary>
    VK_SHIFT = 0x10,
    /// <summary>Control key.</summary>
    VK_CONTROL = 0x11,
    /// <summary>Alt key.</summary>
    VK_MENU = 0x12,
    /// <summary>Pause key.</summary>
    VK_PAUSE = 0x13,
    /// <summary>Caps Lock key.</summary>
    VK_CAPITAL = 0x14,
    /// <summary>Escape key.</summary>
    VK_ESCAPE = 0x1B,
    /// <summary>Spacebar.</summary>
    VK_SPACE = 0x20,
    /// <summary>Page Up key.</summary>
    VK_PRIOR = 0x21,
    /// <summary>Page Down key.</summary>
    VK_NEXT = 0x22,
    /// <summary>End key.</summary>
    VK_END = 0x23,
    /// <summary>Home key.</summary>
    VK_HOME = 0x24,
    /// <summary>Left Arrow key.</summary>
    VK_LEFT = 0x25,
    /// <summary>Up Arrow key.</summary>
    VK_UP = 0x26,
    /// <summary>Right Arrow key.</summary>
    VK_RIGHT = 0x27,
    /// <summary>Down Arrow key.</summary>
    VK_DOWN = 0x28,
    /// <summary>Insert key.</summary>
    VK_INSERT = 0x2D,
    /// <summary>Delete key.</summary>
    VK_DELETE = 0x2E,
    /// <summary>0 key.</summary>
    VK_0 = 0x30,
    /// <summary>1 key.</summary>
    VK_1 = 0x31,
    /// <summary>2 key.</summary>
    VK_2 = 0x32,
    /// <summary>3 key.</summary>
    VK_3 = 0x33,
    /// <summary>4 key.</summary>
    VK_4 = 0x34,
    /// <summary>5 key.</summary>
    VK_5 = 0x35,
    /// <summary>6 key.</summary>
    VK_6 = 0x36,
    /// <summary>7 key.</summary>
    VK_7 = 0x37,
    /// <summary>8 key.</summary>
    VK_8 = 0x38,
    /// <summary>9 key.</summary>
    VK_9 = 0x39,
    /// <summary>A key.</summary>
    VK_A = 0x41,
    /// <summary>B key.</summary>
    VK_B = 0x42,
    /// <summary>C key.</summary>
    VK_C = 0x43,
    /// <summary>D key.</summary>
    VK_D = 0x44,
    /// <summary>E key.</summary>
    VK_E = 0x45,
    /// <summary>F key.</summary>
    VK_F = 0x46,
    /// <summary>G key.</summary>
    VK_G = 0x47,
    /// <summary>H key.</summary>
    VK_H = 0x48,
    /// <summary>I key.</summary>
    VK_I = 0x49,
    /// <summary>J key.</summary>
    VK_J = 0x4A,
    /// <summary>K key.</summary>
    VK_K = 0x4B,
    /// <summary>L key.</summary>
    VK_L = 0x4C,
    /// <summary>M key.</summary>
    VK_M = 0x4D,
    /// <summary>N key.</summary>
    VK_N = 0x4E,
    /// <summary>O key.</summary>
    VK_O = 0x4F,
    /// <summary>P key.</summary>
    VK_P = 0x50,
    /// <summary>Q key.</summary>
    VK_Q = 0x51,
    /// <summary>R key.</summary>
    VK_R = 0x52,
    /// <summary>S key.</summary>
    VK_S = 0x53,
    /// <summary>T key.</summary>
    VK_T = 0x54,
    /// <summary>U key.</summary>
    VK_U = 0x55,
    /// <summary>V key.</summary>
    VK_V = 0x56,
    /// <summary>W key.</summary>
    VK_W = 0x57,
    /// <summary>X key.</summary>
    VK_X = 0x58,
    /// <summary>Y key.</summary>
    VK_Y = 0x59,
    /// <summary>Z key.</summary>
    VK_Z = 0x5A,
    /// <summary>Left Windows key.</summary>
    VK_LWIN = 0x5B,
    /// <summary>Right Windows key.</summary>
    VK_RWIN = 0x5C,
    /// <summary>Numpad 0 key.</summary>
    VK_NUMPAD0 = 0x60,
    /// <summary>Numpad 1 key.</summary>
    VK_NUMPAD1 = 0x61,
    /// <summary>Numpad 2 key.</summary>
    VK_NUMPAD2 = 0x62,
    /// <summary>Numpad 3 key.</summary>
    VK_NUMPAD3 = 0x63,
    /// <summary>Numpad 4 key.</summary>
    VK_NUMPAD4 = 0x64,
    /// <summary>Numpad 5 key.</summary>
    VK_NUMPAD5 = 0x65,
    /// <summary>Numpad 6 key.</summary>
    VK_NUMPAD6 = 0x66,
    /// <summary>Numpad 7 key.</summary>
    VK_NUMPAD7 = 0x67,
    /// <summary>Numpad 8 key.</summary>
    VK_NUMPAD8 = 0x68,
    /// <summary>Numpad 9 key.</summary>
    VK_NUMPAD9 = 0x69,
    /// <summary>Multiply key.</summary>
    VK_MULTIPLY = 0x6A,
    /// <summary>Add key.</summary>
    VK_ADD = 0x6B,
    /// <summary>Separator key.</summary>
    VK_SEPARATOR = 0x6C,
    /// <summary>Subtract key.</summary>
    VK_SUBTRACT = 0x6D,
    /// <summary>Decimal key.</summary>
    VK_DECIMAL = 0x6E,
    /// <summary>Divide key.</summary>
    VK_DIVIDE = 0x6F,
    /// <summary>F1 key.</summary>
    VK_F1 = 0x70,
    /// <summary>F2 key.</summary>
    VK_F2 = 0x71,
    /// <summary>F3 key.</summary>
    VK_F3 = 0x72,
    /// <summary>F4 key.</summary>
    VK_F4 = 0x73,
    /// <summary>F5 key.</summary>
    VK_F5 = 0x74,
    /// <summary>F6 key.</summary>
    VK_F6 = 0x75,
    /// <summary>F7 key.</summary>
    VK_F7 = 0x76,
    /// <summary>F8 key.</summary>
    VK_F8 = 0x77,
    /// <summary>F9 key.</summary>
    VK_F9 = 0x78,
    /// <summary>F10 key.</summary>
    VK_F10 = 0x79,
    /// <summary>F11 key.</summary>
    VK_F11 = 0x7A,
    /// <summary>F12 key.</summary>
    VK_F12 = 0x7B,
    /// <summary>F13 key.</summary>
    VK_F13 = 0x7C,
    /// <summary>F14 key.</summary>
    VK_F14 = 0x7D,
    /// <summary>F15 key.</summary>
    VK_F15 = 0x7E,
    /// <summary>F16 key.</summary>
    VK_F16 = 0x7F,
    /// <summary>F17 key.</summary>
    VK_F17 = 0x80,
    /// <summary>F18 key.</summary>
    VK_F18 = 0x81,
    /// <summary>F19 key.</summary>
    VK_F19 = 0x82,
    /// <summary>F20 key.</summary>
    VK_F20 = 0x83,
    /// <summary>F21 key.</summary>
    VK_F21 = 0x84,
    /// <summary>F22 key.</summary>
    VK_F22 = 0x85,
    /// <summary>F23 key.</summary>
    VK_F23 = 0x86,
    /// <summary>F24 key.</summary>
    VK_F24 = 0x87
}
