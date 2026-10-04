using System.Globalization;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Offline;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Окно «Пока тебя не было» (GDD §6): сколько менеджеры заработали; «Забрать» или «Забрать ×2» за рекламу.
    /// После получения монеты летят в HUD, игра сразу сохраняется.
    /// </summary>
    public sealed class OfflineIncomePopup : MonoBehaviour
    {
        private const int CoinBurst = 10;
        private const float CoinSpread = 160f;

        [SerializeField] private GameObject _root = null;
        [SerializeField] private RectTransform _panel = null;
        [SerializeField] private TMP_Text _subtitleText = null;
        [SerializeField] private TMP_Text _amountText = null;
        [SerializeField] private TMP_Text _limitText = null;
        [SerializeField] private Button _claimButton = null;
        [SerializeField] private Button _claimDoubleButton = null;
        [SerializeField] private TMP_Text _claimDoubleText = null;

        private OfflineIncomeService _offline;
        private IPlatformService _platform;
        private SaveService _save;
        private TweenRunner _tweens;
        private CoinFlyEffect _coins;
        private Loc _loc;

        public void Bind(OfflineIncomeService offline, IPlatformService platform, SaveService save, TweenRunner tweens,
            CoinFlyEffect coins, Loc loc)
        {
            _offline = offline;
            _platform = platform;
            _save = save;
            _tweens = tweens;
            _coins = coins;
            _loc = loc;

            _claimButton.onClick.AddListener(OnClaim);
            _claimDoubleButton.onClick.AddListener(OnClaimDouble);
            _root.SetActive(false);
        }

        public void ShowIfPending()
        {
            if (_offline == null || !_offline.HasPending)
                return;

            OfflineIncome pending = _offline.Pending;
            _subtitleText.text = _loc.Format("offline.subtitle", DurationFormatter.Format(pending.Seconds, _loc));
            _amountText.text = "+" + BigNumberFormatter.Format(pending.Amount);
            _limitText.gameObject.SetActive(pending.IsCapped);
            if (pending.IsCapped)
                _limitText.text = _loc.Format("offline.limit", DurationFormatter.Format(pending.Seconds, _loc));
            _claimDoubleText.text = _loc.Format("offline.claim_ad", "×" + _offline.AdMultiplier.ToString("0.##", CultureInfo.InvariantCulture));

            SetButtonsInteractable(true);
            _root.SetActive(true);
            _tweens.Scale(_panel, Vector3.one * 0.6f, Vector3.one, 0.35f, Ease.OutBack);
        }

        private void OnClaim()
        {
            Claim(withAdBonus: false);
        }

        private void OnClaimDouble()
        {
            SetButtonsInteractable(false);
            _platform.ShowRewarded(rewarded =>
            {
                if (rewarded)
                    Claim(withAdBonus: true);
                else
                    SetButtonsInteractable(true);
            });
        }

        private void Claim(bool withAdBonus)
        {
            if (!_offline.TryClaim(withAdBonus))
                return;

            Vector3 origin = _amountText.rectTransform.position;
            for (int i = 0; i < CoinBurst; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-CoinSpread, CoinSpread), Random.Range(-CoinSpread, CoinSpread) * 0.5f, 0f);
                _coins.Fly(origin + offset * _amountText.canvas.scaleFactor);
            }

            _save?.SaveNow();
            _root.SetActive(false);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _claimButton.interactable = interactable;
            _claimDoubleButton.interactable = interactable;
        }
    }
}
