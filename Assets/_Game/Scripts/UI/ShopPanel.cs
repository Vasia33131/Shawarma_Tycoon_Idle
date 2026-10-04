using System.Globalization;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Shop;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Вкладка «Магазин»: паки купюр за реальные деньги и мешки монет за купюры.
    /// Награда мешков зависит от дохода — пересчитывается раз в секунду, пока вкладка открыта (без аллокаций).
    /// </summary>
    public sealed class ShopPanel : MonoBehaviour, ITickable, ISceneBinding
    {
        private const float RefreshInterval = 1f;
        private const int CoinsPerPurchaseEffect = 6;

        [SerializeField] private RectTransform _billsGroup = null;
        [SerializeField] private RectTransform _coinsGroup = null;
        [SerializeField] private BillShopRowView _rowPrefab = null;

        [Header("Look")]
        [SerializeField] private Color _billColor = new Color(0.25f, 0.62f, 0.33f);
        [SerializeField] private Color _billMarkColor = new Color(0.72f, 0.90f, 0.70f);
        [SerializeField] private Color _coinColor = new Color(1f, 0.80f, 0.20f);
        [SerializeField] private Color _coinMarkColor = new Color(1f, 0.65f, 0.10f);
        [SerializeField] private Color _billButtonColor = new Color(0.25f, 0.62f, 0.33f);
        [SerializeField] private Color _coinButtonColor = new Color(0.95f, 0.55f, 0.15f);

        private BillShop _shop;
        private BillWallet _bills;
        private Loc _loc;
        private CoinFlyEffect _coinFly;
        private BillShopRowView[] _billRows;
        private BillShopRowView[] _coinRows;
        private NumberText[] _rewardTexts;
        private LocTemplate _rewardTemplate;
        private double[] _shownRewards;
        private float _timer;

        public void Bind(BillShop shop, BillWallet bills, Loc loc, CoinFlyEffect coinFly)
        {
            SceneBindings.Track(this);
            _shop = shop;
            _bills = bills;
            _loc = loc;
            _coinFly = coinFly;
            _rewardTemplate = loc.GetTemplate("shop.coins_reward");

            _billRows = new BillShopRowView[shop.BillPackCount];
            for (int i = 0; i < _billRows.Length; i++)
            {
                BillShopRowView row = CreateRow(_billsGroup, "BillPack_" + shop.GetBillPack(i).Id, _billColor, _billMarkColor, _billButtonColor);
                int index = i;
                row.Button.onClick.AddListener(() => OnBuyBills(index));
                _billRows[i] = row;
            }

            _coinRows = new BillShopRowView[shop.CoinPackCount];
            _rewardTexts = new NumberText[_coinRows.Length];
            _shownRewards = new double[_coinRows.Length];
            for (int i = 0; i < _coinRows.Length; i++)
            {
                BillShopRowView row = CreateRow(_coinsGroup, "CoinPack_" + shop.GetCoinPack(i).Id, _coinColor, _coinMarkColor, _coinButtonColor);
                int index = i;
                row.Button.onClick.AddListener(() => OnBuyCoins(index));
                _coinRows[i] = row;
                _rewardTexts[i] = new NumberText();
            }

            _bills.BillsChanged += OnBillsChanged;
            _shop.CoinsPurchased += OnCoinsPurchased;
            _loc.LanguageChanged += RefreshStatic;
            RefreshStatic();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_shop == null)
                return;
            _bills.BillsChanged -= OnBillsChanged;
            _shop.CoinsPurchased -= OnCoinsPurchased;
            _loc.LanguageChanged -= RefreshStatic;
        }

        private void OnEnable()
        {
            if (_shop != null)
                RefreshRewards(force: false);
        }

        public void Tick(float deltaTime)
        {
            if (_shop == null || !gameObject.activeInHierarchy)
                return;

            _timer += deltaTime;
            if (_timer < RefreshInterval)
                return;

            _timer = 0f;
            RefreshRewards(force: false);
        }

        private BillShopRowView CreateRow(RectTransform group, string name, Color icon, Color mark, Color button)
        {
            BillShopRowView row = Instantiate(_rowPrefab, group);
            row.name = name;
            row.SetLook(icon, mark, button);
            return row;
        }

        private void OnBuyBills(int index)
        {
            if (_shop.BuyBillPack(index, _ => RefreshInteractable()))
                RefreshInteractable();
        }

        private void OnBuyCoins(int index)
        {
            _shop.TryBuyCoins(index);
        }

        private void OnCoinsPurchased(int index, double coins)
        {
            if (_coinFly == null)
                return;

            Vector3 from = _coinRows[index].Button.transform.position;
            for (int i = 0; i < CoinsPerPurchaseEffect; i++)
                _coinFly.Fly(from);
        }

        private void OnBillsChanged(long bills, long delta)
        {
            RefreshInteractable();
        }

        private void RefreshStatic()
        {
            for (int i = 0; i < _billRows.Length; i++)
            {
                BillPackConfig pack = _shop.GetBillPack(i);
                BillShopRowView row = _billRows[i];
                row.SetName(_loc.Get(pack.NameKey));
                row.SetDescription(_loc.Format("shop.bills_amount", pack.Bills.ToString(CultureInfo.InvariantCulture)));
                row.SetBadge(pack.Best);
                row.SetButton(_loc.Format("shop.price_yan", pack.DisplayPrice.ToString(CultureInfo.InvariantCulture)), !_shop.IsPurchasing);
            }

            for (int i = 0; i < _coinRows.Length; i++)
            {
                CoinPackConfig pack = _shop.GetCoinPack(i);
                BillShopRowView row = _coinRows[i];
                row.SetName(_loc.Get(pack.NameKey) + " · " + DurationFormatter.Format(pack.IncomeMinutes * 60, _loc));
                row.SetBadge(false);
                row.SetButton(_loc.Format("shop.price_bills", pack.PriceBills.ToString(CultureInfo.InvariantCulture)), _shop.CanBuyCoins(i));
            }

            RefreshRewards(force: true);
        }

        private void RefreshInteractable()
        {
            for (int i = 0; i < _billRows.Length; i++)
                _billRows[i].SetInteractable(!_shop.IsPurchasing);
            for (int i = 0; i < _coinRows.Length; i++)
                _coinRows[i].SetInteractable(_shop.CanBuyCoins(i));
        }

        private void RefreshRewards(bool force)
        {
            for (int i = 0; i < _coinRows.Length; i++)
            {
                double reward = _shop.GetCoinReward(i);
                if (!force && reward == _shownRewards[i])
                    continue;

                _shownRewards[i] = reward;
                _coinRows[i].SetDescription(_rewardTexts[i].Clear().Append(_rewardTemplate.Prefix).AppendNumber(reward)
                    .Append(_rewardTemplate.Suffix));
            }
        }
    }
}
