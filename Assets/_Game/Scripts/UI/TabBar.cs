using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Нижние вкладки: кнопка i показывает панель i и подсвечивается. Цвет меняется плавно, активная вкладка
    /// приподнимается: двигается её «тело» (targetGraphic), если оно — дочерний объект кнопки (саму кнопку держит layout).
    /// </summary>
    public sealed class TabBar : MonoBehaviour, IUiAnimated
    {
        [SerializeField] private Button[] _buttons = new Button[0];
        [SerializeField] private GameObject[] _panels = new GameObject[0];
        [SerializeField] private Color _activeColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField] private Color _inactiveColor = new Color(0.45f, 0.25f, 0.12f);
        [SerializeField] private float _raise = 10f;
        [SerializeField] private float _transitionDuration = 0.15f;
        [Tooltip("Иконки вкладок (по индексу кнопок); у активной увеличиваются с лёгким отскоком. Можно оставить пустым.")]
        [SerializeField] private RectTransform[] _icons = new RectTransform[0];
        [SerializeField] private float _activeIconScale = 1.12f;

        private float[] _blend;
        private PulseDriver _driver;
        private bool _animating;

        public int SelectedIndex { get; private set; } = -1;

        /// <summary>Кнопка вкладки index; null — такой нет.</summary>
        public RectTransform GetButton(int index)
        {
            return index >= 0 && index < _buttons.Length ? (RectTransform)_buttons[index].transform : null;
        }

        private void Awake()
        {
            _blend = new float[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                _buttons[i].onClick.AddListener(() => Select(index));
            }

            Select(0);
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
        }

        public void Select(int index)
        {
            if (index == SelectedIndex || index < 0 || index >= _panels.Length)
                return;

            bool first = SelectedIndex < 0;
            SelectedIndex = index;
            for (int i = 0; i < _panels.Length; i++)
                _panels[i].SetActive(i == index);

            // Первая вкладка при старте — сразу, без анимации; дальше — плавно в Animate.
            if (first || _driver == null || _transitionDuration <= 0f)
            {
                for (int i = 0; i < _buttons.Length; i++)
                    Apply(i, i == index ? 1f : 0f);
                _animating = false;
                return;
            }

            _animating = true;
        }

        public void Animate(float time, float deltaTime, TweenRunner tweens)
        {
            if (!_animating)
                return;

            float step = deltaTime / _transitionDuration;
            bool done = true;
            for (int i = 0; i < _buttons.Length; i++)
            {
                float target = i == SelectedIndex ? 1f : 0f;
                float value = Mathf.MoveTowards(_blend[i], target, step);
                if (!Mathf.Approximately(value, target))
                    done = false;
                if (!Mathf.Approximately(value, _blend[i]))
                    Apply(i, value);
            }
            _animating = !done;
        }

        private void Apply(int index, float blend)
        {
            _blend[index] = blend;
            Graphic body = _buttons[index].targetGraphic;
            if (body == null)
                return;

            body.color = Color.Lerp(_inactiveColor, _activeColor, blend);
            if (index < _icons.Length && _icons[index] != null)
            {
                float scale = Mathf.LerpUnclamped(1f, _activeIconScale, EaseFunctions.Evaluate(Ease.OutBack, blend));
                _icons[index].localScale = new Vector3(scale, scale, 1f);
            }
            if (body.transform == _buttons[index].transform)
                return;

            float eased = EaseFunctions.Evaluate(Ease.OutQuad, blend);
            RectTransform rect = body.rectTransform;
            rect.offsetMin = new Vector2(rect.offsetMin.x, _raise * eased);
            rect.offsetMax = new Vector2(rect.offsetMax.x, _raise * eased);
        }
    }
}
