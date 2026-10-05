using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Retention;
using TMPro;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Окно «Задания дня» (GDD §9): до трёх заданий с прогрессом и наградой, внизу — когда будут новые.
    /// Обновляется при изменении заданий; время до новых — раз в секунду, пока окно открыто.
    /// </summary>
    public sealed class QuestsPopup : MonoBehaviour, ITickable
    {
        private const int CoinBurst = 8;
        private const float CoinSpread = 120f;

        private static readonly string[] DescriptionKeys =
        {
            "quest.buy_levels", "quest.hire_manager", "quest.buy_upgrade", "quest.watch_boost", "quest.open_gift",
        };

        [SerializeField] private GameObject _root = null;
        [SerializeField] private RectTransform _panel = null;
        private PopupAnimator _animator;
        private PopupAnimator Animator => _animator != null ? _animator : _animator = PopupAnimator.For(_root, _panel);
        [SerializeField] private QuestRowView[] _rows = new QuestRowView[0];
        [SerializeField] private TMP_Text _footerText = null;
        [SerializeField] private UnityEngine.UI.Button _closeButton = null;

        private DailyQuestService _quests;
        private TweenRunner _tweens;
        private CoinFlyEffect _coins;
        private Loc _loc;
        private float _timer;

        public bool IsOpen => Animator.IsOpen;

        public void Bind(DailyQuestService quests, TweenRunner tweens, CoinFlyEffect coins, Loc loc)
        {
            _quests = quests;
            _tweens = tweens;
            _coins = coins;
            _loc = loc;
            _closeButton.onClick.AddListener(Hide);
            for (int i = 0; i < _rows.Length; i++)
            {
                int index = i;
                _rows[i].Button.onClick.AddListener(() => OnClaim(index));
            }
            _quests.Quests.Changed += OnChanged;
            _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_quests != null)
                _quests.Quests.Changed -= OnChanged;
        }

        public void Show()
        {
            _quests.Refresh();
            Refresh();
            if (Animator.IsOpen)
                return;
            Animator.Show();
        }

        public void Hide()
        {
            Animator.Hide();
        }

        public void Tick(float deltaTime)
        {
            if (!_root.activeSelf)
                return;

            _timer += deltaTime;
            if (_timer < 1f)
                return;
            _timer = 0f;
            RefreshFooter();
        }

        private void OnChanged()
        {
            if (_root.activeSelf)
                Refresh();
        }

        private void OnClaim(int index)
        {
            if (!_quests.TryClaim(index, out RewardGrant grant))
                return;

            RectTransform row = (RectTransform)_rows[index].transform;
            _tweens.Punch(row, 0.08f, 0.3f);
            if (grant.Kind != RewardKind.Coins)
                return;

            float scale = _panel.GetComponentInParent<Canvas>().scaleFactor;
            Vector3 origin = _rows[index].Button.transform.position;
            for (int i = 0; i < CoinBurst; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-CoinSpread, CoinSpread), Random.Range(-CoinSpread, CoinSpread) * 0.5f, 0f);
                _coins.Fly(origin + offset * scale);
            }
        }

        private void Refresh()
        {
            var active = _quests.Quests.Active;
            for (int i = 0; i < _rows.Length; i++)
            {
                bool visible = i < active.Count;
                _rows[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                DailyQuests.Quest quest = active[i];
                int type = (int)quest.Type;
                string description = _loc.Get(type >= 0 && type < DescriptionKeys.Length ? DescriptionKeys[type] : "quest.buy_levels");
                string button = _loc.Get(quest.Claimed ? "retention.claimed" : "retention.claim");
                _rows[i].Set(description, quest.Progress, quest.Target, _quests.Preview(i), button, quest.CanClaim);
            }
            RefreshFooter();
        }

        private void RefreshFooter()
        {
            double seconds = DailyQuests.TimeUntilNewDay(_quests.Now).TotalSeconds;
            _footerText.text = _loc.Format("retention.quests_new", DurationFormatter.Format(seconds, _loc));
        }
    }
}
