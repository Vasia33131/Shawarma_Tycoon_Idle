using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Offline;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Prestige;
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

        private TickManager _tickManager;
        private CityConfig _currentCity;
        private StationController _stations;
        private ManagerController _managers;
        private UpgradeService _upgrades;
        private ReputationShop _reputationShop;
        private BillShop _billShopService;
        private BillPurchases _billPurchases;
        private AdBillService _adBills;
        private OfflineIncomeService _offline;
        private PrestigeController _prestigeController;
        private PrestigeState _prestige;
        private PrestigeRules _rules;
        private AudioService _audioService;
        private SaveService _save;
        private CitySaveParticipant _saveParticipant;

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
            if (_music != null && Services.TryGet(out MusicService music))
                music.Play(_music.Track, _music.Volume, _music.FadeDuration);

            if (_customers != null)
                Services.Register(_customers);
            else
                Debug.LogWarning($"{nameof(GameSceneInstaller)}: CustomerConfig is not assigned, no customers.", this);

            // Состояние из сейва восстанавливается здесь, до того как UI в Start прочитает модели.
            if (Services.TryGet(out _save))
            {
                _saveParticipant = new CitySaveParticipant(wallet, _stations, _upgrades);
                _save.Register(_saveParticipant);
            }

            // Офлайн-доход считается по времени из только что загруженного сейва — до первого автосейва.
            _offline = new OfflineIncomeService(wallet, CalculateOfflineIncome(), _balance.OfflineAdMultiplier);
            _offline.Claimed += _prestige.AddEarned;
            Services.Register(_offline);

            Services.TryGet(out IPlatformService platform);
            SaveService save = _save;
            _billShopService = new BillShop(_billShop != null ? _billShop.BillPacks : null, _billShop != null ? _billShop.CoinPacks : null,
                Services.Get<BillWallet>(), wallet, () => _stations.IncomePerSecond, _currentCity.ValueScale, platform,
                save != null ? save.SaveNow : (System.Action)null);
            Services.Register(_billShopService);

            double minCoinsPerBill = (_billShop != null ? _billShop.MinCoinsPerBill : 200) * _currentCity.ValueScale;
            var pricing = new BillPricing(() => _stations.IncomePerSecond, _billShop != null ? _billShop.SecondsPerBill : 360,
                minCoinsPerBill > 0 ? minCoinsPerBill : 1);
            _billPurchases = new BillPurchases(_stations, _upgrades, _managers, Services.Get<BillWallet>(), pricing,
                save != null ? save.SaveNow : (System.Action)null);
            Services.Register(_billPurchases);

            // Купюры за рекламу: таймер по серверному времени, состояние — в сейве (переживает переезд).
            _adBills = new AdBillService(_balance.CreateAdBillReward(), Services.Get<BillWallet>(), platform,
                () => platform != null ? platform.ServerTimeUtc : System.DateTime.UtcNow, save != null ? save.SaveNow : (System.Action)null);
            if (_save != null)
                _save.Register(_adBills);
            Services.Register(_adBills);

            _prestigeController = new PrestigeController(_prestige, _rules, _currentCity.PrestigeThreshold, cityIndex, cityCount,
                _save, _saveParticipant, StartCoinsForCity, ReloadScene);
            Services.Register(_prestigeController);

            _tickManager = Services.Get<TickManager>();
            _tickManager.Register(_stations);
        }

        private void OnShopPurchased(int index)
        {
            ApplyPrestigeModifiers();
        }

        /// <summary>Бонус репутации к доходу, ускорение циклов и бесплатные менеджеры из магазина.</summary>
        private void ApplyPrestigeModifiers()
        {
            double cycleTimeFactor = _reputationShop.GetEffect(ShopEffect.CycleTimeFactor);
            double speedMultiplier = cycleTimeFactor > 0 ? 1 / cycleTimeFactor : 1;
            _stations.SetGlobalModifiers(_rules.IncomeMultiplier(_prestige.ReputationEarned), speedMultiplier);
            _managers.SetFreeManagers((int)_reputationShop.GetEffect(ShopEffect.FreeManagers));
        }

        private double StartCoinsForCity(int cityIndex)
        {
            double scale = _cities != null && _cities.Count > 0 ? _cities.Get(cityIndex).ValueScale : 1;
            return _reputationShop.GetEffect(ShopEffect.StartCoins) * scale;
        }

        private void ReloadScene()
        {
            SceneManager.LoadScene(gameObject.scene.name);
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
                _tickManager.Unregister(_stations);
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
