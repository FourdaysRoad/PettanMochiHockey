using System;
using System.Collections.Generic;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class MochiTests
    {
        private const int PlayerA = 1;
        private const int PlayerB = 2;

        // 閾値 3 / 6、再吸着禁止 0.3 秒
        private MochiSettings _settings;
        private Mochi _mochi;
        private List<MochiLevel> _levelChanges;

        [SetUp]
        public void SetUp()
        {
            _settings = TestSettings.CreateMochi();
            _mochi = new Mochi(new MochiGrowth(_settings), _settings);
            _levelChanges = new List<MochiLevel>();
            _mochi.LevelChanged += _levelChanges.Add;
        }

        // ---- 初期状態・コンストラクタ ----

        [Test]
        public void NewMochi_IsLv1WithNoRiceAndNoHolder()
        {
            Assert.That(_mochi.RiceCount, Is.EqualTo(0));
            Assert.That(_mochi.Level, Is.EqualTo(MochiLevel.Lv1));
            Assert.That(_mochi.HolderId, Is.Null);
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new Mochi(null, _settings));
            Assert.Throws<ArgumentNullException>(() => new Mochi(new MochiGrowth(_settings), null));
        }

        // ---- 吸着（REQ-022）----

        [Test]
        public void TryAttach_WhenFree_SetsHolder()
        {
            Assert.That(_mochi.TryAttach(PlayerA), Is.True);
            Assert.That(_mochi.HolderId, Is.EqualTo(PlayerA));
        }

        [Test]
        public void TryAttach_WhenHeldByOther_Fails()
        {
            _mochi.TryAttach(PlayerA);

            Assert.That(_mochi.TryAttach(PlayerB), Is.False);
            Assert.That(_mochi.HolderId, Is.EqualTo(PlayerA));
        }

        [Test]
        public void TryAttach_WhenAlreadyHoldingIt_Succeeds()
        {
            _mochi.TryAttach(PlayerA);

            Assert.That(_mochi.TryAttach(PlayerA), Is.True);
            Assert.That(_mochi.HolderId, Is.EqualTo(PlayerA));
        }

        // ---- もち米の吸収（REQ-012, REQ-013）----

        [Test]
        public void TryAbsorbRice_WithoutHolder_DoesNothing()
        {
            Assert.That(_mochi.TryAbsorbRice(), Is.False);
            Assert.That(_mochi.RiceCount, Is.EqualTo(0));
        }

        [Test]
        public void TryAbsorbRice_WhileHeld_IncrementsRiceCount()
        {
            _mochi.TryAttach(PlayerA);

            Assert.That(_mochi.TryAbsorbRice(), Is.True);
            Assert.That(_mochi.RiceCount, Is.EqualTo(1));
        }

        [Test]
        public void TryAbsorbRice_ReachingThresholds_RaisesLevelChangedOncePerLevel()
        {
            _mochi.TryAttach(PlayerA);

            Absorb(2);
            Assert.That(_levelChanges, Is.Empty);

            Absorb(1); // 3 個 → Lv.2
            Assert.That(_mochi.Level, Is.EqualTo(MochiLevel.Lv2));

            Absorb(2); // 5 個
            Absorb(1); // 6 個 → Lv.3
            Absorb(3); // 9 個

            Assert.That(_mochi.Level, Is.EqualTo(MochiLevel.Lv3));
            Assert.That(_levelChanges, Is.EqualTo(new[] { MochiLevel.Lv2, MochiLevel.Lv3 }));
        }

        // ---- 落とす・手放す（REQ-014, REQ-032）----

        [Test]
        public void Drop_ClearsHolderAndKeepsLevel()
        {
            _mochi.TryAttach(PlayerA);
            Absorb(4);

            _mochi.Drop();

            Assert.That(_mochi.HolderId, Is.Null);
            Assert.That(_mochi.RiceCount, Is.EqualTo(4));
            Assert.That(_mochi.Level, Is.EqualTo(MochiLevel.Lv2));
        }

        [Test]
        public void Release_ClearsHolderAndKeepsLevel()
        {
            _mochi.TryAttach(PlayerA);
            Absorb(6);

            _mochi.Release();

            Assert.That(_mochi.HolderId, Is.Null);
            Assert.That(_mochi.RiceCount, Is.EqualTo(6));
            Assert.That(_mochi.Level, Is.EqualTo(MochiLevel.Lv3));
        }

        [Test]
        public void Drop_DoesNotBlockAnyoneFromReattaching()
        {
            _mochi.TryAttach(PlayerA);
            _mochi.Drop();

            Assert.That(_mochi.TryAttach(PlayerA), Is.True);
            _mochi.Drop();
            Assert.That(_mochi.TryAttach(PlayerB), Is.True);
        }

        [Test]
        public void DropAndRelease_WithoutHolder_DoNothing()
        {
            Assert.DoesNotThrow(() => _mochi.Drop());
            Assert.DoesNotThrow(() => _mochi.Release());

            Assert.That(_mochi.TryAttach(PlayerA), Is.True);
        }

        [Test]
        public void TryAbsorbRice_AfterDrop_DoesNothing()
        {
            _mochi.TryAttach(PlayerA);
            _mochi.Drop();

            Assert.That(_mochi.TryAbsorbRice(), Is.False);
            Assert.That(_mochi.RiceCount, Is.EqualTo(0));
        }

        // ---- 再吸着禁止（REQ-024）----

        [Test]
        public void Release_BlocksSamePlayerUntilCooldownEnds()
        {
            _mochi.TryAttach(PlayerA);
            _mochi.Release();

            Assert.That(_mochi.TryAttach(PlayerA), Is.False);

            _mochi.Tick(0.29f);
            Assert.That(_mochi.TryAttach(PlayerA), Is.False);

            _mochi.Tick(0.01f);
            Assert.That(_mochi.TryAttach(PlayerA), Is.True);
        }

        [Test]
        public void Release_DoesNotBlockOtherPlayer()
        {
            _mochi.TryAttach(PlayerA);
            _mochi.Release();

            Assert.That(_mochi.TryAttach(PlayerB), Is.True);
        }

        [Test]
        public void Release_UsesCooldownFromSettings()
        {
            _settings.RegrabCooldown = 1f;
            var mochi = new Mochi(new MochiGrowth(_settings), _settings);
            mochi.TryAttach(PlayerA);
            mochi.Release();

            mochi.Tick(0.5f);
            Assert.That(mochi.TryAttach(PlayerA), Is.False);

            mochi.Tick(0.5f);
            Assert.That(mochi.TryAttach(PlayerA), Is.True);
        }

        [Test]
        public void Release_ByNewHolder_MovesCooldownToThatPlayer()
        {
            _mochi.TryAttach(PlayerA);
            _mochi.Release();
            _mochi.TryAttach(PlayerB);
            _mochi.Release();

            Assert.That(_mochi.TryAttach(PlayerA), Is.True);
            _mochi.Release();
            Assert.That(_mochi.TryAttach(PlayerB), Is.True);
        }

        [Test]
        public void Tick_NegativeDeltaTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _mochi.Tick(-0.1f));
        }

        private void Absorb(int count)
        {
            for (var i = 0; i < count; i++)
            {
                _mochi.TryAbsorbRice();
            }
        }
    }
}
