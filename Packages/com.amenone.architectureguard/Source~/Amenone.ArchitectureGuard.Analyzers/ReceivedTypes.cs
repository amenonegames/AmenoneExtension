using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal static class ReceivedTypes
    {
        public static ImmutableArray<(ISymbol Declaration, ITypeSymbol Type)> Of(ISymbol member) => member switch
        {
            IFieldSymbol { IsImplicitlyDeclared: false } field => ImmutableArray.Create<(ISymbol, ITypeSymbol)>((field, field.Type)),
            IPropertySymbol { IsImplicitlyDeclared: false } property when !IsPrimaryConstructorProperty(property) =>
                ImmutableArray.Create<(ISymbol, ITypeSymbol)>((property, property.Type)),
            IMethodSymbol method when ReceivesThroughParameters(method) =>
                method.Parameters.Select(parameter => ((ISymbol)parameter, parameter.Type)).ToImmutableArray(),
            _ => ImmutableArray<(ISymbol, ITypeSymbol)>.Empty,
        };

        private static bool ReceivesThroughParameters(IMethodSymbol method) =>
            !method.IsImplicitlyDeclared
            && (method.MethodKind is MethodKind.Ordinary
                or MethodKind.Constructor
                or MethodKind.ExplicitInterfaceImplementation
                or MethodKind.UserDefinedOperator
                or MethodKind.Conversion);

        // record の主コンストラクタの引数から合成されるプロパティは、引数の側で 1 度だけ数える。
        private static bool IsPrimaryConstructorProperty(IPropertySymbol property) =>
            property.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is ParameterSyntax);
    }
}
