using System.Linq;
using MochiHockey.Runtime;
using NUnit.Framework;

namespace MochiHockey.Tests.PlayMode
{
    public class RuntimeAssemblyTests
    {
        [Test]
        public void Runtime_ReferencesCore()
        {
            var referenced = typeof(RuntimeAssembly).Assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name);

            Assert.That(referenced, Does.Contain("MochiHockey.Core"));
        }
    }
}
