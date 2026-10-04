using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Настраивает CanvasScaler под ориентацию экрана:
    /// портрет — референс 1080×1920, подгонка по ширине; ландшафт — 1920×1080, подгонка по высоте.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class CanvasScalerAdapter : MonoBehaviour
    {
        [SerializeField] private Vector2 _portraitReference = new Vector2(1080f, 1920f);

        private CanvasScaler _scaler;
        private int _lastWidth;
        private int _lastHeight;

        public bool IsLandscape { get; private set; }

        /// <summary>Вызывается при смене ориентации; аргумент — IsLandscape.</summary>
        public event Action<bool> OrientationChanged;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            Apply(force: true);
        }

        // Unity вызывает у корневого Canvas при изменении размера экрана — Update не нужен.
        private void OnRectTransformDimensionsChange()
        {
            if (_scaler != null)
                Apply(force: false);
        }

        private void Apply(bool force)
        {
            int width = Screen.width;
            int height = Screen.height;
            if (!force && width == _lastWidth && height == _lastHeight)
                return;

            _lastWidth = width;
            _lastHeight = height;

            bool isLandscape = width > height;
            _scaler.referenceResolution = isLandscape
                ? new Vector2(_portraitReference.y, _portraitReference.x)
                : _portraitReference;
            _scaler.matchWidthOrHeight = isLandscape ? 1f : 0f;

            if (force || isLandscape != IsLandscape)
            {
                IsLandscape = isLandscape;
                OrientationChanged?.Invoke(isLandscape);
            }
        }
    }
}
