using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Amenone.NullObjectGenerator
{
    internal sealed record NullObjectTarget(NullObjectSpec? Spec, EquatableArray<DiagnosticSpec> Diagnostics);

    internal sealed record TargetRead(NullObjectTarget Target, CollisionCandidate? Candidate);

    internal sealed record GeneratedTypeKey(string Namespace, string Name, int Arity);

    internal sealed record CollisionCandidate(GeneratedTypeKey Key, string TargetName, SourceSpot Location);

    internal sealed record NullObjectSpec(
        GeneratedTypeKey Key,
        string HintName,
        string? Namespace,
        string Accessibility,
        bool IsUnsafe,
        string TypeName,
        string TypeParameterList,
        EquatableArray<string> ConstraintClauses,
        EquatableArray<string> Interfaces,
        NullObjLog Log,
        EquatableArray<MemberSpec> Members);

    internal abstract record MemberSpec(string? ExplicitInterface, string LogName);

    internal sealed record MethodSpec(
        string? ExplicitInterface,
        string LogName,
        string Name,
        string ReturnType,
        string TypeParameterList,
        EquatableArray<string> Parameters,
        EquatableArray<string> ConstraintClauses,
        EquatableArray<string> OutParameterNames,
        string? ReturnExpression) : MemberSpec(ExplicitInterface, LogName);

    internal sealed record PropertySpec(
        string? ExplicitInterface,
        string LogName,
        string Type,
        string? Name,
        EquatableArray<string> IndexerParameters,
        bool HasGetter,
        SetterKind Setter,
        string ReturnExpression) : MemberSpec(ExplicitInterface, LogName);

    internal sealed record EventSpec(
        string? ExplicitInterface,
        string LogName,
        string Type,
        string Name) : MemberSpec(ExplicitInterface, LogName);

    internal enum SetterKind
    {
        None,
        Set,
        Init,
    }

    internal sealed record SourceSpot(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
    {
        public static SourceSpot OfIdentifier(SyntaxNode node)
        {
            var span = node is BaseTypeDeclarationSyntax declaration ? declaration.Identifier.Span : node.Span;
            return new SourceSpot(node.SyntaxTree.FilePath, span, node.SyntaxTree.GetLineSpan(span).Span);
        }

        public DiagnosticSpec With(DiagnosticDescriptor descriptor, params string[] arguments) =>
            new DiagnosticSpec(descriptor, this, arguments.ToEquatableArray());
    }

    internal sealed record DiagnosticSpec(DiagnosticDescriptor Descriptor, SourceSpot Spot, EquatableArray<string> Arguments)
    {
        public Diagnostic ToDiagnostic() =>
            Diagnostic.Create(
                Descriptor,
                Location.Create(Spot.FilePath, Spot.Span, Spot.LineSpan),
                Arguments.Cast<object>().ToArray());
    }
}
