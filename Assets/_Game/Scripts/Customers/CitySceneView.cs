using ShaurmaTycoon.Core;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Вечерняя сцена за ларьком (V3): градиент неба, луна, дальний силуэт города и ближний уличный слой.
    /// Слои стоят на линии тротуара и по ширине равны зоне ларька (AspectRatioFitter держит пропорцию картинки).
    /// Слои еле заметно «дышат» по X (дальний меньше, ближний больше), луна мягко светится;
    /// при загрузке сцены (в т. ч. после переезда) всё плавно проявляется.
    /// </summary>
    public sealed class CitySceneView : MonoBehaviour, ITickable
    {
        [SerializeField] private GradientImage _sky = null;
        [SerializeField] private Image _far = null;
        [SerializeField] private Image _near = null;
        [SerializeField] private GameObject _moon = null;
        [SerializeField] private Graphic _moonGlow = null;
        [Tooltip("Слои, которые проявляются при загрузке (всё, кроме неба).")]
        [SerializeField] private CanvasGroup _fadeGroup = null;

        [Header("Motion")]
        [SerializeField] private float _farSway = 4f;
        [SerializeField] private float _nearSway = 10f;
        [SerializeField] private float _swayPeriod = 24f;
        [SerializeField] private float _moonGlowPeriod = 5f;
        [SerializeField] private float _fadeInDuration = 0.6f;

        private float _time;
        private bool _fading;

        /// <summary>Небо, слои и луна города.</summary>
        public void Apply(CityConfig city)
        {
            if (_sky != null)
                _sky.SetColors(city.SkyTop, city.SkyMiddle, city.SkyColor);
            SetLayer(_far, city.BackgroundFar);
            SetLayer(_near, city.BackgroundNear);
            if (_moon != null)
                _moon.SetActive(city.ShowMoon);

            if (_fadeGroup != null && _fadeInDuration > 0f)
            {
                _fadeGroup.alpha = 0f;
                _fading = true;
            }
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;

            if (_fading)
            {
                _fadeGroup.alpha = Mathf.MoveTowards(_fadeGroup.alpha, 1f, deltaTime / _fadeInDuration);
                _fading = _fadeGroup.alpha < 1f;
            }

            if (_swayPeriod > 0f)
            {
                float wave = Mathf.Sin(_time * (2f * Mathf.PI / _swayPeriod));
                Sway(_far, wave * _farSway);
                Sway(_near, wave * _nearSway);
            }

            if (_moonGlow != null && _moonGlowPeriod > 0f && _moonGlow.gameObject.activeInHierarchy)
                _moonGlow.canvasRenderer.SetAlpha(0.75f + 0.25f * Mathf.Sin(_time * (2f * Mathf.PI / _moonGlowPeriod)));
        }

        private static void SetLayer(Image layer, Sprite sprite)
        {
            if (layer == null)
                return;
            layer.sprite = sprite;
            layer.enabled = sprite != null;
            if (sprite != null && layer.TryGetComponent(out AspectRatioFitter fitter))
                fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        private static void Sway(Image layer, float x)
        {
            if (layer == null || !layer.enabled)
                return;
            RectTransform rect = layer.rectTransform;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
        }
    }
}
