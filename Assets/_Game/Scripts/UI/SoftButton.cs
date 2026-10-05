using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// «Объёмная» кнопка: лицо и тёмный нижний бортик — один спрайт (button_fill), цвет по-прежнему задаётся
    /// через targetGraphic.color, поэтому старый код перекраски работает. При нажатии спрайт меняется на
    /// button_fill_pressed (бортик тоньше), а содержимое (подписи, значки) опускается на PressOffset — лицо «проминается».
    /// Недоступная кнопка закрывается бежевым слоем DisabledOverlay, подписи становятся приглушёнными.
    /// </summary>
    public sealed class SoftButton : Button
    {
        [SerializeField] private Sprite _normalSprite = null;
        [SerializeField] private Sprite _pressedSprite = null;
        [Tooltip("Дети лица, которые не опускаются при нажатии (обводка, слой недоступности, свечение).")]
        [SerializeField] private RectTransform[] _decor = new RectTransform[0];
        [SerializeField] private GameObject _disabledOverlay = null;
        [SerializeField] private Color _disabledTextColor = new Color32(0x8A, 0x6B, 0x57, 0xFF);
        [SerializeField] private float _pressOffset = 6f;
        [SerializeField] private float _pressDuration = 0.06f;
        [SerializeField] private float _releaseDuration = 0.18f;

        private RectTransform[] _content;
        private Vector2[] _contentBase;
        private TMP_Text[] _texts;
        private Color[] _textColors;
        private TweenRunner _tweens;
        private bool _down;
        private bool _disabled;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            // В редакторе вне Play не трогаем объекты: OnValidate иначе «пачкал» бы префабы и сцену.
            if (!Application.isPlaying)
                return;

            CacheContent();
            bool down = state == SelectionState.Pressed;
            bool disabled = state == SelectionState.Disabled;

            if (image != null && _normalSprite != null)
                image.sprite = down && _pressedSprite != null ? _pressedSprite : _normalSprite;

            if (down != _down)
            {
                _down = down;
                MoveContent(down ? -_pressOffset : 0f, down ? _pressDuration : _releaseDuration, down ? Ease.OutQuad : Ease.OutBack,
                    instant);
            }

            if (disabled != _disabled)
            {
                _disabled = disabled;
                if (_disabledOverlay != null)
                    _disabledOverlay.SetActive(disabled);
                for (int i = 0; i < _texts.Length; i++)
                    _texts[i].color = disabled ? _disabledTextColor : _textColors[i];
            }
        }

        private void MoveContent(float offset, float duration, Ease ease, bool instant)
        {
            bool animate = !instant && (_tweens != null || Services.TryGet(out _tweens));
            for (int i = 0; i < _content.Length; i++)
            {
                RectTransform rect = _content[i];
                Vector2 target = _contentBase[i] + new Vector2(0f, offset);
                if (animate)
                    _tweens.Move(rect, rect.anchoredPosition, target, duration, ease);
                else
                    rect.anchoredPosition = target;
            }
        }

        // Содержимое — прямые дети лица (targetGraphic), кроме декора. Запоминается один раз: подписи потом не двигаются.
        private void CacheContent()
        {
            if (_content != null)
                return;

            Transform face = targetGraphic != null ? targetGraphic.transform : transform;
            var content = new List<RectTransform>(face.childCount);
            var texts = new List<TMP_Text>(4);
            for (int i = 0; i < face.childCount; i++)
            {
                if (!(face.GetChild(i) is RectTransform child) || IsDecor(child))
                    continue;
                content.Add(child);
                if (child.TryGetComponent(out TMP_Text text))
                    texts.Add(text);
            }

            _content = content.ToArray();
            _contentBase = new Vector2[_content.Length];
            for (int i = 0; i < _content.Length; i++)
                _contentBase[i] = _content[i].anchoredPosition;

            _texts = texts.ToArray();
            _textColors = new Color[_texts.Length];
            for (int i = 0; i < _texts.Length; i++)
                _textColors[i] = _texts[i].color;
        }

        private bool IsDecor(RectTransform child)
        {
            if (_disabledOverlay != null && child == _disabledOverlay.transform)
                return true;
            for (int i = 0; i < _decor.Length; i++)
            {
                if (_decor[i] == child)
                    return true;
            }
            return false;
        }
    }
}
