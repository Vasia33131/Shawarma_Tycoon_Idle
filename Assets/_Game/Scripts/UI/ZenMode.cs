using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Onboarding;
using ShaurmaTycoon.Stations;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Режим любования (V7): кнопка в HUD — HUD уезжает вверх, вкладки и список — вниз, сцена с ларьком растягивается
    /// на весь экран. Доход идёт как обычно, но монеты в HUD не летят: над ларьком раз в пару секунд всплывает «+X».
    /// Выход — тап по сцене (кот и клиенты остаются кликабельными: ловушка тапов лежит под ними) или любой попап.
    /// Во время туториала кнопки нет. Музыка в режиме на 15% тише.
    /// </summary>
    public sealed class ZenMode : MonoBehaviour, ITickable
    {
        [SerializeField] private ResponsiveLayout _layout = null;
        [SerializeField] private Button _enterButton = null;
        [Tooltip("Прозрачная ловушка тапов в зоне ларька: под котом и клиентами, над небом и городом.")]
        [SerializeField] private Button _exitCatcher = null;
        [SerializeField] private RectTransform _hud = null;
        [SerializeField] private RectTransform _tabBar = null;
        [Tooltip("Прячутся (тают и не ловят нажатия): HUD, вкладки, список, кнопки календаря/заданий/подарка в зоне ларька.")]
        [SerializeField] private CanvasGroup[] _hiddenGroups = new CanvasGroup[0];
        [SerializeField] private CanvasGroup _hint = null;

        [Header("Timing")]
        [SerializeField] private float _transition = 0.45f;
        [SerializeField] private float _hintDelay = 3f;
        [SerializeField] private float _hintShown = 4f;
        [SerializeField] private float _hintFade = 0.4f;
        [Tooltip("Как часто над ларьком всплывает накопленный доход.")]
        [SerializeField] private float _incomeInterval = 1.5f;
        [Range(0f, 1f)]
        [SerializeField] private float _musicScale = 0.85f;

        private readonly NumberText _incomeText = new NumberText();
        private CoinFlyEffect _coins;
        private FloatingTextEffect _floatingText;
        private RectTransform _incomeAnchor;
        private StationController _stations;
        private TutorialService _tutorial;
        private MusicService _music;
        private Vector2 _hudBase;
        private Vector2 _tabBarBase;
        private bool _active;
        private float _progress;
        private float _timeInZen;
        private float _incomeTimer;
        private double _income;
        private bool _bound;

        public bool IsActive => _active;

        public void Bind(StationController stations, TutorialService tutorial, CoinFlyEffect coins, FloatingTextEffect floatingText,
            RectTransform incomeAnchor)
        {
            _stations = stations;
            _tutorial = tutorial;
            _coins = coins;
            _floatingText = floatingText;
            _incomeAnchor = incomeAnchor;
            Services.TryGet(out _music);
            _hudBase = _hud != null ? _hud.anchoredPosition : Vector2.zero;
            _tabBarBase = _tabBar != null ? _tabBar.anchoredPosition : Vector2.zero;

            _enterButton.onClick.AddListener(Enter);
            _exitCatcher.onClick.AddListener(Exit);
            _exitCatcher.gameObject.SetActive(false);
            if (_hint != null)
            {
                _hint.alpha = 0f;
                _hint.blocksRaycasts = false;
                _hint.gameObject.SetActive(false);
            }
            if (_stations != null)
                _stations.Earned += OnEarned;
            PopupAnimator.AnyShown += Exit;
            _bound = true;
            RefreshButton();
        }

        public void Enter()
        {
            if (_active || (_tutorial != null && _tutorial.IsActive))
                return;
            _active = true;
            _timeInZen = 0f;
            _income = 0;
            _incomeTimer = _incomeInterval;
            _exitCatcher.gameObject.SetActive(true);
            SetGroupsInteractive(false);
            if (_coins != null)
                _coins.Suppressed = true;
            if (_music != null)
                _music.VolumeScale = _musicScale;
        }

        public void Exit()
        {
            if (!_active)
                return;
            _active = false;
            _exitCatcher.gameObject.SetActive(false);
            if (_coins != null)
                _coins.Suppressed = false;
            if (_music != null)
                _music.VolumeScale = 1f;
            ShowIncome();
        }

        public void Tick(float deltaTime)
        {
            if (!_bound)
                return;
            RefreshButton();

            // Реальное время: ускорение из отладочного окна не должно ускорять анимацию.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float target = _active ? 1f : 0f;
            if (!Mathf.Approximately(_progress, target))
            {
                _progress = Mathf.MoveTowards(_progress, target, dt / Mathf.Max(0.01f, _transition));
                ApplyProgress(EaseFunctions.Evaluate(Ease.InOutSine, _progress));
                if (!_active && _progress <= 0f)
                    SetGroupsInteractive(true);
            }

            if (_active)
            {
                _timeInZen += dt;
                UpdateHint();
                _incomeTimer -= dt;
                if (_incomeTimer <= 0f)
                {
                    _incomeTimer = _incomeInterval;
                    ShowIncome();
                }
            }
            else if (_hint != null && _hint.gameObject.activeSelf)
            {
                _hint.alpha = Mathf.MoveTowards(_hint.alpha, 0f, dt / _hintFade);
                if (_hint.alpha <= 0f)
                    _hint.gameObject.SetActive(false);
            }
        }

        private void ApplyProgress(float t)
        {
            _layout.SetZen(t);
            if (_hud != null)
                _hud.anchoredPosition = _hudBase + new Vector2(0f, _hud.rect.height * 1.2f * t);
            if (_tabBar != null)
                _tabBar.anchoredPosition = _tabBarBase - new Vector2(0f, _tabBar.rect.height * 1.2f * t);
            for (int i = 0; i < _hiddenGroups.Length; i++)
            {
                if (_hiddenGroups[i] != null)
                    _hiddenGroups[i].alpha = 1f - t;
            }
        }

        // Подсказка: через _hintDelay появляется, через _hintShown гаснет; один раз за вход.
        private void UpdateHint()
        {
            if (_hint == null)
                return;
            float shownFor = _timeInZen - _hintDelay;
            float alpha = shownFor < 0f || shownFor > _hintShown + _hintFade
                ? 0f
                : Mathf.Min(Mathf.Clamp01(shownFor / _hintFade), Mathf.Clamp01((_hintShown + _hintFade - shownFor) / _hintFade));
            if (alpha > 0f && !_hint.gameObject.activeSelf)
                _hint.gameObject.SetActive(true);
            _hint.alpha = alpha;
            if (alpha <= 0f && _hint.gameObject.activeSelf && shownFor > 0f)
                _hint.gameObject.SetActive(false);
        }

        private void OnEarned(double amount)
        {
            if (_active)
                _income += amount;
        }

        private void ShowIncome()
        {
            if (_income <= 0 || _floatingText == null || _incomeAnchor == null)
                return;
            _floatingText.Show(_incomeAnchor.position, _incomeText.Clear().Append('+').AppendNumber(_income));
            _income = 0;
        }

        private void SetGroupsInteractive(bool interactive)
        {
            for (int i = 0; i < _hiddenGroups.Length; i++)
            {
                if (_hiddenGroups[i] == null)
                    continue;
                _hiddenGroups[i].interactable = interactive;
                _hiddenGroups[i].blocksRaycasts = interactive;
            }
        }

        private void RefreshButton()
        {
            bool available = _tutorial == null || !_tutorial.IsActive;
            if (_enterButton.gameObject.activeSelf != available)
                _enterButton.gameObject.SetActive(available);
        }

        private void OnDestroy()
        {
            PopupAnimator.AnyShown -= Exit;
            if (_stations != null)
                _stations.Earned -= OnEarned;
            if (_music != null)
                _music.VolumeScale = 1f;
        }
    }
}
