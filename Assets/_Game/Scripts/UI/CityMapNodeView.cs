using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    public enum CityMapNodeState
    {
        Done,
        Current,
        Next,
        Locked,
    }

    /// <summary>
    /// Город на карте. Есть миниатюра (CityConfig.MapIcon) — картинка с бейджем номера (у открытых — галочка),
    /// замком у закрытых и свечением у следующего; нет — прежний цветной кружок с номером.
    /// Текущий город отмечает булавка «Вы здесь», подпись — на светлой ленточке, ширина по имени.
    /// </summary>
    public sealed class CityMapNodeView : MonoBehaviour
    {
        private const float CurrentScale = 1.15f;
        private const float CircleSelectionSize = 128f;
        private const float IconSelectionSize = 156f;

        [SerializeField] private Button _button = null;
        [SerializeField] private Image _circle = null;
        [SerializeField] private TMP_Text _numberText = null;
        [SerializeField] private TMP_Text _nameText = null;
        [SerializeField] private GameObject _selection = null;
        [SerializeField] private GameObject _herePin = null;
        [SerializeField] private RectTransform _body = null;
        [SerializeField] private PulseEffect _pulse = null;

        [Header("Miniature (V5b)")]
        [SerializeField] private Image _icon = null;
        [SerializeField] private GameObject _badge = null;
        [SerializeField] private Image _badgeCircle = null;
        [SerializeField] private TMP_Text _badgeNumber = null;
        [SerializeField] private GameObject _badgeCheck = null;
        [SerializeField] private GameObject _lock = null;
        [SerializeField] private GameObject _glow = null;
        [SerializeField] private RectTransform _namePill = null;
        [SerializeField] private float _nameFontSize = 28f;
        [SerializeField] private float _nameMinFontSize = 22f;
        [SerializeField] private float _nameMaxWidth = 210f;
        [SerializeField] private float _namePadding = 28f;

        [Header("Colors")]
        [SerializeField] private Color _doneColor = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color _nextColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField] private Color _lockedColor = new Color(0.70f, 0.65f, 0.60f);
        [SerializeField] private Color _lockedIconTint = new Color(0.62f, 0.58f, 0.70f, 0.75f);
        [SerializeField] private Color _nameColor = new Color(0.36f, 0.18f, 0.07f);
        [SerializeField] private Color _lockedNameColor = new Color(0.45f, 0.35f, 0.28f, 0.7f);

        public Button Button => _button;

        /// <param name="currentColor">Цвет текущего города — фирменный цвет его ларька.</param>
        /// <param name="icon">Миниатюра города; null — кружок с номером.</param>
        public void Setup(int number, CityMapNodeState state, Color currentColor, Sprite icon)
        {
            Color stateColor;
            switch (state)
            {
                case CityMapNodeState.Done:
                    stateColor = _doneColor;
                    break;
                case CityMapNodeState.Current:
                    stateColor = currentColor;
                    break;
                case CityMapNodeState.Next:
                    stateColor = _nextColor;
                    break;
                default:
                    stateColor = _lockedColor;
                    break;
            }

            bool art = icon != null && _icon != null;
            bool locked = state == CityMapNodeState.Locked;
            _circle.gameObject.SetActive(!art);
            _numberText.text = number.ToString();
            _circle.color = stateColor;
            if (_icon != null)
            {
                _icon.gameObject.SetActive(art);
                _icon.sprite = icon;
                _icon.color = locked ? _lockedIconTint : Color.white;
            }
            if (_badge != null)
            {
                _badge.SetActive(art);
                _badgeCircle.color = stateColor;
                _badgeNumber.text = number.ToString();
                _badgeNumber.gameObject.SetActive(state != CityMapNodeState.Done);
                _badgeCheck.SetActive(state == CityMapNodeState.Done);
            }
            if (_lock != null)
                _lock.SetActive(art && locked);
            if (_glow != null)
                _glow.SetActive(state == CityMapNodeState.Next);

            float selectionSize = art ? IconSelectionSize : CircleSelectionSize;
            ((RectTransform)_selection.transform).sizeDelta = new Vector2(selectionSize, selectionSize);

            bool current = state == CityMapNodeState.Current;
            _nameText.color = locked ? _lockedNameColor : _nameColor;
            _herePin.SetActive(current);
            _body.localScale = Vector3.one * (current ? CurrentScale : 1f);
            if (_pulse != null)
                _pulse.Active = state == CityMapNodeState.Next;
        }

        /// <summary>Имя на ленточке: длинное уменьшается до _nameMinFontSize, ленточка — по ширине текста.</summary>
        public void SetName(string cityName)
        {
            _nameText.text = cityName;
            if (_namePill == null)
                return;

            _nameText.fontSize = _nameFontSize;
            float width = _nameText.GetPreferredValues(cityName).x;
            if (width > _nameMaxWidth)
            {
                _nameText.fontSize = Mathf.Max(_nameMinFontSize, _nameFontSize * _nameMaxWidth / width);
                width = Mathf.Min(_nameMaxWidth, _nameText.GetPreferredValues(cityName).x);
            }
            _namePill.sizeDelta = new Vector2(width + _namePadding, _namePill.sizeDelta.y);
        }

        public void SetSelected(bool selected)
        {
            _selection.SetActive(selected);
        }
    }
}
