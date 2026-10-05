using System.Globalization;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Retention;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Окно календаря (GDD §9): 7 клеток, сегодняшняя подсвечена; «Забрать» или «Следующая награда через …».
    /// Открывается само при входе, если награда доступна, и по кнопке-календарю на экране.
    /// </summary>
    public sealed class DailyRewardPopup : MonoBehaviour
    {
        private const int CoinBurst = 8;
        private const float CoinSpread = 140f;

        [SerializeField] private GameObject _root = null;
        [SerializeField] private RectTransform _panel = null;
        private PopupAnimator _animator;
        private PopupAnimator Animator => _animator != null ? _animator : _animator = PopupAnimator.For(_root, _panel);
        [SerializeField] private DailyRewardTileView[] _tiles = new DailyRewardTileView[0];
        [SerializeField] private TMP_Text _statusText = null;
        [SerializeField] private Button _claimButton = null;
        [SerializeField] private Button _closeButton = null;

        private DailyRewardService _daily;
        private TweenRunner _tweens;
        private CoinFlyEffect _coins;
        private Loc _loc;

        public bool IsOpen => Animator.IsOpen;

        public void Bind(DailyRewardService daily, TweenRunner tweens, CoinFlyEffect coins, Loc loc)
        {
            _daily = daily;
            _tweens = tweens;
            _coins = coins;
            _loc = loc;
            _claimButton.onClick.AddListener(OnClaim);
            _closeButton.onClick.AddListener(Hide);
            _root.SetActive(false);
        }

        public void Show()
        {
            Refresh();
            if (Animator.IsOpen)
                return;
            Animator.Show();
        }

        public void Hide()
        {
            Animator.Hide();
        }

        private void OnClaim()
        {
            int day = _daily.Calendar.NextDay;
            if (!_daily.TryClaim(out RewardGrant grant))
            {
                Refresh();
                return;
            }

            if (day >= 0 && day < _tiles.Length && _tiles[day] != null)
            {
                RectTransform tile = (RectTransform)_tiles[day].transform;
                _tweens.Punch(tile, 0.25f, 0.3f);
                if (grant.Kind == RewardKind.Coins)
                    FlyCoins(tile.position);
            }
            Refresh();
        }

        private void FlyCoins(Vector3 origin)
        {
            float scale = _panel.GetComponentInParent<Canvas>().scaleFactor;
            for (int i = 0; i < CoinBurst; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-CoinSpread, CoinSpread), Random.Range(-CoinSpread, CoinSpread) * 0.5f, 0f);
                _coins.Fly(origin + offset * scale);
            }
        }

        private void Refresh()
        {
            bool canClaim = _daily.CanClaim;
            int next = _daily.Calendar.NextDay;
            string skin = _loc.Get("retention.skin");
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i] == null)
                    continue;

                // Пока сегодня не забрано, «сегодня» — следующий день; после — он уже получен, а следующий ждёт завтра.
                DailyRewardTileView.State state = i < next ? DailyRewardTileView.State.Claimed
                    : i == next && canClaim ? DailyRewardTileView.State.Today
                    : DailyRewardTileView.State.Future;
                if (!canClaim && next == 0)
                    state = DailyRewardTileView.State.Claimed;
                string label = _loc.Format("retention.day", (i + 1).ToString(CultureInfo.InvariantCulture));
                _tiles[i].Set(label, _daily.Preview(i), skin, state);
            }

            _claimButton.interactable = canClaim;
            _statusText.text = canClaim
                ? _loc.Get("retention.daily_ready")
                : _loc.Format("retention.daily_next", DurationFormatter.Format(_daily.Calendar.TimeUntilNext(_daily.Now).TotalSeconds, _loc));
        }
    }
}
