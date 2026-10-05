using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Раскладывает зоны экрана по ориентации (GDD §3).
    /// Портрет: HUD / ларёк / контент / вкладки сверху вниз. Ландшафт: ларёк слева, контент справа.
    /// Третий режим — любование (V7): зона ларька растягивается на весь экран с увеличением, контент уезжает вниз.
    /// </summary>
    public sealed class ResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private CanvasScalerAdapter _scaler = null;
        [SerializeField] private RectTransform _kioskArea = null;
        [SerializeField] private RectTransform _contentArea = null;
        [SerializeField] private float _hudHeight = 200f;
        [SerializeField] private float _tabBarHeight = 180f;
        [SerializeField] private float _portraitKioskHeight = 520f;

        [Header("Zen mode (V7)")]
        [Tooltip("Увеличение сцены в режиме любования: портрет / ландшафт. Ларёк с украшениями должен помещаться по ширине.")]
        [SerializeField] private Vector2 _zenScale = new Vector2(1.3f, 1.2f);

        private float _zen;
        private bool _started;

        /// <summary>Доля перехода в режим любования: 0 — обычная раскладка, 1 — ларёк на весь экран.</summary>
        public float Zen => _zen;

        // Start, а не OnEnable: CanvasScalerAdapter определяет ориентацию в своём Awake.
        private void Start()
        {
            _started = true;
            _scaler.OrientationChanged += Apply;
            Apply(_scaler.IsLandscape);
        }

        private void OnDestroy()
        {
            if (_scaler != null)
                _scaler.OrientationChanged -= Apply;
        }

        /// <summary>Режим любования: t от 0 до 1 (уже со сглаживанием).</summary>
        public void SetZen(float t)
        {
            _zen = Mathf.Clamp01(t);
            if (_started)
                Apply(_scaler.IsLandscape);
        }

        // Размер экрана изменился (окно браузера) — раскладку любования пересчитываем, обычная держится на якорях.
        private void OnRectTransformDimensionsChange()
        {
            if (_started && _zen > 0f)
                Apply(_scaler.IsLandscape);
        }

        private void Apply(bool isLandscape)
        {
            if (_zen > 0f)
            {
                ApplyZen(isLandscape);
                return;
            }

            _kioskArea.localScale = Vector3.one;
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

        // Обычные прямоугольники зон — в координатах экрана (от левого нижнего угла), затем смешивание с «ларёк на весь экран».
        // Зона ларька в любовании меньше экрана в scale раз и увеличена масштабом — после масштаба ровно закрывает экран.
        private void ApplyZen(bool isLandscape)
        {
            Vector2 screen = ((RectTransform)transform).rect.size;
            Vector2 kioskMin, kioskMax, contentMin, contentMax;
            if (isLandscape)
            {
                kioskMin = new Vector2(0f, _tabBarHeight);
                kioskMax = new Vector2(screen.x * 0.5f, screen.y - _hudHeight);
                contentMin = new Vector2(screen.x * 0.5f, _tabBarHeight);
                contentMax = new Vector2(screen.x, screen.y - _hudHeight);
            }
            else
            {
                float kioskBottom = screen.y - _hudHeight - _portraitKioskHeight;
                kioskMin = new Vector2(0f, kioskBottom);
                kioskMax = new Vector2(screen.x, screen.y - _hudHeight);
                contentMin = new Vector2(0f, _tabBarHeight);
                contentMax = new Vector2(screen.x, kioskBottom);
            }

            float scale = Mathf.Max(1f, isLandscape ? _zenScale.y : _zenScale.x);
            Vector2 half = screen / (2f * scale);
            Vector2 center = screen * 0.5f;
            PlaceAbsolute(_kioskArea, Vector2.Lerp(kioskMin, center - half, _zen), Vector2.Lerp(kioskMax, center + half, _zen));
            _kioskArea.localScale = Vector3.one * Mathf.Lerp(1f, scale, _zen);

            Vector2 drop = new Vector2(0f, -screen.y * _zen);
            PlaceAbsolute(_contentArea, contentMin + drop, contentMax + drop);
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float bottom, float top)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(0f, bottom);
            rect.offsetMax = new Vector2(0f, -top);
        }

        // Прямоугольник в координатах родителя от левого нижнего угла; масштаб — вокруг центра.
        private static void PlaceAbsolute(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = min;
            rect.offsetMax = max;
        }
    }
}
