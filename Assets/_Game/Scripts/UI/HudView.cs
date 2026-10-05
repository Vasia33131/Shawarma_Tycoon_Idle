using System;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Stations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Верхняя панель: монеты, доход/сек, купюры и свободная репутация. Тексты обновляются не чаще 10 раз в секунду,
    /// только при изменении и без аллокаций (NumberText → SetCharArray).
    /// </summary>
    public sealed class HudView : MonoBehaviour, ITickable, ISceneBinding
    {
        private const float RefreshInterval = 0.1f;

        [SerializeField] private TMP_Text _coinsText = null;
        [SerializeField] private TMP_Text _incomeText = null;
        [Tooltip("Строка дохода целиком (иконка + текст); пусто — прячется только текст.")]
        [SerializeField] private GameObject _incomeRoot = null;
        [Tooltip("Иконка монет: цель летящих монет, подпрыгивает при их прилёте.")]
        [SerializeField] private RectTransform _coinIcon = null;
        [Tooltip("Репутация (GDD §3): показывается, когда она уже есть.")]
        [SerializeField] private GameObject _reputationRoot = null;
        [SerializeField] private TMP_Text _reputationText = null;
        [Tooltip("Счётчик купюр — виден всегда, по тапу открывает магазин.")]
        [SerializeField] private RectTransform _billsRoot = null;
        [SerializeField] private TMP_Text _billsText = null;
        [SerializeField] private Button _billsButton = null;

        private readonly NumberText _coins = new NumberText();
        private readonly NumberText _income = new NumberText();
        private readonly NumberText _reputation = new NumberText();
        private readonly NumberText _bills = new NumberText();
        private readonly NumberText _billsGain = new NumberText();

        private Wallet _wallet;
        private StationController _stations;
        private Loc _loc;
        private PrestigeState _prestige;
        private BillWallet _billWallet;
        private TweenRunner _tweens;
        private FloatingTextEffect _floatingText;
        private LocTemplate _incomeTemplate;
        private double _shownCoins = double.NaN;
        private double _shownIncome = double.NaN;
        private float _timer;

        public RectTransform CoinIcon => _coinIcon;
        public RectTransform BillsIcon => _billsRoot;

        /// <summary>Тап по счётчику купюр (магазин подключается на этапе 08d).</summary>
        public event Action BillsClicked;

        public void Bind(Wallet wallet, StationController stations, Loc loc, PrestigeState prestige, BillWallet billWallet,
            TweenRunner tweens, FloatingTextEffect floatingText)
        {
            SceneBindings.Track(this);
            _billWallet = billWallet;
            _tweens = tweens;
            _floatingText = floatingText;
            _billWallet.BillsChanged += OnBillsChanged;
            _billsButton.onClick.AddListener(OnBillsClicked);
            OnBillsChanged(_billWallet.Bills, 0);

            _prestige = prestige;
            _prestige.Changed += RefreshReputation;
            RefreshReputation();

            _wallet = wallet;
            _stations = stations;
            _loc = loc;
            _incomeTemplate = loc.GetTemplate("hud.income_per_sec");
            _loc.LanguageChanged += ForceRefresh;
            ForceRefresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_prestige != null)
                _prestige.Changed -= RefreshReputation;
            if (_billWallet != null)
                _billWallet.BillsChanged -= OnBillsChanged;
            if (_loc != null)
                _loc.LanguageChanged -= ForceRefresh;
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < RefreshInterval)
                return;

            _timer = 0f;
            Refresh(force: false);
        }

        private void RefreshReputation()
        {
            bool hasReputation = _prestige.ReputationEarned > 0;
            _reputationRoot.SetActive(hasReputation);
            if (hasReputation)
                _reputationText.SetNumberText(_reputation.Clear().AppendInteger(_prestige.FreeReputation));
        }

        private void OnBillsChanged(long bills, long delta)
        {
            _billsText.SetNumberText(_bills.Clear().AppendInteger(bills));
            if (delta <= 0 || _tweens == null)
                return;

            _tweens.Punch(_billsRoot, 0.2f, 0.25f);
            if (_floatingText != null)
                _floatingText.Show(_billsRoot.position, _billsGain.Clear().Append('+').AppendInteger(delta));
        }

        private void OnBillsClicked()
        {
            BillsClicked?.Invoke();
        }

        private void ForceRefresh()
        {
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            double coins = _wallet.Coins;
            if (force || coins != _shownCoins)
            {
                _shownCoins = coins;
                _coinsText.SetNumberText(_coins.Clear().AppendNumber(coins));
            }

            double income = _stations.IncomePerSecond;
            if (force || income != _shownIncome)
            {
                _shownIncome = income;
                bool hasIncome = income > 0;
                GameObject incomeRoot = _incomeRoot != null ? _incomeRoot : _incomeText.gameObject;
                if (incomeRoot.activeSelf != hasIncome)
                    incomeRoot.SetActive(hasIncome);
                if (hasIncome)
                {
                    _income.Clear().Append(_incomeTemplate.Prefix).AppendNumber(income).Append(_incomeTemplate.Suffix);
                    _incomeText.SetNumberText(_income);
                }
            }
        }
    }
}
