using ShaurmaTycoon.Core;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Platform;
using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Меню ShaurmaTycoon/Editor Language: язык игры в редакторе (в Play переключается сразу).
    /// </summary>
    public static class EditorLanguageMenu
    {
        private const string RussianPath = "ShaurmaTycoon/Editor Language/Русский";
        private const string EnglishPath = "ShaurmaTycoon/Editor Language/English";

        [MenuItem(RussianPath)]
        private static void SetRussian()
        {
            SetLanguage(Loc.Russian);
        }

        [MenuItem(EnglishPath)]
        private static void SetEnglish()
        {
            SetLanguage(Loc.English);
        }

        [MenuItem(RussianPath, true)]
        private static bool ValidateRussian()
        {
            Menu.SetChecked(RussianPath, CurrentLanguage == Loc.Russian);
            return true;
        }

        [MenuItem(EnglishPath, true)]
        private static bool ValidateEnglish()
        {
            Menu.SetChecked(EnglishPath, CurrentLanguage == Loc.English);
            return true;
        }

        private static string CurrentLanguage =>
            Loc.NormalizeLanguage(EditorPrefs.GetString(EditorPlatformService.EditorLanguagePrefKey, Loc.Russian));

        private static void SetLanguage(string language)
        {
            EditorPrefs.SetString(EditorPlatformService.EditorLanguagePrefKey, language);

            if (Application.isPlaying && Services.TryGet(out Loc loc))
                loc.SetLanguage(language);
        }
    }
}
