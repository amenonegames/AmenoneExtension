using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Amenone.NullObjectGenerator
{
    internal static class TypeNames
    {
        // Leaves out nullable reference annotations: generated files are '#nullable disable', where they would warn.
        private static readonly SymbolDisplayFormat FullyQualified = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static string Of(ITypeSymbol type) => type.ToDisplayString(FullyQualified);

        public static string Identifier(string name) =>
            SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

        public static string TypeParameterList(IEnumerable<ITypeParameterSymbol> typeParameters)
        {
            var names = typeParameters.Select(parameter => Identifier(parameter.Name)).ToList();
            return names.Count == 0 ? string.Empty : "<" + string.Join(", ", names) + ">";
        }

        public static EquatableArray<string> ConstraintClauses(IEnumerable<ITypeParameterSymbol> typeParameters) =>
            typeParameters
                .Select(ConstraintClause)
                .Where(clause => clause != null)
                .Select(clause => clause!)
                .ToEquatableArray();

        public static bool ContainsPointer(ITypeSymbol type) =>
            type switch
            {
                IPointerTypeSymbol => true,
                IFunctionPointerTypeSymbol => true,
                IArrayTypeSymbol array => ContainsPointer(array.ElementType),
                INamedTypeSymbol named => named.TypeArguments.Any(ContainsPointer),
                _ => false,
            };

        public static bool IsEffectivelyPublic(ITypeSymbol type) =>
            type switch
            {
                ITypeParameterSymbol => true,
                IArrayTypeSymbol array => IsEffectivelyPublic(array.ElementType),
                IPointerTypeSymbol pointer => IsEffectivelyPublic(pointer.PointedAtType),
                IFunctionPointerTypeSymbol function => IsEffectivelyPublic(function.Signature),
                INamedTypeSymbol { IsTupleType: true } tuple => tuple.TupleElements.All(element => IsEffectivelyPublic(element.Type)),
                INamedTypeSymbol named => IsPublicAlongContainingTypes(named),
                _ => true,
            };

        public static bool IsEffectivelyPublic(IMethodSymbol signature) =>
            IsEffectivelyPublic(signature.ReturnType)
            && signature.Parameters.All(parameter => IsEffectivelyPublic(parameter.Type))
            && signature.TypeParameters.SelectMany(parameter => parameter.ConstraintTypes).All(IsEffectivelyPublic);

        // After a type argument is substituted, the constraints may no longer be writable:
        // CS0701 / CS0702 for sealed classes, value types and arrays, CS0450 for a class next to class / struct / unmanaged.
        public static bool CanWriteConstraints(ITypeParameterSymbol parameter) =>
            parameter.ConstraintTypes.All(CanBeConstraint)
            && !(HasPrimaryConstraintKeyword(parameter) && parameter.ConstraintTypes.Any(type => type.TypeKind == TypeKind.Class));

        public static string SignatureKey(ITypeSymbol type) =>
            type switch
            {
                ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } parameter => "!!" + parameter.Ordinal,
                IArrayTypeSymbol array => SignatureKey(array.ElementType) + "[" + new string(',', array.Rank - 1) + "]",
                IPointerTypeSymbol pointer => SignatureKey(pointer.PointedAtType) + "*",
                IDynamicTypeSymbol => "object",
                INamedTypeSymbol { IsTupleType: true, TupleUnderlyingType: { } underlying } => SignatureKey(underlying),
                INamedTypeSymbol { IsGenericType: true } named =>
                    Of(named.OriginalDefinition) + "<" + string.Join(",", named.TypeArguments.Select(SignatureKey)) + ">",
                _ => Of(type),
            };

        private static bool HasPrimaryConstraintKeyword(ITypeParameterSymbol parameter) =>
            parameter.HasReferenceTypeConstraint || parameter.HasValueTypeConstraint || parameter.HasUnmanagedTypeConstraint;

        private static bool CanBeConstraint(ITypeSymbol type) =>
            type switch
            {
                ITypeParameterSymbol => true,
                INamedTypeSymbol { TypeKind: TypeKind.Interface } => true,
                INamedTypeSymbol { TypeKind: TypeKind.Class, IsSealed: false, IsStatic: false } named =>
                    named.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Array),
                _ => false,
            };

        private static bool IsPublicAlongContainingTypes(INamedTypeSymbol type)
        {
            for (var current = type; current != null; current = current.ContainingType)
            {
                if (current.DeclaredAccessibility != Accessibility.Public || !current.TypeArguments.All(IsEffectivelyPublic))
                {
                    return false;
                }
            }

            return true;
        }

        private static string? ConstraintClause(ITypeParameterSymbol parameter)
        {
            var constraints = new List<string>();
            if (parameter.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }
            else if (parameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (parameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }

            constraints.AddRange(parameter.ConstraintTypes.Select(Of));
            if (parameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            return constraints.Count == 0 ? null : $"where {Identifier(parameter.Name)} : {string.Join(", ", constraints)}";
        }
    }
}
