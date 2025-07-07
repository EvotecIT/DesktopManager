#if !NET5_0_OR_GREATER
namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct |
                    AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property |
                    AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate,
                    AllowMultiple = true, Inherited = false)]
    internal sealed class SupportedOSPlatformAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance for the specified platform name.
        /// </summary>
        /// <param name="platformName">Target platform name.</param>
        public SupportedOSPlatformAttribute(string platformName) { }
    }
}
#endif
