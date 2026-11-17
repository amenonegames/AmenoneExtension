using NUnit.Framework;

namespace Amenone.EcsBridge.Tests
{
    public sealed class LastPublishedValueTest
    {
        private LastPublishedValue<int> _lastPublished;

        [SetUp]
        public void CreateLastPublished() => _lastPublished = new LastPublishedValue<int>();

        [Test]
        public void TryUpdate_FirstDefaultValue_ShouldBeTrue() => Assert.That(_lastPublished.TryUpdate(0), Is.True);

        [Test]
        public void TryUpdate_SameValue_ShouldBeFalse()
        {
            _lastPublished.TryUpdate(3);

            Assert.That(_lastPublished.TryUpdate(3), Is.False);
        }

        [Test]
        public void TryUpdate_ChangedValue_ShouldBeTrue()
        {
            _lastPublished.TryUpdate(3);

            Assert.That(_lastPublished.TryUpdate(4), Is.True);
        }

        [Test]
        public void TryUpdate_SameValueAfterClear_ShouldBeTrue()
        {
            _lastPublished.TryUpdate(3);

            _lastPublished.Clear();

            Assert.That(_lastPublished.TryUpdate(3), Is.True);
        }
    }
}
