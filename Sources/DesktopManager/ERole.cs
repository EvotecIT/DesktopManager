using System;

namespace DesktopManager;

/// <summary>
/// Roles supported when setting default audio device.
/// </summary>
public enum ERole {
    /// <summary>Console audio stream.</summary>
    eConsole = 0,
    /// <summary>Multimedia audio stream.</summary>
    eMultimedia = 1,
    /// <summary>Communications audio stream.</summary>
    eCommunications = 2,
    /// <summary>Number of roles.</summary>
    ERole_enum_count = 3
}
