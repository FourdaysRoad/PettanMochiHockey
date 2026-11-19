using System.Collections;
using System.Collections.Generic;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0029：効果音と BGM（コードで合成した音）を、ゲームのできごとに合わせて鳴らす。</summary>
    public class AudioManagerTests
    {
        private GameBalanceConfig _config;
        private AudioManager _audio;
        private readonly List<SoundId> _played = new List<SoundId>();
        private bool _wasUnlocked;

        [SetUp]
        public void SetUp()
        {
            _wasUnlocked = AudioUnlocker.Unlocked;
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _played.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall" or "AudioManager" or "Wall")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            if (_wasUnlocked)
            {
                AudioUnlocker.Unlock();
            }
            else
            {
                AudioUnlocker.LockForTests();
            }

            yield return null;
        }

        // ---- 最初の入力まで鳴らさない（REQ-052）----

        [UnityTest]
        public IEnumerator BeforeFirstInput_NothingPlays()
        {
            AudioUnlocker.LockForTests();
            CreateAudio();
            yield return null;
            yield return null;

            _audio.Play(SoundId.Pon);

            Assert.That(_audio.BgmStarted, Is.False);
            Assert.That(_audio.PlayedCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator AfterFirstInput_BgmLoops()
        {
            AudioUnlocker.LockForTests();
            CreateAudio();
            yield return null;
            Assert.That(_audio.BgmStarted, Is.False);

            AudioUnlocker.Unlock();
            yield return null;

            Assert.That(_audio.BgmStarted, Is.True);
            var source = _audio.BgmSource;
            Assert.That(source.clip, Is.Not.Null);
            Assert.That(source.loop, Is.True);
            Assert.That(source.volume, Is.EqualTo(_config.Audio.BgmVolume).Within(1e-4f));
            Assert.That(source.clip.frequency, Is.EqualTo(_config.Audio.SampleRate));
            Assert.That(source.clip.length, Is.EqualTo(BgmSynth.Bars * 4 * 60f / _config.Audio.BgmTempo).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator BgmCanBeTurnedOff()
        {
            AudioUnlocker.Unlock();
            CreateAudio(playBgm: false);
            yield return null;

            Assert.That(_audio.BgmStarted, Is.False);
        }

        [UnityTest]
        public IEnumerator MissingConfig_LogsErrorAndDisablesItself()
        {
            var go = new GameObject("AudioManager");
            go.SetActive(false);
            var audio = go.AddComponent<AudioManager>();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("GameBalanceConfig is not assigned"));
            go.SetActive(true);
            yield return null;

            Assert.That(audio.enabled, Is.False);
        }

        // ---- プレイヤー ----

        [UnityTest]
        public IEnumerator Hit_PlaysPettan_ThenStuck_ThenPon()
        {
            AudioUnlocker.Unlock();
            CreateAudio();
            var attacker = TestWorld.CreatePlayer(_config, new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = TestWorld.CreatePlayer(_config, new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            attacker.RequestSwing();
            Assert.That(_played, Does.Contain(SoundId.Swing), "振った音");

            var limit = Time.time + 4f;
            while (target.PettanState != PettanState.Invincible && Time.time < limit)
            {
                yield return null;
            }

            Assert.That(_played, Does.Contain(SoundId.Pettan));
            Assert.That(_played, Does.Contain(SoundId.Stuck));
            Assert.That(_played, Does.Contain(SoundId.Pon), "復帰で「ポンッ！」（REQ-034）");
            Assert.That(_played.IndexOf(SoundId.Pettan), Is.LessThan(_played.IndexOf(SoundId.Stuck)));
            Assert.That(_played.IndexOf(SoundId.Stuck), Is.LessThan(_played.IndexOf(SoundId.Pon)));
        }

        [UnityTest]
        public IEnumerator Dash_PlaysDash()
        {
            AudioUnlocker.Unlock();
            CreateAudio();
            var player = TestWorld.CreatePlayer(_config, Vector3.zero, Vector3.right, Team.A);
            yield return null;

            player.RequestDash();
            yield return null;

            Assert.That(_played, Does.Contain(SoundId.Dash));
        }

        // ---- 餅 ----

        [UnityTest]
        public IEnumerator Ball_ShotRiceAndLevelUp()
        {
            AudioUnlocker.Unlock();
            CreateAudio();
            var player = TestWorld.CreatePlayer(_config, Vector3.zero, Vector3.right, Team.A);
            var ball = TestWorld.CreateBall(_config, new Vector3(1.2f, 1f, 0f));
            yield return null;
            Assert.That(ball.TryAttach(player), Is.True);

            ball.TryAbsorbRice();
            Assert.That(_audio.LastSound, Is.EqualTo(SoundId.RiceGet));
            ball.TryAbsorbRice();
            ball.TryAbsorbRice(); // 3 粒で Lv.2
            Assert.That(ball.Level, Is.EqualTo(MochiLevel.Lv2));
            Assert.That(_audio.LastSound, Is.EqualTo(SoundId.LevelUp));

            ball.Shoot(player, 0);
            Assert.That(_audio.LastSound, Is.EqualTo(SoundId.Shot));
        }

        [UnityTest]
        public IEnumerator Ball_HittingWallFast_PlaysBounce()
        {
            AudioUnlocker.Unlock();
            CreateAudio();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.position = new Vector3(3f, 1f, 0f);
            wall.transform.localScale = new Vector3(0.5f, 3f, 5f);
            var ball = TestWorld.CreateBall(_config, new Vector3(0f, 1f, 0f));
            yield return null;

            ball.GetComponent<Rigidbody>().linearVelocity = Vector3.right * 10f;
            yield return new WaitForSeconds(0.5f);

            Assert.That(_played, Does.Contain(SoundId.BallBounce));
        }

        [UnityTest]
        public IEnumerator Ball_TouchingWallSlowly_IsSilent()
        {
            AudioUnlocker.Unlock();
            CreateAudio();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.position = new Vector3(1f, 1f, 0f);
            wall.transform.localScale = new Vector3(0.5f, 3f, 5f);
            var ball = TestWorld.CreateBall(_config, new Vector3(0f, 1f, 0f));
            yield return null;

            ball.GetComponent<Rigidbody>().linearVelocity = Vector3.right * 0.5f;
            yield return new WaitForSeconds(1.5f);

            Assert.That(_played, Has.No.Member(SoundId.BallBounce));
        }

        // ---- 試合 ----

        [Test]
        public void Rules_KickoffWhistleGoalAndFinalWhistle()
        {
            AudioUnlocker.Unlock();
            CreateAudio();
            var settings = _config.ToSettings();
            var rules = new MatchRules(settings.Match, new MochiGrowth(settings.Mochi));
            _audio.BindRules(rules);

            rules.StartKickoff();
            rules.Tick(_config.Match.KickoffCountdown + 0.1f);
            Assert.That(rules.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(_audio.LastSound, Is.EqualTo(SoundId.Whistle));

            rules.RegisterGoal(Team.B, MochiLevel.Lv1);
            Assert.That(_audio.LastSound, Is.EqualTo(SoundId.Goal));

            rules.Tick(_config.Match.MochiRespawnDelay + 0.1f);
            Assert.That(_played.FindAll(s => s == SoundId.Whistle).Count, Is.EqualTo(1), "ゴールのあとの再開では笛を鳴らさない");

            rules.Tick(_config.Match.MatchDuration + 1f);
            Assert.That(rules.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(_audio.LastSound, Is.EqualTo(SoundId.FinalWhistle));
        }

        [Test]
        public void Kickoff_BeepsThreeTimesThenWhistles()
        {
            AudioUnlocker.Unlock();
            CreateAudio();
            var settings = _config.ToSettings();
            var rules = new MatchRules(settings.Match, new MochiGrowth(settings.Mochi));
            _audio.BindRules(rules);
            rules.StartKickoff();

            for (var t = 0f; t < _config.Match.KickoffCountdown + 0.05f; t += 0.1f)
            {
                _audio.UpdateCountdown();
                rules.Tick(0.1f);
            }

            _audio.UpdateCountdown();
            Assert.That(_played.FindAll(s => s == SoundId.Count).Count, Is.EqualTo(3), "3・2・1");
            Assert.That(_audio.LastSound, Is.EqualTo(SoundId.Whistle), "GO! で笛");
        }

        [Test]
        public void Play_UsesEveryVoiceInTurn()
        {
            AudioUnlocker.Unlock();
            CreateAudio();

            for (var i = 0; i < _config.Audio.Voices + 2; i++)
            {
                _audio.Play(SoundId.RiceGet);
            }

            Assert.That(_audio.PlayedCount, Is.EqualTo(_config.Audio.Voices + 2));
            Assert.That(_audio.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(_config.Audio.Voices + 1), "効果音の数 ＋ BGM");
        }

        private void CreateAudio(bool playBgm = true)
        {
            var go = new GameObject("AudioManager");
            go.SetActive(false);
            _audio = go.AddComponent<AudioManager>();
            _audio.Config = _config;
            _audio.PlayBgm = playBgm;
            _audio.Played += id => _played.Add(id);
            go.SetActive(true);
        }
    }
}
