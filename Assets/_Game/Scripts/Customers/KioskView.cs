using ShaurmaTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Ларёк: продавец в окне (стоит или «работает» при заказе) и вертел, полосы которого бегут вниз — иллюзия вращения.
    /// </summary>
    public sealed class KioskView : MonoBehaviour, ITickable
    {
        [SerializeField] private Image _seller = null;
        [SerializeField] private RectTransform _spit = null;
        [SerializeField] private RectTransform[] _spitStripes = new RectTransform[0];
        [Tooltip("Скорость полос вертела, доля высоты вертела в секунду.")]
        [SerializeField] private float _stripeSpeed = 0.25f;
        [Tooltip("Точка у окна, откуда вылетают монеты, когда список станций не виден.")]
        [SerializeField] private RectTransform _window = null;

        private CustomerSkin _sellerSkin;
        private float _offset;

        public RectTransform Window => _window;

        public void Bind(CustomerSkin sellerSkin)
        {
            _sellerSkin = sellerSkin;
            SetServing(false);
        }

        public void SetServing(bool serving)
        {
            if (_sellerSkin == null)
                return;

            Sprite sprite = serving && _sellerSkin.Interact != null ? _sellerSkin.Interact : _sellerSkin.Idle;
            if (sprite != null)
                _seller.sprite = sprite;
        }

        public void Tick(float deltaTime)
        {
            int count = _spitStripes.Length;
            if (count == 0)
                return;

            float height = _spit.rect.height;
            float spacing = 1f / count;
            _offset = Mathf.Repeat(_offset + _stripeSpeed * deltaTime, spacing);

            for (int i = 0; i < count; i++)
            {
                float normalized = 1f - Mathf.Repeat(i * spacing + _offset, 1f);
                _spitStripes[i].anchoredPosition = new Vector2(0f, (normalized - 0.5f) * height);
            }
        }
    }
}
