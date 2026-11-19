namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    internal static class VContainerStub
    {
        public const string Source = @"
namespace VContainer
{
    [System.AttributeUsage(System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public sealed class InjectAttribute : System.Attribute {}
}
";
    }
}
