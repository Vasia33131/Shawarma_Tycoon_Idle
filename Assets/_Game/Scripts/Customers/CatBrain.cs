using System;

namespace ShaurmaTycoon.Customers
{
    public enum CatState
    {
        Sleep,
        Stretch,
        Sit,
        Purr,
    }

    /// <summary>Что кот «сказал» в ответ на тап.</summary>
    public enum CatReaction
    {
        None,
        Meow,
        Purr,
    }

    /// <summary>
    /// Поведение кота Лаваша (V4), без Unity: спит → потягивается → сидит → снова спит; на тап мурчит.
    /// Тап по спящему будит его (мяу, садится); тап по сидящему или потягивающемуся — мурчание;
    /// повторный тап во время мурчания продлевает его не чаще раза в TapCooldown.
    /// Монет за кота нет — только настроение.
    /// </summary>
    public sealed class CatBrain
    {
        public const float StretchDuration = 1.5f;
        public const float PurrDuration = 2.5f;
        public const float TapCooldown = 0.5f;

        private readonly Random _random;
        private readonly float _sleepMin;
        private readonly float _sleepMax;
        private readonly float _sitMin;
        private readonly float _sitMax;

        private float _timeLeft;
        private float _sinceTap = float.MaxValue;

        public CatState State { get; private set; } = CatState.Sleep;

        /// <summary>Сменилось состояние (позу пора перерисовать).</summary>
        public event Action<CatState> StateChanged;

        public CatBrain(Random random, float sleepMin = 40f, float sleepMax = 90f, float sitMin = 10f, float sitMax = 20f)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _sleepMin = sleepMin;
            _sleepMax = Math.Max(sleepMin, sleepMax);
            _sitMin = sitMin;
            _sitMax = Math.Max(sitMin, sitMax);
            _timeLeft = Range(_sleepMin, _sleepMax);
        }

        /// <summary>Сколько секунд осталось в текущем состоянии.</summary>
        public float TimeLeft => _timeLeft;

        public void Tick(float deltaTime)
        {
            _sinceTap += deltaTime;
            _timeLeft -= deltaTime;
            if (_timeLeft > 0f)
                return;

            switch (State)
            {
                case CatState.Sleep:
                    Enter(CatState.Stretch, StretchDuration);
                    break;
                case CatState.Stretch:
                case CatState.Purr:
                    Enter(CatState.Sit, Range(_sitMin, _sitMax));
                    break;
                default:
                    Enter(CatState.Sleep, Range(_sleepMin, _sleepMax));
                    break;
            }
        }

        public CatReaction Tap()
        {
            if (State == CatState.Sleep)
            {
                _sinceTap = 0f;
                Enter(CatState.Sit, Range(_sitMin, _sitMax));
                return CatReaction.Meow;
            }

            if (State == CatState.Purr && _sinceTap < TapCooldown)
                return CatReaction.None;

            _sinceTap = 0f;
            if (State == CatState.Purr)
            {
                _timeLeft = PurrDuration;
                return CatReaction.Purr;
            }

            Enter(CatState.Purr, PurrDuration);
            return CatReaction.Purr;
        }

        private void Enter(CatState state, float duration)
        {
            State = state;
            _timeLeft = duration;
            StateChanged?.Invoke(state);
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
