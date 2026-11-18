using System.Collections;
using System.Linq;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0026：試合中にゲームパッドが抜けたら自動でポーズし、挿し直したら同じプレイヤーに割り当て直す。</summary>
    public class ControllerDisconnectTests
    {
        private GameBalanceConfig _config;
        private Gamepad _pad;
        private PlayerController _padPlayer;
        private PlayerController _keyboardPlayer;
        private MatchManager _manager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _config.Match.KickoffCountdown = 0.1f;
            _pad = InputSystem.AddDevice<Gamepad>();

            _keyboardPlayer = CreatePlayerWithReader(new Vector3(-4f, 1f, 0f), Team.A,
                PlayerInputReader.KeyboardP1Scheme, PlayerInputReader.AnyDevice);
            _padPlayer = CreatePlayerWithReader(new Vector3(4f, 1f, 0f), Team.B,
                PlayerInputReader.GamepadScheme, _pad.deviceId);
            var ball = TestWorld.CreateBall(_config, new Vector3(0f, 0.5f, 0f));
            _manager = TestWorld.CreateMatchManager(_config, ball);
            yield return new WaitForSeconds(0.3f);
            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            foreach (var device in InputSystem.devices.ToArray())
            {
                if (device is Gamepad)
                {
                    InputSystem.RemoveDevice(device);
                }
            }

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall" or "MatchManager")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PadRemoved_PausesAndCountsDisconnectedPlayer()
        {
            InputSystem.RemoveDevice(_pad);
            yield return null;

            Assert.That(_manager.IsPaused, Is.True);
            Assert.That(_manager.DisconnectedPlayerCount, Is.EqualTo(1));
            Assert.That(_padPlayer.GetComponent<PlayerInputReader>().HasDevice, Is.False);
        }

        [UnityTest]
        public IEnumerator SamePadPluggedBack_IsGivenBackToThatPlayer()
        {
            InputSystem.RemoveDevice(_pad);
            yield return null;

            InputSystem.AddDevice(_pad);
            yield return null;

            var reader = _padPlayer.GetComponent<PlayerInputReader>();
            Assert.That(_manager.DisconnectedPlayerCount, Is.EqualTo(0));
            Assert.That(reader.HasDevice, Is.True);
            Assert.That(reader.DeviceId, Is.EqualTo(_pad.deviceId));
            Assert.That(_manager.IsPaused, Is.True, "挿し直しただけでは再開しない（ポーズ解除で再開）");
        }

        [UnityTest]
        public IEnumerator NewPadPluggedIn_IsGivenToTheDisconnectedPlayer()
        {
            InputSystem.RemoveDevice(_pad);
            yield return null;

            var newPad = InputSystem.AddDevice<Gamepad>();
            yield return null;

            var reader = _padPlayer.GetComponent<PlayerInputReader>();
            Assert.That(reader.HasDevice, Is.True);
            Assert.That(reader.DeviceId, Is.EqualTo(newPad.deviceId));
            Assert.That(_manager.DisconnectedPlayerCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Resume_AfterReconnect_ContinuesMatch()
        {
            InputSystem.RemoveDevice(_pad);
            yield return null;
            InputSystem.AddDevice(_pad);
            yield return null;

            _manager.SetPaused(false);
            var before = _manager.Rules.RemainingTime;
            yield return new WaitForSeconds(0.2f);

            Assert.That(_manager.IsPaused, Is.False);
            Assert.That(_manager.Rules.RemainingTime, Is.LessThan(before));
        }

        [UnityTest]
        public IEnumerator UnusedPadRemoved_DoesNotPause()
        {
            var spare = InputSystem.AddDevice<Gamepad>();
            yield return null;

            InputSystem.RemoveDevice(spare);
            yield return null;

            Assert.That(_manager.IsPaused, Is.False);
            Assert.That(_manager.DisconnectedPlayerCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator NewPadWhileNobodyIsDisconnected_IsNotAssigned()
        {
            var spare = InputSystem.AddDevice<Gamepad>();
            yield return null;

            Assert.That(_padPlayer.GetComponent<PlayerInputReader>().DeviceId, Is.EqualTo(_pad.deviceId));
            Assert.That(_keyboardPlayer.GetComponent<PlayerInputReader>().DeviceId, Is.Not.EqualTo(spare.deviceId));
        }

        private PlayerController CreatePlayerWithReader(Vector3 position, Team team, string scheme, int deviceId)
        {
            var player = TestWorld.CreatePlayer(_config, position, team == Team.A ? Vector3.right : Vector3.left, team);
            player.gameObject.SetActive(false);
            var reader = player.gameObject.AddComponent<PlayerInputReader>();
#if UNITY_EDITOR
            reader.Actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/_Project/Input/MochiHockeyControls.inputactions");
#endif
            reader.Configure(scheme, deviceId);
            player.gameObject.SetActive(true);
            return player;
        }
    }
}
