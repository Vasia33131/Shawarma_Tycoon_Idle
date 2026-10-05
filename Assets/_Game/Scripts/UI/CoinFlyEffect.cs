using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Tweening;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Монеты, летящие по дуге к иконке монет в HUD. По прилёте иконка подпрыгивает и звучит «дзынь».
    /// Пул монет создаётся сборщиком UI; если все в полёте — новая монета просто не появляется.
    /// </summary>
    public sealed class CoinFlyEffect : MonoBehaviour, ITweenListener
    {
        private const float PopDuration = 0.15f;
        private const float FlyDurationMin = 0.5f;
        private const float FlyDurationMax = 0.7f;
        private const float ArcHeight = 160f;

        [Tooltip("Слой эффектов: растянут на весь экран, пивот в центре.")]
        [SerializeField] private RectTransform _layer = null;
        [SerializeField] private RectTransform[] _coins = new RectTransform[0];
        [Tooltip("Куда летят монеты (иконка монет в HUD).")]
        [SerializeField] private RectTransform _target = null;

        private TweenRunner _tweens;
        private AudioService _audio;
        private bool[] _busy;

        public void Bind(TweenRunner tweens, AudioService audio)
        {
            _tweens = tweens;
            _audio = audio;
            _busy = new bool[_coins.Length];
            for (int i = 0; i < _coins.Length; i++)
                _coins[i].gameObject.SetActive(false);
        }

        /// <summary>Режим любования (V7): монеты в HUD не летят и не звенят — HUD спрятан.</summary>
        public bool Suppressed { get; set; }

        /// <summary>Запускает монету из точки в мировых координатах. false — свободных монет нет или эффект выключен.</summary>
        public bool Fly(Vector3 worldFrom)
        {
            if (_busy == null || Suppressed)
                return false;

            for (int i = 0; i < _coins.Length; i++)
            {
                if (_busy[i])
                    continue;

                _busy[i] = true;
                RectTransform coin = _coins[i];
                Vector2 from = ToLayer(worldFrom);
                Vector2 to = ToLayer(_target.position);

                coin.gameObject.SetActive(true);
                coin.anchoredPosition = from;
                _tweens.Scale(coin, Vector3.one * 0.4f, Vector3.one, PopDuration, Ease.OutBack);
                _tweens.Move(coin, from, to, Random.Range(FlyDurationMin, FlyDurationMax), Ease.InQuad,
                    arc: ArcHeight, delay: PopDuration * 0.5f, listener: this, tag: i);
                return true;
            }

            return false;
        }

        public void OnTweenCompleted(int tag)
        {
            _busy[tag] = false;
            _coins[tag].gameObject.SetActive(false);
            _tweens.Punch(_target, 0.25f, 0.2f);
            _audio?.Play(SoundId.Coin);
        }

        private Vector2 ToLayer(Vector3 world)
        {
            return _layer.InverseTransformPoint(world);
        }
    }
}
