using System;
using System.Collections;
using System.Reflection;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace MochiHockey.Tests.EditMode
{
    public class GameBalanceConfigTests
    {
        private GameBalanceConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            FillWithDistinctValues(_config);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void ToSettings_CopiesMochiValues()
        {
            var mochi = _config.ToSettings().Mochi;

            Assert.That(mochi.Lv2Threshold, Is.EqualTo(4));
            Assert.That(mochi.Lv3Threshold, Is.EqualTo(9));
            Assert.That(mochi.ScaleByLevel, Is.EqualTo(new[] { 1.1f, 1.6f, 2.7f }));
            Assert.That(mochi.PointsByLevel, Is.EqualTo(new[] { 2, 4, 7 }));
            Assert.That(mochi.SpeedMultiplierByLevel, Is.EqualTo(new[] { 0.95f, 0.85f, 0.7f }));
            Assert.That(mochi.RegrabCooldown, Is.EqualTo(0.35f));
        }

        [Test]
        public void ToSettings_CopiesPettanValues()
        {
            var pettan = _config.ToSettings().Pettan;

            Assert.That(pettan.HitStopDuration, Is.EqualTo(0.09f));
            Assert.That(pettan.LaunchNominalDuration, Is.EqualTo(0.65f));
            Assert.That(pettan.LaunchMaxDuration, Is.EqualTo(1.25f));
            Assert.That(pettan.LaunchSpeed, Is.EqualTo(13f));
            Assert.That(pettan.LaunchBounceHeight, Is.EqualTo(1.1f));
            Assert.That(pettan.SlideDownStartTime, Is.EqualTo(1.55f));
            Assert.That(pettan.RecoverTime, Is.EqualTo(2.65f));
            Assert.That(pettan.InvincibleEndTime, Is.EqualTo(3.65f));
            Assert.That(pettan.SlideDownSpeed, Is.EqualTo(0.55f));
            Assert.That(pettan.StuckScale, Is.EqualTo(new System.Numerics.Vector3(1.35f, 1.25f, 0.06f)));
        }

        [Test]
        public void ToSettings_CopiesMatchValues()
        {
            var match = _config.ToSettings().Match;

            Assert.That(match.MatchDuration, Is.EqualTo(170f));
            Assert.That(match.KickoffCountdown, Is.EqualTo(2.5f));
            Assert.That(match.MochiRespawnDelay, Is.EqualTo(1.2f));
        }

        [Test]
        public void ToSettings_CopiesRiceValues()
        {
            var rice = _config.ToSettings().Rice;

            Assert.That(rice.MinCount, Is.EqualTo(4));
            Assert.That(rice.MaxCount, Is.EqualTo(11));
            Assert.That(rice.RefillInterval, Is.EqualTo(1.4f));
        }

        [Test]
        public void ToSettings_CopiesPlayerValues()
        {
            var player = _config.ToSettings().Player;

            Assert.That(player.MaxSpeed, Is.EqualTo(6.5f));
            Assert.That(player.Acceleration, Is.EqualTo(21f));
            Assert.That(player.Friction, Is.EqualTo(3.5f));
            Assert.That(player.StaminaMax, Is.EqualTo(110f));
            Assert.That(player.DashCost, Is.EqualTo(36f));
            Assert.That(player.StaminaRegenPerSecond, Is.EqualTo(21f));
            Assert.That(player.DashImpulse, Is.EqualTo(8.5f));
            Assert.That(player.ChargeMaxTime, Is.EqualTo(1.1f));
            Assert.That(player.ChargeStages, Is.EqualTo(3));
            Assert.That(player.SwingActiveTime, Is.EqualTo(0.16f));
            Assert.That(player.WhiffRecoveryTime, Is.EqualTo(0.45f));
        }

        [Test]
        public void ToSettings_CreatesRinkCenteredOnOriginFromSize()
        {
            var rink = _config.ToSettings().Rink;

            Assert.That(rink.Min, Is.EqualTo(new System.Numerics.Vector2(-13f, -7.5f)));
            Assert.That(rink.Max, Is.EqualTo(new System.Numerics.Vector2(13f, 7.5f)));
        }

        /// <summary>
        /// 設定クラスに項目を足したのに ToSettings で写し忘れた場合に気づけるよう、
        /// すべての公開フィールドが既定値のままになっていないことを確かめる。
        /// </summary>
        [Test]
        public void ToSettings_LeavesNoFieldAtDefault()
        {
            var settings = _config.ToSettings();

            foreach (var child in typeof(BalanceSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var childValue = child.GetValue(settings);
                Assert.That(childValue, Is.Not.Null, child.Name);

                foreach (var field in child.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var value = field.GetValue(childValue);
                    var label = $"{child.Name}.{field.Name}";
                    Assert.That(value, Is.Not.Null, label);
                    if (value is IList list)
                    {
                        Assert.That(list.Count, Is.GreaterThan(0), label);
                    }
                    else if (field.FieldType.IsValueType)
                    {
                        Assert.That(value, Is.Not.EqualTo(Activator.CreateInstance(field.FieldType)), label);
                    }
                }
            }
        }

        [Test]
        public void ToSettings_CopiesArraysInsteadOfSharing()
        {
            var settings = _config.ToSettings();

            _config.Mochi.ScaleByLevel[0] = 99f;
            _config.Mochi.PointsByLevel[0] = 99;
            _config.Mochi.SpeedMultiplierByLevel[0] = 99f;

            Assert.That(settings.Mochi.ScaleByLevel[0], Is.EqualTo(1.1f));
            Assert.That(settings.Mochi.PointsByLevel[0], Is.EqualTo(2));
            Assert.That(settings.Mochi.SpeedMultiplierByLevel[0], Is.EqualTo(0.95f));
        }

        [Test]
        public void ToSettings_InvalidLevelArray_Throws()
        {
            _config.Mochi.PointsByLevel = new[] { 1, 2 };

            Assert.Throws<InvalidOperationException>(() => _config.ToSettings());
        }

        [Test]
        public void ProjectAsset_ConvertsToValidSettings()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(
                "Assets/_Project/ScriptableObjects/GameBalanceConfig.asset");
            Assert.That(asset, Is.Not.Null, "GameBalanceConfig.asset が見つからない");

            Assert.DoesNotThrow(() => asset.ToSettings());
        }

        [Test]
        public void ProjectAsset_MochiBallArraysMatchStagesAndLevels()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(
                "Assets/_Project/ScriptableObjects/GameBalanceConfig.asset");

            Assert.That(asset.MochiBall.ShotSpeedByStage.Length, Is.EqualTo(asset.Player.ChargeStages + 1));
            Assert.That(asset.MochiBall.ShotSpeedMultiplierByLevel.Length, Is.EqualTo(BalanceSettings.MochiLevelCount));
        }

        /// <summary>仕様書の値とは別の、項目ごとに異なる値を入れる（写し間違いを見つけるため）。</summary>
        private static void FillWithDistinctValues(GameBalanceConfig config)
        {
            config.Mochi.Lv2Threshold = 4;
            config.Mochi.Lv3Threshold = 9;
            config.Mochi.ScaleByLevel = new[] { 1.1f, 1.6f, 2.7f };
            config.Mochi.PointsByLevel = new[] { 2, 4, 7 };
            config.Mochi.SpeedMultiplierByLevel = new[] { 0.95f, 0.85f, 0.7f };
            config.Mochi.RegrabCooldown = 0.35f;

            config.Pettan.HitStopDuration = 0.09f;
            config.Pettan.LaunchNominalDuration = 0.65f;
            config.Pettan.LaunchMaxDuration = 1.25f;
            config.Pettan.LaunchSpeed = 13f;
            config.Pettan.LaunchBounceHeight = 1.1f;
            config.Pettan.SlideDownStartTime = 1.55f;
            config.Pettan.RecoverTime = 2.65f;
            config.Pettan.InvincibleEndTime = 3.65f;
            config.Pettan.SlideDownSpeed = 0.55f;
            config.Pettan.StuckScale = new UnityEngine.Vector3(1.35f, 1.25f, 0.06f);

            config.Match.MatchDuration = 170f;
            config.Match.KickoffCountdown = 2.5f;
            config.Match.MochiRespawnDelay = 1.2f;

            config.Rice.MinCount = 4;
            config.Rice.MaxCount = 11;
            config.Rice.RefillInterval = 1.4f;

            config.Player.MaxSpeed = 6.5f;
            config.Player.Acceleration = 21f;
            config.Player.Friction = 3.5f;
            config.Player.StaminaMax = 110f;
            config.Player.DashCost = 36f;
            config.Player.StaminaRegenPerSecond = 21f;
            config.Player.DashImpulse = 8.5f;
            config.Player.ChargeMaxTime = 1.1f;
            config.Player.ChargeStages = 3;
            config.Player.SwingActiveTime = 0.16f;
            config.Player.WhiffRecoveryTime = 0.45f;

            config.Rink.Size = new UnityEngine.Vector2(26f, 15f);
        }
    }
}
