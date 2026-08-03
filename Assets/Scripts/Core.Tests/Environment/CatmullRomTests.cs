using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Environment;

namespace ProjectAstra.Core.Tests.Environment
{
    [TestFixture]
    public class CatmullRomTests
    {
        private static readonly List<Vector2> Path = new()
        {
            new Vector2(0, 0), new Vector2(4, 3), new Vector2(8, 0), new Vector2(12, 2)
        };

        [Test]
        public void StartsAtFirstPoint()
        {
            Assert.AreEqual(Path[0], CatmullRom.Evaluate(Path, 0f));
        }

        [Test]
        public void EndsAtLastPoint()
        {
            Vector2 end = CatmullRom.Evaluate(Path, 1f);
            Assert.That(Vector2.Distance(end, Path[^1]), Is.LessThan(0.001f));
        }

        [Test]
        public void PassesThroughInteriorControlPoints()
        {
            // With 4 points → 3 segments; t = 1/3 and 2/3 land exactly on points 1 and 2.
            Vector2 mid1 = CatmullRom.Evaluate(Path, 1f / 3f);
            Vector2 mid2 = CatmullRom.Evaluate(Path, 2f / 3f);
            Assert.That(Vector2.Distance(mid1, Path[1]), Is.LessThan(0.001f));
            Assert.That(Vector2.Distance(mid2, Path[2]), Is.LessThan(0.001f));
        }

        [Test]
        public void ClampsTBeyondRange()
        {
            Assert.AreEqual(CatmullRom.Evaluate(Path, 1f), CatmullRom.Evaluate(Path, 5f));
            Assert.AreEqual(CatmullRom.Evaluate(Path, 0f), CatmullRom.Evaluate(Path, -2f));
        }

        [Test]
        public void SinglePoint_ReturnsThatPoint()
        {
            var one = new List<Vector2> { new(5, 5) };
            Assert.AreEqual(new Vector2(5, 5), CatmullRom.Evaluate(one, 0.5f));
        }

        [Test]
        public void EmptyOrNull_ReturnsZero()
        {
            Assert.AreEqual(Vector2.zero, CatmullRom.Evaluate(new List<Vector2>(), 0.5f));
            Assert.AreEqual(Vector2.zero, CatmullRom.Evaluate(null, 0.5f));
        }

        [Test]
        public void StraightLine_EvaluatesOnTheLine()
        {
            var line = new List<Vector2> { new(0, 0), new(10, 0) };
            Vector2 mid = CatmullRom.Evaluate(line, 0.5f);
            Assert.That(mid.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(mid.x, Is.EqualTo(5f).Within(0.001f));
        }
    }
}
