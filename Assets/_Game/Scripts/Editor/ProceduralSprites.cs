using System.IO;
using ShaurmaTycoon.UI;
using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Меню ShaurmaTycoon/Art/Generate UI Sprites: рисует базовые формы UI-кита в Sprites/UI/Generated/.
    /// Все спрайты белые или серые — цвет задаёт Image.color. Края сглажены по расстоянию до контура (SDF),
    /// поэтому результат детерминирован: одни и те же байты при каждом запуске.
    /// Build Game UI вызывает EnsureGenerated: недостающие файлы дорисовываются, существующие не трогаются.
    /// </summary>
    public static class ProceduralSprites
    {
        public const string Folder = "Assets/_Game/Sprites/UI/Generated";

        public const string RoundedFill = Folder + "/rounded_fill.png";
        public const string RoundedOutline = Folder + "/rounded_outline.png";
        public const string RoundedShade = Folder + "/rounded_shade.png";
        public const string RoundedGlow = Folder + "/rounded_glow.png";
        public const string CardFill = Folder + "/card_fill.png";
        public const string ButtonFill = Folder + "/button_fill.png";
        public const string ButtonFillPressed = Folder + "/button_fill_pressed.png";
        public const string CircleFill = Folder + "/circle_fill.png";
        public const string CircleOutline = Folder + "/circle_outline.png";
        public const string GlowSoft = Folder + "/glow_soft.png";
        public const string PillFill = Folder + "/pill_fill.png";
        public const string EdgeShadow = Folder + "/edge_shadow.png";
        public const string Heart = Folder + "/heart.png";

        private const int Size = 128;
        private const float Radius = 32f;
        private const float OutlineWidth = 5f;
        private const float ShadeBlur = 24f;
        private const float GlowBlur = 18f;
        // 9-slice: скругление + запас на сглаживание.
        private const int Border = 40;

        [MenuItem("ShaurmaTycoon/Art/Generate UI Sprites")]
        public static void GenerateMenu()
        {
            Generate(ContentSetup.EnsureTheme(), overwrite: true);
            Debug.Log("[Generate UI Sprites] Done: " + Folder);
        }

        public static void EnsureGenerated(UiTheme theme)
        {
            Generate(theme, overwrite: false);
        }

        public static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning("[Generate UI Sprites] Sprite not found: " + path);
            return sprite;
        }

        private static void Generate(UiTheme theme, bool overwrite)
        {
            Directory.CreateDirectory(Folder);
            float lipShade = theme != null ? theme.ButtonLipShade : 0.72f;
            int lip = theme != null ? theme.ButtonLip : 10;
            int lipPressed = theme != null ? theme.ButtonLipPressed : 4;
            int cardShade = theme != null ? theme.CardShadeHeight : 6;
            // Нижняя полоса карточки: RowShade относительно Row, чтобы при тонировке Row получился ровно RowShade.
            Color cardBand = theme != null ? Divide(theme.RowShade, theme.Row) : new Color(0.92f, 0.86f, 0.79f);

            Write(RoundedFill, overwrite, Size, Size, Border, (x, y) => Solid(RoundedRect(x, y, Size, Size, Radius)));
            Write(RoundedOutline, overwrite, Size, Size, Border, (x, y) => Solid(Ring(RoundedRect(x, y, Size, Size, Radius), OutlineWidth)));
            Write(PillFill, overwrite, Size, Size / 2, Size / 4, (x, y) => Solid(RoundedRect(x, y, Size, Size / 2, Size / 4f)));
            Write(CardFill, overwrite, Size, Size, Border,
                (x, y) => Banded(RoundedRect(x, y, Size, Size, Radius), y < cardShade, cardBand));
            Write(ButtonFill, overwrite, Size, Size, Border,
                (x, y) => Banded(RoundedRect(x, y, Size, Size, Radius), y < lip, Gray(lipShade)));
            Write(ButtonFillPressed, overwrite, Size, Size, Border,
                (x, y) => Banded(RoundedRect(x, y, Size, Size, Radius), y < lipPressed, Gray(lipShade)));
            Write(CircleFill, overwrite, Size, Size, 0, (x, y) => Solid(Circle(x, y, Size)));
            Write(CircleOutline, overwrite, Size, Size, 0, (x, y) => Solid(Ring(Circle(x, y, Size), OutlineWidth)));

            // Тень и свечение: сам прямоугольник внутри, вокруг — мягкое поле шириной blur.
            int shadeSize = Size + 2 * (int)ShadeBlur;
            Write(RoundedShade, overwrite, shadeSize, shadeSize, Border + (int)ShadeBlur,
                (x, y) => Soft(RoundedRect(x - ShadeBlur, y - ShadeBlur, Size, Size, Radius), ShadeBlur, inside: true));
            int glowSize = Size + 2 * (int)GlowBlur;
            Write(RoundedGlow, overwrite, glowSize, glowSize, Border + (int)GlowBlur,
                (x, y) => Soft(RoundedRect(x - GlowBlur, y - GlowBlur, Size, Size, Radius), GlowBlur, inside: false));
            // Вертикальный градиент тени: непрозрачный сверху, тает книзу.
            const int edgeHeight = 64;
            Write(EdgeShadow, overwrite, 4, edgeHeight, 0, (x, y) =>
            {
                float fade = Mathf.SmoothStep(0f, 1f, (y + 0.5f) / edgeHeight);
                return new Color(1f, 1f, 1f, fade * fade);
            });
            // Сердечко (V4: реакции клиентов и кота) — неявная кривая (x²+y²−1)³ − x²y³ ≤ 0, сглаживание 4×4 выборками.
            Write(Heart, overwrite, Size, Size, 0, (x, y) =>
            {
                int inside = 0;
                for (int sy = 0; sy < 4; sy++)
                {
                    for (int sx = 0; sx < 4; sx++)
                    {
                        float u = ((x + (sx + 0.5f) / 4f) / Size - 0.5f) * 2.7f;
                        float v = ((y + (sy + 0.5f) / 4f) / Size - 0.45f) * 2.7f;
                        float a = u * u + v * v - 1f;
                        if (a * a * a - u * u * v * v * v <= 0f)
                            inside++;
                    }
                }
                return new Color(1f, 1f, 1f, inside / 16f);
            });
            Write(GlowSoft, overwrite, Size, Size, 0, (x, y) =>
            {
                float r = Mathf.Clamp01(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(Size * 0.5f, Size * 0.5f)) / (Size * 0.5f));
                float a = 1f - Mathf.SmoothStep(0f, 1f, r);
                return new Color(1f, 1f, 1f, a * a);
            });
        }

        // ---------- Shapes: signed distance (px) from the pixel centre to the contour, < 0 inside ----------

        private static float RoundedRect(float x, float y, float width, float height, float radius)
        {
            var p = new Vector2(x + 0.5f - width * 0.5f, y + 0.5f - height * 0.5f);
            var q = new Vector2(Mathf.Abs(p.x) - (width * 0.5f - radius), Mathf.Abs(p.y) - (height * 0.5f - radius));
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float Circle(float x, float y, int size)
        {
            float half = size * 0.5f;
            return Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) - half;
        }

        // Кольцо шириной width по внутренней стороне контура.
        private static float Ring(float distance, float width)
        {
            return Mathf.Max(distance, -(distance + width));
        }

        private static float Coverage(float distance)
        {
            return Mathf.Clamp01(0.5f - distance);
        }

        private static Color Solid(float distance)
        {
            return new Color(1f, 1f, 1f, Coverage(distance));
        }

        private static Color Banded(float distance, bool inBand, Color band)
        {
            Color color = inBand ? band : Color.white;
            color.a = Coverage(distance);
            return color;
        }

        // inside: true — тень (внутри сплошная, наружу тает); false — свечение только снаружи контура.
        private static Color Soft(float distance, float blur, bool inside)
        {
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / blur));
            float a = inside ? (distance <= 0f ? 1f : fade) : fade * Mathf.Clamp01(distance + 0.5f);
            return new Color(1f, 1f, 1f, a * a);
        }

        private static Color Gray(float value)
        {
            return new Color(value, value, value, 1f);
        }

        private static Color Divide(Color a, Color b)
        {
            return new Color(Ratio(a.r, b.r), Ratio(a.g, b.g), Ratio(a.b, b.b), 1f);
        }

        private static float Ratio(float a, float b)
        {
            return b > 0.001f ? Mathf.Clamp01(a / b) : 1f;
        }

        // ---------- Files ----------

        private static void Write(string path, bool overwrite, int width, int height, int border, System.Func<int, int, Color> pixel)
        {
            if (!overwrite && File.Exists(path))
                return;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = pixel(x, y);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }
}
