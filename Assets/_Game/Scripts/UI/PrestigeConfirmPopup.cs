using System.Globalization;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Prestige;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Подтверждение перехода в новый город: что сбросится, что останется, сколько репутации и какой станет бонус.
    /// </summary>
    public sealed class PrestigeConfirmPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root = null;
        [SerializeField] private RectTransform _panel = null;
        private PopupAnimator _animator;
        private PopupAnimator Animator => _animator != null ? _animator : _animator = PopupAnimator.For(_root, _panel);
        [SerializeField] private TMP_Text _titleText = null;
        [SerializeField] private TMP_Text _gainText = null;
        [SerializeField] private Button _confirmButton = null;
        [SerializeField] private Button _cancelButton = null;

        private PrestigeController _controller;

        public void Bind(TweenRunner tweens)
        {
            _confirmButton.onClick.AddListener(OnConfirm);
            _cancelButton.onClick.AddListener(Hide);
            _root.SetActive(false);
        }

        public void Show(PrestigeController controller, string targetCityName, bool sameCity, Loc loc)
        {
            _controller = controller;
            _titleText.text = loc.Format(sameCity ? "prestige.confirm_title_same" : "prestige.confirm_title", targetCityName);
            double bonusPercent = (controller.IncomeMultiplierAfterPrestige - 1) * 100;
            _gainText.text = loc.Format("prestige.confirm_gain",
                controller.ForecastGain.ToString(CultureInfo.InvariantCulture),
                bonusPercent.ToString("0", CultureInfo.InvariantCulture));

            _confirmButton.interactable = controller.CanPrestige;
            Animator.Show();
        }

        private void OnConfirm()
        {
            _confirmButton.interactable = false;
            if (_controller == null || !_controller.TryPrestige())
                Hide();
        }

        private void Hide()
        {
            Animator.Hide();
        }
    }
}
