using System.Linq;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class CoreAssemblyTests
    {
        [Test]
        public void Core_DoesNotReferenceUnityEngine()
        {
            var referenced = typeof(CoreAssembly).Assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor"))
                .ToArray();

            Assert.That(referenced, Is.Empty,
                "MochiHockey.Core は UnityEngine 非依存にする（CLAUDE.md）");
        }
    }
}
