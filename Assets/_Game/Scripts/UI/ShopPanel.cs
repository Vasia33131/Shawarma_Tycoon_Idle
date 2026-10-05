using System.Globalization;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Shop;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Вкладка «Магазин»: купюры за рекламу, паки купюр за реальные деньги и мешки монет за купюры.
    /// Награда мешков и таймер рекламы обновляются раз в секунду, пока вкладка открыта (без аллокаций);
    /// бейдж «!» на вкладке — даже когда она закрыта.
    /// </summary>
    public sealed class ShopPanel : MonoBehaviour, ITickable, ISceneBinding
    {
        private const float RefreshInterval = 1f;
        private const int CoinsPerPurchaseEffect = 6;
        private static readonly PermanentProduct[] PremiumProducts = { PermanentProduct.NoAds, PermanentProduct.IncomeX2 };

        [SerializeField] private StarterPackCard _starterCard = null;
        [SerializeField] private RectTransform _premiumGroup = null;
        [SerializeField] private RectTransform _adGroup = null;
        [SerializeField] private RectTransform _billsGroup = null;
        [SerializeField] private RectTransform _coinsGroup = null;
        [SerializeField] private BillShopRowView _rowPrefab = null;
        [Tooltip("Бейдж «!» на кнопке вкладки: реклама за купюры доступна.")]
        [SerializeField] private GameObject _tabBadge = null;
        [Tooltip("Иконка строки «Купюры за рекламу» (icon_ad); пусто — цветная плашка.")]
        [SerializeField] private Sprite _adIcon = null;

        [Header("Look")]
        [SerializeField] private Color _billColor = new Color(0.25f, 0.62f, 0.33f);
        [SerializeField] private Color _billMarkColor = new Color(0.72f, 0.90f, 0.70f);
        [SerializeField] private Color _coinColor = new Color(1f, 0.80f, 0.20f);
        [SerializeField] private Color _coinMarkColor = new Color(1f, 0.65f, 0.10f);
        [SerializeField] private Color _billButtonColor = new Color(0.25f, 0.62f, 0.33f);
        [SerializeField] private Color _coinButtonColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField] private Color _adColor = new Color(0.55f, 0.35f, 0.85f);
        [SerializeField] private Color _adMarkColor = new Color(1f, 1f, 1f);
        [SerializeField] private Color _premiumColor = new Color(0.85f, 0.30f, 0.20f);
        [SerializeField] private Color _premiumMarkColor = new Color(1f, 0.90f, 0.60f);
        [SerializeField] private Color _premiumButtonColor = new Color(0.30f, 0.69f, 0.31f);

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

        private PermanentProducts _permanent;
        private BillShopRowView[] _premiumRows;
        private AdBillService _adBills;
        private BillShopRowView _adRow;
        private readonly NumberText _adWaitText = new NumberText();
        private LocTemplate _adWaitTemplate;
        private int _shownAdState = -1;
        private int _shownAdRemaining = -1;
        private long _shownAdSeconds = -1;

        /// <param name="adBills">Купюры за рекламу; null — строки рекламы нет.</param>
        /// <param name="starterPack">Стартовый набор; null — карточки нет.</param>
        /// <param name="permanent">«Без рекламы» и «Постоянный ×2»; null — строк нет.</param>
        public void Bind(BillShop shop, BillWallet bills, Loc loc, CoinFlyEffect coinFly, AdBillService adBills, StarterPack starterPack = null,
            PermanentProducts permanent = null)
        {
            SceneBindings.Track(this);
            _shop = shop;
            _bills = bills;
            _loc = loc;
            _coinFly = coinFly;
            _adBills = adBills;
            _rewardTemplate = loc.GetTemplate("shop.coins_reward");
            _adWaitTemplate = loc.GetTemplate("bills.ad_wait");

            _permanent = permanent;
            _premiumRows = new BillShopRowView[permanent != null && _premiumGroup != null ? PremiumProducts.Length : 0];
            for (int i = 0; i < _premiumRows.Length; i++)
            {
                BillShopRowView row = CreateRow(_premiumGroup, "Premium_" + PremiumProducts[i], _premiumColor, _premiumMarkColor, _premiumButtonColor);
                PermanentProduct product = PremiumProducts[i];
                row.Button.onClick.AddListener(() => OnBuyPremium(product));
                _premiumRows[i] = row;
            }
            if (_premiumGroup != null && _premiumRows.Length == 0)
                _premiumGroup.gameObject.SetActive(false);
            if (_permanent != null)
                _permanent.Purchased += OnPremiumPurchased;

            if (_starterCard != null)
            {
                if (starterPack != null)
                    _starterCard.Bind(starterPack, loc, coinFly);
                else
                    _starterCard.gameObject.SetActive(false);
            }

            if (_adBills != null && _adGroup != null)
            {
                _adRow = CreateRow(_adGroup, "AdBills", _adColor, _adMarkColor, _adColor);
                _adRow.Button.onClick.AddListener(OnWatchAd);
            }

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
            if (_shop == null)
                return;
            RefreshRewards(force: false);
            RefreshAd(force: false);
            if (_starterCard != null)
                _starterCard.Refresh();
        }

        public void Tick(float deltaTime)
        {
            if (_shop == null)
                return;

            _timer += deltaTime;
            if (_timer < RefreshInterval)
                return;

            _timer = 0f;
            RefreshBadge();
            if (!gameObject.activeInHierarchy)
                return;

            RefreshRewards(force: false);
            RefreshAd(force: false);
            if (_starterCard != null)
                _starterCard.Refresh();
        }

        private void OnWatchAd()
        {
            if (_adBills.TryWatch(_ => RefreshAfterAd()))
                RefreshAd(force: true);
        }

        private void RefreshAfterAd()
        {
            RefreshAd(force: true);
            RefreshBadge();
        }

        private void RefreshBadge()
        {
            if (_tabBadge == null)
                return;
            bool available = _adBills != null && _adBills.CanWatch;
            if (_tabBadge.activeSelf != available)
                _tabBadge.SetActive(available);
        }

        // Состояния кнопки: 0 — смотреть, 1 — таймер кулдауна, 2 — лимит на сегодня, 3 — реклама открыта.
        private void RefreshAd(bool force)
        {
            if (_adRow == null)
                return;

            System.DateTime now = _adBills.Now;
            AdBillReward rules = _adBills.Rules;
            int remaining = rules.RemainingToday(now);
            int state = _adBills.IsShowing ? 3 : remaining == 0 ? 2 : rules.CanClaim(now) ? 0 : 1;
            long seconds = state == 1 ? (long)System.Math.Ceiling(rules.TimeLeft(now).TotalSeconds) : 0;

            if (force || remaining != _shownAdRemaining)
            {
                _shownAdRemaining = remaining;
                _adRow.SetDescription(_loc.Format("bills.ad_desc", rules.Reward.ToString(CultureInfo.InvariantCulture),
                    remaining.ToString(CultureInfo.InvariantCulture)));
            }

            if (!force && state == _shownAdState && seconds == _shownAdSeconds)
                return;

            bool stateChanged = force || state != _shownAdState;
            _shownAdState = state;
            _shownAdSeconds = seconds;
            if (state == 1)
            {
                _adWaitText.Clear().Append(_adWaitTemplate.Prefix).AppendInteger(seconds / 60).Append(':');
                if (seconds % 60 < 10)
                    _adWaitText.Append('0');
                _adRow.SetButtonText(_adWaitText.AppendInteger(seconds % 60).Append(_adWaitTemplate.Suffix));
            }
            else if (stateChanged)
            {
                _adRow.SetButton(_loc.Get(state == 2 ? "bills.ad_tomorrow" : "bills.ad_watch"), state == 0);
            }

            if (stateChanged)
                _adRow.SetInteractable(state == 0);
        }

        private BillShopRowView CreateRow(RectTransform group, string name, Color icon, Color mark, Color button)
        {
            BillShopRowView row = Instantiate(_rowPrefab, group);
            row.name = name;
            row.SetLook(icon, mark, button);
            return row;
        }

        private void OnBuyPremium(PermanentProduct product)
        {
            if (_permanent.Buy(product, _ => RefreshInteractable()))
                RefreshInteractable();
        }

        private void OnPremiumPurchased(PermanentProduct product)
        {
            RefreshStatic();
        }

        private void RefreshPremiumRows()
        {
            for (int i = 0; i < _premiumRows.Length; i++)
            {
                PermanentProduct product = PremiumProducts[i];
                BillShopRowView row = _premiumRows[i];
                bool owned = _permanent.Has(product);
                string prefix = product == PermanentProduct.NoAds ? "shop.no_ads" : "shop.x2";
                row.SetIcon(_permanent.GetOffer(product).Icon);
                row.SetName(_loc.Get(prefix + "_name"));
                row.SetDescription(_loc.Get(prefix + "_desc"));
                row.SetBadge(false);
                row.SetButton(owned ? _loc.Get("shop.owned")
                    : _loc.Format("shop.price_yan", _permanent.GetOffer(product).DisplayPrice.ToString(CultureInfo.InvariantCulture)),
                    _permanent.CanBuy(product));
            }
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
            RefreshPremiumRows();
            if (_starterCard != null)
                _starterCard.RefreshStatic();

            if (_adRow != null)
            {
                _adRow.SetName(_loc.Get("bills.ad_name"));
                _adRow.SetIcon(_adIcon);
                _adRow.SetBadge(false);
                RefreshAd(force: true);
            }

            for (int i = 0; i < _billRows.Length; i++)
            {
                BillPackConfig pack = _shop.GetBillPack(i);
                BillShopRowView row = _billRows[i];
                row.SetName(_loc.Get(pack.NameKey));
                row.SetDescription(_loc.Format("shop.bills_amount", pack.Bills.ToString(CultureInfo.InvariantCulture)));
                row.SetBadge(pack.Best);
                row.SetIcon(pack.Icon);
                row.SetButton(_loc.Format("shop.price_yan", pack.DisplayPrice.ToString(CultureInfo.InvariantCulture)), !_shop.IsPurchasing);
            }

            for (int i = 0; i < _coinRows.Length; i++)
            {
                CoinPackConfig pack = _shop.GetCoinPack(i);
                BillShopRowView row = _coinRows[i];
                row.SetName(_loc.Get(pack.NameKey) + " · " + DurationFormatter.Format(pack.IncomeMinutes * 60, _loc));
                row.SetIcon(pack.Icon);
                row.SetBadge(false);
                row.SetButton(_loc.Format("shop.price_bills", pack.PriceBills.ToString(CultureInfo.InvariantCulture)), _shop.CanBuyCoins(i));
            }

            RefreshRewards(force: true);
        }

        private void RefreshInteractable()
        {
            for (int i = 0; i < _premiumRows.Length; i++)
                _premiumRows[i].SetInteractable(_permanent.CanBuy(PremiumProducts[i]));
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
