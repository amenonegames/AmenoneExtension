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

        [Test]
        public void TryUpdate_ValueTreatedAsPublished_ShouldBeFalse()
        {
            _lastPublished.TreatAsPublished(0);

            Assert.That(_lastPublished.TryUpdate(0), Is.False);
        }

        [Test]
        public void TryUpdate_ValueOtherThanTreatedAsPublished_ShouldBeTrue()
        {
            _lastPublished.TreatAsPublished(0);

            Assert.That(_lastPublished.TryUpdate(1), Is.True);
        }

        [Test]
        public void TryUpdate_PublishedValueAfterAnotherTreatedAsPublished_ShouldBeTrue()
        {
            _lastPublished.TryUpdate(3);

            _lastPublished.TreatAsPublished(0);

            Assert.That(_lastPublished.TryUpdate(3), Is.True);
        }

        [Test]
        public void TryUpdate_ValueTreatedAsPublishedAfterClear_ShouldBeTrue()
        {
            _lastPublished.TreatAsPublished(0);

            _lastPublished.Clear();

            Assert.That(_lastPublished.TryUpdate(0), Is.True);
        }

        [Test]
        public void TryGetPublished_BeforeAnyUpdate_ShouldBeFalse() => Assert.That(_lastPublished.TryGetPublished(out _), Is.False);

        [Test]
        public void TryGetPublished_AfterTryUpdate_ShouldReturnUpdatedValue()
        {
            _lastPublished.TryUpdate(3);

            Assert.That(_lastPublished.TryGetPublished(out var value), Is.True);
            Assert.That(value, Is.EqualTo(3));
        }

        [Test]
        public void TryGetPublished_AfterTreatAsPublished_ShouldReturnTreatedValue()
        {
            _lastPublished.TreatAsPublished(5);

            Assert.That(_lastPublished.TryGetPublished(out var value), Is.True);
            Assert.That(value, Is.EqualTo(5));
        }

        [Test]
        public void TryGetPublished_AfterClear_ShouldBeFalse()
        {
            _lastPublished.TryUpdate(3);

            _lastPublished.Clear();

            Assert.That(_lastPublished.TryGetPublished(out _), Is.False);
        }

        [Test]
        public void TryGetPublished_AfterTreatAsPublishedDefault_ShouldBeTrue()
        {
            _lastPublished.TreatAsPublished(default);

            Assert.That(_lastPublished.TryGetPublished(out var value), Is.True);
            Assert.That(value, Is.Zero);
        }

        [Test]
        public void TryUpdate_DefaultAfterTreatAsPublishedDefault_ShouldBeFalse()
        {
            _lastPublished.TreatAsPublished(default);

            Assert.That(_lastPublished.TryUpdate(default), Is.False);
        }
    }
}
