using NUnit.Framework;
using Unity.Entities;

namespace Amenone.EcsBridge.Tests
{
    public sealed class DefaultWorldQueryTest : DefaultWorldFixture
    {
        [Test]
        public void Current_WhenDefaultWorldHasMatchingEntities_ShouldMatchThem()
        {
            var query = new DefaultWorldQuery(ComponentType.ReadOnly<TestValue>());
            CreateValueEntities(DefaultWorld, 2);

            Assert.That(query.Current().CalculateEntityCount(), Is.EqualTo(2));
        }

        [Test]
        public void Current_AfterDefaultWorldReplaced_ShouldMatchEntitiesOfNewWorld()
        {
            var query = new DefaultWorldQuery(ComponentType.ReadOnly<TestValue>());
            CreateValueEntities(DefaultWorld, 2);
            query.Current();

            var nextWorld = ReplaceDefaultWorld();
            CreateValueEntities(nextWorld, 1);

            Assert.That(query.Current().CalculateEntityCount(), Is.EqualTo(1));
        }

        [Test]
        public void Current_WhenDefaultWorldUnchanged_ShouldReuseQuery()
        {
            var query = new DefaultWorldQuery(ComponentType.ReadOnly<TestValue>());

            Assert.That(query.Current(), Is.EqualTo(query.Current()));
        }

        private static void CreateValueEntities(World world, int count)
        {
            for (var i = 0; i < count; i++) world.EntityManager.CreateEntity(typeof(TestValue));
        }
    }
}
