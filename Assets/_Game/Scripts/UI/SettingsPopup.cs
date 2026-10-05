using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Окно настроек (шестерёнка в HUD): переключатели звуков и музыки. Каждое нажатие сразу меняет и сохраняет настройку.
    /// </summary>
    public sealed class SettingsPopup : MonoBehaviour, ISceneBinding
    {
        [SerializeField] private GameObject _root = null;
        [SerializeField] private RectTransform _panel = null;
        private PopupAnimator _animator;
        private PopupAnimator Animator => _animator != null ? _animator : _animator = PopupAnimator.For(_root, _panel);
        [Tooltip("Кнопка в HUD, открывающая окно.")]
        [SerializeField] private Button _openButton = null;
        [SerializeField] private Button _closeButton = null;

        [Header("Toggles")]
        [SerializeField] private Button _soundButton = null;
        [SerializeField] private TMP_Text _soundText = null;
        [SerializeField] private Button _musicButton = null;
        [SerializeField] private TMP_Text _musicText = null;
        [Tooltip("«Эффекты фона» (V5). Пусто — строки нет.")]
        [SerializeField] private Button _effectsButton = null;
        [SerializeField] private TMP_Text _effectsText = null;
        [SerializeField] private Color _onColor = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color _offColor = new Color(0.62f, 0.58f, 0.55f);

        private GameSettings _settings;
        private Loc _loc;

        public void Bind(GameSettings settings, TweenRunner tweens, Loc loc)
        {
            SceneBindings.Track(this);
            _settings = settings;
            _loc = loc;

            _openButton.onClick.AddListener(Show);
            _closeButton.onClick.AddListener(Hide);
            _soundButton.onClick.AddListener(OnSoundClicked);
            _musicButton.onClick.AddListener(OnMusicClicked);
            if (_effectsButton != null)
                _effectsButton.onClick.AddListener(OnEffectsClicked);
            _settings.Changed += Refresh;
            _loc.LanguageChanged += Refresh;

            _root.SetActive(false);
            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_settings == null)
                return;
            _settings.Changed -= Refresh;
            _loc.LanguageChanged -= Refresh;
        }

        private void Show()
        {
            Refresh();
            Animator.Show();
        }

        private void Hide()
        {
            Animator.Hide();
        }

        private void OnSoundClicked()
        {
            _settings.SetSound(!_settings.SoundOn);
        }

        private void OnMusicClicked()
        {
            _settings.SetMusic(!_settings.MusicOn);
        }

        private void OnEffectsClicked()
        {
            _settings.SetEffects(!_settings.EffectsOn);
        }

        private void Refresh()
        {
            ApplyToggle(_soundButton, _soundText, _settings.SoundOn);
            ApplyToggle(_musicButton, _musicText, _settings.MusicOn);
            if (_effectsButton != null)
                ApplyToggle(_effectsButton, _effectsText, _settings.EffectsOn);
        }

        private void ApplyToggle(Button button, TMP_Text label, bool on)
        {
            label.text = _loc.Get(on ? "settings.on" : "settings.off");
            if (button.targetGraphic != null)
                button.targetGraphic.color = on ? _onColor : _offColor;
        }
    }
}
