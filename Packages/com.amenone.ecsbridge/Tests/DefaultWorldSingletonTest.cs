using NUnit.Framework;

namespace Amenone.EcsBridge.Tests
{
    public sealed class DefaultWorldSingletonTest : DefaultWorldFixture
    {
        [Test]
        public void Read_FirstTouchOfWorldWithoutSingleton_ShouldCreateWithInitialValue()
        {
            var singleton = new DefaultWorldSingleton<TestValue>(new TestValue { Value = 7 });

            Assert.That(singleton.Read().Value, Is.EqualTo(7));
            Assert.That(EntityCountOf<TestValue>(DefaultWorld), Is.EqualTo(1));
        }

        [Test]
        public void Read_FirstTouchOfWorldWithSingleton_ShouldKeepExistingValue()
        {
            new DefaultWorldSingleton<TestValue>().Write(new TestValue { Value = 5 });

            var another = new DefaultWorldSingleton<TestValue>(new TestValue { Value = 1 });

            Assert.That(another.Read().Value, Is.EqualTo(5));
            Assert.That(EntityCountOf<TestValue>(DefaultWorld), Is.EqualTo(1));
        }

        [Test]
        public void Read_AfterWrite_ShouldReturnWrittenValue()
        {
            var singleton = new DefaultWorldSingleton<TestValue>();

            singleton.Write(new TestValue { Value = 3 });

            Assert.That(singleton.Read().Value, Is.EqualTo(3));
            Assert.That(EntityCountOf<TestValue>(DefaultWorld), Is.EqualTo(1));
        }

        [Test]
        public void Read_AfterRemove_ShouldThrow()
        {
            var singleton = new DefaultWorldSingleton<TestValue>();
            singleton.Prepare();

            singleton.Remove();

            Assert.That(() => singleton.Read(), Throws.InvalidOperationException);
        }

        [Test]
        public void Write_AfterRemove_ShouldCreateSingletonAgain()
        {
            var singleton = new DefaultWorldSingleton<TestValue>();
            singleton.Prepare();
            singleton.Remove();

            singleton.Write(new TestValue { Value = 2 });

            Assert.That(singleton.Read().Value, Is.EqualTo(2));
        }

        [Test]
        public void Prepare_WhenMissing_ShouldCreateSingleton()
        {
            new DefaultWorldSingleton<TestValue>().Prepare();

            Assert.That(EntityCountOf<TestValue>(DefaultWorld), Is.EqualTo(1));
        }

        [Test]
        public void Exists_WhenNotCreatingMissing_ShouldBeFalse()
        {
            var singleton = new DefaultWorldSingleton<TestValue>(createsWhenMissing: false);

            Assert.That(singleton.Exists(), Is.False);
            Assert.That(EntityCountOf<TestValue>(DefaultWorld), Is.Zero);
        }

        [Test]
        public void Write_WhenNotCreatingMissing_ShouldCreateSingleton()
        {
            var singleton = new DefaultWorldSingleton<TestValue>(createsWhenMissing: false);

            singleton.Write(new TestValue { Value = 4 });

            Assert.That(singleton.Exists(), Is.True);
            Assert.That(singleton.Read().Value, Is.EqualTo(4));
        }

        [Test]
        public void Remove_WhenExists_ShouldDestroySingleton()
        {
            var singleton = new DefaultWorldSingleton<TestValue>(createsWhenMissing: false);
            singleton.Write(new TestValue { Value = 4 });

            singleton.Remove();

            Assert.That(singleton.Exists(), Is.False);
        }

        [Test]
        public void Read_AfterDefaultWorldReplaced_ShouldFollowNewWorld()
        {
            var singleton = new DefaultWorldSingleton<TestValue>(new TestValue { Value = 1 });
            singleton.Write(new TestValue { Value = 5 });
            var previousWorld = DefaultWorld;

            var nextWorld = ReplaceDefaultWorld();

            Assert.That(singleton.Read().Value, Is.EqualTo(1));
            Assert.That(EntityCountOf<TestValue>(nextWorld), Is.EqualTo(1));
            Assert.That(EntityCountOf<TestValue>(previousWorld), Is.EqualTo(1));
        }

        [Test]
        public void Read_AfterDefaultWorldDisposedAndReplaced_ShouldFollowNewWorld()
        {
            var singleton = new DefaultWorldSingleton<TestValue>(new TestValue { Value = 1 });
            singleton.Write(new TestValue { Value = 5 });
            DefaultWorld.Dispose();

            ReplaceDefaultWorld();

            Assert.That(singleton.Read().Value, Is.EqualTo(1));
        }
    }
}
