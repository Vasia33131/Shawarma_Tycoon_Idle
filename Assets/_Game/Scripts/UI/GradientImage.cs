using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Вертикальный градиент из трёх цветов (верх → середина → низ) — вечернее небо (V3).
    /// Рисует свой меш из двух полос без текстуры; перестраивается только при смене цветов.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GradientImage : MaskableGraphic
    {
        [SerializeField] private Color _top = new Color32(0x5B, 0x4B, 0x8A, 0xFF);
        [SerializeField] private Color _middle = new Color32(0xF2, 0xA0, 0x7B, 0xFF);
        [SerializeField] private Color _bottom = new Color32(0xFF, 0xD7, 0xA0, 0xFF);
        [Tooltip("Где проходит средний цвет: доля высоты снизу.")]
        [SerializeField, Range(0.05f, 0.95f)] private float _middleAt = 0.45f;

        // Страховка: в сцене, собранной до появления RequireComponent, CanvasRenderer мог не добавиться,
        // а без него RectMask2D падает с MissingComponentException. Awake идёт раньше OnEnable у Graphic.
        protected override void Awake()
        {
            if (!TryGetComponent(out CanvasRenderer _))
                gameObject.AddComponent<CanvasRenderer>();
            base.Awake();
        }

        public void SetColors(Color top, Color middle, Color bottom)
        {
            if (top == _top && middle == _middle && bottom == _bottom)
                return;
            _top = top;
            _middle = middle;
            _bottom = bottom;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float midY = rect.yMin + rect.height * _middleAt;
            Color tint = color;

            AddRow(vh, rect, rect.yMin, _bottom * tint);
            AddRow(vh, rect, midY, _middle * tint);
            AddRow(vh, rect, rect.yMax, _top * tint);
            vh.AddTriangle(0, 2, 3);
            vh.AddTriangle(0, 3, 1);
            vh.AddTriangle(2, 4, 5);
            vh.AddTriangle(2, 5, 3);
        }

        private static void AddRow(VertexHelper vh, Rect rect, float y, Color rowColor)
        {
            vh.AddVert(new Vector3(rect.xMin, y), rowColor, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, y), rowColor, Vector2.zero);
        }
    }
}
