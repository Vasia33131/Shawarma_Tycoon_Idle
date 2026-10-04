using ShaurmaTycoon.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Строка вкладки «Магазин»: иконка, название, описание, метка «Выгодно!» и кнопка цены.
    /// Сама ничего не решает — тексты и доступность выставляет ShopPanel.
    /// </summary>
    public sealed class BillShopRowView : MonoBehaviour
    {
        [SerializeField] private Image _icon = null;
        [SerializeField] private Image _iconMark = null;
        [SerializeField] private TMP_Text _nameText = null;
        [SerializeField] private TMP_Text _descriptionText = null;
        [SerializeField] private GameObject _badge = null;
        [SerializeField] private Button _button = null;
        [SerializeField] private TMP_Text _buttonText = null;

        public Button Button => _button;

        public void SetLook(Color icon, Color mark, Color button)
        {
            _icon.color = icon;
            _iconMark.color = mark;
            if (_button.targetGraphic != null)
                _button.targetGraphic.color = button;
        }

        public void SetName(string text)
        {
            _nameText.text = text;
        }

        public void SetDescription(string text)
        {
            _descriptionText.text = text;
        }

        /// <summary>Описание, которое меняется по таймеру, — без аллокаций.</summary>
        public void SetDescription(NumberText text)
        {
            _descriptionText.SetNumberText(text);
        }

        public void SetBadge(bool visible)
        {
            _badge.SetActive(visible);
        }

        public void SetButton(string text, bool interactable)
        {
            _buttonText.text = text;
            _button.interactable = interactable;
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }
    }
}
