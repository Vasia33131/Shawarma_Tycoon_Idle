using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Managers;
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

        private TickManager _tickManager;

        private void Start()
        {
            var wallet = Services.Get<Wallet>();
            var stations = Services.Get<StationController>();
            var loc = Services.Get<Loc>();

            _hud.Bind(wallet, stations, loc);
            _stationsPanel.Bind(stations, wallet, loc);
            _managersPanel.Bind(Services.Get<ManagerController>(), stations, wallet, loc);
            _upgradesPanel.Bind(Services.Get<UpgradeService>(), stations, wallet, loc);

            _tickManager = Services.Get<TickManager>();
            _tickManager.Register(_hud);
            _tickManager.Register(_stationsPanel);
        }

        private void OnDestroy()
        {
            if (_tickManager == null)
                return;

            _tickManager.Unregister(_hud);
            _tickManager.Unregister(_stationsPanel);
        }
    }
}
