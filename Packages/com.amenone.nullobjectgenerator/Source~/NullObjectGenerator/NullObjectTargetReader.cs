using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Amenone.NullObjectGenerator
{
    internal static class NullObjectTargetReader
    {
        private const string GeneratedTypeSuffix = "AsNullObj";

        public static TargetRead? ReadInterface(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.TargetSymbol is not INamedTypeSymbol target || !IsFirstApplication(context, target))
            {
                return null;
            }

            var implemented = new[] { target }.Concat(target.AllInterfaces);
            return Read(context, target, new[] { target }, implemented, cancellationToken);
        }

        public static TargetRead? ReadClass(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.TargetSymbol is not INamedTypeSymbol target || !IsFirstApplication(context, target))
            {
                return null;
            }

            var compilation = context.SemanticModel.Compilation;
            var accessible = target.AllInterfaces
                .Where(candidate => compilation.IsSymbolAccessibleWithin(candidate, compilation.Assembly))
                .ToList();
            return Read(context, target, accessible, accessible, cancellationToken);
        }

        private static TargetRead Read(
            GeneratorAttributeSyntaxContext context,
            INamedTypeSymbol target,
            IEnumerable<INamedTypeSymbol> baseInterfaces,
            IEnumerable<INamedTypeSymbol> implementedInterfaces,
            CancellationToken cancellationToken)
        {
            var location = SourceSpot.OfIdentifier(context.TargetNode);
            var targetName = target.ToDisplayString();
            var accessibility = GeneratedAccessibility(target);
            if (accessibility is null)
            {
                return Failure(location.With(NullObjectDiagnostics.TargetNotAccessible, targetName, target.ContainingType!.ToDisplayString()));
            }

            var typeParameters = AllTypeParameters(target);
            var repeatedName = typeParameters.GroupBy(parameter => parameter.Name).FirstOrDefault(group => group.Count() > 1)?.Key;
            if (repeatedName != null)
            {
                return Failure(location.With(NullObjectDiagnostics.TypeParameterNameCollision, targetName, repeatedName));
            }

            var generatedName = target.Name + GeneratedTypeSuffix;
            var reader = new MemberSpecReader(generatedName, accessibility == "public");
            var members = new List<MemberSpec>();
            var problems = new List<DiagnosticSpec>();
            foreach (var owner in implementedInterfaces)
            {
                foreach (var member in owner.GetMembers().Where(MemberSpecReader.NeedsImplementation))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var unsupported = MemberSpecReader.UnsupportedReason(member);
                    if (unsupported is null)
                    {
                        members.Add(reader.Read(owner, member));
                    }
                    else
                    {
                        problems.Add(location.With(unsupported, targetName, member.ToDisplayString()));
                    }
                }
            }

            if (problems.Count > 0)
            {
                return new TargetRead(new NullObjectTarget(null, problems.ToEquatableArray()), null);
            }

            var ns = target.ContainingNamespace.IsGlobalNamespace ? null : target.ContainingNamespace.ToDisplayString();
            var key = new GeneratedTypeKey(ns ?? string.Empty, generatedName, typeParameters.Count);
            var spec = new NullObjectSpec(
                key,
                HintName(target),
                ns,
                accessibility,
                reader.UsesPointers,
                TypeNames.Identifier(generatedName),
                TypeNames.TypeParameterList(typeParameters),
                TypeNames.ConstraintClauses(typeParameters),
                baseInterfaces.Select(TypeNames.Of).ToEquatableArray(),
                ReadLog(OwnAttribute(context)),
                members.ToEquatableArray());
            return new TargetRead(
                new NullObjectTarget(spec, EquatableArray<DiagnosticSpec>.Empty),
                new CollisionCandidate(key, targetName, location));
        }

        private static bool IsFirstApplication(GeneratorAttributeSyntaxContext context, INamedTypeSymbol target)
        {
            var own = OwnAttribute(context);
            var first = target.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, own?.AttributeClass));
            return own?.ApplicationSyntaxReference is { } current
                && first?.ApplicationSyntaxReference is { } reference
                && reference.SyntaxTree == current.SyntaxTree
                && reference.Span == current.Span;
        }

        // ForAttributeWithMetadataName passes every matching attribute of the file, so attributes on another partial declaration in the same file are included.
        private static AttributeData? OwnAttribute(GeneratorAttributeSyntaxContext context) =>
            context.Attributes.FirstOrDefault(attribute =>
                attribute.ApplicationSyntaxReference is { } reference
                && reference.SyntaxTree == context.TargetNode.SyntaxTree
                && context.TargetNode.Span.Contains(reference.Span));

        private static string? GeneratedAccessibility(INamedTypeSymbol target)
        {
            var isPublic = true;
            for (var current = target; current != null; current = current.ContainingType)
            {
                switch (current.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                        break;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                        isPublic = false;
                        break;
                    default:
                        return null;
                }
            }

            return isPublic ? "public" : "internal";
        }

        private static IReadOnlyList<ITypeParameterSymbol> AllTypeParameters(INamedTypeSymbol target)
        {
            var chain = new List<INamedTypeSymbol>();
            for (var current = target; current != null; current = current.ContainingType)
            {
                chain.Insert(0, current);
            }

            return chain.SelectMany(type => type.TypeParameters).ToList();
        }

        private static string HintName(INamedTypeSymbol target)
        {
            var names = new List<string>();
            for (var current = target; current != null; current = current.ContainingType)
            {
                names.Insert(0, current.MetadataName);
            }

            var typePath = string.Join("+", names);
            var fullName = target.ContainingNamespace.IsGlobalNamespace ? typePath : $"{target.ContainingNamespace.ToDisplayString()}.{typePath}";

            // Hint names must be unique ignoring case, while 'IFoo' and 'Ifoo' are different types.
            return $"{fullName}.{GeneratedTypeSuffix}.{StableHash.Of(fullName)}.g.cs";
        }

        private static NullObjLog ReadLog(AttributeData? attribute) =>
            attribute is { ConstructorArguments.Length: 1 } && attribute.ConstructorArguments[0].Value is int value
                ? (NullObjLog)value
                : NullObjLog.None;

        private static TargetRead Failure(DiagnosticSpec diagnostic) =>
            new TargetRead(new NullObjectTarget(null, new[] { diagnostic }.ToEquatableArray()), null);
    }
}
