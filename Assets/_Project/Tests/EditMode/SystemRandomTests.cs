using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class SystemRandomTests
    {
        [Test]
        public void NextFloat_ReturnsValuesFromZeroUpToOne()
        {
            var random = new SystemRandom(42);

            for (var i = 0; i < 1000; i++)
            {
                var value = random.NextFloat();
                Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void NextFloat_SameSeed_ReturnsSameSequence()
        {
            var a = new SystemRandom(7);
            var b = new SystemRandom(7);

            for (var i = 0; i < 10; i++)
            {
                Assert.That(a.NextFloat(), Is.EqualTo(b.NextFloat()));
            }
        }
    }
}
