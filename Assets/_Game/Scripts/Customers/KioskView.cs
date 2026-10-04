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

        private readonly NumberText _upgradedText = new NumberText();

        private CustomerSkin _sellerSkin;
        private float _offset;
        private ManagerController _managers;
        private TweenRunner _tweens;
        private FloatingTextEffect _floatingText;
        private Loc _loc;
        private int _shownManagers = -1;

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
            if (_sellerSkin == null)
                return;

            Sprite sprite = serving && _sellerSkin.Interact != null ? _sellerSkin.Interact : _sellerSkin.Idle;
            if (sprite != null)
                _seller.sprite = sprite;
        }

        public void Tick(float deltaTime)
        {
            int count = _spitStripes.Length;
            if (count == 0)
                return;

            float height = _spit.rect.height;
            float spacing = 1f / count;
            _offset = Mathf.Repeat(_offset + _stripeSpeed * deltaTime, spacing);

            for (int i = 0; i < count; i++)
            {
                float normalized = 1f - Mathf.Repeat(i * spacing + _offset, 1f);
                _spitStripes[i].anchoredPosition = new Vector2(0f, (normalized - 0.5f) * height);
            }
        }

        private void OnManagerHired(int index)
        {
            ShowManagers(CountHired(), animate: true);
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
