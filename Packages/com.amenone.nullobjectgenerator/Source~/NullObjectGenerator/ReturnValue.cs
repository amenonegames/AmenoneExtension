using Microsoft.CodeAnalysis;

namespace Amenone.NullObjectGenerator
{
    internal static class ReturnValue
    {
        private const string TasksNamespace = "System.Threading.Tasks";

        public static string Of(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol { ContainingType: null } named || named.ContainingNamespace.ToDisplayString() != TasksNamespace)
            {
                return "default";
            }

            return named.MetadataName switch
            {
                "Task" => "global::System.Threading.Tasks.Task.CompletedTask",
                "Task`1" => $"global::System.Threading.Tasks.Task.FromResult<{TypeNames.Of(named.TypeArguments[0])}>(default)",
                _ => "default",
            };
        }
    }
}
