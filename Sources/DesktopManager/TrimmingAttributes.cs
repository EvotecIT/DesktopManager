#if !NET8_0_OR_GREATER
// Trimming and AOT annotations are only analyzed on .NET 8+. These declarations let shared
// source carry the same annotations when it compiles for net472 and netstandard2.0.
namespace System.Diagnostics.CodeAnalysis;

[Flags]
internal enum DynamicallyAccessedMemberTypes {
    None = 0,
    PublicParameterlessConstructor = 0x0001,
    PublicConstructors = 0x0002 | PublicParameterlessConstructor,
    NonPublicConstructors = 0x0004
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
    AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class |
    AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute : Attribute {
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) {
        MemberTypes = memberTypes;
    }

    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

#endif
