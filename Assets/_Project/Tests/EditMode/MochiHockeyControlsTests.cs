using System.Linq;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>MochiHockeyControls.inputactions の中身（REQ-042）。</summary>
    public class MochiHockeyControlsTests
    {
        private const string Path = "Assets/_Project/Input/MochiHockeyControls.inputactions";

        private InputActionAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Path);
            Assert.That(_asset, Is.Not.Null, $"{Path} が見つからない");
        }

        [Test]
        public void HasPlayerMapWithAllActions()
        {
            var map = _asset.FindActionMap("Player");

            Assert.That(map, Is.Not.Null);
            Assert.That(map.actions.Select(a => a.name),
                Is.EquivalentTo(new[] { "Move", "Shoot", "Swing", "Dash", "Pause" }));
        }

        [Test]
        public void HasSchemesUsedByPlayerInputReader()
        {
            Assert.That(_asset.controlSchemes.Select(s => s.bindingGroup), Is.EquivalentTo(new[]
            {
                PlayerInputReader.KeyboardP1Scheme,
                PlayerInputReader.KeyboardP2Scheme,
                PlayerInputReader.GamepadScheme,
            }));
        }

        [TestCase("KeyboardP1", "Dash", "<Keyboard>/l")]
        [TestCase("KeyboardP1", "Shoot", "<Keyboard>/j")]
        [TestCase("KeyboardP1", "Swing", "<Keyboard>/k")]
        [TestCase("KeyboardP2", "Dash", "<Keyboard>/numpad3")]
        [TestCase("KeyboardP2", "Shoot", "<Keyboard>/numpad1")]
        [TestCase("KeyboardP2", "Swing", "<Keyboard>/numpad2")]
        [TestCase("Gamepad", "Dash", "<Gamepad>/rightShoulder")]
        [TestCase("Gamepad", "Shoot", "<Gamepad>/buttonSouth")]
        [TestCase("Gamepad", "Swing", "<Gamepad>/buttonEast")]
        public void ButtonIsBoundForScheme(string scheme, string action, string path)
        {
            var bindings = _asset.FindAction($"Player/{action}").bindings
                .Where(b => b.groups != null && b.groups.Split(';').Contains(scheme))
                .Select(b => b.path);

            Assert.That(bindings, Has.Member(path));
        }

        [TestCase("KeyboardP1", "<Keyboard>/w")]
        [TestCase("KeyboardP2", "<Keyboard>/upArrow")]
        [TestCase("Gamepad", "<Gamepad>/leftStick")]
        public void MoveIsBoundForScheme(string scheme, string path)
        {
            var bindings = _asset.FindAction("Player/Move").bindings
                .Where(b => b.groups != null && b.groups.Split(';').Contains(scheme))
                .Select(b => b.path);

            Assert.That(bindings, Has.Member(path));
        }

        [Test]
        public void KeyboardSchemesDoNotShareMoveOrButtonKeys()
        {
            var p1 = KeysOf("KeyboardP1").Where(k => k != "<Keyboard>/escape");
            var p2 = KeysOf("KeyboardP2").Where(k => k != "<Keyboard>/escape");

            Assert.That(p1.Intersect(p2), Is.Empty);
        }

        private string[] KeysOf(string scheme)
        {
            return _asset.FindActionMap("Player").bindings
                .Where(b => b.groups != null && b.groups.Split(';').Contains(scheme))
                .Select(b => b.path)
                .ToArray();
        }
    }
}
