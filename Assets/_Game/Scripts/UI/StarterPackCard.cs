using System.Globalization;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Яркая карточка «Стартовый набор» в начале вкладки «Магазин»: купюры + монеты за одну покупку.
    /// Показывается, пока набор не куплен; после покупки скрывается и монеты летят в HUD.
    /// Тексты и сумму монет выставляет ShopPanel (раз в секунду, без аллокаций).
    /// </summary>
    public sealed class StarterPackCard : MonoBehaviour
    {
        private const int CoinsPerPurchaseEffect = 12;

        [SerializeField] private Image _icon = null;
        [SerializeField] private TMP_Text _titleText = null;
        [SerializeField] private TMP_Text _ribbonText = null;
        [SerializeField] private TMP_Text _onceText = null;
        [SerializeField] private TMP_Text _billsText = null;
        [SerializeField] private TMP_Text _coinsText = null;
        [SerializeField] private Button _buyButton = null;
        [SerializeField] private TMP_Text _buyText = null;
        [Tooltip("Ленточка «Супер-выгода!» пульсирует всегда, пока карточка видна.")]
        [SerializeField] private PulseEffect _ribbonPulse = null;

        private readonly NumberText _coinsNumber = new NumberText();
        private StarterPack _pack;
        private Loc _loc;
        private PriceCatalog _prices;
        private CoinFlyEffect _coinFly;
        private LocTemplate _coinsTemplate;
        private double _shownCoins = -1;

        public void Bind(StarterPack pack, Loc loc, CoinFlyEffect coinFly)
        {
            _pack = pack;
            _loc = loc;
            _coinFly = coinFly;
            Services.TryGet(out _prices);
            _coinsTemplate = loc.GetTemplate("shop.coins_reward");
            if (_icon != null)
            {
                _icon.sprite = pack.Config.Icon;
                _icon.gameObject.SetActive(pack.Config.Icon != null);
            }
            if (_ribbonPulse != null)
                _ribbonPulse.Active = true;

            _buyButton.onClick.AddListener(OnBuy);
            _pack.Purchased += OnPurchased;
            if (_prices != null)
                _prices.Changed += RefreshStatic;
            RefreshStatic();
        }

        private void OnDestroy()
        {
            if (_pack != null)
                _pack.Purchased -= OnPurchased;
            if (_prices != null)
                _prices.Changed -= RefreshStatic;
        }

        /// <summary>Тексты, зависящие от языка; вызывать при смене языка.</summary>
        public void RefreshStatic()
        {
            if (_pack == null)
                return;

            _titleText.text = _loc.Get("shop.starter_title");
            _ribbonText.text = _loc.Get("shop.starter_badge");
            _onceText.text = _loc.Get("shop.starter_once");
            _billsText.text = _loc.Format("shop.bills_amount", _pack.Bills.ToString(CultureInfo.InvariantCulture));
            _buyText.text = _prices != null ? _prices.Format(_loc, _pack.Config.ProductId, _pack.DisplayPrice)
                : _loc.Format("shop.price_yan", _pack.DisplayPrice.ToString(CultureInfo.InvariantCulture));
            Refresh(force: true);
        }

        /// <summary>Видимость, доступность кнопки и сумма монет (зависит от текущего дохода).</summary>
        public void Refresh(bool force = false)
        {
            if (_pack == null)
                return;

            // Набора нет в каталоге консоли — карточку не показываем (требование 1.13.6).
            bool visible = !_pack.IsPurchased && (_prices == null || _prices.IsAvailable(_pack.Config.ProductId));
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            if (!visible)
                return;

            _buyButton.interactable = _pack.CanBuy;

            double coins = _pack.GetCoinReward();
            if (!force && coins == _shownCoins)
                return;

            _shownCoins = coins;
            _coinsNumber.Clear().Append(_coinsTemplate.Prefix).AppendNumber(coins).Append(_coinsTemplate.Suffix);
            _coinsText.SetNumberText(_coinsNumber);
        }

        private void OnBuy()
        {
            if (_pack.Buy(_ => Refresh()))
                Refresh();
        }

        private void OnPurchased()
        {
            if (_coinFly != null)
            {
                Vector3 from = _buyButton.transform.position;
                for (int i = 0; i < CoinsPerPurchaseEffect; i++)
                    _coinFly.Fly(from);
            }

            Refresh();
        }
    }
}
