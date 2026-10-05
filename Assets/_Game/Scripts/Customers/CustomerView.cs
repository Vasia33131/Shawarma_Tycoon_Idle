using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Один клиент на экране. Корень двигается по улице, тело (Body) анимируется:
    /// персонажи без кадров ходьбы (V4) идут вприпрыжку — подскок, наклон и сжатие при «приземлении»,
    /// а стоя «дышат» со своей фазой. Персонажи Kenney листают кадры (CustomerQueue), тело только отражается.
    /// Логикой управляет CustomerQueue; аллокаций в кадре нет.
    /// </summary>
    public sealed class CustomerView : MonoBehaviour
    {
        [SerializeField] private Image _body = null;
        [SerializeField] private Button _button = null;
        [Tooltip("Мягкая тень под ногами (не двигается вместе с подскоком).")]
        [SerializeField] private RectTransform _shadow = null;
        [Tooltip("Зонтик в руке (дочерний к _umbrellaRig). Пусто — без зонтика.")]
        [SerializeField] private Image _umbrella = null;
        [Tooltip("Держатель зонтика позади тела: повторяет подскок, наклон и отражение тела. Пусто — зонтик не двигается.")]
        [SerializeField] private RectTransform _umbrellaRig = null;

        private CustomerSkin _skin;
        private CustomerConfig _config;
        private float _facing = 1f;
        private float _stepPhase;
        private float _breathPhase;
        private float _time;
        private float _cheerLeft;

        public RectTransform Rect => (RectTransform)transform;
        public Button Button => _button;
        public bool IsCheering => _cheerLeft > 0f;
        public RectTransform Umbrella => _umbrella != null ? _umbrella.rectTransform : null;
        public bool HasUmbrella => _umbrella != null && _umbrella.gameObject.activeSelf;

        /// <summary>Раскрыть (tint — цвет купола) или убрать зонтик.</summary>
        public void SetUmbrella(bool open, Color tint)
        {
            if (_umbrella == null)
                return;
            if (open)
                _umbrella.color = tint;
            _umbrella.gameObject.SetActive(open);
        }

        /// <summary>Новый персонаж выходит на улицу.</summary>
        public void Setup(CustomerSkin skin, CustomerConfig config)
        {
            _skin = skin;
            _config = config;
            // Клиенты идут справа налево: смотреть должны влево.
            _facing = skin.FacesLeft ? 1f : -1f;
            _stepPhase = 0f;
            _breathPhase = Random.value * Mathf.PI * 2f;
            _cheerLeft = 0f;
            SetSprite(skin.Idle);
            ApplyPose(0f, 0f, 1f, 1f);
        }

        public void SetSprite(Sprite sprite)
        {
            if (sprite != null && _body.sprite != sprite)
                _body.sprite = sprite;
        }

        public void SetX(float x)
        {
            Rect.anchoredPosition = new Vector2(x, 0f);
        }

        /// <summary>Порадоваться seconds секунд (спрайт Cheer), потом вернуться к обычной позе.</summary>
        public void Cheer(float seconds)
        {
            if (_skin == null || _skin.Cheer == null)
                return;
            _cheerLeft = seconds;
            SetSprite(_skin.Cheer);
        }

        /// <param name="walking">Идёт (подскок) или стоит («дышит»).</param>
        /// <param name="speedFactor">Скорость ходьбы относительно обычной — так же быстрее шаги.</param>
        public void Animate(float deltaTime, bool walking, float speedFactor)
        {
            if (_skin == null)
                return;

            _time += deltaTime;
            if (_cheerLeft > 0f)
            {
                _cheerLeft -= deltaTime;
                if (_cheerLeft <= 0f)
                    SetSprite(_skin.Idle);
            }

            // У Kenney есть кадры ходьбы — подпрыгивать не нужно.
            bool procedural = _skin.Walk.Length == 0;
            if (walking && procedural)
            {
                _stepPhase += Mathf.PI * _config.BobFrequency * speedFactor * deltaTime;
                float step = Mathf.Abs(Mathf.Sin(_stepPhase));
                // У земли (step → 0) — сжатие, в верхней точке — чуть вытянут.
                float land = 1f - step;
                float squash = _config.Squash * (land * land * land - 0.3f);
                ApplyPose(step * _config.BobHeight, Mathf.Sin(_stepPhase) * _config.TiltDegrees, 1f + squash, 1f - squash);
                return;
            }

            float breath = Mathf.Sin(_time * (2f * Mathf.PI / _config.BreathPeriod) + _breathPhase) * _config.BreathAmount;
            ApplyPose(0f, 0f, 1f - breath * 0.5f, 1f + breath);
        }

        private void ApplyPose(float lift, float tilt, float scaleX, float scaleY)
        {
            RectTransform body = _body.rectTransform;
            body.anchoredPosition = new Vector2(0f, lift);
            body.localRotation = Quaternion.Euler(0f, 0f, tilt * _facing);
            body.localScale = new Vector3(scaleX * _facing, scaleY, 1f);
            // На экране клиент всегда смотрит влево (спрайт Kenney отражён), поэтому держатель не отражается.
            if (_umbrellaRig != null)
            {
                _umbrellaRig.anchoredPosition = body.anchoredPosition;
                _umbrellaRig.localRotation = body.localRotation;
            }
            if (_shadow != null)
            {
                // В прыжке тень чуть меньше.
                float shrink = _config != null && _config.BobHeight > 0f ? 1f - 0.25f * lift / _config.BobHeight : 1f;
                _shadow.localScale = new Vector3(shrink, shrink, 1f);
            }
        }
    }
}
