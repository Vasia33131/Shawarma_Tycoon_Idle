using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// «Дыхание» элемента. Если задана кнопка — пульсирует, пока она активна; иначе — пока Active = true.
    /// Анимирует PulseDriver (без своего Update); пока на объекте идёт твин масштаба (нажатие), не мешает ему.
    /// </summary>
    public sealed class PulseEffect : MonoBehaviour, IUiAnimated
    {
        [Tooltip("Пульсировать, пока эта кнопка интерактивна. Пусто — управлять через Active.")]
        [SerializeField] private Button _whileInteractable = null;
        [SerializeField] private float _amplitude = 0.04f;
        [SerializeField] private float _speed = 2.5f;

        private RectTransform _rect;
        private PulseDriver _driver;
        private bool _pulsing;

        public bool Active { get; set; }

        private bool ShouldPulse => _whileInteractable != null ? _whileInteractable.interactable : Active;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

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
            StopPulse();
        }

        public void Animate(float time, float deltaTime, TweenRunner tweens)
        {
            if (!ShouldPulse)
            {
                StopPulse();
                return;
            }

            if (tweens != null && tweens.IsTweening(_rect, TweenProperty.Scale))
                return;

            _pulsing = true;
            float scale = 1f + _amplitude * (0.5f + 0.5f * Mathf.Sin(time * _speed));
            _rect.localScale = new Vector3(scale, scale, 1f);
        }

        private void StopPulse()
        {
            if (!_pulsing)
                return;
            _pulsing = false;
            _rect.localScale = Vector3.one;
        }
    }
}
