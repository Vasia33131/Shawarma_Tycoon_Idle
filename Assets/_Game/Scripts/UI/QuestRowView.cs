using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Retention;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>Строка задания дня: что сделать, прогресс «12/50» с полосой, награда, кнопка «Забрать».</summary>
    public sealed class QuestRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _descriptionText = null;
        [SerializeField] private TMP_Text _progressText = null;
        [Tooltip("Заливка полосы: ширина задаётся anchorMax.x.")]
        [SerializeField] private RectTransform _progressFill = null;
        [SerializeField] private RewardIconView _reward = null;
        [SerializeField] private Button _button = null;
        [SerializeField] private TMP_Text _buttonText = null;

        private readonly NumberText _progress = new NumberText();

        public Button Button => _button;

        public void Set(string description, int progress, int target, RewardGrant reward, string buttonLabel, bool canClaim)
        {
            _descriptionText.text = description;
            _progressText.SetNumberText(_progress.Clear().AppendInteger(progress).Append('/').AppendInteger(target));
            _progressFill.anchorMax = new Vector2(target > 0 ? Mathf.Clamp01((float)progress / target) : 0f, 1f);
            _reward.Set(reward, string.Empty);
            _buttonText.text = buttonLabel;
            _button.interactable = canClaim;
        }
    }
}
