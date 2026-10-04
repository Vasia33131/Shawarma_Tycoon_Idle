using System;
using ShaurmaTycoon.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Кнопка «Купить: ×1 / ×10 / Макс» — по нажатию переключает режим по кругу.
    /// </summary>
    public sealed class BuyModeButton : MonoBehaviour
    {
        [SerializeField] private Button _button = null;
        [SerializeField] private TMP_Text _label = null;

        private Loc _loc;

        public event Action<BuyMode> ModeChanged;

        public BuyMode Mode { get; private set; } = BuyMode.One;

        public void Bind(Loc loc)
        {
            _loc = loc;
            _loc.LanguageChanged += Refresh;
            _button.onClick.AddListener(NextMode);
            Refresh();
        }

        private void OnDestroy()
        {
            if (_loc != null)
                _loc.LanguageChanged -= Refresh;
        }

        private void NextMode()
        {
            Mode = Mode == BuyMode.One ? BuyMode.Ten : Mode == BuyMode.Ten ? BuyMode.Max : BuyMode.One;
            Refresh();
            ModeChanged?.Invoke(Mode);
        }

        private void Refresh()
        {
            string mode = Mode == BuyMode.One ? "×1" : Mode == BuyMode.Ten ? "×10" : _loc.Get("buy_mode.max");
            _label.text = _loc.Format("buy_mode.label", mode);
        }
    }
}
