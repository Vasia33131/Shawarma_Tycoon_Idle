using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Prestige;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Вкладка «Город» (GDD §7): карта филиалов, под ней на той же карточке — прогресс к порогу,
    /// прогноз репутации, бонус к доходу и кнопка перехода в новый город; ниже — магазин репутации.
    /// Динамические тексты — 4 раза в секунду и без аллокаций (NumberText).
    /// </summary>
    public sealed class CityPanel : MonoBehaviour, ITickable, ISceneBinding
    {
        private const float RefreshInterval = 0.25f;

        [Header("City")]
        [SerializeField] private CityMapView _map = null;
        [SerializeField] private RectTransform _progressFill = null;
        [SerializeField] private TMP_Text _progressText = null;
        [SerializeField] private TMP_Text _gainText = null;
        [SerializeField] private TMP_Text _bonusText = null;
        [SerializeField] private TMP_Text _reputationText = null;
        [SerializeField] private Button _prestigeButton = null;
        [SerializeField] private TMP_Text _prestigeButtonText = null;
        [SerializeField] private PrestigeConfirmPopup _confirmPopup = null;

        [Header("Shop")]
        [SerializeField] private RectTransform _shopContent = null;
        [SerializeField] private ShopItemRowView _shopRowPrefab = null;

        private readonly NumberText _progressBuilder = new NumberText();
        private readonly NumberText _gainBuilder = new NumberText();
        private readonly NumberText _bonusBuilder = new NumberText();
        private readonly NumberText _reputationBuilder = new NumberText();

        private PrestigeController _controller;
        private PrestigeState _state;
        private Loc _loc;
        private CityConfig _nextCity;
        private LocTemplate _earnedTemplate;
        private LocTemplate _gainTemplate;
        private LocTemplate _bonusTemplate;
        private LocTemplate _reputationTemplate;

        private float _timer;
        private double _shownEarned = double.NaN;
        private long _shownGain = -1;
        private long _shownFree = -1;
        private double _shownMultiplier = double.NaN;
        private bool _shownCanPrestige;

        /// <param name="cities">Все города по порядку (для карты); текущий — controller.CityIndex.</param>
        public void Bind(PrestigeController controller, ReputationShop shop, PrestigeState state, IReadOnlyList<CityConfig> cities,
            Loc loc, double baseOfflineHours)
        {
            SceneBindings.Track(this);
            CityConfig city = cities[controller.CityIndex];
            CityConfig nextCity = cities[controller.NextCityIndex];

            _controller = controller;
            _state = state;
            _loc = loc;
            _nextCity = nextCity;
            _earnedTemplate = loc.GetTemplate("prestige.earned_progress");
            _gainTemplate = loc.GetTemplate("prestige.gain");
            _bonusTemplate = loc.GetTemplate("prestige.bonus");
            _reputationTemplate = loc.GetTemplate("prestige.reputation");

            for (int i = 0; i < shop.Count; i++)
            {
                ShopItemRowView row = Instantiate(_shopRowPrefab, _shopContent);
                row.name = $"ShopRow_{shop.GetItem(i).Id}";
                row.Bind(i, shop, state, loc, baseOfflineHours, city.ValueScale);
            }

            _map.Bind(cities, controller.CityIndex, loc);
            _prestigeButton.onClick.AddListener(OnPrestigeClicked);
            _loc.LanguageChanged += RefreshStatic;
            _state.Changed += ForceRefresh;
            RefreshStatic();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_loc == null)
                return;
            _loc.LanguageChanged -= RefreshStatic;
            _state.Changed -= ForceRefresh;
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < RefreshInterval)
                return;

            _timer = 0f;
            Refresh(force: false);
        }

        private void OnPrestigeClicked()
        {
            _confirmPopup.Show(_controller, _loc.Get(_nextCity.NameKey), _controller.IsLastCity, _loc);
        }

        private void RefreshStatic()
        {
            _prestigeButtonText.text = _loc.Format(_controller.IsLastCity ? "prestige.go_same" : "prestige.go_next",
                _loc.Get(_nextCity.NameKey));
            ForceRefresh();
        }

        private void ForceRefresh()
        {
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            double earned = _state.CityEarned;
            if (force || earned != _shownEarned)
            {
                _shownEarned = earned;
                _progressFill.anchorMax = new Vector2((float)_controller.Progress, 1f);
                _map.SetProgress((float)_controller.Progress);
                _progressText.SetNumberText(_progressBuilder.Clear().Append(_earnedTemplate.Prefix).AppendNumber(earned)
                    .Append(" / ").AppendNumber(_controller.Threshold).Append(_earnedTemplate.Suffix));
            }

            long gain = _controller.ForecastGain;
            if (force || gain != _shownGain)
            {
                _shownGain = gain;
                _gainText.SetNumberText(_gainBuilder.Clear().Append(_gainTemplate.Prefix).AppendInteger(gain).Append(_gainTemplate.Suffix));
            }

            double multiplier = _controller.CurrentIncomeMultiplier;
            if (force || multiplier != _shownMultiplier)
            {
                _shownMultiplier = multiplier;
                _bonusText.SetNumberText(_bonusBuilder.Clear().Append(_bonusTemplate.Prefix)
                    .AppendNumber(Mathf.Round((float)((multiplier - 1) * 100))).Append(_bonusTemplate.Suffix));
            }

            long free = _state.FreeReputation;
            if (force || free != _shownFree)
            {
                _shownFree = free;
                _reputationText.SetNumberText(_reputationBuilder.Clear().Append(_reputationTemplate.Prefix)
                    .AppendInteger(free).Append(_reputationTemplate.Suffix));
            }

            bool canPrestige = _controller.CanPrestige;
            if (force || canPrestige != _shownCanPrestige)
            {
                _shownCanPrestige = canPrestige;
                _prestigeButton.interactable = canPrestige;
            }
        }
    }
}
