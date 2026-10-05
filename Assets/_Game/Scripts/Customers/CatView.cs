using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Кот Лаваш на коробке у ларька (V4). Поведение — CatBrain; здесь только картинка:
    /// во сне «дышит» и иногда над ним всплывает «z», сидя моргает, мурча покачивается; на тап — мяу или
    /// мурчание с сердечками. Анимации идут в реальном времени (отладочное «время ×10» их не ускоряет).
    /// Монет за кота нет.
    /// </summary>
    public sealed class CatView : MonoBehaviour, ITickable
    {
        [SerializeField] private CatConfig _config = null;
        [SerializeField] private Image _body = null;
        [SerializeField] private Button _button = null;
        [SerializeField] private TMP_Text _snore = null;
        [SerializeField] private Image[] _hearts = new Image[0];

        private CatBrain _brain;
        private AudioService _audio;
        private TweenRunner _tweens;
        private TickManager _ticks;
        private float _time;
        private float _snoreTimer;
        private float _blinkTimer;
        private float _blinkLeft;
        private int _nextHeart;

        public void Bind(AudioService audio, TweenRunner tweens, TickManager ticks)
        {
            _audio = audio;
            _tweens = tweens;
            _ticks = ticks;
            if (_config == null || !_config.HasPoses)
            {
                gameObject.SetActive(false);
                return;
            }

            _brain = new CatBrain(new System.Random(), _config.SleepTime.x, _config.SleepTime.y, _config.SitTime.x, _config.SitTime.y);
            _brain.StateChanged += OnStateChanged;
            _button.onClick.AddListener(OnTap);
            _snoreTimer = _config.SnoreInterval;
            _blinkTimer = Random.Range(_config.BlinkInterval.x, _config.BlinkInterval.y);
            if (_snore != null)
                _snore.gameObject.SetActive(false);
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i].gameObject.SetActive(false);
            ApplyPose(_brain.State, animate: false);
        }

        private void OnDestroy()
        {
            if (_brain != null)
                _brain.StateChanged -= OnStateChanged;
        }

        /// <summary>Клиент из очереди умилился коту — над котом сердечко.</summary>
        public void Admired()
        {
            ShowHeart(0f);
        }

        public void Tick(float deltaTime)
        {
            if (_brain == null)
                return;

            float scale = _ticks != null && _ticks.TimeScale > 0f ? _ticks.TimeScale : 1f;
            float dt = deltaTime / scale;
            _time += dt;
            _brain.Tick(dt);

            RectTransform body = _body.rectTransform;
            switch (_brain.State)
            {
                case CatState.Sleep:
                    float breath = 1f + _config.BreathAmount * (0.5f + 0.5f * Mathf.Sin(_time * (2f * Mathf.PI / _config.BreathPeriod)));
                    SetPose(body, breath, breath, 0f);
                    _snoreTimer -= dt;
                    if (_snoreTimer <= 0f)
                    {
                        _snoreTimer = _config.SnoreInterval;
                        ShowSnore();
                    }
                    break;
                case CatState.Sit:
                    _blinkTimer -= dt;
                    if (_blinkTimer <= 0f)
                    {
                        _blinkTimer = Random.Range(_config.BlinkInterval.x, _config.BlinkInterval.y);
                        _blinkLeft = 0.15f;
                    }
                    _blinkLeft -= dt;
                    SetPose(body, 1f, _blinkLeft > 0f ? 0.97f : 1f, 0f);
                    break;
                case CatState.Purr:
                    SetPose(body, 1f, 1f, Mathf.Sin(_time * 5f) * 3f);
                    break;
                default:
                    SetPose(body, 1f, 1f, 0f);
                    break;
            }
        }

        private void OnTap()
        {
            switch (_brain.Tap())
            {
                case CatReaction.Meow:
                    _audio?.Play(SoundId.CatMeow);
                    break;
                case CatReaction.Purr:
                    _audio?.Play(SoundId.CatPurr);
                    for (int i = 0; i < 3; i++)
                        ShowHeart((i - 1) * 34f);
                    break;
            }
        }

        private void OnStateChanged(CatState state)
        {
            ApplyPose(state, animate: true);
        }

        private void ApplyPose(CatState state, bool animate)
        {
            Sprite sprite = state == CatState.Sleep ? _config.Sleep
                : state == CatState.Stretch && _config.Stretch != null ? _config.Stretch
                : state == CatState.Purr && _config.Purr != null ? _config.Purr
                : _config.Sit;
            _body.sprite = sprite;
            if (animate && _tweens != null)
                _tweens.Punch(_body.rectTransform, 0.06f, 0.2f);
        }

        // Масштаб «дыхания» не перебивает подскок при смене позы.
        private void SetPose(RectTransform body, float scaleX, float scaleY, float tilt)
        {
            if (_tweens == null || !_tweens.IsTweening(body, TweenProperty.Scale))
                body.localScale = new Vector3(scaleX, scaleY, 1f);
            body.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }

        private void ShowSnore()
        {
            if (_snore == null || _tweens == null)
                return;
            RectTransform rect = _snore.rectTransform;
            Vector2 from = new Vector2(30f, _body.rectTransform.rect.height * 0.6f);
            _snore.gameObject.SetActive(true);
            _tweens.Move(rect, from, from + new Vector2(24f, 60f), 2f, Ease.OutQuad);
            _tweens.Fade(_snore, 1f, 0f, 2f, Ease.InQuad);
        }

        private void ShowHeart(float x)
        {
            if (_hearts.Length == 0 || _tweens == null)
                return;
            Image heart = _hearts[_nextHeart];
            _nextHeart = (_nextHeart + 1) % _hearts.Length;
            RectTransform rect = heart.rectTransform;
            Vector2 from = new Vector2(x, _body.rectTransform.rect.height * 0.8f);
            heart.gameObject.SetActive(true);
            _tweens.Move(rect, from, from + new Vector2(0f, 80f), 1f, Ease.OutQuad);
            _tweens.Scale(rect, Vector3.one * 0.4f, Vector3.one, 0.25f, Ease.OutBack);
            _tweens.Fade(heart, 1f, 0f, 1f, Ease.InQuad);
        }
    }
}
