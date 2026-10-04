using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;

namespace ShaurmaTycoon.UI
{
    /// <summary>Тикает все включённые PulseEffect — один цикл вместо Update на каждом объекте.</summary>
    public sealed class PulseDriver : ITickable
    {
        private readonly List<PulseEffect> _effects = new List<PulseEffect>(32);
        private readonly TweenRunner _tweens;
        private float _time;

        public PulseDriver(TweenRunner tweens)
        {
            _tweens = tweens;
        }

        public void Register(PulseEffect effect)
        {
            if (effect != null && !_effects.Contains(effect))
                _effects.Add(effect);
        }

        public void Unregister(PulseEffect effect)
        {
            _effects.Remove(effect);
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i] == null)
                    _effects.RemoveAt(i);
                else
                    _effects[i].Pulse(_time, _tweens);
            }
        }
    }
}
