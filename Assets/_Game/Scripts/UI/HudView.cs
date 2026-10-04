using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Stations;
using TMPro;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Верхняя панель: монеты, доход/сек и свободная репутация. Тексты обновляются не чаще 10 раз в секунду,
    /// только при изменении и без аллокаций (NumberText → SetCharArray).
    /// </summary>
    public sealed class HudView : MonoBehaviour, ITickable
    {
        private const float RefreshInterval = 0.1f;

        [SerializeField] private TMP_Text _coinsText = null;
        [SerializeField] private TMP_Text _incomeText = null;
        [Tooltip("Иконка монет: цель летящих монет, подпрыгивает при их прилёте.")]
        [SerializeField] private RectTransform _coinIcon = null;
        [Tooltip("Репутация (GDD §3): показывается, когда она уже есть.")]
        [SerializeField] private GameObject _reputationRoot = null;
        [SerializeField] private TMP_Text _reputationText = null;

        private readonly NumberText _coins = new NumberText();
        private readonly NumberText _income = new NumberText();
        private readonly NumberText _reputation = new NumberText();

        private Wallet _wallet;
        private StationController _stations;
        private Loc _loc;
        private PrestigeState _prestige;
        private LocTemplate _incomeTemplate;
        private double _shownCoins = double.NaN;
        private double _shownIncome = double.NaN;
        private float _timer;

        public RectTransform CoinIcon => _coinIcon;

        public void Bind(Wallet wallet, StationController stations, Loc loc, PrestigeState prestige)
        {
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
            if (_prestige != null)
                _prestige.Changed -= RefreshReputation;
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
                if (_incomeText.gameObject.activeSelf != hasIncome)
                    _incomeText.gameObject.SetActive(hasIncome);
                if (hasIncome)
                {
                    _income.Clear().Append(_incomeTemplate.Prefix).AppendNumber(income).Append(_incomeTemplate.Suffix);
                    _incomeText.SetNumberText(_income);
                }
            }
        }
    }
}
