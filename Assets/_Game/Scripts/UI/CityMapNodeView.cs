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

    /// <summary>Город на карте: кружок с номером, название, метка «Вы здесь» и рамка выбора.</summary>
    public sealed class CityMapNodeView : MonoBehaviour
    {
        private const float CurrentScale = 1.15f;

        [SerializeField] private Button _button = null;
        [SerializeField] private Image _circle = null;
        [SerializeField] private TMP_Text _numberText = null;
        [SerializeField] private TMP_Text _nameText = null;
        [SerializeField] private GameObject _selection = null;
        [SerializeField] private GameObject _herePin = null;
        [SerializeField] private RectTransform _body = null;
        [SerializeField] private PulseEffect _pulse = null;

        [Header("Colors")]
        [SerializeField] private Color _doneColor = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color _nextColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField] private Color _lockedColor = new Color(0.70f, 0.65f, 0.60f);
        [SerializeField] private Color _nameColor = new Color(0.36f, 0.18f, 0.07f);
        [SerializeField] private Color _lockedNameColor = new Color(0.45f, 0.35f, 0.28f, 0.7f);

        public Button Button => _button;

        /// <param name="currentColor">Цвет текущего города — фирменный цвет его ларька.</param>
        public void Setup(int number, CityMapNodeState state, Color currentColor)
        {
            _numberText.text = number.ToString();

            switch (state)
            {
                case CityMapNodeState.Done:
                    _circle.color = _doneColor;
                    break;
                case CityMapNodeState.Current:
                    _circle.color = currentColor;
                    break;
                case CityMapNodeState.Next:
                    _circle.color = _nextColor;
                    break;
                default:
                    _circle.color = _lockedColor;
                    break;
            }

            bool current = state == CityMapNodeState.Current;
            _nameText.color = state == CityMapNodeState.Locked ? _lockedNameColor : _nameColor;
            _herePin.SetActive(current);
            _body.localScale = Vector3.one * (current ? CurrentScale : 1f);
            if (_pulse != null)
                _pulse.Active = state == CityMapNodeState.Next;
        }

        public void SetName(string cityName)
        {
            _nameText.text = cityName;
        }

        public void SetSelected(bool selected)
        {
            _selection.SetActive(selected);
        }
    }
}
