using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Core.Tweening
{
    /// <summary>
    /// Минимальный твинер для UI: масштаб, позиция (с дугой) и прозрачность.
    /// Твины хранятся в массиве структур и переиспользуются — после прогрева аллокаций нет.
    /// Новый твин того же свойства того же объекта заменяет старый.
    /// </summary>
    public sealed class TweenRunner : ITickable
    {
        private struct Tween
        {
            public bool Active;
            public RectTransform Rect;
            public Graphic Graphic;
            public TweenProperty Property;
            public Vector3 From;
            public Vector3 To;
            public float Arc;
            public float Delay;
            public float Duration;
            public float Elapsed;
            public Ease Ease;
            public ITweenListener Listener;
            public int Tag;
        }

        private Tween[] _tweens;
        private int _count;

        public TweenRunner(int capacity = 64)
        {
            _tweens = new Tween[Math.Max(capacity, 4)];
        }

        public int ActiveCount
        {
            get
            {
                int active = 0;
                for (int i = 0; i < _count; i++)
                {
                    if (_tweens[i].Active)
                        active++;
                }
                return active;
            }
        }

        public void Scale(RectTransform rect, Vector3 from, Vector3 to, float duration, Ease ease,
            float delay = 0f, ITweenListener listener = null, int tag = 0)
        {
            Start(rect, null, TweenProperty.Scale, from, to, 0f, duration, ease, delay, listener, tag);
        }

        /// <summary>Отскок: масштаб от (1 + strength) обратно к 1.</summary>
        public void Punch(RectTransform rect, float strength, float duration)
        {
            Scale(rect, Vector3.one * (1f + strength), Vector3.one, duration, Ease.OutQuad);
        }

        /// <param name="arc">Высота дуги (вверх) в середине пути, в единицах anchoredPosition.</param>
        public void Move(RectTransform rect, Vector2 from, Vector2 to, float duration, Ease ease,
            float arc = 0f, float delay = 0f, ITweenListener listener = null, int tag = 0)
        {
            Start(rect, null, TweenProperty.AnchoredPosition, from, to, arc, duration, ease, delay, listener, tag);
        }

        public void Fade(Graphic graphic, float from, float to, float duration, Ease ease,
            float delay = 0f, ITweenListener listener = null, int tag = 0)
        {
            Start(graphic.rectTransform, graphic, TweenProperty.Alpha, new Vector3(from, 0f, 0f), new Vector3(to, 0f, 0f),
                0f, duration, ease, delay, listener, tag);
        }

        public bool IsTweening(RectTransform rect, TweenProperty property)
        {
            return Find(rect, property) >= 0;
        }

        /// <summary>Останавливает твин; complete — сразу выставить конечное значение (колбэк не вызывается).</summary>
        public void Kill(RectTransform rect, TweenProperty property, bool complete = false)
        {
            int index = Find(rect, property);
            if (index < 0)
                return;

            if (complete)
                Apply(ref _tweens[index], 1f, 1f);
            _tweens[index].Active = false;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _count; i++)
            {
                if (!_tweens[i].Active)
                    continue;

                if (_tweens[i].Rect == null)
                {
                    _tweens[i].Active = false;
                    continue;
                }

                if (_tweens[i].Delay > 0f)
                {
                    _tweens[i].Delay -= deltaTime;
                    continue;
                }

                _tweens[i].Elapsed += deltaTime;
                float t = _tweens[i].Duration > 0f ? Mathf.Clamp01(_tweens[i].Elapsed / _tweens[i].Duration) : 1f;
                Apply(ref _tweens[i], t, EaseFunctions.Evaluate(_tweens[i].Ease, t));

                if (t < 1f)
                    continue;

                // Колбэк может запустить новый твин в этот же слот — после него слот не трогаем.
                ITweenListener listener = _tweens[i].Listener;
                int tag = _tweens[i].Tag;
                _tweens[i].Active = false;
                _tweens[i].Listener = null;
                listener?.OnTweenCompleted(tag);
            }
        }

        private void Start(RectTransform rect, Graphic graphic, TweenProperty property, Vector3 from, Vector3 to,
            float arc, float duration, Ease ease, float delay, ITweenListener listener, int tag)
        {
            if (rect == null)
                return;

            int index = Find(rect, property);
            if (index < 0)
                index = FindFreeSlot();

            _tweens[index] = new Tween
            {
                Active = true,
                Rect = rect,
                Graphic = graphic,
                Property = property,
                From = from,
                To = to,
                Arc = arc,
                Delay = delay,
                Duration = duration,
                Elapsed = 0f,
                Ease = ease,
                Listener = listener,
                Tag = tag,
            };

            Apply(ref _tweens[index], 0f, 0f);
        }

        private static void Apply(ref Tween tween, float t, float eased)
        {
            switch (tween.Property)
            {
                case TweenProperty.Scale:
                    tween.Rect.localScale = Vector3.LerpUnclamped(tween.From, tween.To, eased);
                    break;
                case TweenProperty.AnchoredPosition:
                    Vector2 position = Vector2.LerpUnclamped(tween.From, tween.To, eased);
                    position.y += tween.Arc * 4f * t * (1f - t);
                    tween.Rect.anchoredPosition = position;
                    break;
                case TweenProperty.Alpha:
                    if (tween.Graphic != null)
                    {
                        Color color = tween.Graphic.color;
                        color.a = Mathf.LerpUnclamped(tween.From.x, tween.To.x, eased);
                        tween.Graphic.color = color;
                    }
                    break;
            }
        }

        private int Find(RectTransform rect, TweenProperty property)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_tweens[i].Active && _tweens[i].Property == property && ReferenceEquals(_tweens[i].Rect, rect))
                    return i;
            }
            return -1;
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < _count; i++)
            {
                if (!_tweens[i].Active)
                    return i;
            }

            if (_count == _tweens.Length)
                Array.Resize(ref _tweens, _tweens.Length * 2);
            return _count++;
        }
    }
}
