using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Плавное покачивание вверх-вниз вокруг исходной позиции (булавка «Вы здесь» на карте).
    /// Анимирует PulseDriver (без своего Update), пока объект активен.
    /// </summary>
    public sealed class BobEffect : MonoBehaviour, IUiAnimated
    {
        [SerializeField] private float _amplitude = 6f;
        [SerializeField] private float _period = 1.6f;

        private RectTransform _rect;
        private Vector2 _origin;
        private PulseDriver _driver;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _origin = _rect.anchoredPosition;
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
            if (_rect != null)
                _rect.anchoredPosition = _origin;
        }

        public void Animate(float time, float deltaTime, TweenRunner tweens)
        {
            float offset = _amplitude * Mathf.Sin(time * (2f * Mathf.PI / _period));
            _rect.anchoredPosition = new Vector2(_origin.x, _origin.y + offset);
        }
    }
}
