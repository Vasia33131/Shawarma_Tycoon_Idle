using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Мягкое свечение-рамка вокруг кнопки, пока она доступна: спокойная замена пульса у кнопок покупки в строках.
    /// Меняет альфу CanvasRenderer, а не Graphic.color, — без перестройки меша каждый кадр.
    /// </summary>
    public sealed class AvailableGlow : MonoBehaviour, IUiAnimated
    {
        [SerializeField] private Button _button = null;
        [SerializeField] private Graphic _glow = null;
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.8f;
        [SerializeField] private float _period = 2f;

        private PulseDriver _driver;

        private void OnEnable()
        {
            if (_driver == null && !Services.TryGet(out _driver))
                return;
            _driver.Register(this);
        }

        private void OnDisable()
        {
            if (_driver != null)
                _driver.Unregister(this);
        }

        public void Animate(float time, float deltaTime, TweenRunner tweens)
        {
            bool visible = _button.IsInteractable();
            if (_glow.enabled != visible)
                _glow.enabled = visible;
            if (!visible)
                return;

            float wave = 0.5f + 0.5f * Mathf.Sin(time * (2f * Mathf.PI / _period));
            _glow.canvasRenderer.SetAlpha(Mathf.Lerp(_minAlpha, _maxAlpha, wave));
        }
    }
}
