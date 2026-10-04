using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Раскладывает зоны экрана по ориентации (GDD §3).
    /// Портрет: HUD / ларёк / контент / вкладки сверху вниз. Ландшафт: ларёк слева, контент справа.
    /// </summary>
    public sealed class ResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private CanvasScalerAdapter _scaler = null;
        [SerializeField] private RectTransform _kioskArea = null;
        [SerializeField] private RectTransform _contentArea = null;
        [SerializeField] private float _hudHeight = 200f;
        [SerializeField] private float _tabBarHeight = 180f;
        [SerializeField] private float _portraitKioskHeight = 520f;

        // Start, а не OnEnable: CanvasScalerAdapter определяет ориентацию в своём Awake.
        private void Start()
        {
            _scaler.OrientationChanged += Apply;
            Apply(_scaler.IsLandscape);
        }

        private void OnDestroy()
        {
            if (_scaler != null)
                _scaler.OrientationChanged -= Apply;
        }

        private void Apply(bool isLandscape)
        {
            if (isLandscape)
            {
                Place(_kioskArea, new Vector2(0f, 0f), new Vector2(0.5f, 1f), _tabBarHeight, _hudHeight);
                Place(_contentArea, new Vector2(0.5f, 0f), new Vector2(1f, 1f), _tabBarHeight, _hudHeight);
            }
            else
            {
                float kioskBottom = _hudHeight + _portraitKioskHeight;
                Place(_kioskArea, new Vector2(0f, 1f), new Vector2(1f, 1f), 0f, 0f);
                _kioskArea.offsetMin = new Vector2(0f, -kioskBottom);
                _kioskArea.offsetMax = new Vector2(0f, -_hudHeight);
                Place(_contentArea, new Vector2(0f, 0f), new Vector2(1f, 1f), _tabBarHeight, kioskBottom);
            }
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float bottom, float top)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(0f, bottom);
            rect.offsetMax = new Vector2(0f, -top);
        }
    }
}
