namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    internal static class MessagePipeStub
    {
        public const string Source = @"
namespace MessagePipe
{
    public interface IPublisher<TMessage> { void Publish(TMessage message); }
    public interface IAsyncPublisher<TMessage> {}
    public interface ISubscriber<TMessage> {}
    public interface IAsyncSubscriber<TMessage> {}
    public interface IBufferedSubscriber<TMessage> {}
    public interface IBufferedAsyncSubscriber<TMessage> {}
    public interface ISubscriber<TKey, TMessage> {}
    public interface IAsyncSubscriber<TKey, TMessage> {}
    public interface IScopedSubscriber<TMessage> : ISubscriber<TMessage> {}
    public interface IRequestHandler<TRequest, TResponse> {}
}
";
    }
}
