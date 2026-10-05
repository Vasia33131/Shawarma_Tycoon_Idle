using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Offline;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Shop;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Корень игрового экрана: связывает вью с сервисами и подключает их к TickManager.
    /// Start, а не Awake: сервисы города создаёт GameSceneInstaller в своём Awake.
    /// </summary>
    public sealed class GameScreen : MonoBehaviour
    {
        [SerializeField] private HudView _hud = null;
        [SerializeField] private StationsPanel _stationsPanel = null;
        [SerializeField] private ManagersPanel _managersPanel = null;
        [SerializeField] private UpgradesPanel _upgradesPanel = null;
        [SerializeField] private CityPanel _cityPanel = null;
        [SerializeField] private PrestigeConfirmPopup _prestigePopup = null;
        [SerializeField] private SettingsPopup _settingsPopup = null;
        [SerializeField] private ShopPanel _shopPanel = null;
        [SerializeField] private TabBar _tabBar = null;
        [SerializeField] private BillOfferPopup _billOfferPopup = null;
        [Tooltip("Индекс вкладки «Магазин» в TabBar — её открывает тап по купюрам в HUD.")]
        [SerializeField] private int _shopTabIndex = 4;

        [Header("Kiosk & effects")]
        [SerializeField] private KioskView _kiosk = null;
        [SerializeField] private CustomerQueue _customers = null;
        [SerializeField] private CoinFlyEffect _coinFly = null;
        [SerializeField] private FloatingTextEffect _floatingText = null;
        [SerializeField] private OfflineIncomePopup _offlinePopup = null;

        private readonly IncomeRateTracker _incomeRate = new IncomeRateTracker();

        private TickManager _tickManager;
        private StationController _stations;

        private void Start()
        {
            var wallet = Services.Get<Wallet>();
            var loc = Services.Get<Loc>();
            var tweens = Services.Get<TweenRunner>();
            var audio = Services.Get<AudioService>();
            _stations = Services.Get<StationController>();

            _coinFly.Bind(tweens, audio);
            _floatingText.Bind(tweens);

            var prestige = Services.Get<PrestigeState>();
            var city = Services.Get<CityConfig>();
            _kiosk.ApplyTheme(city.SkyColor, city.BoothColor);

            _hud.Bind(wallet, _stations, loc, prestige, Services.Get<BillWallet>(), tweens, _floatingText);
            BillOfferPopup offers = null;
            if (_billOfferPopup != null && Services.TryGet(out BillPurchases billPurchases))
            {
                _billOfferPopup.Bind(billPurchases, tweens, loc, OpenShop);
                offers = _billOfferPopup;
            }
            _stationsPanel.Bind(_stations, wallet, loc, _coinFly, _floatingText, _kiosk.Window, offers);
            var managers = Services.Get<ManagerController>();
            _managersPanel.Bind(managers, _stations, wallet, loc, offers);
            _kiosk.BindManagers(managers, tweens, _floatingText, loc);
            _upgradesPanel.Bind(Services.Get<UpgradeService>(), _stations, wallet, loc, offers);
            BindCityPanel(prestige, city, tweens, loc);
            _settingsPopup.Bind(Services.Get<GameSettings>(), tweens, loc);
            _shopPanel.Bind(Services.Get<BillShop>(), Services.Get<BillWallet>(), loc, _coinFly);
            _hud.BillsClicked += OpenShop;

            _tickManager = Services.Get<TickManager>();
            _tickManager.Register(_hud);
            _tickManager.Register(_stationsPanel);
            _tickManager.Register(_kiosk);
            _tickManager.Register(_cityPanel);
            _tickManager.Register(_shopPanel);

            _stations.Earned += _incomeRate.Add;
            if (Services.TryGet(out OfflineIncomeService offline))
            {
                Services.TryGet(out SaveService save);
                _offlinePopup.Bind(offline, Services.Get<IPlatformService>(), save, tweens, _coinFly, loc);
                _offlinePopup.ShowIfPending();
            }

            if (Services.TryGet(out CustomerConfig customers))
            {
                _customers.Bind(customers, _incomeRate, _stations, _coinFly, audio, tweens, loc);
                _tickManager.Register(_customers);
            }
        }

        private void BindCityPanel(PrestigeState prestige, CityConfig city, TweenRunner tweens, Loc loc)
        {
            var controller = Services.Get<PrestigeController>();
            IReadOnlyList<CityConfig> cities = Services.TryGet(out CityCatalog catalog) && catalog.Count > 0
                ? catalog.Cities
                : new[] { city };
            double baseOfflineHours = Services.TryGet(out BalanceConfig balance) ? balance.OfflineLimitHours : 0;

            _prestigePopup.Bind(tweens);
            _cityPanel.Bind(controller, Services.Get<ReputationShop>(), prestige, cities, loc, baseOfflineHours);
        }

        private void OpenShop()
        {
            _tabBar.Select(_shopTabIndex);
        }

        private void OnDestroy()
        {
            // Отписать вью от сервисов Boot до загрузки нового города (см. SceneBindings).
            SceneBindings.UnbindAll();

            if (_hud != null)
                _hud.BillsClicked -= OpenShop;

            if (_stations != null)
                _stations.Earned -= _incomeRate.Add;

            if (_tickManager == null)
                return;

            _tickManager.Unregister(_hud);
            _tickManager.Unregister(_stationsPanel);
            _tickManager.Unregister(_kiosk);
            _tickManager.Unregister(_cityPanel);
            _tickManager.Unregister(_shopPanel);
            _tickManager.Unregister(_customers);
        }
    }
}
