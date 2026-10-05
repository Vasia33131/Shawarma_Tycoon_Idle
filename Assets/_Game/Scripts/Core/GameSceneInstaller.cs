using ShaurmaTycoon.Ambient;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Economy;
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
using UnityEngine.SceneManagement;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Сцена Game: создаёт системы текущего города (из каталога по PrestigeState.CityIndex)
    /// и регистрирует их в Services и TickManager. Переход в новый город перезагружает эту сцену.
    /// </summary>
    public sealed class GameSceneInstaller : MonoBehaviour
    {
        [SerializeField] private BalanceConfig _balance = null;
        [SerializeField] private CityCatalog _cities = null;
        [Tooltip("Запасной вариант, если каталог городов не назначен (до Build Game UI).")]
        [SerializeField] private CityConfig _city = null;
        [SerializeField] private ReputationShopConfig _shop = null;
        [SerializeField] private BillShopConfig _billShop = null;
        [SerializeField] private AudioConfig _audio = null;
        [SerializeField] private MusicConfig _music = null;
        [SerializeField] private CustomerConfig _customers = null;
        [Tooltip("Удержание: календарь, задания дня, буст ×2, подарок. Пусто — значения по умолчанию.")]
        [SerializeField] private RetentionConfig _retention = null;
        [Tooltip("Живой мир и погода (V5). Пусто — значения по умолчанию.")]
        [SerializeField] private AmbientConfig _ambient = null;
        [Tooltip("Звуковая атмосфера (V6): гриль, дождь, ветер, громкости. Пусто — без эмбиента.")]
        [SerializeField] private AmbientAudioConfig _ambientAudio = null;

        private TickManager _tickManager;
        private AmbientConfig _ambientConfig;
        private WeatherService _weather;
        private AmbientAudioService _ambientSound;
        private GameSettings _settings;
        private CityConfig _currentCity;
        private StationController _stations;
        private ManagerController _managers;
        private UpgradeService _upgrades;
        private ReputationShop _reputationShop;
        private BillShop _billShopService;
        private BillPurchases _billPurchases;
        private StarterPack _starterPack;
        private PermanentProducts _permanent;
        private AdBillService _adBills;
        private OfflineIncomeService _offline;
        private PrestigeController _prestigeController;
        private PrestigeState _prestige;
        private PrestigeRules _rules;
        private AudioService _audioService;
        private SaveService _save;
        private CitySaveParticipant _saveParticipant;
        private RetentionConfig _retentionConfig;
        private BoostService _boost;
        private GiftBoxService _gift;
        private DailyRewardService _daily;
        private DailyQuestService _quests;
        private QuestTracker _questTracker;
        private TutorialService _tutorial;

        private void Awake()
        {
            _prestige = Services.Get<PrestigeState>();
            int cityCount = _cities != null && _cities.Count > 0 ? _cities.Count : 1;
            int cityIndex = Mathf.Clamp(_prestige.CityIndex, 0, cityCount - 1);
            _currentCity = _cities != null && _cities.Count > 0 ? _cities.Get(cityIndex) : _city;

            if (_balance == null || _currentCity == null)
            {
                Debug.LogError($"{nameof(GameSceneInstaller)}: assign Balance and City Catalog (run ShaurmaTycoon/Build Game UI).", this);
                enabled = false;
                return;
            }

            var wallet = Services.Get<Wallet>();
            _rules = _balance.CreatePrestigeRules();
            Services.Register(_balance);
            Services.Register(_currentCity);
            if (_cities != null)
                Services.Register(_cities);

            _stations = new StationController(_currentCity.Stations, _balance.CreateMilestoneTable(), wallet);
            _managers = new ManagerController(_stations, wallet, _balance.ManagerCostMultiplier);
            _upgrades = new UpgradeService(_currentCity.Upgrades, _stations, wallet);
            _reputationShop = new ReputationShop(_shop != null ? _shop.Items : new ShopItemConfig[0], _prestige);
            Services.Register(_stations);
            Services.Register(_managers);
            Services.Register(_upgrades);
            Services.Register(_reputationShop);

            ApplyPrestigeModifiers();
            _reputationShop.Purchased += OnShopPurchased;
            _stations.Earned += _prestige.AddEarned;

            Services.TryGet(out GameSettings settings);
            _audioService = new AudioService(_audio, settings);
            Services.Register(_audioService);
            PlayCityMusic();

            if (_customers != null)
                Services.Register(_customers);
            else
                Debug.LogWarning($"{nameof(GameSceneInstaller)}: CustomerConfig is not assigned, no customers.", this);

            Services.TryGet(out IPlatformService platform);

            // Состояние из сейва восстанавливается здесь, до того как UI в Start прочитает модели.
            if (Services.TryGet(out _save))
            {
                _saveParticipant = new CitySaveParticipant(wallet, _stations, _upgrades);
                _save.Register(_saveParticipant);
            }

            // Постоянные покупки (из сейва): «Постоянный ×2» должен действовать уже на офлайн-доход, «Без рекламы» — на показ рекламы.
            _permanent = new PermanentProducts(_billShop != null ? _billShop.NoAds : new PermanentOfferConfig { ProductId = "no_ads", DisplayPrice = 149 },
                _billShop != null ? _billShop.IncomeX2 : new PermanentOfferConfig { ProductId = "income_x2", DisplayPrice = 299 }, platform,
                _save != null ? _save.SaveNow : (System.Action)null);
            if (_save != null)
                _save.Register(_permanent);
            Services.Register(_permanent);
            ApplyPrestigeModifiers();
            ApplyAdsSetting();
            _permanent.Purchased += OnPermanentPurchased;

            // Офлайн-доход считается по времени из только что загруженного сейва — до первого автосейва.
            _offline = new OfflineIncomeService(wallet, CalculateOfflineIncome(), _balance.OfflineAdMultiplier);
            _offline.Claimed += _prestige.AddEarned;
            Services.Register(_offline);

            SaveService save = _save;
            _billShopService = new BillShop(_billShop != null ? _billShop.BillPacks : null, _billShop != null ? _billShop.CoinPacks : null,
                Services.Get<BillWallet>(), wallet, BaseIncomePerSecond, _currentCity.ValueScale, platform,
                save != null ? save.SaveNow : (System.Action)null);
            Services.Register(_billShopService);

            double minCoinsPerBill = (_billShop != null ? _billShop.MinCoinsPerBill : 200) * _currentCity.ValueScale;
            var pricing = new BillPricing(BaseIncomePerSecond, _billShop != null ? _billShop.SecondsPerBill : 360,
                minCoinsPerBill > 0 ? minCoinsPerBill : 1);
            _billPurchases = new BillPurchases(_stations, _upgrades, _managers, Services.Get<BillWallet>(), pricing,
                save != null ? save.SaveNow : (System.Action)null);
            Services.Register(_billPurchases);

            // Стартовый набор: постоянная покупка, факт покупки — в сейве.
            _starterPack = new StarterPack(_billShop != null ? _billShop.StarterPack : new StarterPackConfig(), Services.Get<BillWallet>(), wallet,
                pricing, BaseIncomePerSecond, platform, save != null ? save.SaveNow : (System.Action)null);
            if (_save != null)
                _save.Register(_starterPack);
            Services.Register(_starterPack);

            // Покупки, оплаченные, но не выданные до закрытия игры (требование модерации Яндекса).
            PurchaseRecovery.Run(platform, id => _billShopService.TryGrantPurchasedPack(id) || _starterPack.TryGrantPurchased(id) || _permanent.TryGrantPurchased(id), Debug.Log);

            // Купюры за рекламу: таймер по серверному времени, состояние — в сейве (переживает переезд).
            _adBills = new AdBillService(_balance.CreateAdBillReward(), Services.Get<BillWallet>(), platform,
                () => platform != null ? platform.ServerTimeUtc : System.DateTime.UtcNow, save != null ? save.SaveNow : (System.Action)null);
            if (_save != null)
                _save.Register(_adBills);
            Services.Register(_adBills);

            CreateRetention(wallet, platform);

            // Онбординг (этап 11): после восстановления станций из сейва — выполненные шаги сразу пропускаются.
            _tutorial = new TutorialService(_stations, _managers, wallet, save != null ? save.SaveNow : (System.Action)null);
            RegisterParticipant(_tutorial);
            Services.Register(_tutorial);

            _prestigeController = new PrestigeController(_prestige, _rules, _currentCity.PrestigeThreshold, cityIndex, cityCount,
                _save, _saveParticipant, StartCoinsForCity, ReloadScene);
            Services.Register(_prestigeController);

            _tickManager = Services.Get<TickManager>();
            _tickManager.Register(_stations);
            _tickManager.Register(_boost);
            _tickManager.Register(_quests);
            CreateWeather(platform);

            // Репутация из заданий дня сразу даёт бонус к доходу.
            _prestige.Changed += ApplyPrestigeModifiers;
        }

        /// <summary>
        /// Удержание (этап 10): буст ×2, подарок-ящик, календарь и задания дня. Создаётся после расчёта офлайн-дохода —
        /// буст на него не действует. Состояние — в SaveData.Retention, переживает переезд.
        /// </summary>
        private void CreateRetention(Wallet wallet, IPlatformService platform)
        {
            _retentionConfig = _retention != null ? _retention : ScriptableObject.CreateInstance<RetentionConfig>();
            System.Func<System.DateTime> utcNow = () => platform != null ? platform.ServerTimeUtc : System.DateTime.UtcNow;
            System.Action saveNow = _save != null ? _save.SaveNow : (System.Action)null;
            double minCoinsPerMinute = _retentionConfig.MinCoinsPerMinute * _currentCity.ValueScale;
            System.Func<double> coinsPerMinute = () => RewardMath.CoinsPerMinute(BaseIncomePerSecond(), minCoinsPerMinute);

            _boost = new BoostService(_retentionConfig.CreateBoost(), platform, saveNow);
            RegisterParticipant(_boost);
            Services.Register(_boost);
            _boost.MultiplierChanged += ApplyPrestigeModifiers;
            ApplyPrestigeModifiers();

            _gift = new GiftBoxService(System.TimeSpan.FromMinutes(_retentionConfig.GiftCooldownMinutes), _retentionConfig.GiftIncomeMinutes,
                wallet, coinsPerMinute, platform, utcNow, saveNow);
            RegisterParticipant(_gift);
            Services.Register(_gift);

            var granter = new RewardGranter(wallet, Services.Get<BillWallet>(), _prestige, coinsPerMinute);
            _daily = new DailyRewardService(new DailyRewardCalendar(), _retentionConfig.Days, _retentionConfig.SkinRepeatBills, granter,
                utcNow, saveNow);
            RegisterParticipant(_daily);
            Services.Register(_daily);

            ManagerController managers = _managers;
            UpgradeService upgrades = _upgrades;
            _quests = new DailyQuestService(new DailyQuests(_retentionConfig.Quests, _retentionConfig.QuestsPerDay), granter, utcNow,
                (type, target) => QuestTracker.IsAvailable(type, target, managers, upgrades), saveNow);
            RegisterParticipant(_quests);
            Services.Register(_quests);
            _questTracker = new QuestTracker(_quests, _stations, _managers, _upgrades, _boost, _gift);

            Services.Register(_retentionConfig);
        }

        /// <summary>Погода (V5) — только визуал: по серверному времени, детерминированно для города; не сохраняется.</summary>
        private void CreateWeather(IPlatformService platform)
        {
            _ambientConfig = _ambient != null ? _ambient : ScriptableObject.CreateInstance<AmbientConfig>();
            _weather = new WeatherService(_ambientConfig.CreateSchedule(), () => platform != null ? platform.ServerTimeUtc : System.DateTime.UtcNow,
                _currentCity.Id, _currentCity.RainChance, _currentCity.SnowAllowed, _ambientConfig.WeatherCheckInterval);
            Services.Register(_ambientConfig);
            Services.Register(_weather);
            _tickManager.Register(_weather);

            if (Services.TryGet(out _ambientSound))
            {
                if (Services.TryGet(out _settings))
                    _settings.Changed += OnSettingsChanged;
                _ambientSound.Apply(_ambientAudio, _currentCity.AmbientBase);
                _ambientSound.SetWeather(WeatherClip(_weather.Current));
                _weather.WeatherChanged += OnWeatherChanged;
            }
        }

        /// <summary>Музыка города (V6); при переезде MusicService сменит трек кроссфейдом. Нет своей — запасной трек.</summary>
        private void PlayCityMusic()
        {
            if (_music == null || !Services.TryGet(out MusicService music))
                return;
            AudioClip track = _currentCity.Music != null ? _currentCity.Music : _music.Track;
            music.Play(track, _music.Volume, _music.FadeDuration);
        }

        private void OnWeatherChanged(WeatherKind kind)
        {
            _ambientSound.SetWeather(WeatherClip(kind));
        }

        // Эффекты фона выключены — дождя не видно, значит и не слышно.
        private void OnSettingsChanged()
        {
            _ambientSound.SetWeather(WeatherClip(_weather.Current));
        }

        private AudioClip WeatherClip(WeatherKind kind)
        {
            if (_ambientAudio == null || (_settings != null && !_settings.EffectsOn))
                return null;
            switch (kind)
            {
                case WeatherKind.Rain:
                    return _ambientAudio.Rain;
                case WeatherKind.Snow:
                    return _ambientAudio.Wind;
                default:
                    return null;
            }
        }

        private void RegisterParticipant(ISaveParticipant participant)
        {
            if (_save != null)
                _save.Register(participant);
        }

        /// <summary>Доход/с без буста ×2: награды «минуты дохода» и курс купюр не зависят от того, идёт ли буст.</summary>
        private double BaseIncomePerSecond()
        {
            double boost = _boost != null ? _boost.Boost.Multiplier : 1;
            return _stations.IncomePerSecond / boost;
        }

        private void OnPermanentPurchased(PermanentProduct product)
        {
            ApplyPrestigeModifiers();
            ApplyAdsSetting();
        }

        private void ApplyAdsSetting()
        {
            if (Services.TryGet(out InterstitialService interstitial))
                interstitial.AdsDisabled = _permanent.Has(PermanentProduct.NoAds);
        }

        private void OnShopPurchased(int index)
        {
            ApplyPrestigeModifiers();
        }

        /// <summary>Бонус репутации и «Постоянный ×2» к доходу, ускорение циклов и бесплатные менеджеры из магазина.</summary>
        private void ApplyPrestigeModifiers()
        {
            double cycleTimeFactor = _reputationShop.GetEffect(ShopEffect.CycleTimeFactor);
            double speedMultiplier = cycleTimeFactor > 0 ? 1 / cycleTimeFactor : 1;
            double permanent = _permanent != null ? _permanent.IncomeMultiplier : 1;
            double boost = _boost != null ? _boost.Boost.Multiplier : 1;
            _stations.SetGlobalModifiers(_rules.IncomeMultiplier(_prestige.ReputationEarned) * permanent * boost, speedMultiplier);
            _managers.SetFreeManagers((int)_reputationShop.GetEffect(ShopEffect.FreeManagers));
        }

        private double StartCoinsForCity(int cityIndex)
        {
            double scale = _cities != null && _cities.Count > 0 ? _cities.Get(cityIndex).ValueScale : 1;
            return _reputationShop.GetEffect(ShopEffect.StartCoins) * scale;
        }

        // Переход в новый город: результат в лидерборд, затем (если правила позволяют) межстраничная реклама и перезагрузка сцены.
        private void ReloadScene()
        {
            if (Services.TryGet(out LeaderboardService leaderboard))
                leaderboard.SendIfGrown();

            string sceneName = gameObject.scene.name;
            if (Services.TryGet(out InterstitialService interstitial))
                interstitial.TryShow(() => SceneManager.LoadScene(sceneName));
            else
                SceneManager.LoadScene(sceneName);
        }

        private void UnregisterParticipant(ISaveParticipant participant)
        {
            if (_save != null && participant != null)
                _save.Unregister(participant);
        }

        private OfflineIncome CalculateOfflineIncome()
        {
            if (_save == null || !Services.TryGet(out IPlatformService platform))
                return OfflineIncome.None;

            OfflineSettings settings = _balance.CreateOfflineSettings()
                .WithExtraLimit(_reputationShop.GetEffect(ShopEffect.OfflineLimitHours) * 3600);
            return OfflineIncomeCalculator.Calculate(_stations.IncomePerSecond, _save.Data.SavedAtUtcTicks,
                platform.ServerTimeUtc, settings);
        }

        private void OnDestroy()
        {
            if (_stations == null)
                return;

            if (_tickManager != null)
            {
                _tickManager.Unregister(_stations);
                _tickManager.Unregister(_boost);
                _tickManager.Unregister(_quests);
                _tickManager.Unregister(_weather);
            }
            if (_weather != null)
                _weather.WeatherChanged -= OnWeatherChanged;
            if (_settings != null)
                _settings.Changed -= OnSettingsChanged;
            Services.Unregister(_weather);
            Services.Unregister(_ambientConfig);
            _prestige.Changed -= ApplyPrestigeModifiers;
            _questTracker?.Dispose();
            UnregisterParticipant(_tutorial);
            _tutorial?.Dispose();
            Services.Unregister(_tutorial);
            if (_boost != null)
                _boost.MultiplierChanged -= ApplyPrestigeModifiers;
            UnregisterParticipant(_boost);
            UnregisterParticipant(_gift);
            UnregisterParticipant(_daily);
            UnregisterParticipant(_quests);
            Services.Unregister(_boost);
            Services.Unregister(_gift);
            Services.Unregister(_daily);
            Services.Unregister(_quests);
            Services.Unregister(_retentionConfig);
            if (_save != null && _saveParticipant != null)
                _save.Unregister(_saveParticipant);
            _stations.Earned -= _prestige.AddEarned;
            if (_offline != null)
                _offline.Claimed -= _prestige.AddEarned;
            if (_reputationShop != null)
                _reputationShop.Purchased -= OnShopPurchased;
            _audioService?.Dispose();

            // Снимаем только свои экземпляры: при перезагрузке сцены новые уже могли зарегистрироваться.
            Services.Unregister(_prestigeController);
            Services.Unregister(_billShopService);
            Services.Unregister(_billPurchases);
            Services.Unregister(_starterPack);
            Services.Unregister(_permanent);
            if (_save != null && _permanent != null)
                _save.Unregister(_permanent);
            if (_save != null && _starterPack != null)
                _save.Unregister(_starterPack);
            Services.Unregister(_adBills);
            if (_save != null && _adBills != null)
                _save.Unregister(_adBills);
            Services.Unregister(_offline);
            Services.Unregister(_customers);
            Services.Unregister(_audioService);
            Services.Unregister(_reputationShop);
            Services.Unregister(_upgrades);
            Services.Unregister(_managers);
            Services.Unregister(_stations);
            Services.Unregister(_currentCity);
            Services.Unregister(_balance);
            Services.Unregister(_cities);
        }
    }
}
