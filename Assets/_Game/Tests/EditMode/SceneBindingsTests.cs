using NUnit.Framework;
using ShaurmaTycoon.UI;

namespace ShaurmaTycoon.Tests
{
    public class SceneBindingsTests
    {
        private sealed class Binding : ISceneBinding
        {
            public int Unbinds;
            public void Unbind() => Unbinds++;
        }

        [Test]
        public void UnbindAll_UnbindsEachTrackedOnce_AndForgetsThem()
        {
            var first = new Binding();
            var second = new Binding();
            SceneBindings.Track(first);
            SceneBindings.Track(second);
            SceneBindings.Track(first);

            SceneBindings.UnbindAll();
            SceneBindings.UnbindAll();

            Assert.AreEqual(1, first.Unbinds);
            Assert.AreEqual(1, second.Unbinds);
        }
    }
}
