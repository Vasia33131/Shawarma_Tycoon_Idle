using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Stations;
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

        private TickManager _tickManager;
        private StationController _stations;

        private void Awake()
        {
            if (_balance == null || _city == null)
            {
                Debug.LogError($"{nameof(GameSceneInstaller)}: assign Balance and City configs.", this);
                enabled = false;
                return;
            }

            _stations = new StationController(_city.Stations, _balance.CreateMilestoneTable(), Services.Get<Wallet>());
            Services.Register(_stations);

            _tickManager = Services.Get<TickManager>();
            _tickManager.Register(_stations);
        }

        private void OnDestroy()
        {
            if (_stations == null)
                return;

            if (_tickManager != null)
                _tickManager.Unregister(_stations);
            Services.Unregister<StationController>();
        }
    }
}
