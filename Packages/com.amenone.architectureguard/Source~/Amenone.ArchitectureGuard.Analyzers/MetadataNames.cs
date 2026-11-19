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
        public const string PublishSystem = "Amenone.EcsBridge.PublishSystem`1";

        public static readonly string[] MessagePipeSubscribers =
        {
            "MessagePipe.ISubscriber`1",
            "MessagePipe.IAsyncSubscriber`1",
            "MessagePipe.IBufferedSubscriber`1",
            "MessagePipe.IBufferedAsyncSubscriber`1",
            "MessagePipe.ISubscriber`2",
            "MessagePipe.IAsyncSubscriber`2",
        };

        public static readonly string[] MessagePipePublishers =
        {
            "MessagePipe.IPublisher`1",
            "MessagePipe.IAsyncPublisher`1",
            "MessagePipe.IBufferedPublisher`1",
            "MessagePipe.IBufferedAsyncPublisher`1",
            "MessagePipe.IPublisher`2",
            "MessagePipe.IAsyncPublisher`2",
        };
    }
}
