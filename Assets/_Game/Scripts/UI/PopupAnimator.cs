using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Появление и скрытие попапа: затемнение плавно проявляется, панель всплывает снизу из чуть уменьшенного размера
    /// с лёгким отскоком; скрытие вдвое быстрее, после него корень выключается. Пока идёт анимация, кнопки окна не нажимаются
    /// (blocksRaycasts, а не interactable — иначе кнопки на миг стали бы «недоступными» на вид).
    /// Висит на корне попапа (корень = затемнение). IsOpen становится false сразу при вызове Hide.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopupAnimator : MonoBehaviour, ITweenListener
    {
        [SerializeField] private RectTransform _panel = null;
        [SerializeField] private Graphic _overlay = null;
        [Tooltip("CanvasGroup панели: на время анимации перестаёт ловить нажатия (затемнение при этом закрывает игру).")]
        [SerializeField] private CanvasGroup _panelGroup = null;
        [SerializeField, Range(0f, 1f)] private float _overlayAlpha = 0.6f;
        [SerializeField] private float _showDuration = 0.25f;
        [SerializeField] private float _hideDuration = 0.12f;
        [SerializeField] private float _overlayFade = 0.2f;
        [SerializeField] private float _startScale = 0.92f;
        [SerializeField] private float _startOffset = 40f;

        private TweenRunner _tweens;
        private Vector2 _panelBase;
        private bool _baseKnown;
        private bool _open;
        private int _generation;

        public bool IsOpen => _open && gameObject.activeSelf;

        /// <summary>Какой-то попап начал появляться (из скрытого состояния). Режим любования по нему закрывается.</summary>
        public static event System.Action AnyShown;

        /// <summary>Аниматор попапа; если сцена собрана без него — добавляется с настройками по умолчанию.</summary>
        public static PopupAnimator For(GameObject root, RectTransform panel)
        {
            if (root.TryGetComponent(out PopupAnimator animator))
                return animator;

            animator = root.AddComponent<PopupAnimator>();
            animator._panel = panel;
            animator._overlay = root.GetComponent<Graphic>();
            if (animator._overlay != null)
                animator._overlayAlpha = animator._overlay.color.a;
            return animator;
        }

        public void Show()
        {
            bool wasVisible = gameObject.activeSelf;
            if (_open && wasVisible)
                return;

            _open = true;
            _generation++;
            gameObject.SetActive(true);
            RememberBase();
            if (!wasVisible)
            {
                if (Services.TryGet(out AudioService audio))
                    audio.Play(SoundId.PopupOpen);
                AnyShown?.Invoke();
            }

            if (!ResolveTweens())
            {
                Snap(visible: true);
                return;
            }

            SetInteractable(false);
            // Повторный Show во время скрытия разворачивает анимацию с текущего положения.
            Vector3 fromScale = wasVisible ? _panel.localScale : Vector3.one * _startScale;
            Vector2 fromPosition = wasVisible ? _panel.anchoredPosition : _panelBase + new Vector2(0f, -_startOffset);
            float fromAlpha = wasVisible && _overlay != null ? _overlay.color.a : 0f;

            _tweens.Scale(_panel, fromScale, Vector3.one, _showDuration, Ease.OutBack, listener: this, tag: _generation);
            _tweens.Move(_panel, fromPosition, _panelBase, _showDuration, Ease.OutQuad);
            if (_overlay != null)
                _tweens.Fade(_overlay, fromAlpha, _overlayAlpha, _overlayFade, Ease.OutQuad);
        }

        public void Hide()
        {
            if (!_open && !gameObject.activeSelf)
                return;

            _open = false;
            _generation++;
            if (!gameObject.activeSelf)
                return;

            RememberBase();
            if (!ResolveTweens())
            {
                HideInstant();
                return;
            }

            SetInteractable(false);
            _tweens.Scale(_panel, _panel.localScale, Vector3.one * _startScale, _hideDuration, Ease.InQuad, listener: this, tag: _generation);
            _tweens.Move(_panel, _panel.anchoredPosition, _panelBase + new Vector2(0f, -_startOffset * 0.5f), _hideDuration, Ease.InQuad);
            if (_overlay != null)
                _tweens.Fade(_overlay, _overlay.color.a, 0f, _hideDuration, Ease.OutQuad);
        }

        /// <summary>Закрыть без анимации (начальное состояние сцены).</summary>
        public void HideInstant()
        {
            _open = false;
            _generation++;
            if (_baseKnown)
                Snap(visible: false);
            gameObject.SetActive(false);
        }

        public void OnTweenCompleted(int tag)
        {
            // Устаревший колбэк: после него уже вызвали Show/Hide.
            if (tag != _generation)
                return;

            if (_open)
            {
                SetInteractable(true);
                return;
            }

            Snap(visible: false);
            gameObject.SetActive(false);
        }

        private void Snap(bool visible)
        {
            _panel.localScale = Vector3.one;
            _panel.anchoredPosition = _panelBase;
            if (_overlay != null)
            {
                Color color = _overlay.color;
                color.a = visible ? _overlayAlpha : 0f;
                _overlay.color = color;
            }
            SetInteractable(true);
        }

        private void RememberBase()
        {
            if (_baseKnown)
                return;
            _baseKnown = true;
            _panelBase = _panel.anchoredPosition;
        }

        private void SetInteractable(bool interactable)
        {
            if (_panelGroup != null)
                _panelGroup.blocksRaycasts = interactable;
        }

        private bool ResolveTweens()
        {
            return _tweens != null || Services.TryGet(out _tweens);
        }
    }
}
