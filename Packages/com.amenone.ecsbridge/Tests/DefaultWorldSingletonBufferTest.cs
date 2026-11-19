using NUnit.Framework;

namespace Amenone.EcsBridge.Tests
{
    public sealed class DefaultWorldSingletonBufferTest : DefaultWorldFixture
    {
        [Test]
        public void Buffer_WhenMissing_ShouldCreateEmptyBuffer()
        {
            var buffer = new DefaultWorldSingletonBuffer<TestElement>();

            Assert.That(buffer.Buffer().Length, Is.Zero);
        }

        [Test]
        public void Prepare_WhenMissing_ShouldCreateEmptyBuffer()
        {
            new DefaultWorldSingletonBuffer<TestElement>().Prepare();

            Assert.That(EntityCountOf<TestElement>(DefaultWorld), Is.EqualTo(1));
            Assert.That(DefaultWorld.EntityManager.CreateEntityQuery(typeof(TestElement)).GetSingletonBuffer<TestElement>().Length, Is.Zero);
        }

        [Test]
        public void Buffer_AfterAdd_ShouldKeepElements()
        {
            var buffer = new DefaultWorldSingletonBuffer<TestElement>();

            buffer.Buffer().Add(new TestElement { Value = 3 });

            Assert.That(buffer.Buffer().Length, Is.EqualTo(1));
            Assert.That(buffer.Buffer()[0].Value, Is.EqualTo(3));
        }

        [Test]
        public void Buffer_AfterDefaultWorldReplaced_ShouldUseBufferOfNewWorld()
        {
            var buffer = new DefaultWorldSingletonBuffer<TestElement>();
            buffer.Buffer().Add(new TestElement { Value = 3 });

            ReplaceDefaultWorld();

            Assert.That(buffer.Buffer().Length, Is.Zero);
        }
    }
}
