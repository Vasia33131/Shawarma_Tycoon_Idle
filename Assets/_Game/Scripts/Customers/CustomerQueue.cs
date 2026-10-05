using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Очередь клиентов у окна ларька (только визуал):
    /// клиент приходит справа, ждёт, у окна говорит реплику, платит (монета в HUD) и уходит влево.
    /// Длина очереди и скорость обслуживания зависят от сглаженного дохода в секунду.
    /// Тап по клиенту запускает простаивающие станции без менеджера (GDD §4).
    /// </summary>
    public sealed class CustomerQueue : MonoBehaviour, ITickable
    {
        private enum State
        {
            Inactive,
            Walking,
            Waiting,
            Ordering,
            Leaving,
        }

        private const float ArriveThreshold = 2f;
        private const float LeaveSpeedFactor = 1.3f;
        private const float BubbleHeight = 250f;
        private const float BubbleOffsetX = 70f;

        [Tooltip("Улица: родитель клиентов, растянут по зоне ларька. x = 0 — напротив окна.")]
        [SerializeField] private RectTransform _street = null;
        [SerializeField] private CustomerView[] _pool = new CustomerView[0];
        [SerializeField] private RectTransform _bubble = null;
        [SerializeField] private TMP_Text _bubbleText = null;
        [SerializeField] private KioskView _kiosk = null;
        [Tooltip("Сердечки-реакции над клиентами и котом (пул, по кругу).")]
        [SerializeField] private Image[] _hearts = new Image[0];
        [Tooltip("Кот у ларька: иногда клиент в очереди ему умиляется. Можно оставить пустым.")]
        [SerializeField] private CatView _cat = null;

        private readonly List<int> _queue = new List<int>(16);

        private State[] _states;
        private float[] _x;
        private float[] _frameTimer;
        private int[] _frame;
        private int[] _skin;

        private CustomerConfig _config;
        private CustomerDemand _demand;
        private IncomeRateTracker _rate;
        private StationController _stations;
        private CoinFlyEffect _coins;
        private AudioService _audio;
        private TweenRunner _tweens;
        private Loc _loc;

        private float _spawnTimer;
        private float _serviceTimer;
        private int _lastPhrase = -1;
        private int _servedCount;
        private int _nextHeart;
        private float _charmTimer;
        private bool _raining;
        private Color[] _umbrellaColors;

        /// <summary>Заполненность очереди 0–1 (от числа мест): громкость гриля в V6.</summary>
        public float Fullness => _pool.Length > 0 ? Mathf.Clamp01((float)_queue.Count / _pool.Length) : 0f;

        public void Bind(CustomerConfig config, IncomeRateTracker rate, StationController stations, CoinFlyEffect coins,
            AudioService audio, TweenRunner tweens, Loc loc)
        {
            _config = config;
            _demand = config.CreateDemand();
            _rate = rate;
            _stations = stations;
            _coins = coins;
            _audio = audio;
            _tweens = tweens;
            _loc = loc;

            int count = _pool.Length;
            _states = new State[count];
            _x = new float[count];
            _frameTimer = new float[count];
            _frame = new int[count];
            _skin = new int[count];

            for (int i = 0; i < count; i++)
            {
                int index = i;
                _pool[i].Button.onClick.AddListener(() => OnCustomerTapped(index));
                _pool[i].gameObject.SetActive(false);
            }

            _bubble.gameObject.SetActive(false);
            _kiosk.Bind(config.SellerSkin);
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i].gameObject.SetActive(false);
            _charmTimer = Random.Range(config.CatCharmInterval.x, config.CatCharmInterval.y);
        }

        /// <summary>Сердечко всплывает в точке улицы (координаты как у клиентов: x от окна, y от тротуара).</summary>
        public void ShowHeart(Vector2 position)
        {
            if (_hearts.Length == 0 || _tweens == null)
                return;

            Image heart = _hearts[_nextHeart];
            _nextHeart = (_nextHeart + 1) % _hearts.Length;
            RectTransform rect = heart.rectTransform;
            heart.gameObject.SetActive(true);
            _tweens.Move(rect, position, position + new Vector2(0f, 80f), 0.9f, Ease.OutQuad);
            _tweens.Scale(rect, Vector3.one * 0.4f, Vector3.one, 0.25f, Ease.OutBack);
            _tweens.Fade(heart, 1f, 0f, 0.9f, Ease.InQuad);
        }

        /// <summary>Дождь (V5): все клиенты на улице и новые раскрывают зонтики случайного цвета из colors.</summary>
        public void SetRaining(bool raining, Color[] colors)
        {
            _umbrellaColors = colors;
            if (_raining == raining || _states == null)
                return;
            _raining = raining;

            for (int i = 0; i < _pool.Length; i++)
            {
                if (_states[i] == State.Inactive)
                    continue;
                _pool[i].SetUmbrella(raining, PickUmbrellaColor());
                RectTransform umbrella = _pool[i].Umbrella;
                if (raining && umbrella != null)
                    _tweens.Scale(umbrella, Vector3.one * 0.2f, Vector3.one, 0.3f, Ease.OutBack);
            }
        }

        private Color PickUmbrellaColor()
        {
            return _umbrellaColors != null && _umbrellaColors.Length > 0
                ? _umbrellaColors[Random.Range(0, _umbrellaColors.Length)]
                : Color.white;
        }

        public void Tick(float deltaTime)
        {
            if (_config == null || _config.CustomerSkins.Count == 0)
                return;

            _rate.Tick(deltaTime);
            double rate = _rate.Rate;
            float halfWidth = _street.rect.width * 0.5f;

            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0f && _queue.Count < _demand.GetQueueSize(rate) && TrySpawn(halfWidth))
                _spawnTimer = (float)_demand.GetServiceTime(rate) * 0.6f;

            UpdateQueue(deltaTime, rate);
            UpdateLeaving(deltaTime, halfWidth);
            AnimateCustomers(deltaTime);
            UpdateCatCharm(deltaTime);
        }

        // Тело каждого клиента: вприпрыжку в пути, «дыхание» на месте.
        private void AnimateCustomers(float deltaTime)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                State state = _states[i];
                if (state == State.Inactive)
                    continue;
                bool walking = state == State.Walking || state == State.Leaving;
                _pool[i].Animate(deltaTime, walking, state == State.Leaving ? LeaveSpeedFactor : 1f);
            }
        }

        // Раз в пару минут клиент из очереди (не у окна) умиляется коту: радуется, над котом — сердечко.
        private void UpdateCatCharm(float deltaTime)
        {
            if (_cat == null || !_cat.isActiveAndEnabled)
                return;

            _charmTimer -= deltaTime;
            if (_charmTimer > 0f)
                return;
            _charmTimer = Random.Range(_config.CatCharmInterval.x, _config.CatCharmInterval.y);

            for (int place = 1; place < _queue.Count; place++)
            {
                int i = _queue[place];
                if (_states[i] != State.Waiting || _pool[i].IsCheering)
                    continue;
                _pool[i].Cheer(1.5f);
                _cat.Admired();
                return;
            }
        }

        private bool TrySpawn(float halfWidth)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_states[i] != State.Inactive)
                    continue;

                _states[i] = State.Walking;
                _skin[i] = Random.Range(0, _config.CustomerSkins.Count);
                _x[i] = halfWidth + _config.OffscreenMargin;
                _frame[i] = 0;
                _frameTimer[i] = 0f;
                _pool[i].Rect.localScale = Vector3.one;
                _pool[i].Setup(_config.CustomerSkins[_skin[i]], _config);
                _pool[i].SetUmbrella(_raining, PickUmbrellaColor());
                _pool[i].SetX(_x[i]);
                _pool[i].gameObject.SetActive(true);
                _queue.Add(i);
                return true;
            }
            return false;
        }

        private void UpdateQueue(float deltaTime, double rate)
        {
            for (int place = 0; place < _queue.Count; place++)
            {
                int i = _queue[place];
                CustomerSkin skin = _config.CustomerSkins[_skin[i]];
                float target = place * _config.QueueSpacing;

                if (_states[i] == State.Ordering)
                {
                    _serviceTimer -= deltaTime;
                    if (_serviceTimer <= 0f)
                    {
                        Serve(i, skin);
                        place--;
                    }
                    continue;
                }

                if (_x[i] - target > ArriveThreshold)
                {
                    _states[i] = State.Walking;
                    _x[i] = Mathf.Max(target, _x[i] - _config.WalkSpeed * deltaTime);
                    Animate(i, skin, deltaTime);
                    _pool[i].SetX(_x[i]);
                    continue;
                }

                if (place == 0)
                    BeginOrder(i, skin, rate);
                else if (_states[i] != State.Waiting)
                {
                    _states[i] = State.Waiting;
                    if (!_pool[i].IsCheering)
                        _pool[i].SetSprite(skin.Idle);
                }
            }
        }

        private void UpdateLeaving(float deltaTime, float halfWidth)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_states[i] != State.Leaving)
                    continue;

                _x[i] -= _config.WalkSpeed * LeaveSpeedFactor * deltaTime;
                Animate(i, _config.CustomerSkins[_skin[i]], deltaTime);
                _pool[i].SetX(_x[i]);

                if (_x[i] < -halfWidth - _config.OffscreenMargin)
                {
                    _states[i] = State.Inactive;
                    _pool[i].gameObject.SetActive(false);
                }
            }
        }

        private void BeginOrder(int i, CustomerSkin skin, double rate)
        {
            _states[i] = State.Ordering;
            _pool[i].SetSprite(skin.Idle);
            _serviceTimer = (float)_demand.GetServiceTime(rate);

            _bubbleText.text = _loc.Get(PickPhrase(skin));
            _bubble.anchoredPosition = new Vector2(_x[i] + BubbleOffsetX, BubbleHeight);
            _bubble.gameObject.SetActive(true);
            _tweens.Scale(_bubble, Vector3.one * 0.3f, Vector3.one, 0.25f, Ease.OutBack);
            _audio?.Play(SoundId.Bubble);
            _kiosk.SetServing(true);
        }

        private void Serve(int i, CustomerSkin skin)
        {
            _queue.RemoveAt(0);
            _states[i] = State.Leaving;
            if (skin.Cheer != null)
                _pool[i].Cheer(_config.CheerDuration);
            else
                _pool[i].SetSprite(skin.Idle);
            _tweens.Punch(_pool[i].Rect, 0.12f, 0.25f);
            _coins.Fly(_pool[i].Rect.position + Vector3.up * 120f);
            if (Random.value < _config.ReactionChance)
                ShowHeart(new Vector2(_x[i], _config.CharacterHeight + 10f));

            _bubble.gameObject.SetActive(false);
            _kiosk.SetServing(false);
            _servedCount++;
            if (_servedCount % _config.SellerHappyEvery == 0)
                _kiosk.ShowHappy();
        }

        private void Animate(int i, CustomerSkin skin, float deltaTime)
        {
            if (skin.Walk.Length == 0)
                return;

            _frameTimer[i] += deltaTime;
            float frameTime = 1f / _config.WalkFrameRate;
            while (_frameTimer[i] >= frameTime)
            {
                _frameTimer[i] -= frameTime;
                _frame[i] = (_frame[i] + 1) % skin.Walk.Length;
            }
            _pool[i].SetSprite(skin.Walk[_frame[i] % skin.Walk.Length]);
        }

        // Своя реплика персонажа (бабушка — про укроп, курьер — про заказы) или общая.
        private string PickPhrase(CustomerSkin skin)
        {
            if (!string.IsNullOrEmpty(skin.PhraseKey) && Random.value < _config.OwnPhraseChance)
                return skin.PhraseKey;

            int count = _config.PhraseKeys.Count;
            if (count == 0)
                return string.Empty;

            int index = Random.Range(0, count);
            if (count > 1 && index == _lastPhrase)
                index = (index + 1) % count;
            _lastPhrase = index;
            return _config.PhraseKeys[index];
        }

        private void OnCustomerTapped(int index)
        {
            if (_states[index] == State.Inactive)
                return;

            _stations.TapAllIdle();
            _audio?.Play(SoundId.Tap);
            _tweens.Punch(_pool[index].Rect, 0.15f, 0.25f);
        }
    }
}
