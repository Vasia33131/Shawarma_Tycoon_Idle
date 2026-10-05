using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Ларёк: продавец в окне (стоит или «работает» при заказе) и вертел, полосы которого бегут вниз — иллюзия вращения.
    /// Растёт вместе с командой: каждый нанятый менеджер добавляет украшение и зажигает лампу на фасаде.
    /// </summary>
    public sealed class KioskView : MonoBehaviour, ITickable
    {
        private const float DecorPopDuration = 0.45f;
        private const float BoothPunch = 0.06f;
        private const float BoothPunchDuration = 0.3f;

        [SerializeField] private Image _seller = null;
        [Tooltip("Сколько секунд продавец показывает «класс».")]
        [SerializeField] private float _sellerHappyDuration = 1.2f;
        [Tooltip("«Дыхание» продавца: доля роста (0 — выключено).")]
        [SerializeField] private float _sellerBreath = 0.015f;
        [SerializeField] private RectTransform _spit = null;
        [SerializeField] private RectTransform[] _spitStripes = new RectTransform[0];
        [Tooltip("Скорость полос вертела, доля высоты вертела в секунду.")]
        [SerializeField] private float _stripeSpeed = 0.25f;
        [Tooltip("Точка у окна, откуда вылетают монеты, когда список станций не виден.")]
        [SerializeField] private RectTransform _window = null;

        [Header("City look")]
        [SerializeField] private Image _sky = null;
        [Tooltip("Части ларька в фирменном цвете города (корпус, полосы навеса).")]
        [SerializeField] private Image[] _boothTinted = new Image[0];

        [Header("Managers")]
        [Tooltip("Украшения по числу нанятых менеджеров: элемент i появляется, когда нанято i + 1.")]
        [SerializeField] private RectTransform[] _managerDecor = new RectTransform[0];
        [Tooltip("Лампы на фасаде: горит по одной за каждого менеджера.")]
        [SerializeField] private Image[] _levelLamps = new Image[0];
        [SerializeField] private Color _lampOnColor = new Color(1f, 0.80f, 0.20f);
        [SerializeField] private Color _lampOffColor = new Color(0.25f, 0.14f, 0.09f, 0.45f);
        [Tooltip("Ореолы ламп (по индексу ламп): видны у горящих, мягко «дышат». Можно оставить пустым.")]
        [SerializeField] private Graphic[] _lampGlows = new Graphic[0];

        [Header("Drawn spit (V3)")]
        [Tooltip("Нарисованный вертел без полос: «вращение» — покачивание ширины и яркости.")]
        [SerializeField] private float _spitWobble = 0.06f;
        [SerializeField] private float _spitPeriod = 1.6f;

        private readonly NumberText _upgradedText = new NumberText();

        private CustomerSkin _sellerSkin;
        private float _offset;
        private ManagerController _managers;
        private TweenRunner _tweens;
        private FloatingTextEffect _floatingText;
        private Loc _loc;
        private int _shownManagers = -1;
        private float _time;
        private Graphic _spitGraphic;
        private bool _serving;
        private float _happyLeft;

        public RectTransform Window => _window;

        public void Bind(CustomerSkin sellerSkin)
        {
            _sellerSkin = sellerSkin;
            SetServing(false);
        }

        /// <summary>Подключает рост ларька к найму менеджеров (уже нанятые показываются сразу, без анимации).</summary>
        public void BindManagers(ManagerController managers, TweenRunner tweens, FloatingTextEffect floatingText, Loc loc)
        {
            _managers = managers;
            _tweens = tweens;
            _floatingText = floatingText;
            _loc = loc;
            _managers.ManagerHired += OnManagerHired;
            ShowManagers(CountHired(), animate: false);
        }

        private void OnDestroy()
        {
            if (_managers != null)
                _managers.ManagerHired -= OnManagerHired;
        }

        /// <summary>Цвета города (GDD §7: «новый город — новый фон»).</summary>
        public void ApplyTheme(Color sky, Color booth)
        {
            if (_sky != null)
                _sky.color = sky;
            for (int i = 0; i < _boothTinted.Length; i++)
            {
                if (_boothTinted[i] != null)
                    _boothTinted[i].color = booth;
            }
        }

        public void SetServing(bool serving)
        {
            _serving = serving;
            RefreshSeller();
        }

        /// <summary>Продавец показывает «класс» на _sellerHappyDuration секунд (каждый N-й заказ, найм менеджера, улучшение).</summary>
        public void ShowHappy()
        {
            if (_sellerSkin == null || _sellerSkin.Cheer == null)
                return;
            _happyLeft = _sellerHappyDuration;
            RefreshSeller();
        }

        // Поза продавца: «класс» → работает (идёт заказ) → стоит. Смена позы — с маленьким подскоком.
        private void RefreshSeller()
        {
            if (_sellerSkin == null)
                return;

            Sprite sprite = _happyLeft > 0f && _sellerSkin.Cheer != null ? _sellerSkin.Cheer
                : _serving && _sellerSkin.Interact != null ? _sellerSkin.Interact
                : _sellerSkin.Idle;
            if (sprite == null || _seller.sprite == sprite)
                return;

            bool changed = _seller.sprite != null;
            _seller.sprite = sprite;
            if (changed && _tweens != null)
                _tweens.Punch(_seller.rectTransform, 0.04f, 0.12f);
        }

        // «Дыхание» продавца: чуть тянется вверх, пока на нём нет твина подскока.
        private void BreatheSeller(float deltaTime)
        {
            if (_happyLeft > 0f)
            {
                _happyLeft -= deltaTime;
                if (_happyLeft <= 0f)
                    RefreshSeller();
            }

            RectTransform rect = _seller.rectTransform;
            if (_sellerBreath <= 0f || (_tweens != null && _tweens.IsTweening(rect, TweenProperty.Scale)))
                return;
            float wave = 0.5f + 0.5f * Mathf.Sin(_time * (2f * Mathf.PI / 3f));
            rect.localScale = new Vector3(1f, 1f + _sellerBreath * wave, 1f);
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            if (_seller != null)
                BreatheSeller(deltaTime);
            AnimateLampGlows();

            int count = _spitStripes.Length;
            if (count == 0)
            {
                WobbleSpit();
                return;
            }

            float height = _spit.rect.height;
            float spacing = 1f / count;
            _offset = Mathf.Repeat(_offset + _stripeSpeed * deltaTime, spacing);

            for (int i = 0; i < count; i++)
            {
                float normalized = 1f - Mathf.Repeat(i * spacing + _offset, 1f);
                _spitStripes[i].anchoredPosition = new Vector2(0f, (normalized - 0.5f) * height);
            }
        }

        // Нарисованный вертел «вращается»: ширина и яркость слегка пульсируют.
        private void WobbleSpit()
        {
            if (_spit == null || _spitPeriod <= 0f)
                return;
            float wave = 0.5f + 0.5f * Mathf.Sin(_time * (2f * Mathf.PI / _spitPeriod));
            _spit.localScale = new Vector3(1f - _spitWobble * wave, 1f, 1f);
            if (_spitGraphic != null || _spit.TryGetComponent(out _spitGraphic))
                _spitGraphic.canvasRenderer.SetColor(Color.Lerp(Color.white, new Color(0.9f, 0.86f, 0.82f), wave));
        }

        // Ореолы горящих ламп «дышат» вразнобой (своя фаза у каждой).
        private void AnimateLampGlows()
        {
            for (int i = 0; i < _lampGlows.Length; i++)
            {
                Graphic glow = _lampGlows[i];
                if (glow == null || !glow.enabled)
                    continue;
                glow.canvasRenderer.SetAlpha(0.6f + 0.3f * (0.5f + 0.5f * Mathf.Sin(_time * 1.6f + i * 1.3f)));
            }
        }

        private void OnManagerHired(int index)
        {
            ShowManagers(CountHired(), animate: true);
            ShowHappy();
        }

        private int CountHired()
        {
            int hired = 0;
            for (int i = 0; i < _managers.Count; i++)
            {
                if (_managers.IsHired(i))
                    hired++;
            }
            return hired;
        }

        private void ShowManagers(int count, bool animate)
        {
            bool grew = count > _shownManagers;
            _shownManagers = count;

            for (int i = 0; i < _managerDecor.Length; i++)
            {
                RectTransform decor = _managerDecor[i];
                if (decor == null)
                    continue;

                bool visible = i < count;
                bool appeared = visible && !decor.gameObject.activeSelf;
                decor.gameObject.SetActive(visible);
                if (appeared && animate && _tweens != null)
                    _tweens.Scale(decor, Vector3.zero, Vector3.one, DecorPopDuration, Ease.OutBack);
            }

            for (int i = 0; i < _levelLamps.Length; i++)
            {
                if (_levelLamps[i] != null)
                    _levelLamps[i].color = i < count ? _lampOnColor : _lampOffColor;
                if (i < _lampGlows.Length && _lampGlows[i] != null)
                    _lampGlows[i].enabled = i < count;
            }

            if (!animate || !grew || _tweens == null)
                return;

            _tweens.Punch((RectTransform)transform, BoothPunch, BoothPunchDuration);
            if (count - 1 < _levelLamps.Length && _levelLamps[count - 1] != null)
                _tweens.Punch(_levelLamps[count - 1].rectTransform, 0.6f, 0.4f);
            if (_floatingText != null && _loc != null && count <= _managerDecor.Length)
                _floatingText.Show(_window.position, _upgradedText.Clear().Append(_loc.Get("kiosk.upgraded")));
        }
    }
}
