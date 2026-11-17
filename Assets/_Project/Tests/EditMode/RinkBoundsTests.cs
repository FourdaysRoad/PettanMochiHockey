using System;
using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class RinkBoundsTests
    {
        private const float Tolerance = 1e-4f;

        // X: -12〜12、Z: -7〜7
        private RinkBounds _rink;

        [SetUp]
        public void SetUp()
        {
            _rink = TestSettings.CreateRink();
        }

        [Test]
        public void Constructor_MinNotBelowMax_Throws()
        {
            Assert.Throws<ArgumentException>(() => new RinkBounds(new Vector2(0f, 0f), new Vector2(0f, 5f)));
            Assert.Throws<ArgumentException>(() => new RinkBounds(new Vector2(0f, 5f), new Vector2(5f, 5f)));
            Assert.Throws<ArgumentException>(() => new RinkBounds(new Vector2(5f, 5f), new Vector2(0f, 0f)));
        }

        // ---- 4 方向（REQ-033）----

        [TestCase(1f, 0f, 12f, 0f, -1f, 0f)]
        [TestCase(-1f, 0f, -12f, 0f, 1f, 0f)]
        [TestCase(0f, 1f, 0f, 7f, 0f, -1f)]
        [TestCase(0f, -1f, 0f, -7f, 0f, 1f)]
        public void TryGetWallHit_FromCenter_HitsWallInThatDirection(
            float dx, float dy, float hitX, float hitY, float normalX, float normalY)
        {
            var hit = _rink.TryGetWallHit(Vector2.Zero, new Vector2(dx, dy), out var point, out var normal);

            Assert.That(hit, Is.True);
            AssertVector(point, hitX, hitY);
            AssertVector(normal, normalX, normalY);
        }

        [Test]
        public void TryGetWallHit_DirectionLengthDoesNotMatter()
        {
            _rink.TryGetWallHit(Vector2.Zero, new Vector2(5f, 0f), out var point, out _);

            AssertVector(point, 12f, 0f);
        }

        // ---- 斜め ----

        [Test]
        public void TryGetWallHit_Diagonal_HitsWallReachedFirst()
        {
            // (1, 1) 方向：Z の壁（7 m 先）が X の壁（12 m 先）より先
            var hit = _rink.TryGetWallHit(Vector2.Zero, new Vector2(1f, 1f), out var point, out var normal);

            Assert.That(hit, Is.True);
            AssertVector(point, 7f, 7f);
            AssertVector(normal, 0f, -1f);
        }

        [Test]
        public void TryGetWallHit_ShallowDiagonal_HitsSideWall()
        {
            // (4, 1) 方向：X の壁に 3 秒で届く（Z は 7 秒）
            var hit = _rink.TryGetWallHit(Vector2.Zero, new Vector2(4f, 1f), out var point, out var normal);

            Assert.That(hit, Is.True);
            AssertVector(point, 12f, 3f);
            AssertVector(normal, -1f, 0f);
        }

        [Test]
        public void TryGetWallHit_FromOffCenterPoint_UsesOrigin()
        {
            var hit = _rink.TryGetWallHit(new Vector2(10f, -6f), new Vector2(-1f, 0f), out var point, out var normal);

            Assert.That(hit, Is.True);
            AssertVector(point, -12f, -6f);
            AssertVector(normal, 1f, 0f);
        }

        [Test]
        public void TryGetWallHit_TowardExactCorner_ReturnsSideWall()
        {
            // 角に同時に届く場合は X 側（左右）の壁を返す
            var rink = new RinkBounds(new Vector2(-5f, -5f), new Vector2(5f, 5f));

            var hit = rink.TryGetWallHit(Vector2.Zero, new Vector2(1f, 1f), out var point, out var normal);

            Assert.That(hit, Is.True);
            AssertVector(point, 5f, 5f);
            AssertVector(normal, -1f, 0f);
        }

        [Test]
        public void TryGetWallHit_OnWallMovingOutward_HitsImmediately()
        {
            var hit = _rink.TryGetWallHit(new Vector2(12f, 2f), new Vector2(1f, 0f), out var point, out var normal);

            Assert.That(hit, Is.True);
            AssertVector(point, 12f, 2f);
            AssertVector(normal, -1f, 0f);
        }

        // ---- 当たらない場合 ----

        [Test]
        public void TryGetWallHit_ZeroDirection_ReturnsFalse()
        {
            Assert.That(_rink.TryGetWallHit(Vector2.Zero, Vector2.Zero, out _, out _), Is.False);
        }

        [Test]
        public void TryGetWallHit_OriginOutside_ReturnsFalse()
        {
            Assert.That(_rink.TryGetWallHit(new Vector2(20f, 0f), new Vector2(-1f, 0f), out _, out _), Is.False);
        }

        // ---- Contains ----

        [TestCase(0f, 0f, true)]
        [TestCase(12f, 7f, true)]
        [TestCase(-12f, -7f, true)]
        [TestCase(12.01f, 0f, false)]
        [TestCase(0f, -7.01f, false)]
        public void Contains_ReturnsWhetherPointIsInsideOrOnWall(float x, float y, bool expected)
        {
            Assert.That(_rink.Contains(new Vector2(x, y)), Is.EqualTo(expected));
        }

        private static void AssertVector(Vector2 actual, float x, float y)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(Tolerance), "X");
            Assert.That(actual.Y, Is.EqualTo(y).Within(Tolerance), "Y");
        }
    }
}
