using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Amenone.NullObjectGenerator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class NullObjectSourceGenerator : IIncrementalGenerator
    {
        public const string InterfaceTargetsStep = "InterfaceTargets";
        public const string ClassTargetsStep = "ClassTargets";
        public const string InterfaceOutputsStep = "InterfaceOutputs";
        public const string ClassOutputsStep = "ClassOutputs";
        public const string CollidingKeysStep = "CollidingKeys";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static output =>
                output.AddSource(AttributeSource.HintName, SourceText.From(AttributeSource.Text, Encoding.UTF8)));

            var interfaceReads = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeSource.InterfaceAttributeMetadataName,
                    static (node, _) => node is InterfaceDeclarationSyntax,
                    static (attributeContext, cancellationToken) => NullObjectTargetReader.ReadInterface(attributeContext, cancellationToken))
                .Where(static read => read is not null);

            var classReads = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeSource.ClassAttributeMetadataName,
                    static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                    static (attributeContext, cancellationToken) => NullObjectTargetReader.ReadClass(attributeContext, cancellationToken))
                .Where(static read => read is not null);

            // Candidates carry source locations, so they change on any edit that moves a target; the key set does not.
            var candidates = CandidatesOf(interfaceReads).Collect()
                .Combine(CandidatesOf(classReads).Collect())
                .Select(static (pair, _) => GeneratedNameCollisions.Merge(pair.Left, pair.Right));
            var collidingKeys = candidates
                .Select(static (all, _) => GeneratedNameCollisions.CollidingKeys(all))
                .WithTrackingName(CollidingKeysStep);
            context.RegisterSourceOutput(candidates, static (output, all) => GeneratedNameCollisions.Report(output, all));

            RegisterTargets(context, interfaceReads, collidingKeys, InterfaceTargetsStep, InterfaceOutputsStep);
            RegisterTargets(context, classReads, collidingKeys, ClassTargetsStep, ClassOutputsStep);
        }

        private static IncrementalValuesProvider<CollisionCandidate> CandidatesOf(IncrementalValuesProvider<TargetRead?> reads) =>
            reads
                .Select(static (read, _) => read!.Candidate)
                .Where(static candidate => candidate is not null)
                .Select(static (candidate, _) => candidate!);

        private static void RegisterTargets(
            IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<TargetRead?> reads,
            IncrementalValueProvider<EquatableArray<GeneratedTypeKey>> collidingKeys,
            string targetsStep,
            string outputsStep)
        {
            var outputs = reads
                .Select(static (read, _) => read!.Target)
                .WithTrackingName(targetsStep)
                .Combine(collidingKeys)
                .WithTrackingName(outputsStep);
            context.RegisterSourceOutput(outputs, static (output, pair) => NullObjectEmitter.Emit(output, pair.Left, pair.Right));
        }
    }
}
