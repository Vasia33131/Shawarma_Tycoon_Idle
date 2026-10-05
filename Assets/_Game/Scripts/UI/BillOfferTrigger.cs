using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Висит на кнопке покупки за монеты. Когда монет не хватает (кнопка выключена), показывает значок банкноты,
    /// а нажатие на выключенную кнопку предлагает купить за купюры (Clicked → BillOfferPopup).
    /// Включённую кнопку не трогает: обычная покупка за монеты идёт как раньше.
    /// </summary>
    public sealed class BillOfferTrigger : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button _button = null;
        [Tooltip("Значок банкноты на кнопке — виден, когда можно купить за купюры.")]
        [SerializeField] private GameObject _hint = null;

        private bool _offer;

        /// <summary>Нажата выключенная кнопка, для которой есть предложение за купюры.</summary>
        public event Action Clicked;

        /// <param name="interactable">Покупка за монеты доступна.</param>
        /// <param name="offer">Предлагать купюры (обычно: монет не хватает, но купить в принципе можно).</param>
        public void Set(bool interactable, bool offer)
        {
            _button.interactable = interactable;
            _offer = offer && !interactable;
            if (_hint != null && _hint.activeSelf != _offer)
                _hint.SetActive(_offer);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_offer && !_button.interactable)
                Clicked?.Invoke();
        }
    }
}
