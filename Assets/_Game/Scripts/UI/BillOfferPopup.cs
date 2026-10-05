using System;
using System.Globalization;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// «Не хватает монет»: предложение купить станцию, улучшение или менеджера за купюры (этап 08e).
    /// Всегда с подтверждением — случайный тап не тратит купюры. Если купюр мало — кнопка ведёт в магазин.
    /// </summary>
    public sealed class BillOfferPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root = null;
        [SerializeField] private RectTransform _panel = null;
        private PopupAnimator _animator;
        private PopupAnimator Animator => _animator != null ? _animator : _animator = PopupAnimator.For(_root, _panel);
        [SerializeField] private TMP_Text _bodyText = null;
        [SerializeField] private Button _confirmButton = null;
        [SerializeField] private TMP_Text _confirmText = null;
        [SerializeField] private Button _cancelButton = null;

        private BillPurchases _purchases;
        private Loc _loc;
        private Action _openShop;
        private Func<long, bool> _buy;
        private long _price;

        public void Bind(BillPurchases purchases, TweenRunner tweens, Loc loc, Action openShop)
        {
            _purchases = purchases;
            _loc = loc;
            _openShop = openShop;
            _confirmButton.onClick.AddListener(OnConfirm);
            _cancelButton.onClick.AddListener(Hide);
            _root.SetActive(false);
        }

        /// <param name="unlock">Станция ещё закрыта — покупка первого уровня.</param>
        public void OfferStation(int index, int count, string stationName, bool unlock)
        {
            string item = unlock
                ? _loc.Format("bills.item_unlock", stationName)
                : _loc.Format("bills.item_levels", stationName, count.ToString(CultureInfo.InvariantCulture));
            Show(item, _purchases.StationPrice(index, count), quote => _purchases.TryBuyStation(index, count, quote));
        }

        public void OfferUpgrade(int index, string upgradeName)
        {
            Show(_loc.Format("bills.item_upgrade", upgradeName), _purchases.UpgradePrice(index), quote => _purchases.TryBuyUpgrade(index, quote));
        }

        public void OfferManager(int index, string stationName)
        {
            Show(_loc.Format("bills.item_manager", stationName), _purchases.ManagerPrice(index), quote => _purchases.TryHireManager(index, quote));
        }

        private void Show(string item, long price, Func<long, bool> buy)
        {
            if (price <= 0)
                return;

            _price = price;
            _buy = buy;
            string priceText = price.ToString(CultureInfo.InvariantCulture);
            long have = _purchases.Bills.Bills;
            if (have >= price)
            {
                _bodyText.text = _loc.Format("bills.offer_body", item, priceText);
                _confirmText.text = _loc.Format("bills.offer_buy", priceText);
            }
            else
            {
                _bodyText.text = _loc.Format("bills.offer_poor", priceText, have.ToString(CultureInfo.InvariantCulture));
                _confirmText.text = _loc.Get("bills.offer_shop");
            }

            Animator.Show();
        }

        private void OnConfirm()
        {
            if (_purchases.Bills.Bills >= _price)
                _buy?.Invoke(_price);
            else
                _openShop?.Invoke();
            Hide();
        }

        private void Hide()
        {
            _buy = null;
            Animator.Hide();
        }
    }
}
