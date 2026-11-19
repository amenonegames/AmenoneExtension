using NUnit.Framework;

namespace Amenone.EcsBridge.Tests
{
    public sealed class LastPublishedTest
    {
        private LastPublished<int, string> _lastPublished;

        [SetUp]
        public void CreateLastPublished() => _lastPublished = new LastPublished<int, string>();

        [Test]
        public void TryUpdate_FirstValueOfKey_ShouldBeTrue() => Assert.That(_lastPublished.TryUpdate(1, "a"), Is.True);

        [Test]
        public void TryUpdate_SameValueOfKey_ShouldBeFalse()
        {
            _lastPublished.TryUpdate(1, "a");

            Assert.That(_lastPublished.TryUpdate(1, "a"), Is.False);
        }

        [Test]
        public void TryUpdate_ChangedValueOfKey_ShouldBeTrue()
        {
            _lastPublished.TryUpdate(1, "a");

            Assert.That(_lastPublished.TryUpdate(1, "b"), Is.True);
        }

        [Test]
        public void TryUpdate_SameValueOfAnotherKey_ShouldBeTrue()
        {
            _lastPublished.TryUpdate(1, "a");

            Assert.That(_lastPublished.TryUpdate(2, "a"), Is.True);
        }

        [Test]
        public void TryUpdate_SameValueAfterClear_ShouldBeTrue()
        {
            _lastPublished.TryUpdate(1, "a");

            _lastPublished.Clear();

            Assert.That(_lastPublished.TryUpdate(1, "a"), Is.True);
        }
    }
}
