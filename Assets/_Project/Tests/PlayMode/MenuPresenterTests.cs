using System.Collections;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0022：タイトル → 対戦設定の画面遷移、参加枠の表示、音声の有効化。</summary>
    public class MenuPresenterTests
    {
        private const int Keyboard = 1;
        private const int Pad = 9001;

        private GameObject _ui;
        private PlayerJoinManager _join;
        private MenuPresenter _presenter;
        private VisualElement _root;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _join = new GameObject("JoinManager").AddComponent<PlayerJoinManager>();

            _ui = new GameObject("MenuUI");
            _ui.SetActive(false);
            var document = _ui.AddComponent<UIDocument>();
#if UNITY_EDITOR
            document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Assets/_Project/UI/MochiPanelSettings.asset");
            document.visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/UI/Menu/Menu.uxml");
#endif
            Assert.That(document.panelSettings, Is.Not.Null, "MochiPanelSettings.asset がない");
            Assert.That(document.visualTreeAsset, Is.Not.Null, "Menu.uxml がない");
            _presenter = _ui.AddComponent<MenuPresenter>();
            _presenter.JoinManager = _join;
            _ui.SetActive(true);
            _root = document.rootVisualElement;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_ui);
            Object.Destroy(_join.gameObject);
        }

        [Test]
        public void StartsOnTitleWithoutAcceptingJoins()
        {
            Assert.That(IsShown("title-screen"), Is.True);
            Assert.That(IsShown("setup-screen"), Is.False);
            Assert.That(_root.Q<Label>("game-title").text, Is.EqualTo(UiText.GameTitle));
            Assert.That(_join.AcceptingInput, Is.False);
        }

        [UnityTest]
        public IEnumerator ProceedFromTitle_ShowsSetupUnlocksAudioAndAcceptsJoinsFromNextFrame()
        {
            _presenter.ProceedFromTitle();

            Assert.That(IsShown("title-screen"), Is.False);
            Assert.That(IsShown("setup-screen"), Is.True);
            Assert.That(AudioUnlocker.Unlocked, Is.True);
            Assert.That(AudioListener.pause, Is.False);
            Assert.That(_root.Q<Label>("gamepad-hint").text, Is.EqualTo(UiText.GamepadHint));
            Assert.That(_root.Q<Label>("start-hint").text, Is.EqualTo(UiText.NeedTwoOrFour));

            yield return null;
            Assert.That(_join.AcceptingInput, Is.True);
        }

        [Test]
        public void JoinedPlayers_AppearInSlotsWithDeviceAndTeam()
        {
            _presenter.ProceedFromTitle();

            _join.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme);
            _join.TryJoin(Pad, PlayerInputReader.GamepadScheme);

            var slot0 = _root.Q("slot-0");
            var slot1 = _root.Q("slot-1");
            var slot2 = _root.Q("slot-2");
            Assert.That(slot0.Q<Label>(className: "slot-device").text, Is.EqualTo(UiText.DeviceKeyboardP1));
            Assert.That(slot0.Q<Label>(className: "slot-team").text, Is.EqualTo(UiText.TeamA));
            Assert.That(slot0.ClassListContains("slot--team-a"), Is.True);
            Assert.That(slot1.Q<Label>(className: "slot-device").text, Is.EqualTo(UiText.DeviceGamepad));
            Assert.That(slot1.ClassListContains("slot--team-b"), Is.True);
            Assert.That(slot2.Q<Label>(className: "slot-device").text, Is.EqualTo(UiText.SlotEmpty));
            Assert.That(slot2.ClassListContains("slot--joined"), Is.False);
            Assert.That(_root.Q<Label>("start-hint").text, Is.EqualTo(UiText.PressToStart));
        }

        [Test]
        public void SwitchingTeam_UpdatesSlotAndStartHint()
        {
            _presenter.ProceedFromTitle();
            _join.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme);
            _join.TryJoin(Keyboard, PlayerInputReader.KeyboardP2Scheme);

            _join.SetTeamOf(Keyboard, PlayerInputReader.KeyboardP2Scheme, MochiHockey.Core.Team.A);

            var slot1 = _root.Q("slot-1");
            Assert.That(slot1.Q<Label>(className: "slot-team").text, Is.EqualTo(UiText.TeamA));
            Assert.That(slot1.ClassListContains("slot--team-a"), Is.True);
            Assert.That(slot1.ClassListContains("slot--team-b"), Is.False);
            Assert.That(_root.Q<Label>("start-hint").text, Is.EqualTo(UiText.TeamsMustBeEven));
        }

        private bool IsShown(string name) => _root.Q(name).resolvedStyle.display != DisplayStyle.None
                                            && _root.Q(name).style.display != DisplayStyle.None;
    }
}
