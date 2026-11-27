using Microsoft.CodeAnalysis;

namespace Amenone.NullObjectGenerator
{
    internal static class NullObjectDiagnostics
    {
        private const string Category = "NullObjectGenerator";

        public static readonly DiagnosticDescriptor RefReturnNotSupported = new DiagnosticDescriptor(
            "NOG001",
            "Members that return by reference cannot get a null object",
            "No null object is generated for '{0}': its member '{1}' returns by reference",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor StaticAbstractNotSupported = new DiagnosticDescriptor(
            "NOG002",
            "Static abstract members cannot get a null object",
            "No null object is generated for '{0}': its member '{1}' is static abstract",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TargetNotAccessible = new DiagnosticDescriptor(
            "NOG003",
            "Private or protected nested types cannot get a null object",
            "No null object is generated for '{0}': it is private or protected inside '{1}', and the null object is declared at namespace level",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    

        public static readonly DiagnosticDescriptor GeneratedNameCollision = new DiagnosticDescriptor(
            "NOG004",
            "Several null objects would get the same name",
            "No null object is generated for '{0}': '{1}' would also be generated for another type",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TypeParameterNameCollision = new DiagnosticDescriptor(
            "NOG005",
            "Type parameters of nested generic types share a name",
            "No null object is generated for '{0}': its type parameters and those of the types containing it share the name '{1}'",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
