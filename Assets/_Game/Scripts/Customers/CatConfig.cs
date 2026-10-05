using UnityEngine;

namespace ShaurmaTycoon.Customers
{
    /// <summary>Кот Лаваш у ларька (V4): позы и тайминги. Заполняет ShaurmaTycoon/Art/Assign Icons из Sprites/Characters/Cat.</summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Cat Config", fileName = "Cat")]
    public sealed class CatConfig : ScriptableObject
    {
        [Header("Poses")]
        [SerializeField] private Sprite _sleep = null;
        [SerializeField] private Sprite _sit = null;
        [SerializeField] private Sprite _purr = null;
        [SerializeField] private Sprite _stretch = null;
        [SerializeField] private Sprite _bed = null;

        [Header("Timing, s")]
        [SerializeField] private Vector2 _sleepTime = new Vector2(40f, 90f);
        [SerializeField] private Vector2 _sitTime = new Vector2(10f, 20f);
        [Tooltip("«Дыхание» во сне: доля размера и период.")]
        [SerializeField] private float _breathAmount = 0.03f;
        [SerializeField] private float _breathPeriod = 3.5f;
        [Tooltip("Как часто над спящим всплывает «z».")]
        [SerializeField] private float _snoreInterval = 6f;
        [Tooltip("Как часто сидящий кот моргает (случайно в интервале).")]
        [SerializeField] private Vector2 _blinkInterval = new Vector2(3f, 5f);

        public Sprite Sleep => _sleep;
        public Sprite Sit => _sit;
        public Sprite Purr => _purr;
        public Sprite Stretch => _stretch;
        public Sprite Bed => _bed;
        public Vector2 SleepTime => _sleepTime;
        public Vector2 SitTime => _sitTime;
        public float BreathAmount => _breathAmount;
        public float BreathPeriod => _breathPeriod;
        public float SnoreInterval => _snoreInterval;
        public Vector2 BlinkInterval => _blinkInterval;

        public bool HasPoses => _sleep != null && _sit != null;

        private void OnValidate()
        {
            _breathPeriod = Mathf.Max(0.1f, _breathPeriod);
            _snoreInterval = Mathf.Max(0.5f, _snoreInterval);
        }
    }
}
