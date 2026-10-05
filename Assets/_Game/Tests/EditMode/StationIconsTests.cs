using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Значок станции (V2) и имена файлов иконок на диске (защита от опечаток в именах).</summary>
    public class StationIconsTests
    {
        private const string StationConfigsFolder = "Assets/_Game/Configs/Stations";
        private const string StationIconsFolder = "Assets/_Game/Sprites/Stations";
        private const string UiIconsFolder = "Assets/_Game/Sprites/UI/Icons";

        // Пакет A, Docs/Visual/ASSET_PROMPTS.md (то же, что ArtAssigner.IconFiles).
        private static readonly HashSet<string> UiIconNames = new HashSet<string>
        {
            "icon_coin", "icon_bill", "icon_reputation", "icon_income",
            "tab_stations", "tab_managers", "tab_upgrades", "tab_city", "tab_shop",
            "icon_settings", "icon_gift", "icon_calendar", "icon_quests", "icon_ad", "icon_boost", "icon_lock",
            "icon_offline", "icon_check", "icon_close", "tutorial_hand", "icon_zen",
        };

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in _created)
                Object.DestroyImmediate(item);
            _created.Clear();
        }

        private Image NewImage()
        {
            var go = new GameObject("Image", typeof(RectTransform));
            _created.Add(go);
            return go.AddComponent<Image>();
        }

        private Sprite NewSprite()
        {
            var texture = new Texture2D(4, 4);
            _created.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            _created.Add(sprite);
            return sprite;
        }

        [Test]
        public void Get_DefinitionWithoutConfig_ReturnsNull()
        {
            Assert.IsNull(StationIcons.Get(new TestStationDefinition()));
        }

        [Test]
        public void Get_ConfigWithoutIcon_ReturnsNull()
        {
            var config = ScriptableObject.CreateInstance<StationConfig>();
            _created.Add(config);
            Assert.IsNull(StationIcons.Get(config));
        }

        [Test]
        public void Apply_WithIcon_ShowsArtOverTintedPlate()
        {
            Image plate = NewImage();
            Image art = NewImage();
            Sprite sprite = NewSprite();

            StationIcons.Apply(plate, art, null, sprite, Color.red);

            Assert.IsTrue(art.enabled);
            Assert.AreSame(sprite, art.sprite);
            Assert.Less(plate.color.a, 1f, "plate is a light tint under the art");
            Assert.AreEqual(1f, plate.color.r, 1e-4f);
        }

        [Test]
        public void Apply_WithoutIcon_KeepsColoredCircle()
        {
            Image plate = NewImage();
            Image art = NewImage();

            StationIcons.Apply(plate, art, null, null, Color.red);

            Assert.IsFalse(art.enabled);
            Assert.AreEqual(Color.red, plate.color);
        }

        [Test]
        public void StationIconFiles_MatchStationAssets()
        {
            if (!Directory.Exists(StationIconsFolder))
                Assert.Pass("Пакет B (иконки станций) ещё не добавлен: " + StationIconsFolder);

            var assets = new HashSet<string>(Directory.GetFiles(StationConfigsFolder, "*.asset", SearchOption.AllDirectories)
                .Select(path => Path.GetFileName(Path.GetDirectoryName(path)) + "/" + Path.GetFileNameWithoutExtension(path)));
            var orphans = Directory.GetFiles(StationIconsFolder, "*.png", SearchOption.AllDirectories)
                .Select(path => Path.GetFileName(Path.GetDirectoryName(path)) + "/" + Path.GetFileNameWithoutExtension(path))
                .Where(name => !assets.Contains(name))
                .ToList();

            Assert.IsEmpty(orphans, "Иконки без станции (опечатка в имени или папке?): " + string.Join(", ", orphans));
        }

        [Test]
        public void UiIconFiles_HaveKnownNames()
        {
            if (!Directory.Exists(UiIconsFolder))
                Assert.Pass("Пакет A ещё не добавлен: " + UiIconsFolder);

            var unknown = Directory.GetFiles(UiIconsFolder, "*.png")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !UiIconNames.Contains(name))
                .ToList();

            Assert.IsEmpty(unknown, "Неизвестные иконки (опечатка в имени?): " + string.Join(", ", unknown));
        }
    }
}
