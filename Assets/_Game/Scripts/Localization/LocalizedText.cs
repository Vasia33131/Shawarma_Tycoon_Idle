using ShaurmaTycoon.Core;
using TMPro;
using UnityEngine;

namespace ShaurmaTycoon.Localization
{
    /// <summary>
    /// Статичный текст по ключу локализации. Обновляется при смене языка.
    /// Тексты с подстановкой чисел задают их вью через Loc.Format.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key = string.Empty;

        private TMP_Text _text;
        private Loc _loc;

        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                Apply();
            }
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (_loc == null && !Services.TryGet(out _loc))
                return;

            _loc.LanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            if (_loc != null)
                _loc.LanguageChanged -= Apply;
        }

        private void Apply()
        {
            if (_loc != null && _text != null && !string.IsNullOrEmpty(_key))
                _text.text = _loc.Get(_key);
        }
    }
}
