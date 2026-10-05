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
    /// Решение принимается по состоянию кнопки в момент нажатия: Button обрабатывает клик раньше нас,
    /// и после удачной покупки за монеты кнопка уже выключена — окно при этом открываться не должно.
    /// </summary>
    public sealed class BillOfferTrigger : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] private Button _button = null;
        [Tooltip("Значок банкноты на кнопке — виден, когда можно купить за купюры.")]
        [SerializeField] private GameObject _hint = null;

        private bool _offer;
        private bool _offerOnPress;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            _offerOnPress = _offer && !_button.interactable;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Выключена и при нажатии, и сейчас: иначе клик уже обработал Button (покупка за монеты).
            bool offer = _offerOnPress && !_button.interactable;
            _offerOnPress = false;
            if (offer)
                Clicked?.Invoke();
        }
    }
}
