using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Economy;
using TMPro;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Всплывающий текст «+1.23K»: поднимается и тает. Пул текстов создаётся сборщиком UI.
    /// </summary>
    public sealed class FloatingTextEffect : MonoBehaviour, ITweenListener
    {
        private const float RiseHeight = 90f;
        private const float RiseDuration = 0.8f;
        private const float FadeDelay = 0.35f;
        private const float FadeDuration = 0.45f;

        [SerializeField] private RectTransform _layer = null;
        [SerializeField] private TMP_Text[] _texts = new TMP_Text[0];

        private TweenRunner _tweens;
        private bool[] _busy;

        public void Bind(TweenRunner tweens)
        {
            _tweens = tweens;
            _busy = new bool[_texts.Length];
            for (int i = 0; i < _texts.Length; i++)
                _texts[i].gameObject.SetActive(false);
        }

        public bool Show(Vector3 worldPosition, NumberText text)
        {
            if (_busy == null)
                return false;

            for (int i = 0; i < _texts.Length; i++)
            {
                if (_busy[i])
                    continue;

                _busy[i] = true;
                TMP_Text label = _texts[i];
                RectTransform rect = label.rectTransform;
                Vector2 from = _layer.InverseTransformPoint(worldPosition);

                label.gameObject.SetActive(true);
                label.SetNumberText(text);
                _tweens.Move(rect, from, from + Vector2.up * RiseHeight, RiseDuration, Ease.OutQuad);
                _tweens.Fade(label, 1f, 0f, FadeDuration, Ease.InQuad, delay: FadeDelay, listener: this, tag: i);
                return true;
            }

            return false;
        }

        public void OnTweenCompleted(int tag)
        {
            _busy[tag] = false;
            _texts[tag].gameObject.SetActive(false);
        }
    }
}
