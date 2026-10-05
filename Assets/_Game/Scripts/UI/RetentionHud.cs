using System.Globalization;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Retention;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Кнопки удержания: «×2 доход» в HUD (с таймером, пока буст идёт), календарь и задания дня с бейджем «!»,
    /// подарок-ящик (пульсирует, когда готов; иначе — таймер). Состояние обновляется дважды в секунду, без аллокаций.
    /// </summary>
    public sealed class RetentionHud : MonoBehaviour, ITickable, ISceneBinding
    {
        private const float RefreshInterval = 0.5f;

        [Header("Boost ×2 (HUD)")]
        [SerializeField] private Button _boostButton = null;
        [SerializeField] private TMP_Text _boostText = null;
        [SerializeField] private Image _boostBackground = null;
        [SerializeField] private PulseEffect _boostPulse = null;
        [SerializeField] private Color _boostIdleColor = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color _boostActiveColor = new Color(0.55f, 0.35f, 0.85f);

        [Header("Calendar & quests")]
        [SerializeField] private Button _calendarButton = null;
        [SerializeField] private GameObject _calendarBadge = null;
        [SerializeField] private Button _questsButton = null;
        [SerializeField] private GameObject _questsBadge = null;

        [Header("Gift box")]
        [SerializeField] private Button _giftButton = null;
        [SerializeField] private CanvasGroup _giftGroup = null;
        [SerializeField] private PulseEffect _giftPulse = null;
        [SerializeField] private GameObject _giftBadge = null;
        [SerializeField] private TMP_Text _giftTimerText = null;

        [Header("Popups")]
        [SerializeField] private DailyRewardPopup _dailyPopup = null;
        [SerializeField] private QuestsPopup _questsPopup = null;
        [SerializeField] private AdOfferPopup _adOffer = null;

        private readonly NumberText _boostLabel = new NumberText();
        private readonly NumberText _giftLabel = new NumberText();

        private BoostService _boost;
        private GiftBoxService _gift;
        private DailyRewardService _daily;
        private DailyQuestService _quests;
        private CoinFlyEffect _coins;
        private Loc _loc;
        private string _boostIdleText;
        private float _timer;
        private long _shownBoostSeconds = -1;
        private long _shownGiftSeconds = -1;

        public void Bind(BoostService boost, GiftBoxService gift, DailyRewardService daily, DailyQuestService quests, CoinFlyEffect coins, Loc loc)
        {
            SceneBindings.Track(this);
            _boost = boost;
            _gift = gift;
            _daily = daily;
            _quests = quests;
            _coins = coins;
            _loc = loc;

            _boostButton.onClick.AddListener(OnBoostClicked);
            _calendarButton.onClick.AddListener(_dailyPopup.Show);
            _questsButton.onClick.AddListener(_questsPopup.Show);
            _giftButton.onClick.AddListener(OnGiftClicked);
            _gift.Opened += OnGiftOpened;
            _loc.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_loc != null)
                _loc.LanguageChanged -= OnLanguageChanged;
            if (_gift != null)
                _gift.Opened -= OnGiftOpened;
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < RefreshInterval)
                return;

            _timer = 0f;
            Refresh(force: false);
        }

        private void OnLanguageChanged()
        {
            _boostIdleText = _loc.Format("boost.button", "×" + _boost.Boost.BoostMultiplier.ToString("0.##", CultureInfo.InvariantCulture));
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            RefreshBoost(force);
            RefreshGift(force);

            bool calendar = _daily.CanClaim;
            if (_calendarBadge.activeSelf != calendar)
                _calendarBadge.SetActive(calendar);
            bool quests = _quests.Quests.HasClaimable;
            if (_questsBadge.activeSelf != quests)
                _questsBadge.SetActive(quests);
        }

        private void RefreshBoost(bool force)
        {
            IncomeBoost boost = _boost.Boost;
            long seconds = boost.IsActive ? (long)System.Math.Ceiling(boost.RemainingSeconds) : 0;
            if (!force && seconds == _shownBoostSeconds)
                return;

            _shownBoostSeconds = seconds;
            _boostPulse.Active = !boost.IsActive && _boost.CanWatch;
            _boostBackground.color = boost.IsActive ? _boostActiveColor : _boostIdleColor;
            if (boost.IsActive)
            {
                _boostLabel.Clear().Append('×').AppendNumber(boost.BoostMultiplier).Append(' ').AppendTimer(seconds);
                _boostText.SetNumberText(_boostLabel);
            }
            else
            {
                _boostText.text = _boostIdleText;
            }
        }

        private void RefreshGift(bool force)
        {
            bool ready = _gift.IsReady;
            long seconds = ready ? 0 : (long)System.Math.Ceiling(_gift.TimeLeft.TotalSeconds);
            if (!force && seconds == _shownGiftSeconds)
                return;

            _shownGiftSeconds = seconds;
            _giftPulse.Active = ready;
            _giftGroup.alpha = ready ? 1f : 0.6f;
            if (_giftBadge.activeSelf != ready)
                _giftBadge.SetActive(ready);
            if (_giftTimerText.gameObject.activeSelf == ready)
                _giftTimerText.gameObject.SetActive(!ready);
            if (!ready)
                _giftTimerText.SetNumberText(_giftLabel.Clear().AppendTimer(seconds));
        }

        private void OnBoostClicked()
        {
            IncomeBoost boost = _boost.Boost;
            string body = _loc.Format("boost.offer_body", DurationFormatter.Format(boost.SecondsPerAd, _loc),
                DurationFormatter.Format(boost.MaxSeconds, _loc));
            if (boost.IsActive)
                body += "\n" + _loc.Format("boost.offer_left", DurationFormatter.Format(boost.RemainingSeconds, _loc));

            bool canWatch = _boost.CanWatch;
            _adOffer.Show(_boostIdleText, body, _loc.Get(canWatch || !boost.IsActive ? "retention.watch" : "boost.full"), canWatch, WatchBoost);
        }

        private void WatchBoost()
        {
            _boost.TryWatch(_ => Refresh(force: true));
        }

        private void OnGiftClicked()
        {
            if (!_gift.IsReady)
            {
                _adOffer.Show(_loc.Get("gift.title"), _loc.Format("gift.wait", DurationFormatter.Format(_gift.TimeLeft.TotalSeconds, _loc)),
                    _loc.Get("retention.watch"), false, null);
                return;
            }

            string body = _loc.Format("gift.body", BigNumberFormatter.Format(_gift.Reward));
            _adOffer.Show(_loc.Get("gift.title"), body, _loc.Get("retention.watch"), _gift.CanOpen, OpenGift);
        }

        private void OpenGift()
        {
            _gift.TryOpen(_ => Refresh(force: true));
        }

        private void OnGiftOpened(double coins)
        {
            Vector3 origin = _giftButton.transform.position;
            for (int i = 0; i < 8; i++)
                _coins.Fly(origin);
            Refresh(force: true);
        }
    }
}
