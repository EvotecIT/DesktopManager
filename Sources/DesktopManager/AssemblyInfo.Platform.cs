using System.Runtime.Versioning;

// The standard asset also exposes portable desktop contracts. Native Windows
// owners carry type-level annotations; Windows-specific assets retain their
// assembly-wide requirement.
#if !NETSTANDARD2_0
[assembly: SupportedOSPlatform("windows")]
#endif
