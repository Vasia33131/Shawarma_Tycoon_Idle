using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Один клиент на экране. Корень двигается и подпрыгивает, тело (Body) отражено по X — клиенты идут влево.
    /// Логикой управляет CustomerQueue.
    /// </summary>
    public sealed class CustomerView : MonoBehaviour
    {
        [SerializeField] private Image _body = null;
        [SerializeField] private Button _button = null;

        public RectTransform Rect => (RectTransform)transform;
        public Button Button => _button;

        public void SetSprite(Sprite sprite)
        {
            if (sprite != null && _body.sprite != sprite)
                _body.sprite = sprite;
        }

        public void SetX(float x)
        {
            Rect.anchoredPosition = new Vector2(x, 0f);
        }
    }
}
