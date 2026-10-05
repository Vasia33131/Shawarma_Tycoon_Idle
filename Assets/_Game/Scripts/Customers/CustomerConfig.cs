using System.Collections.Generic;
using UnityEngine;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Визуал клиентов и продавца. На экономику не влияет.
    /// </summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Customer Config", fileName = "Customers")]
    public sealed class CustomerConfig : ScriptableObject
    {
        [SerializeField] private CustomerSkin[] _customerSkins = new CustomerSkin[0];
        [SerializeField] private CustomerSkin _sellerSkin = new CustomerSkin();

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 280f;
        [SerializeField] private float _walkFrameRate = 12f;
        [SerializeField] private float _queueSpacing = 120f;
        [Tooltip("Насколько за краем экрана клиент появляется и исчезает.")]
        [SerializeField] private float _offscreenMargin = 140f;

        [Header("Procedural walk (skins without walk frames, V4)")]
        [Tooltip("Высота персонажа на экране (холст спрайта), px канваса.")]
        [SerializeField] private float _characterHeight = 230f;
        [SerializeField] private float _bobHeight = 10f;
        [Tooltip("Шагов в секунду при обычной скорости ходьбы.")]
        [SerializeField] private float _bobFrequency = 2.2f;
        [SerializeField] private float _tiltDegrees = 3f;
        [SerializeField] private float _squash = 0.04f;
        [Tooltip("«Дыхание» стоящего клиента: доля роста и период, с.")]
        [SerializeField] private float _breathAmount = 0.015f;
        [SerializeField] private float _breathPeriod = 3f;

        [Header("Reactions")]
        [Tooltip("Шанс, что над довольным клиентом всплывёт сердечко.")]
        [SerializeField, Range(0f, 1f)] private float _reactionChance = 0.3f;
        [Tooltip("Сколько секунд клиент радуется после оплаты и при виде кота.")]
        [SerializeField] private float _cheerDuration = 0.8f;
        [Tooltip("Продавец показывает «класс» каждый N-й заказ.")]
        [SerializeField] private int _sellerHappyEvery = 8;
        [Tooltip("Шанс сказать свою реплику персонажа вместо общей.")]
        [SerializeField, Range(0f, 1f)] private float _ownPhraseChance = 0.5f;
        [Tooltip("Раз в столько секунд (случайно в интервале) клиент в очереди умиляется коту.")]
        [SerializeField] private Vector2 _catCharmInterval = new Vector2(120f, 180f);

        [Header("Demand (visual only)")]
        [SerializeField] private int _maxQueue = 7;
        [Tooltip("Клиентов в очереди добавляется за каждый порядок (×10) дохода в секунду.")]
        [SerializeField] private float _queueGrowth = 1.2f;
        [SerializeField] private float _serviceTimeMax = 3f;
        [SerializeField] private float _serviceTimeMin = 0.7f;
        [SerializeField] private float _serviceSpeedup = 0.6f;

        [Header("Speech")]
        [Tooltip("Ключи локализации реплик клиентов у окна.")]
        [SerializeField] private string[] _phraseKeys = new string[0];

        public IReadOnlyList<CustomerSkin> CustomerSkins => _customerSkins;
        public CustomerSkin SellerSkin => _sellerSkin;
        public float WalkSpeed => _walkSpeed;
        public float WalkFrameRate => _walkFrameRate;
        public float QueueSpacing => _queueSpacing;
        public float OffscreenMargin => _offscreenMargin;
        public IReadOnlyList<string> PhraseKeys => _phraseKeys;
        public float CharacterHeight => _characterHeight;
        public float BobHeight => _bobHeight;
        public float BobFrequency => _bobFrequency;
        public float TiltDegrees => _tiltDegrees;
        public float Squash => _squash;
        public float BreathAmount => _breathAmount;
        public float BreathPeriod => _breathPeriod;
        public float ReactionChance => _reactionChance;
        public float CheerDuration => _cheerDuration;
        public int SellerHappyEvery => _sellerHappyEvery;
        public float OwnPhraseChance => _ownPhraseChance;
        public Vector2 CatCharmInterval => _catCharmInterval;

        public CustomerDemand CreateDemand()
        {
            return new CustomerDemand(_maxQueue, _queueGrowth, _serviceTimeMax, _serviceTimeMin, _serviceSpeedup);
        }

        private void OnValidate()
        {
            _maxQueue = Mathf.Max(1, _maxQueue);
            _serviceTimeMin = Mathf.Max(0.1f, _serviceTimeMin);
            _serviceTimeMax = Mathf.Max(_serviceTimeMin, _serviceTimeMax);
            _walkSpeed = Mathf.Max(1f, _walkSpeed);
            _walkFrameRate = Mathf.Max(1f, _walkFrameRate);
            _characterHeight = Mathf.Max(10f, _characterHeight);
            _bobFrequency = Mathf.Max(0.1f, _bobFrequency);
            _breathPeriod = Mathf.Max(0.1f, _breathPeriod);
            _sellerHappyEvery = Mathf.Max(1, _sellerHappyEvery);
            _catCharmInterval = new Vector2(Mathf.Max(1f, _catCharmInterval.x), Mathf.Max(_catCharmInterval.x, _catCharmInterval.y));
        }
    }
}
