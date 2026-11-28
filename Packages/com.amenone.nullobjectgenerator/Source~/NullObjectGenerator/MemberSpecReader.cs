using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Amenone.NullObjectGenerator
{
    internal sealed class MemberSpecReader
    {
        private readonly ImplementationSlots _slots;
        private readonly bool _generatedTypeIsPublic;

        public MemberSpecReader(string generatedTypeName, bool generatedTypeIsPublic)
        {
            _slots = new ImplementationSlots(generatedTypeName);
            _generatedTypeIsPublic = generatedTypeIsPublic;
        }

        public bool UsesPointers { get; private set; }

        public static bool NeedsImplementation(ISymbol member) =>
            member switch
            {
                IMethodSymbol method => method.MethodKind == MethodKind.Ordinary && IsOverridable(member) && method.ExplicitInterfaceImplementations.IsEmpty,
                IPropertySymbol property => IsOverridable(member) && property.ExplicitInterfaceImplementations.IsEmpty,
                IEventSymbol eventSymbol => IsOverridable(member) && eventSymbol.ExplicitInterfaceImplementations.IsEmpty,
                _ => false,
            };

        public static DiagnosticDescriptor? UnsupportedReason(ISymbol member)
        {
            if (member.IsStatic)
            {
                return NullObjectDiagnostics.StaticAbstractNotSupported;
            }

            return member switch
            {
                IMethodSymbol { ReturnsByRef: true } or IMethodSymbol { ReturnsByRefReadonly: true } => NullObjectDiagnostics.RefReturnNotSupported,
                IPropertySymbol { ReturnsByRef: true } or IPropertySymbol { ReturnsByRefReadonly: true } => NullObjectDiagnostics.RefReturnNotSupported,
                _ => null,
            };
        }

        public MemberSpec Read(INamedTypeSymbol owner, ISymbol member)
        {
            var explicitInterface = CanBeImplicit(owner, member) && _slots.TryClaim(member) ? null : TypeNames.Of(owner);
            return member switch
            {
                IMethodSymbol method => ReadMethod(method, explicitInterface),
                IPropertySymbol property => ReadProperty(property, explicitInterface),
                _ => ReadEvent((IEventSymbol)member, explicitInterface),
            };
        }

        private static bool IsOverridable(ISymbol member) =>
            (member.IsAbstract || (member.IsVirtual && !member.IsStatic)) && (member.CanBeReferencedByName || member is IPropertySymbol { IsIndexer: true });

        private bool CanBeImplicit(INamedTypeSymbol owner, ISymbol member) =>
            member.DeclaredAccessibility == Accessibility.Public
            && (!_generatedTypeIsPublic || (TypeNames.IsEffectivelyPublic(owner) && HasPublicSignature(member)))
            && (member is not IMethodSymbol method || method.TypeParameters.All(TypeNames.CanWriteConstraints));

        private static bool HasPublicSignature(ISymbol member) =>
            member switch
            {
                IMethodSymbol method => TypeNames.IsEffectivelyPublic(method),
                IPropertySymbol property => TypeNames.IsEffectivelyPublic(property.Type) && property.Parameters.All(parameter => TypeNames.IsEffectivelyPublic(parameter.Type)),
                IEventSymbol eventSymbol => TypeNames.IsEffectivelyPublic(eventSymbol.Type),
                _ => true,
            };

        private MethodSpec ReadMethod(IMethodSymbol method, string? explicitInterface)
        {
            var isImplicit = explicitInterface is null;
            NoteTypes(method.Parameters.Select(parameter => parameter.Type).Append(method.ReturnType));
            return new MethodSpec(
                explicitInterface,
                method.Name,
                TypeNames.Identifier(method.Name),
                TypeNames.Of(method.ReturnType),
                TypeNames.TypeParameterList(method.TypeParameters),
                Parameters(method.Parameters, isImplicit),
                isImplicit ? TypeNames.ConstraintClauses(method.TypeParameters) : EquatableArray<string>.Empty,
                method.Parameters.Where(parameter => parameter.RefKind == RefKind.Out).Select(ParameterName).ToEquatableArray(),
                method.ReturnsVoid ? null : ReturnValue.Of(method.ReturnType));
        }

        private PropertySpec ReadProperty(IPropertySymbol property, string? explicitInterface)
        {
            NoteTypes(property.Parameters.Select(parameter => parameter.Type).Append(property.Type));
            return new PropertySpec(
                explicitInterface,
                property.IsIndexer ? "this[]" : property.Name,
                TypeNames.Of(property.Type),
                property.IsIndexer ? null : TypeNames.Identifier(property.Name),
                Parameters(property.Parameters, explicitInterface is null),
                property.GetMethod != null,
                SetterOf(property),
                ReturnValue.Of(property.Type));
        }

        private EventSpec ReadEvent(IEventSymbol eventSymbol, string? explicitInterface)
        {
            NoteTypes(new[] { eventSymbol.Type });
            return new EventSpec(explicitInterface, eventSymbol.Name, TypeNames.Of(eventSymbol.Type), TypeNames.Identifier(eventSymbol.Name));
        }

        private void NoteTypes(IEnumerable<ITypeSymbol> types) => UsesPointers |= types.Any(TypeNames.ContainsPointer);

        private static SetterKind SetterOf(IPropertySymbol property) =>
            property.SetMethod switch
            {
                null => SetterKind.None,
                { IsInitOnly: true } => SetterKind.Init,
                _ => SetterKind.Set,
            };

        private static EquatableArray<string> Parameters(IEnumerable<IParameterSymbol> parameters, bool includeDefaultValues) =>
            parameters.Select(parameter => Parameter(parameter, includeDefaultValues)).ToEquatableArray();

        private static string Parameter(IParameterSymbol parameter, bool includeDefaultValue)
        {
            var modifier = parameter.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => parameter.IsParams ? "params " : string.Empty,
            };
            var declaration = $"{modifier}{TypeNames.Of(parameter.Type)} {ParameterName(parameter)}";
            return includeDefaultValue && parameter.HasExplicitDefaultValue
                ? $"{declaration} = {DefaultValueLiteral.Of(parameter)}"
                : declaration;
        }

        private static string ParameterName(IParameterSymbol parameter) =>
            string.IsNullOrEmpty(parameter.Name) ? $"arg{parameter.Ordinal}" : TypeNames.Identifier(parameter.Name);
    }
}
