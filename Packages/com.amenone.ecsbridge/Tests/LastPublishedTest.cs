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

        [Test]
        public void TryUpdate_UnseenKeyWithValueTreatedAsPublished_ShouldBeFalse()
        {
            _lastPublished.TreatEveryKeyAsPublished("idle");

            Assert.That(_lastPublished.TryUpdate(1, "idle"), Is.False);
        }

        [Test]
        public void TryUpdate_UnseenKeyWithOtherValue_ShouldBeTrue()
        {
            _lastPublished.TreatEveryKeyAsPublished("idle");

            Assert.That(_lastPublished.TryUpdate(1, "a"), Is.True);
        }

        [Test]
        public void TryUpdate_KeyBackToValueTreatedAsPublished_ShouldBeTrue()
        {
            _lastPublished.TreatEveryKeyAsPublished("idle");
            _lastPublished.TryUpdate(1, "a");

            Assert.That(_lastPublished.TryUpdate(1, "idle"), Is.True);
        }

        [Test]
        public void TryUpdate_KeyPublishedBeforeTreatEveryKey_ShouldCompareWithTreatedValue()
        {
            _lastPublished.TryUpdate(1, "a");

            _lastPublished.TreatEveryKeyAsPublished("idle");

            Assert.That(_lastPublished.TryUpdate(1, "a"), Is.True);
            Assert.That(_lastPublished.TryUpdate(2, "idle"), Is.False);
        }

        [Test]
        public void TryUpdate_ValueTreatedAsPublishedAfterClear_ShouldBeTrue()
        {
            _lastPublished.TreatEveryKeyAsPublished("idle");

            _lastPublished.Clear();

            Assert.That(_lastPublished.TryUpdate(1, "idle"), Is.True);
        }

        [Test]
        public void TryGetPublished_UnseenKey_ShouldBeFalse() => Assert.That(_lastPublished.TryGetPublished(1, out _), Is.False);

        [Test]
        public void TryGetPublished_AfterTryUpdate_ShouldReturnUpdatedValueOfKey()
        {
            _lastPublished.TryUpdate(1, "a");

            Assert.That(_lastPublished.TryGetPublished(1, out var value), Is.True);
            Assert.That(value, Is.EqualTo("a"));
            Assert.That(_lastPublished.TryGetPublished(2, out _), Is.False);
        }

        [Test]
        public void TryGetPublished_UnseenKeyAfterTreatEveryKeyAsPublished_ShouldReturnTreatedValue()
        {
            _lastPublished.TreatEveryKeyAsPublished("idle");

            Assert.That(_lastPublished.TryGetPublished(1, out var value), Is.True);
            Assert.That(value, Is.EqualTo("idle"));
        }

        [Test]
        public void TryGetPublished_AfterTreatAsPublishedForKey_ShouldReturnTreatedValueOfThatKeyOnly()
        {
            _lastPublished.TreatAsPublished(1, "a");

            Assert.That(_lastPublished.TryGetPublished(1, out var value), Is.True);
            Assert.That(value, Is.EqualTo("a"));
            Assert.That(_lastPublished.TryGetPublished(2, out _), Is.False);
        }

        [Test]
        public void TryUpdate_SameValueAfterTreatAsPublishedForKey_ShouldBeFalse()
        {
            _lastPublished.TreatAsPublished(1, "a");

            Assert.That(_lastPublished.TryUpdate(1, "a"), Is.False);
        }

        [Test]
        public void TryGetPublished_AfterClear_ShouldBeFalse()
        {
            _lastPublished.TreatEveryKeyAsPublished("idle");
            _lastPublished.TryUpdate(1, "a");

            _lastPublished.Clear();

            Assert.That(_lastPublished.TryGetPublished(1, out _), Is.False);
            Assert.That(_lastPublished.TryGetPublished(2, out _), Is.False);
        }

        [Test]
        public void TryGetPublished_AfterTreatAsPublishedForKeyOnTreatedEveryKey_ShouldReturnKeyValueAndTreatedValueForOthers()
        {
            _lastPublished.TreatEveryKeyAsPublished("idle");

            _lastPublished.TreatAsPublished(1, "a");

            Assert.That(_lastPublished.TryGetPublished(1, out var first), Is.True);
            Assert.That(first, Is.EqualTo("a"));
            Assert.That(_lastPublished.TryGetPublished(2, out var second), Is.True);
            Assert.That(second, Is.EqualTo("idle"));
        }

        [Test]
        public void TryUpdate_AfterTreatEveryKeyAsPublishedTwice_ShouldCompareWithLatterValue()
        {
            _lastPublished.TreatEveryKeyAsPublished("idle");

            _lastPublished.TreatEveryKeyAsPublished("busy");

            Assert.That(_lastPublished.TryUpdate(1, "busy"), Is.False);
            Assert.That(_lastPublished.TryUpdate(2, "idle"), Is.True);
        }
    }
}
