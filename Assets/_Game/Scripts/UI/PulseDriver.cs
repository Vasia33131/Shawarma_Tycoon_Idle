using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Tweening;

namespace ShaurmaTycoon.UI
{
    /// <summary>Тикает все включённые анимации интерфейса (IUiAnimated) — один цикл вместо Update на каждом объекте.</summary>
    public sealed class PulseDriver : ITickable
    {
        private readonly List<IUiAnimated> _effects = new List<IUiAnimated>(32);
        private readonly TweenRunner _tweens;
        private float _time;

        public PulseDriver(TweenRunner tweens)
        {
            _tweens = tweens;
        }

        public void Register(IUiAnimated effect)
        {
            if (effect != null && !_effects.Contains(effect))
                _effects.Add(effect);
        }

        public void Unregister(IUiAnimated effect)
        {
            _effects.Remove(effect);
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                // Уничтоженный компонент: сравнение через UnityEngine.Object видит «фейковый null».
                if (_effects[i] is UnityEngine.Object unityObject && unityObject == null)
                    _effects.RemoveAt(i);
                else
                    _effects[i].Animate(_time, deltaTime, _tweens);
            }
        }
    }
}
