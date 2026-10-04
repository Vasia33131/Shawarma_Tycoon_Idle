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
        }
    }
}
