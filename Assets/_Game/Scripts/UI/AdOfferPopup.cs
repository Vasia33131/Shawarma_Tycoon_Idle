using System;
using ShaurmaTycoon.Core.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Предложение посмотреть рекламу за награду (буст ×2, подарок-ящик): rewarded запускается только по кнопке игрока.
    /// </summary>
    public sealed class AdOfferPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root = null;
        [SerializeField] private RectTransform _panel = null;
        private PopupAnimator _animator;
        private PopupAnimator Animator => _animator != null ? _animator : _animator = PopupAnimator.For(_root, _panel);
        [SerializeField] private TMP_Text _titleText = null;
        [SerializeField] private TMP_Text _bodyText = null;
        [SerializeField] private Button _confirmButton = null;
        [SerializeField] private TMP_Text _confirmText = null;
        [SerializeField] private Button _cancelButton = null;

        private Action _onConfirm;

        public bool IsOpen => Animator.IsOpen;

        public void Bind(TweenRunner tweens)
        {
            _confirmButton.onClick.AddListener(OnConfirm);
            _cancelButton.onClick.AddListener(Hide);
            _root.SetActive(false);
        }

        /// <param name="canConfirm">false — кнопка неактивна (например, запас буста полон).</param>
        public void Show(string title, string body, string confirmLabel, bool canConfirm, Action onConfirm)
        {
            _titleText.text = title;
            _bodyText.text = body;
            _confirmText.text = confirmLabel;
            _confirmButton.interactable = canConfirm;
            _onConfirm = onConfirm;

            if (Animator.IsOpen)
                return;
            Animator.Show();
        }

        public void Hide()
        {
            _onConfirm = null;
            Animator.Hide();
        }

        private void OnConfirm()
        {
            Action confirm = _onConfirm;
            Hide();
            confirm?.Invoke();
        }
    }
}
