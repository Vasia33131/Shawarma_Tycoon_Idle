using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Offline;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;
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

            _hud.Bind(wallet, _stations, loc);
            _stationsPanel.Bind(_stations, wallet, loc, _coinFly, _floatingText, _kiosk.Window);
            _managersPanel.Bind(Services.Get<ManagerController>(), _stations, wallet, loc);
            _upgradesPanel.Bind(Services.Get<UpgradeService>(), _stations, wallet, loc);

            _tickManager = Services.Get<TickManager>();
            _tickManager.Register(_hud);
            _tickManager.Register(_stationsPanel);
            _tickManager.Register(_kiosk);

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

        private void OnDestroy()
        {
            if (_stations != null)
                _stations.Earned -= _incomeRate.Add;

            if (_tickManager == null)
                return;

            _tickManager.Unregister(_hud);
            _tickManager.Unregister(_stationsPanel);
            _tickManager.Unregister(_kiosk);
            _tickManager.Unregister(_customers);
        }
    }
}
