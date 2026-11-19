using System.Collections;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0031：F3 で出す fps 表示。起動時に自分で作られ、最初は隠れている。</summary>
    public class FpsOverlayTests
    {
        [UnityTest]
        public IEnumerator Overlay_IsInstalledHiddenAndShowsFpsWhenToggled()
        {
            yield return null;
            var overlay = Object.FindFirstObjectByType<FpsOverlay>();
            Assert.That(overlay, Is.Not.Null, "起動時に自分で作られる");

            if (overlay.Visible)
            {
                overlay.Toggle();
            }

            Assert.That(overlay.Visible, Is.False);

            overlay.Toggle();
            Assert.That(overlay.Visible, Is.True);

            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(overlay.Meter.HasReading, Is.True);
            Assert.That(overlay.Text, Does.Contain("fps"));
            Assert.That(overlay.Text, Does.Not.Contain("--"));

            overlay.Toggle();
            Assert.That(overlay.Visible, Is.False);
        }
    }
}
