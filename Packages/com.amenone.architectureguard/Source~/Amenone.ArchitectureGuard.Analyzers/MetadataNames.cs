namespace Amenone.ArchitectureGuard.Analyzers
{
    internal static class MetadataNames
    {
        public const string UnityTime = "UnityEngine.Time";
        public const string UnityMonoBehaviour = "UnityEngine.MonoBehaviour";
        public const string AllowUnityTimeAttribute = "Amenone.ArchitectureGuard.AllowUnityTimeAttribute";
        public const string AllowUnityMessageAttribute = "Amenone.ArchitectureGuard.AllowUnityMessageAttribute";
        public const string SuppressMessageAttribute = "System.Diagnostics.CodeAnalysis.SuppressMessageAttribute";
        public const string EntityQuery = "Unity.Entities.EntityQuery";
        public const string SystemBase = "Unity.Entities.SystemBase";
        public const string ISystem = "Unity.Entities.ISystem";
        public const string SystemApiQueryBuilder = "Unity.Entities.SystemAPIQueryBuilder";

        public static readonly string[] MessagePipeSubscribers =
        {
            "MessagePipe.ISubscriber`1",
            "MessagePipe.IAsyncSubscriber`1",
            "MessagePipe.IBufferedSubscriber`1",
            "MessagePipe.IBufferedAsyncSubscriber`1",
            "MessagePipe.ISubscriber`2",
            "MessagePipe.IAsyncSubscriber`2",
        };
    }
}
