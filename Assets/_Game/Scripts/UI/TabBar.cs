using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Нижние вкладки: кнопка i показывает панель i и подсвечивается.
    /// </summary>
    public sealed class TabBar : MonoBehaviour
    {
        [SerializeField] private Button[] _buttons = new Button[0];
        [SerializeField] private GameObject[] _panels = new GameObject[0];
        [SerializeField] private Color _activeColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField] private Color _inactiveColor = new Color(0.45f, 0.25f, 0.12f);

        public int SelectedIndex { get; private set; } = -1;

        private void Awake()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                _buttons[i].onClick.AddListener(() => Select(index));
            }

            Select(0);
        }

        public void Select(int index)
        {
            if (index == SelectedIndex || index < 0 || index >= _panels.Length)
                return;

            SelectedIndex = index;
            for (int i = 0; i < _panels.Length; i++)
                _panels[i].SetActive(i == index);

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i].targetGraphic != null)
                    _buttons[i].targetGraphic.color = i == index ? _activeColor : _inactiveColor;
            }
        }
    }
}
