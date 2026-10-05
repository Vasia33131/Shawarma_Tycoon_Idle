using ShaurmaTycoon.Retention;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>Клетка календаря: «День N», награда, состояние (получено / сегодня / впереди).</summary>
    public sealed class DailyRewardTileView : MonoBehaviour
    {
        public enum State
        {
            Claimed,
            Today,
            Future,
        }

        [SerializeField] private Image _background = null;
        [SerializeField] private TMP_Text _dayText = null;
        [SerializeField] private RewardIconView _reward = null;
        [SerializeField] private GameObject _check = null;
        [SerializeField] private Color _claimedColor = new Color(0.80f, 0.76f, 0.70f);
        [SerializeField] private Color _todayColor = new Color(1f, 0.80f, 0.20f);
        [SerializeField] private Color _futureColor = new Color(1f, 0.99f, 0.96f);

        public void Set(string dayLabel, RewardGrant reward, string skinLabel, State state)
        {
            _dayText.text = dayLabel;
            _reward.Set(reward, skinLabel);
            _check.SetActive(state == State.Claimed);
            _background.color = state == State.Claimed ? _claimedColor : state == State.Today ? _todayColor : _futureColor;
        }
    }
}
