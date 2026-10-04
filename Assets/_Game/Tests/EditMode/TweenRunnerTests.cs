using NUnit.Framework;
using ShaurmaTycoon.Core.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Тесты с объектами Unity — только в Unity Test Runner.</summary>
    public class TweenRunnerTests
    {
        private sealed class Listener : ITweenListener
        {
            public int Calls;
            public int LastTag = -1;

            public void OnTweenCompleted(int tag)
            {
                Calls++;
                LastTag = tag;
            }
        }

        private GameObject _go;
        private RectTransform _rect;
        private TweenRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("tween-test", typeof(RectTransform), typeof(Image));
            _rect = (RectTransform)_go.transform;
            _runner = new TweenRunner(4);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void Scale_ReachesTarget_AndCallsListener()
        {
            var listener = new Listener();
            _runner.Scale(_rect, Vector3.zero, Vector3.one * 2f, 1f, Ease.Linear, listener: listener, tag: 7);

            _runner.Tick(0.5f);
            Assert.AreEqual(1f, _rect.localScale.x, 1e-4f);

            _runner.Tick(0.6f);
            Assert.AreEqual(2f, _rect.localScale.x, 1e-4f);
            Assert.AreEqual(1, listener.Calls);
            Assert.AreEqual(7, listener.LastTag);
            Assert.AreEqual(0, _runner.ActiveCount);
        }

        [Test]
        public void Move_WithArc_PeaksInMiddle()
        {
            _runner.Move(_rect, Vector2.zero, new Vector2(100f, 0f), 1f, Ease.Linear, arc: 50f);

            _runner.Tick(0.5f);

            Assert.AreEqual(50f, _rect.anchoredPosition.x, 1e-3f);
            Assert.AreEqual(50f, _rect.anchoredPosition.y, 1e-3f);
        }

        [Test]
        public void Fade_ChangesAlpha()
        {
            var image = _go.GetComponent<Image>();
            _runner.Fade(image, 1f, 0f, 1f, Ease.Linear);

            _runner.Tick(1f);

            Assert.AreEqual(0f, image.color.a, 1e-4f);
        }

        [Test]
        public void SamePropertyTween_ReplacesPrevious()
        {
            _runner.Scale(_rect, Vector3.one, Vector3.one * 3f, 1f, Ease.Linear);
            _runner.Scale(_rect, Vector3.one, Vector3.one * 5f, 1f, Ease.Linear);

            Assert.AreEqual(1, _runner.ActiveCount);
            _runner.Tick(1f);
            Assert.AreEqual(5f, _rect.localScale.x, 1e-4f);
        }

        [Test]
        public void Delay_PostponesStart()
        {
            _runner.Scale(_rect, Vector3.one, Vector3.one * 2f, 1f, Ease.Linear, delay: 0.5f);

            _runner.Tick(0.4f);
            Assert.AreEqual(1f, _rect.localScale.x, 1e-4f);
            Assert.IsTrue(_runner.IsTweening(_rect, TweenProperty.Scale));
        }

        [Test]
        public void DestroyedTarget_IsDropped()
        {
            _runner.Scale(_rect, Vector3.one, Vector3.one * 2f, 1f, Ease.Linear);
            Object.DestroyImmediate(_go);

            _runner.Tick(0.1f);

            Assert.AreEqual(0, _runner.ActiveCount);
        }

        [Test]
        public void Kill_WithComplete_SetsFinalValue()
        {
            _runner.Scale(_rect, Vector3.one, Vector3.one * 4f, 1f, Ease.Linear);

            _runner.Kill(_rect, TweenProperty.Scale, complete: true);

            Assert.AreEqual(4f, _rect.localScale.x, 1e-4f);
            Assert.IsFalse(_runner.IsTweening(_rect, TweenProperty.Scale));
        }
    }
}
