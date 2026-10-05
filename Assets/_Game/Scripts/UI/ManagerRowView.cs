using System.Globalization;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Stations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Строка менеджера: станция, описание и кнопка «Нанять за X» / «Нанят» / «Открой станцию».
    /// </summary>
    public sealed class ManagerRowView : MonoBehaviour, ISceneBinding
    {
        private enum State
        {
            None,
            StationLocked,
            CanHire,
            Hired,
        }

        [SerializeField] private Image _icon = null;
        [SerializeField] private TMP_Text _iconLabel = null;
        [SerializeField] private TMP_Text _nameText = null;
        [SerializeField] private TMP_Text _descriptionText = null;
        [SerializeField] private Button _hireButton = null;
        [SerializeField] private TMP_Text _hireText = null;
        [SerializeField] private BillOfferTrigger _hireOffer = null;

        private readonly NumberText _hireBuilder = new NumberText();

        private int _index;
        private LocTemplate _hireTemplate;
        private StationModel _station;
        private ManagerController _managers;
        private Wallet _wallet;
        private Loc _loc;

        private State _shownState;
        private bool _shownAffordable;
        private BillOfferPopup _offers;

        /// <param name="offers">Найм за купюры, когда не хватает монет; null — без него.</param>
        public void Bind(int index, StationModel station, ManagerController managers, Wallet wallet, Loc loc, BillOfferPopup offers)
        {
            SceneBindings.Track(this);
            _index = index;
            _station = station;
            _managers = managers;
            _wallet = wallet;
            _loc = loc;
            _offers = offers;
            _hireTemplate = loc.GetTemplate("manager.hire");

            _icon.color = StationColors.Get(index);
            _iconLabel.text = (index + 1).ToString(CultureInfo.InvariantCulture);
            _hireButton.onClick.AddListener(OnHire);
            if (_hireOffer != null)
                _hireOffer.Clicked += OnHireOffer;

            _station.Changed += OnStationChanged;
            _wallet.CoinsChanged += OnCoinsChanged;
            _loc.LanguageChanged += ForceRefresh;

            ForceRefresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_station == null)
                return;

            _station.Changed -= OnStationChanged;
            _wallet.CoinsChanged -= OnCoinsChanged;
            _loc.LanguageChanged -= ForceRefresh;
        }

        private void OnHire()
        {
            _managers.TryHire(_index);
        }

        private void OnHireOffer()
        {
            _offers.OfferManager(_index, _nameText.text);
        }

        private void OnStationChanged(StationModel station)
        {
            Refresh(force: false);
        }

        private void OnCoinsChanged(double coins)
        {
            Refresh(force: false);
        }

        private void SetHireState(bool interactable, bool offer)
        {
            if (_hireOffer != null)
                _hireOffer.Set(interactable, offer && _offers != null);
            else
                _hireButton.interactable = interactable;
        }

        private void ForceRefresh()
        {
            _nameText.text = _loc.Get(_station.Definition.NameKey);
            _descriptionText.text = _loc.Get("manager.description");
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            State state = _managers.IsHired(_index) ? State.Hired
                : _managers.CanHire(_index) ? State.CanHire
                : State.StationLocked;
            bool affordable = state == State.CanHire && _wallet.CanAfford(_managers.GetCost(_index));

            if (!force && state == _shownState && affordable == _shownAffordable)
                return;

            _shownState = state;
            _shownAffordable = affordable;

            switch (state)
            {
                case State.Hired:
                    _hireText.text = _loc.Get("manager.hired");
                    SetHireState(false, offer: false);
                    break;
                case State.CanHire:
                    _hireBuilder.Clear().Append(_hireTemplate.Prefix).AppendNumber(_managers.GetCost(_index)).Append(_hireTemplate.Suffix);
                    _hireText.SetNumberText(_hireBuilder);
                    SetHireState(affordable, offer: true);
                    break;
                default:
                    _hireText.text = _loc.Get("manager.locked");
                    SetHireState(false, offer: false);
                    break;
            }
        }
    }
}
