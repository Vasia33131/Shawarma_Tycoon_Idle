using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Stations;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Корень игрового экрана: связывает вью с сервисами и подключает их к TickManager.
    /// Start, а не Awake: StationController создаёт GameSceneInstaller в своём Awake.
    /// </summary>
    public sealed class GameScreen : MonoBehaviour
    {
        [SerializeField] private HudView _hud = null;
        [SerializeField] private StationsPanel _stationsPanel = null;

        private TickManager _tickManager;

        private void Start()
        {
            var wallet = Services.Get<Wallet>();
            var stations = Services.Get<StationController>();
            var loc = Services.Get<Loc>();

            _hud.Bind(wallet, stations, loc);
            _stationsPanel.Bind(stations, wallet, loc);

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
