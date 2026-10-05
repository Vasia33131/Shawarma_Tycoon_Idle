using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Отклик кнопки: при нажатии сжимается, при отпускании отскакивает обратно; по клику — звук.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private SoundId _sound = SoundId.Tap;
        [Tooltip("Масштаб при нажатии. У объёмных кнопок (SoftButton) — почти 1: «проминание» делает сама кнопка.")]
        [SerializeField] private float _pressedScale = 0.94f;
        [SerializeField] private float _pressDuration = 0.06f;
        [SerializeField] private float _releaseDuration = 0.18f;

        private Button _button;
        private RectTransform _rect;
        private TweenRunner _tweens;
        private AudioService _audio;
        private bool _pressed;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _rect = (RectTransform)transform;
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _pressed = false;
            if (_tweens != null)
                _tweens.Kill(_rect, TweenProperty.Scale);
            _rect.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.IsInteractable() || !ResolveTweens())
                return;

            _pressed = true;
            _tweens.Scale(_rect, _rect.localScale, Vector3.one * _pressedScale, _pressDuration, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed || !ResolveTweens())
                return;

            _pressed = false;
            _tweens.Scale(_rect, _rect.localScale, Vector3.one, _releaseDuration, Ease.OutBack);
        }

        private void OnClick()
        {
            if (_audio != null || Services.TryGet(out _audio))
                _audio.Play(_sound);
        }

        private bool ResolveTweens()
        {
            return _tweens != null || Services.TryGet(out _tweens);
        }
    }
}
