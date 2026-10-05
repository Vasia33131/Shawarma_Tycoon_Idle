using System.Collections.Generic;
using ShaurmaTycoon.Ambient;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Offline;
using ShaurmaTycoon.Onboarding;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Retention;
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
        [Tooltip("Вечерняя сцена города за ларьком (V3); пусто — только цвет неба из KioskView.")]
        [SerializeField] private CitySceneView _scene = null;
        [SerializeField] private CustomerQueue _customers = null;
        [Tooltip("Кот Лаваш у ларька (V4); пусто — кота нет.")]
        [SerializeField] private CatView _cat = null;
        [Tooltip("Живой мир (V5): облака, пар, птицы, машины; пусто — нет.")]
        [SerializeField] private AmbientWorldView _ambient = null;
        [Tooltip("Погода (V5): дождь и снег; пусто — нет.")]
        [SerializeField] private WeatherView _weather = null;
        [SerializeField] private CoinFlyEffect _coinFly = null;
        [SerializeField] private FloatingTextEffect _floatingText = null;
        [SerializeField] private OfflineIncomePopup _offlinePopup = null;

        [Header("Retention (stage 10)")]
        [SerializeField] private RetentionHud _retentionHud = null;
        [SerializeField] private DailyRewardPopup _dailyPopup = null;
        [SerializeField] private QuestsPopup _questsPopup = null;
        [SerializeField] private AdOfferPopup _adOfferPopup = null;

        [Header("Onboarding (stage 11)")]
        [SerializeField] private TutorialHand _tutorialHand = null;

        [Header("Zen mode (V7)")]
        [SerializeField] private ZenMode _zenMode = null;

        // Панорама машины: не до упора в одно ухо — в наушниках так мягче.
        private const float VehiclePanRange = 0.6f;

        private readonly IncomeRateTracker _incomeRate = new IncomeRateTracker();

        private TickManager _tickManager;
        private StationController _stations;
        private DailyRewardService _daily;
        private CityConfig _city;
        private TutorialService _tutorial;
        private UpgradeService _upgrades;
        private AudioService _audio;
        private AmbientAudioService _ambientSound;

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
            _city = city;
            Services.TryGet(out _daily);
            Services.TryGet(out _tutorial);
            ApplyKioskTheme();
            if (_scene != null)
                _scene.Apply(city);

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
            _upgrades = Services.Get<UpgradeService>();
            _upgradesPanel.Bind(_upgrades, _stations, wallet, loc, offers);
            _upgrades.UpgradePurchased += OnUpgradePurchased;
            BindCityPanel(prestige, city, tweens, loc);
            _settingsPopup.Bind(Services.Get<GameSettings>(), tweens, loc);
            Services.TryGet(out AdBillService adBills);
            Services.TryGet(out StarterPack starterPack);
            Services.TryGet(out PermanentProducts permanent);
            _shopPanel.Bind(Services.Get<BillShop>(), Services.Get<BillWallet>(), loc, _coinFly, adBills, starterPack, permanent);
            _hud.BillsClicked += OpenShop;

            _tickManager = Services.Get<TickManager>();
            _tickManager.Register(_hud);
            _tickManager.Register(_stationsPanel);
            _tickManager.Register(_kiosk);
            if (_scene != null)
                _tickManager.Register(_scene);
            _tickManager.Register(_cityPanel);
            _tickManager.Register(_shopPanel);

            _stations.Earned += _incomeRate.Add;
            if (Services.TryGet(out OfflineIncomeService offline))
            {
                Services.TryGet(out SaveService save);
                _offlinePopup.Bind(offline, Services.Get<IPlatformService>(), save, tweens, _coinFly, loc);
                _offlinePopup.ShowIfPending();
            }
            BindRetention(tweens, loc);

            if (_tutorialHand != null && _tutorial != null)
            {
                _tutorialHand.Bind(_tutorial, _stationsPanel, _managersPanel, _tabBar, loc);
                _tickManager.Register(_tutorialHand);
            }

            if (Services.TryGet(out CustomerConfig customers))
            {
                _customers.Bind(customers, _incomeRate, _stations, _coinFly, audio, tweens, loc);
                _tickManager.Register(_customers);
            }

            if (_cat != null)
            {
                _cat.Bind(audio, tweens, _tickManager);
                _tickManager.Register(_cat);
            }
            BindLivingWorld(city);
            BindAmbientSound(audio);
            if (_zenMode != null)
            {
                _zenMode.Bind(_stations, _tutorial, _coinFly, _floatingText, _kiosk.Window);
                _tickManager.Register(_zenMode);
            }

            // Сцена собрана и показана игроку — платформе можно сообщить, что игра готова (Game Ready).
            if (Services.TryGet(out IPlatformService platform))
                platform.GameReady();
        }

        private void BindLivingWorld(CityConfig city)
        {
            if (!Services.TryGet(out AmbientConfig config))
                return;
            Services.TryGet(out WeatherService weather);
            var settings = Services.Get<GameSettings>();

            if (_ambient != null)
            {
                _ambient.Bind(config, city, settings, weather, _tickManager);
                _tickManager.Register(_ambient);
                Services.Register(_ambient);
            }
            if (_weather != null)
            {
                _weather.Bind(config, settings, weather, _customers, _tickManager);
                _tickManager.Register(_weather);
            }
        }

        /// <summary>Звуки живого мира (V6): гриль громче при длинной очереди, машина «едет» из правого уха в левое.</summary>
        private void BindAmbientSound(AudioService audio)
        {
            _audio = audio;
            if (_customers != null && Services.TryGet(out _ambientSound))
                _ambientSound.SetQueueSource(() => _customers.Fullness);
            if (_ambient != null && _audio != null)
            {
                _ambient.VehiclePassed += OnVehiclePassed;
                _ambient.VehicleMoved += OnVehicleMoved;
            }
        }

        private void OnVehiclePassed(float x)
        {
            _audio.PlayPanned(SoundId.CarPass, VehiclePanRange);
        }

        private void OnVehicleMoved(float normalizedX)
        {
            _audio.SetPan(normalizedX * VehiclePanRange);
        }

        /// <summary>Буст, подарок, календарь и задания дня. Календарь открывается сам, когда есть награда, — после окна офлайн-дохода.</summary>
        private void BindRetention(TweenRunner tweens, Loc loc)
        {
            if (_retentionHud == null || _daily == null || !Services.TryGet(out BoostService boost) || !Services.TryGet(out GiftBoxService gift)
                || !Services.TryGet(out DailyQuestService quests))
                return;

            _adOfferPopup.Bind(tweens);
            _dailyPopup.Bind(_daily, tweens, _coinFly, loc);
            _questsPopup.Bind(quests, tweens, _coinFly, loc);
            _retentionHud.Bind(boost, gift, _daily, quests, _coinFly, loc);
            _tickManager.Register(_retentionHud);
            _tickManager.Register(_questsPopup);

            if (_offlinePopup != null && _offlinePopup.IsOpen)
                _offlinePopup.Closed += ShowDailyIfAvailable;
            else
                ShowDailyIfAvailable();
        }

        private void ShowDailyIfAvailable()
        {
            if (_offlinePopup != null)
                _offlinePopup.Closed -= ShowDailyIfAvailable;
            // Новичку календарь не мешает туториалу: откроется, когда туториал пройден.
            if (_tutorial != null && _tutorial.IsActive)
            {
                _tutorial.StepChanged -= OnTutorialStepChanged;
                _tutorial.StepChanged += OnTutorialStepChanged;
                return;
            }
            if (_daily.CanClaim)
                _dailyPopup.Show();
        }

        private void OnTutorialStepChanged(TutorialStep step)
        {
            if (step != TutorialStep.Done)
                return;
            _tutorial.StepChanged -= OnTutorialStepChanged;
            ShowDailyIfAvailable();
        }

        // Скин «Золотой ларёк» убран из календаря: ларёк всегда в цвете города (даже если скин остался в старом сейве).
        private void ApplyKioskTheme()
        {
            _kiosk.ApplyTheme(_city.SkyColor, _city.BoothColor);
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

        private void OnUpgradePurchased(int index)
        {
            _kiosk.ShowHappy();
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
            if (_upgrades != null)
                _upgrades.UpgradePurchased -= OnUpgradePurchased;
            if (_tutorial != null)
                _tutorial.StepChanged -= OnTutorialStepChanged;
            if (_offlinePopup != null)
                _offlinePopup.Closed -= ShowDailyIfAvailable;
            if (_ambientSound != null)
                _ambientSound.SetQueueSource(null);
            if (_ambient != null)
            {
                _ambient.VehiclePassed -= OnVehiclePassed;
                _ambient.VehicleMoved -= OnVehicleMoved;
            }

            if (_tickManager == null)
                return;

            _tickManager.Unregister(_hud);
            _tickManager.Unregister(_stationsPanel);
            _tickManager.Unregister(_kiosk);
            if (_scene != null)
                _tickManager.Unregister(_scene);
            _tickManager.Unregister(_cityPanel);
            _tickManager.Unregister(_shopPanel);
            _tickManager.Unregister(_customers);
            if (_cat != null)
                _tickManager.Unregister(_cat);
            if (_ambient != null)
            {
                _tickManager.Unregister(_ambient);
                Services.Unregister(_ambient);
            }
            if (_weather != null)
                _tickManager.Unregister(_weather);
            _tickManager.Unregister(_retentionHud);
            _tickManager.Unregister(_questsPopup);
            _tickManager.Unregister(_tutorialHand);
            if (_zenMode != null)
                _tickManager.Unregister(_zenMode);
        }
    }
}
