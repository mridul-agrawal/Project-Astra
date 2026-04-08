using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class StatArrayTests
    {
        [Test]
        public void Create_ReturnsNineZeroes()
        {
            var arr = StatArray.Create();
            for (int i = 0; i < StatArray.Length; i++)
                Assert.AreEqual(0, arr[(StatIndex)i]);
        }

        [Test]
        public void Indexer_SetAndGet_RoundTrips()
        {
            var arr = StatArray.Create();
            arr[StatIndex.HP] = 20;
            arr[StatIndex.Str] = 8;
            arr[StatIndex.Niyati] = 5;

            Assert.AreEqual(20, arr[StatIndex.HP]);
            Assert.AreEqual(8, arr[StatIndex.Str]);
            Assert.AreEqual(5, arr[StatIndex.Niyati]);
        }

        [Test]
        public void From_SetsAllValues()
        {
            var arr = StatArray.From(20, 8, 3, 7, 9, 5, 2, 6, 4);

            Assert.AreEqual(20, arr[StatIndex.HP]);
            Assert.AreEqual(8, arr[StatIndex.Str]);
            Assert.AreEqual(3, arr[StatIndex.Mag]);
            Assert.AreEqual(7, arr[StatIndex.Skl]);
            Assert.AreEqual(9, arr[StatIndex.Spd]);
            Assert.AreEqual(5, arr[StatIndex.Def]);
            Assert.AreEqual(2, arr[StatIndex.Res]);
            Assert.AreEqual(6, arr[StatIndex.Con]);
            Assert.AreEqual(4, arr[StatIndex.Niyati]);
        }

        [Test]
        public void Indexer_OnUninitializedStruct_AutoInitializes()
        {
            StatArray arr = default;
            Assert.AreEqual(0, arr[StatIndex.HP]);
            arr[StatIndex.Str] = 5;
            Assert.AreEqual(5, arr[StatIndex.Str]);
        }

        [Test]
        public void Length_IsNine()
        {
            Assert.AreEqual(9, StatArray.Length);
        }
    }
}
