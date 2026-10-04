using NUnit.Framework;
using ShaurmaTycoon.Core.Tweening;

namespace ShaurmaTycoon.Tests
{
    public class EaseTests
    {
        [TestCase(Ease.Linear)]
        [TestCase(Ease.InQuad)]
        [TestCase(Ease.OutQuad)]
        [TestCase(Ease.InOutSine)]
        [TestCase(Ease.OutBack)]
        public void Endpoints_AreExact(Ease ease)
        {
            Assert.AreEqual(0f, EaseFunctions.Evaluate(ease, 0f));
            Assert.AreEqual(1f, EaseFunctions.Evaluate(ease, 1f));
            Assert.AreEqual(0f, EaseFunctions.Evaluate(ease, -1f));
            Assert.AreEqual(1f, EaseFunctions.Evaluate(ease, 2f));
        }

        [Test]
        public void Midpoints_HaveExpectedShape()
        {
            Assert.AreEqual(0.5, EaseFunctions.Evaluate(Ease.Linear, 0.5f), 1e-6);
            Assert.AreEqual(0.25, EaseFunctions.Evaluate(Ease.InQuad, 0.5f), 1e-6);
            Assert.AreEqual(0.75, EaseFunctions.Evaluate(Ease.OutQuad, 0.5f), 1e-6);
            Assert.AreEqual(0.5, EaseFunctions.Evaluate(Ease.InOutSine, 0.5f), 1e-6);
        }

        [Test]
        public void OutBack_Overshoots()
        {
            Assert.Greater(EaseFunctions.Evaluate(Ease.OutBack, 0.8f), 1f);
        }
    }
}
