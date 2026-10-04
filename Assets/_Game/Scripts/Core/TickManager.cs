using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Единственный Update в игре. Раздаёт Tick всем зарегистрированным ITickable.
    /// </summary>
    public sealed class TickManager : MonoBehaviour
    {
        // Длинные паузы (свёрнутая вкладка) покрывает офлайн-доход, а не один гигантский тик.
        private const float MaxDeltaTime = 1f;

        private readonly List<ITickable> _tickables = new List<ITickable>(64);
        private readonly List<ITickable> _pendingAdd = new List<ITickable>(16);
        private readonly List<ITickable> _pendingRemove = new List<ITickable>(16);
        private bool _isTicking;

        /// <summary>Множитель времени игры (отладочное ускорение на этапе 11).</summary>
        public float TimeScale { get; set; } = 1f;

        /// <summary>Пауза игры (реклама, потеря фокуса).</summary>
        public bool IsPaused { get; set; }

        public void Register(ITickable tickable)
        {
            if (tickable == null)
                return;

            if (_isTicking)
            {
                _pendingRemove.Remove(tickable);
                if (!_tickables.Contains(tickable) && !_pendingAdd.Contains(tickable))
                    _pendingAdd.Add(tickable);
                return;
            }

            if (!_tickables.Contains(tickable))
                _tickables.Add(tickable);
        }

        public void Unregister(ITickable tickable)
        {
            if (tickable == null)
                return;

            if (_isTicking)
            {
                _pendingAdd.Remove(tickable);
                if (_tickables.Contains(tickable) && !_pendingRemove.Contains(tickable))
                    _pendingRemove.Add(tickable);
                return;
            }

            _tickables.Remove(tickable);
        }

        private void Update()
        {
            if (IsPaused)
                return;

            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, MaxDeltaTime) * TimeScale;

            _isTicking = true;
            for (int i = 0; i < _tickables.Count; i++)
            {
                try
                {
                    _tickables[i].Tick(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            _isTicking = false;

            ApplyPending();
        }

        private void ApplyPending()
        {
            if (_pendingRemove.Count > 0)
            {
                for (int i = 0; i < _pendingRemove.Count; i++)
                    _tickables.Remove(_pendingRemove[i]);
                _pendingRemove.Clear();
            }

            if (_pendingAdd.Count > 0)
            {
                for (int i = 0; i < _pendingAdd.Count; i++)
                    _tickables.Add(_pendingAdd[i]);
                _pendingAdd.Clear();
            }
        }
    }
}
