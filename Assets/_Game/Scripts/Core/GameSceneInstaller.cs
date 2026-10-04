using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;
using UnityEngine;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Сцена Game: создаёт системы текущего города из конфигов и регистрирует их в Services и TickManager.
    /// </summary>
    public sealed class GameSceneInstaller : MonoBehaviour
    {
        [SerializeField] private BalanceConfig _balance = null;
        [SerializeField] private CityConfig _city = null;
        [SerializeField] private AudioConfig _audio = null;
        [SerializeField] private CustomerConfig _customers = null;

        private TickManager _tickManager;
        private StationController _stations;
        private AudioService _audioService;

        private void Awake()
        {
            if (_balance == null || _city == null)
            {
                Debug.LogError($"{nameof(GameSceneInstaller)}: assign Balance and City configs.", this);
                enabled = false;
                return;
            }

            var wallet = Services.Get<Wallet>();

            _stations = new StationController(_city.Stations, _balance.CreateMilestoneTable(), wallet);
            Services.Register(_stations);
            Services.Register(new ManagerController(_stations, wallet, _balance.ManagerCostMultiplier));
            Services.Register(new UpgradeService(_city.Upgrades, _stations, wallet));

            _audioService = new AudioService(_audio);
            Services.Register(_audioService);

            if (_customers != null)
                Services.Register(_customers);
            else
                Debug.LogWarning($"{nameof(GameSceneInstaller)}: CustomerConfig is not assigned, no customers.", this);

            _tickManager = Services.Get<TickManager>();
            _tickManager.Register(_stations);
        }

        private void OnDestroy()
        {
            if (_stations == null)
                return;

            if (_tickManager != null)
                _tickManager.Unregister(_stations);
            _audioService?.Dispose();

            Services.Unregister<CustomerConfig>();
            Services.Unregister<AudioService>();
            Services.Unregister<UpgradeService>();
            Services.Unregister<ManagerController>();
            Services.Unregister<StationController>();
        }
    }
}
