using NUnit.Framework;
using Root.Abstract;
using RootContainer;
using VContainer;

namespace Messaging.Tests
{
    public sealed class CancellationTokenHolderTest
    {
        [Test]
        public void Get_NotDisposed_ShouldReturnCancelableTokenNotYetCanceled()
        {
            using var holder = new CancellationTokenHolder();

            var token = holder.Get();

            Assert.That(token.CanBeCanceled, Is.True);
            Assert.That(token.IsCancellationRequested, Is.False);
        }

        [Test]
        public void Dispose_CalledOnce_ShouldCancelToken()
        {
            var holder = new CancellationTokenHolder();
            var token = holder.Get();

            holder.Dispose();

            Assert.That(token.IsCancellationRequested, Is.True);
        }

        [Test]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var holder = new CancellationTokenHolder();
            holder.Dispose();

            Assert.DoesNotThrow(holder.Dispose);
        }

        [Test]
        public void Get_AfterDispose_ShouldReturnCanceledToken()
        {
            var holder = new CancellationTokenHolder();
            holder.Dispose();

            Assert.That(holder.Get().IsCancellationRequested, Is.True);
        }

        [Test]
        public void ContainerDispose_RegisteredAsImplementedInterfaces_ShouldCancelToken()
        {
            var builder = new ContainerBuilder();
            builder.Register<CancellationTokenHolder>(Lifetime.Singleton).AsImplementedInterfaces();
            var container = builder.Build();
            var token = container.Resolve<ITokenGettable>().Get();

            container.Dispose();

            Assert.That(token.IsCancellationRequested, Is.True);
        }
    }
}
