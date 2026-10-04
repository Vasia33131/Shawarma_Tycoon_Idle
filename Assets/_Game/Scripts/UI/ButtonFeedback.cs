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
        private const float PressedScale = 0.92f;
        private const float PressDuration = 0.08f;
        private const float ReleaseDuration = 0.3f;

        [SerializeField] private SoundId _sound = SoundId.Tap;

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
            _tweens.Scale(_rect, _rect.localScale, Vector3.one * PressedScale, PressDuration, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed || !ResolveTweens())
                return;

            _pressed = false;
            _tweens.Scale(_rect, _rect.localScale, Vector3.one, ReleaseDuration, Ease.OutBack);
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
