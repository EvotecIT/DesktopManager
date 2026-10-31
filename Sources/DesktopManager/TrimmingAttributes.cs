#if !NET8_0_OR_GREATER
// Trimming and AOT annotations are only analyzed on .NET 8+. These declarations let shared
// source carry the same annotations when it compiles for net472 and netstandard2.0.
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
internal sealed class RequiresUnreferencedCodeAttribute : Attribute {
    public RequiresUnreferencedCodeAttribute(string message) {
        Message = message;
    }

    public string Message { get; }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
internal sealed class RequiresDynamicCodeAttribute : Attribute {
    public RequiresDynamicCodeAttribute(string message) {
        Message = message;
    }

    public string Message { get; }
}

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

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class UnconditionalSuppressMessageAttribute : Attribute {
    public UnconditionalSuppressMessageAttribute(string category, string checkId) {
        Category = category;
        CheckId = checkId;
    }

    public string Category { get; }
    public string CheckId { get; }
    public string? Justification { get; set; }
}
#endif
