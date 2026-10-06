using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Этап 12 — релизная сборка для Яндекс Игр.
    /// Release Settings: настройки WebGL на минимальный размер (Brotli, High stripping, IL2CPP «по размеру», исключения без стека).
    /// Preflight: проверка пунктов модерации, которые видно из редактора. Build: всё вместе + сборка в Builds/Yandex и отчёт о размере
    /// (Builds/Yandex_size_report.txt: файлы билда и 40 самых тяжёлых ассетов). Архив для консоли — Builds/Yandex.zip (index.html в корне).
    /// SDK подключает шаблон WebGLTemplates/YandexGames, вся работа с ним — Plugins/WebGL/YandexBridge.jslib; PluginYG2 не используется.
    /// Из командной строки: Unity.exe -batchmode -quit -projectPath . -executeMethod ShaurmaTycoon.EditorTools.YandexRelease.BuildFromCommandLine
    /// </summary>
    public static class YandexRelease
    {
        public const string BuildFolder = "Builds/Yandex";
        public const string ReportPath = "Builds/Yandex_size_report.txt";
        private const string TemplateName = "PROJECT:YandexGames";
        private const string FontPath = "Assets/_Game/Fonts/Rubik SDF.asset";
        private const string StringsPath = "Assets/_Game/Configs/Localization/strings.txt";
        private const string ScriptsFolder = "Assets/_Game/Scripts";
        private const string TemplateIndex = "Assets/WebGLTemplates/YandexGames/index.html";
        private const string LegacyPluginFolder = "Assets/PluginYourGames";
        private static readonly string[] LegacyPluginDefines = { "PLUGIN_YG_2", "YandexGamesPlatform_yg", "TMP_YG2" };

        /// <summary>Архив для консоли разработчика: содержимое Builds/Yandex, index.html в корне (требование 1.22).</summary>
        public const string ArchivePath = "Builds/Yandex.zip";

        /// <summary>Тестовая сборка с чекером модерации (меню Build Yandex Debug) — не для загрузки на модерацию.</summary>
        public const string DebugBuildFolder = "Builds/YandexDebug";
        private const string DebugCheckerFile = "debugcheck.js";
        private const string DebugCheckerSource = "Tools/YandexDebugChecker/" + DebugCheckerFile;

        /// <summary>MP3 музыки и эмбиента для билда (меню Encode Web Audio); Unity копирует StreamingAssets в билд как есть.</summary>
        private const string WebAudioFolder = "Assets/StreamingAssets/" + ShaurmaTycoon.Core.Audio.AudioClipStore.WebFolder;
        private const int MusicKbps = 64;
        private const int AmbientKbps = 40;

        /// <summary>Цель из требований: билд до 20 МБ (лимит платформы — 100 МБ без сжатия).</summary>
        private const long TargetBytes = 20L * 1024 * 1024;
        private const int HeaviestAssetsInReport = 40;

        [MenuItem("ShaurmaTycoon/Release/Apply WebGL Release Settings", priority = 200)]
        public static void ApplyReleaseSettings()
        {
            var webgl = NamedBuildTarget.WebGL;

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;

            PlayerSettings.WebGL.template = TemplateName;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithoutStacktrace;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.Default;

            PlayerSettings.SetScriptingBackend(webgl, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCompilerConfiguration(webgl, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetIl2CppCodeGeneration(webgl, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;

            // Логи без стека: в релизе стек не нужен, а его сбор — лишний код и время.
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
            PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

            // Шрифт динамический: глифы строятся из TTF во время игры, заполненный в редакторе атлас в билд не нужен.
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null)
            {
                var fontSo = new SerializedObject(font);
                SerializedProperty clear = fontSo.FindProperty("m_ClearDynamicDataOnBuild");
                if (clear != null && !clear.boolValue)
                {
                    clear.boolValue = true;
                    fontSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            RemoveLegacyPluginDefines();

            AssetDatabase.SaveAssets();
            Debug.Log("[Release] WebGL release settings applied: Brotli, High stripping, IL2CPP OptimizeSize/Master, exceptions without stack trace.");
        }

        /// <summary>PluginYG2 удалён (SDK — через свой мост); его символы компиляции больше не нужны ни на одной платформе.</summary>
        private static void RemoveLegacyPluginDefines()
        {
            foreach (NamedBuildTarget target in new[] { NamedBuildTarget.WebGL, NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                string current = PlayerSettings.GetScriptingDefineSymbols(target);
                string cleaned = string.Join(";", current.Split(';').Where(d => d.Length > 0 && Array.IndexOf(LegacyPluginDefines, d) < 0));
                if (cleaned != current)
                    PlayerSettings.SetScriptingDefineSymbols(target, cleaned);
            }
        }

        [MenuItem("ShaurmaTycoon/Release/Preflight Check", priority = 201)]
        public static bool Preflight()
        {
            var problems = new List<string>();
            var notes = new List<string>();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                problems.Add("Активная платформа не WebGL (File → Build Settings → WebGL → Switch Platform).");
            if (EditorUserBuildSettings.development)
                problems.Add("Включён Development Build — в релизе будут отладочные логи и плашка Development Build.");
            if (PlayerSettings.WebGL.template != TemplateName)
                problems.Add($"WebGL-шаблон {PlayerSettings.WebGL.template}, нужен {TemplateName}.");
            if (PlayerSettings.WebGL.exceptionSupport == WebGLExceptionSupport.FullWithStacktrace)
                problems.Add("Исключения с полным стеком — заметно больше код и медленнее игра.");

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0 || !scenes[0].EndsWith("Boot.unity", StringComparison.Ordinal))
                problems.Add("Первой сценой в Build Settings должна быть Boot.");

            // 1.22: index.html есть в шаблоне; 8.4: внешних ссылок в коде игры нет (кроме sdk.js платформы).
            if (!File.Exists(TemplateIndex))
                problems.Add("Нет шаблона " + TemplateIndex);
            else if (!File.ReadAllText(TemplateIndex).Contains("/sdk.js"))
                problems.Add("Шаблон не подключает /sdk.js (требование 1.1).");

            foreach (string file in Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Editor/"))
                    continue;
                string text = File.ReadAllText(file);
                if (text.Contains("Application.OpenURL") || text.Contains("http://") || text.Contains("https://"))
                    problems.Add("Внешняя ссылка в коде (требование 8.4): " + file);
            }

            // 1.6.1.6, 1.6.2.5: Unity WebGL (Audio.js) играет Streaming и Compressed In Memory клипы через HTML5 <audio> —
            // браузер показывает системный плеер. Через Web Audio идут только Decompress On Load.
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Game" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is AudioImporter audio
                    && (audio.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad
                        || audio.GetOverrideSampleSettings("WebGL").loadType != AudioClipLoadType.DecompressOnLoad))
                    problems.Add("Звук не Decompress On Load — играет через HTML5 <audio>, системный плеер браузера (1.6.1.6, 1.6.2.5): " + path);
            }

            // Музыка и эмбиент в билде — MP3 из StreamingAssets/Audio (меню Encode Web Audio).
            foreach (string source in DeferredAudioSources())
            {
                string mp3 = WebAudioPath(source);
                if (!File.Exists(mp3) || new FileInfo(mp3).Length == 0)
                    problems.Add($"Нет {mp3} для {source} — ShaurmaTycoon/Release/Encode Web Audio (MP3).");
            }

            // Музыка и эмбиент идут отдельными файлами по имени клипа (AudioClipStore): опечатка в конфиге — тишина в билде.
            var deferredClips = new HashSet<string>(AssetDatabase.FindAssets("t:AudioClip", ShaurmaTycoon.Core.Audio.AudioClipStore.DeferredFolders)
                .Select(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g))));
            var clipNames = new List<(string Owner, string Clip)>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ShaurmaTycoon.Prestige.CityConfig)))
            {
                var city = AssetDatabase.LoadAssetAtPath<ShaurmaTycoon.Prestige.CityConfig>(AssetDatabase.GUIDToAssetPath(guid));
                clipNames.Add((city.name, city.MusicName));
                clipNames.Add((city.name, city.AmbientBaseName));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ShaurmaTycoon.Core.Audio.MusicConfig)))
                clipNames.Add(("Music", AssetDatabase.LoadAssetAtPath<ShaurmaTycoon.Core.Audio.MusicConfig>(AssetDatabase.GUIDToAssetPath(guid)).TrackName));
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ShaurmaTycoon.Core.Audio.AmbientAudioConfig)))
            {
                var ambient = AssetDatabase.LoadAssetAtPath<ShaurmaTycoon.Core.Audio.AmbientAudioConfig>(AssetDatabase.GUIDToAssetPath(guid));
                clipNames.Add((ambient.name, ambient.SizzleName));
                clipNames.Add((ambient.name, ambient.RainName));
                clipNames.Add((ambient.name, ambient.WindName));
            }
            foreach (var (owner, clip) in clipNames)
            {
                if (!string.IsNullOrEmpty(clip) && !deferredClips.Contains(clip))
                    problems.Add($"{owner}: клип «{clip}» не найден в {string.Join(", ", ShaurmaTycoon.Core.Audio.AudioClipStore.DeferredFolders)}.");
            }

            // 2.14, 8.2.3: у каждой строки есть ru и en.
            if (File.Exists(StringsPath))
            {
                int line = 0;
                foreach (string row in File.ReadAllLines(StringsPath))
                {
                    line++;
                    if (row.Length == 0 || row[0] == '#')
                        continue;
                    string[] cells = row.Split('\t');
                    if (cells.Length < 3 || cells[1].Trim().Length == 0 || cells[2].Trim().Length == 0)
                        problems.Add($"strings.txt:{line}: нет перевода ru/en — «{cells[0]}».");
                }
            }

            // SDK подключается только шаблоном и мостом: второй обёртки поверх SDK (PluginYG2) в проекте быть не должно.
            if (Directory.Exists(LegacyPluginFolder))
                problems.Add($"В проекте снова есть {LegacyPluginFolder} — второй слой поверх SDK (Game Ready, пауза, свой index.html). Удалить.");
            if (File.Exists(TemplateIndex) && File.ReadAllText(TemplateIndex).Contains("YG2Instance"))
                problems.Add("Шаблон шлёт сообщения YG2Instance — такого объекта без PluginYG2 нет (ошибки в консоли, 6.4).");
            if (File.Exists(TemplateIndex) && File.ReadAllText(TemplateIndex).Contains(DebugCheckerFile))
                problems.Add($"Шаблон подключает {DebugCheckerFile} — чекер только для Build Yandex Debug, не для релиза.");
            if (Directory.GetFiles("Assets", DebugCheckerFile, SearchOption.AllDirectories).Length > 0)
                problems.Add($"{DebugCheckerFile} лежит в Assets — его место Tools/YandexDebugChecker (иначе может попасть в билд).");

            if (PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL) < ManagedStrippingLevel.Medium)
                notes.Add("Managed stripping ниже Medium — билд больше, чем нужно (Apply WebGL Release Settings).");
            if (PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Brotli)
                notes.Add("Сжатие не Brotli — архив больше.");

            notes.Add("Вручную в консоли: товары bills_small/medium/large (расходуемые), starter_pack, no_ads, income_x2 (постоянные); лидерборд richest; включить облачные сохранения (1.11).");

            var report = new StringBuilder();
            report.AppendLine(problems.Count == 0 ? "[Release] Preflight OK." : $"[Release] Preflight: {problems.Count} problem(s).");
            foreach (string p in problems)
                report.AppendLine("  ✗ " + p);
            foreach (string n in notes)
                report.AppendLine("  • " + n);

            if (problems.Count == 0)
                Debug.Log(report.ToString());
            else
                Debug.LogError(report.ToString());
            return problems.Count == 0;
        }

        [MenuItem("ShaurmaTycoon/Release/Build Yandex (WebGL)", priority = 202)]
        public static void BuildMenu()
        {
            Build();
        }

        /// <summary>Точка входа для -executeMethod: код выхода 1, если сборка не удалась.</summary>
        public static void BuildFromCommandLine()
        {
            bool ok = Build();
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Build()
        {
            if (!PrepareBuild())
                return false;

            BuildReport report = BuildPlayerTo(BuildFolder);
            if (report == null)
                return false;

            // Чекер модерации — только в тестовой сборке; в архив для консоли он попасть не должен.
            if (Directory.GetFiles(BuildFolder, "*", SearchOption.AllDirectories).Any(f => Path.GetFileName(f).IndexOf(DebugCheckerFile, StringComparison.OrdinalIgnoreCase) >= 0)
                || File.ReadAllText(Path.Combine(BuildFolder, "index.html")).Contains(DebugCheckerFile))
            {
                Debug.LogError($"[Release] {DebugCheckerFile} found in {BuildFolder} — the release build must not contain the debug checker.");
                return false;
            }

            TimeSpan buildTime = report.summary.totalTime;
            var packedAssets = report.packedAssets
                .SelectMany(p => p.contents)
                .GroupBy(c => c.sourceAssetPath)
                .Select(g => (Path: g.Key, Size: g.Sum(c => (long)c.packedSize)))
                .OrderByDescending(a => a.Size)
                .ToList();

            WriteSizeReport(buildTime, packedAssets);
            WriteArchive();
            return true;
        }

        /// <summary>
        /// Тестовая сборка с неофициальным чекером модерации (Tools/YandexDebugChecker, github.com/Nioris/yandex-games-debug-checker):
        /// те же настройки, что у релиза, но без сжатия (любой локальный сервер и sdk-dev-proxy отдают файлы без Content-Encoding)
        /// и с debugcheck.js сразу после /sdk.js — до YaGames.init() в шаблоне. Папка Builds/YandexDebug, архива нет: на модерацию не грузить.
        /// Панель в игре — Ctrl+Shift+2 три раза или YGDebugChecker.open() в консоли.
        /// </summary>
        [MenuItem("ShaurmaTycoon/Release/Build Yandex Debug (checker)", priority = 203)]
        public static void BuildDebugMenu()
        {
            BuildDebug();
        }

        public static bool BuildDebug()
        {
            if (!File.Exists(DebugCheckerSource))
            {
                Debug.LogError($"[Release] No {DebugCheckerSource} — see Tools/YandexDebugChecker/README.md.");
                return false;
            }
            if (!PrepareBuild())
                return false;

            BuildReport report;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                report = BuildPlayerTo(DebugBuildFolder);
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
                AssetDatabase.SaveAssets();
            }
            if (report == null)
                return false;

            const string sdkTag = "<script src=\"/sdk.js\"></script>";
            string indexPath = Path.Combine(DebugBuildFolder, "index.html");
            string index = File.ReadAllText(indexPath);
            if (!index.Contains(sdkTag))
            {
                Debug.LogError($"[Release] {indexPath}: no {sdkTag} to put the checker after.");
                return false;
            }
            File.WriteAllText(indexPath, index.Replace(sdkTag, sdkTag + "\n    <script src=\"" + DebugCheckerFile + "\"></script>"));
            File.Copy(DebugCheckerSource, Path.Combine(DebugBuildFolder, DebugCheckerFile), true);

            Debug.Log($"[Release] Debug build with the moderation checker: {DebugBuildFolder}. Not for upload to moderation. "
                + "In game: Ctrl+Shift+2 ×3 or YGDebugChecker.open(). Static scan doesn't see Build/*.framework.js — ad/SDK code checks there may show FAIL/N/V, trust the runtime ones.");
            return true;
        }

        private static bool PrepareBuild()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[Release] Can't switch the active build target to WebGL.");
                return false;
            }

            ApplyReleaseSettings();
            if (!Preflight())
            {
                Debug.LogError("[Release] Fix the preflight problems first.");
                return false;
            }
            return true;
        }

        private static BuildReport BuildPlayerTo(string folder)
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = folder,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Release] Build failed: {report.summary.result}, errors: {report.summary.totalErrors}.");
                return null;
            }
            return report;
        }

        /// <summary>
        /// Музыка и эмбиент для билда — свои MP3 в Assets/StreamingAssets/Audio (Unity копирует папку в билд как есть,
        /// игра догружает их после старта — AudioClipStore). Unity на WebGL кодирует любой клип в AAC ~128 кбит/с,
        /// а здесь: музыка 64 кбит/с стерео, эмбиент 40 кбит/с моно — звук в архиве в 2–3 раза легче.
        /// Нужен ffmpeg (winget install Gyan.FFmpeg); для самой сборки он не нужен — MP3 лежат в проекте.
        /// Запускать после замены или добавления клипов в Audio/Music и Audio/Ambient (Preflight напомнит).
        /// </summary>
        [MenuItem("ShaurmaTycoon/Release/Encode Web Audio (MP3)", priority = 199)]
        public static void EncodeWebAudio()
        {
            string ffmpeg = FindFfmpeg();
            if (ffmpeg == null)
            {
                Debug.LogError("[Release] ffmpeg not found. Install it: winget install Gyan.FFmpeg");
                return;
            }

            Directory.CreateDirectory(WebAudioFolder);
            int encoded = 0;
            foreach (string source in DeferredAudioSources())
            {
                bool music = source.Replace('\\', '/').StartsWith(ShaurmaTycoon.Core.Audio.AudioClipStore.DeferredFolders[0] + "/", StringComparison.Ordinal);
                string target = WebAudioPath(source);
                string args = music
                    ? $"-y -hide_banner -loglevel error -i \"{Path.GetFullPath(source)}\" -vn -ac 2 -ar 44100 -c:a libmp3lame -b:a {MusicKbps}k \"{Path.GetFullPath(target)}\""
                    : $"-y -hide_banner -loglevel error -i \"{Path.GetFullPath(source)}\" -vn -ac 1 -ar 32000 -c:a libmp3lame -b:a {AmbientKbps}k \"{Path.GetFullPath(target)}\"";

                using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ffmpeg, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                }))
                {
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        Debug.LogError($"[Release] ffmpeg failed on {source}: {error}");
                        return;
                    }
                }

                encoded++;
                Debug.Log($"[Release] {source} ({Mb(new FileInfo(source).Length)}) → {target} ({Mb(new FileInfo(target).Length)})");
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Release] Web audio encoded: {encoded} file(s) in {WebAudioFolder}.");
        }

        private static IEnumerable<string> DeferredAudioSources()
        {
            return AssetDatabase.FindAssets("t:AudioClip", ShaurmaTycoon.Core.Audio.AudioClipStore.DeferredFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(p => p, StringComparer.Ordinal);
        }

        private static string WebAudioPath(string sourcePath)
        {
            return WebAudioFolder + "/" + ShaurmaTycoon.Core.Audio.AudioClipStore.WebFileName(Path.GetFileNameWithoutExtension(sourcePath));
        }

        /// <summary>ffmpeg из PATH или из папки winget (PATH редактора обновится только после перезапуска Unity).</summary>
        private static string FindFfmpeg()
        {
            var candidates = new List<string>();
            foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
            {
                if (dir.Length > 0)
                    candidates.Add(Path.Combine(dir.Trim('"'), "ffmpeg.exe"));
            }

            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            candidates.Add(Path.Combine(local, "Microsoft", "WinGet", "Links", "ffmpeg.exe"));
            string packages = Path.Combine(local, "Microsoft", "WinGet", "Packages");
            if (Directory.Exists(packages))
            {
                foreach (string dir in Directory.GetDirectories(packages, "Gyan.FFmpeg*"))
                    candidates.AddRange(Directory.GetFiles(dir, "ffmpeg.exe", SearchOption.AllDirectories));
            }

            return candidates.FirstOrDefault(File.Exists);
        }

        /// <summary>
        /// Zip для консоли: файлы из Builds/Yandex с путями от корня билда, разделитель «/» — index.html в корне архива (1.22).
        /// Основной вес (.br) уже сжат Brotli — zip почти не уменьшает его, но и не мешает.
        /// </summary>
        private static void WriteArchive()
        {
            if (File.Exists(ArchivePath))
                File.Delete(ArchivePath);

            string root = Path.GetFullPath(BuildFolder);
            using (var archive = System.IO.Compression.ZipFile.Open(ArchivePath, System.IO.Compression.ZipArchiveMode.Create))
            {
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string entry = file.Substring(root.Length + 1).Replace('\\', '/');
                    archive.CreateEntryFromFile(file, entry, System.IO.Compression.CompressionLevel.Optimal);
                }
            }

            Debug.Log($"[Release] Archive for the developer console: {ArchivePath} ({Mb(new FileInfo(ArchivePath).Length)}).");
        }

        private static void WriteSizeReport(TimeSpan buildTime, List<(string Path, long Size)> assets)
        {
            var text = new StringBuilder();
            text.AppendLine($"Yandex WebGL build — {DateTime.Now:yyyy-MM-dd HH:mm}");
            text.AppendLine($"Build time: {buildTime:mm\\:ss}");
            text.AppendLine();

            long total = 0;
            text.AppendLine("Files (as uploaded, Brotli):");
            foreach (string file in Directory.GetFiles(BuildFolder, "*", SearchOption.AllDirectories).OrderByDescending(f => new FileInfo(f).Length))
            {
                long size = new FileInfo(file).Length;
                total += size;
                text.AppendLine($"  {Mb(size),9}  {file.Substring(BuildFolder.Length + 1).Replace('\\', '/')}");
            }
            text.AppendLine($"  {Mb(total),9}  TOTAL (target {Mb(TargetBytes)}) — {(total <= TargetBytes ? "OK" : "OVER")}");
            string streaming = Path.Combine(BuildFolder, "StreamingAssets");
            long deferred = Directory.Exists(streaming)
                ? Directory.GetFiles(streaming, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length)
                : 0;
            text.AppendLine($"  {Mb(total - deferred),9}  до старта игры (без StreamingAssets: звук {Mb(deferred)} догружается после запуска)");
            text.AppendLine();

            // Размер ассетов до сжатия Brotli — что именно занимает .data.
            long assetsTotal = assets.Sum(a => a.Size);
            text.AppendLine($"Packed assets (uncompressed, total {Mb(assetsTotal)}), top {HeaviestAssetsInReport}:");
            foreach (var asset in assets.Take(HeaviestAssetsInReport))
                text.AppendLine($"  {Mb(asset.Size),9}  {(string.IsNullOrEmpty(asset.Path) ? "(built-in)" : asset.Path)}");

            text.AppendLine();
            text.AppendLine("By type:");
            foreach (var group in assets.GroupBy(a => AssetKind(a.Path)).Select(g => new { Kind = g.Key, Size = g.Sum(a => a.Size) }).OrderByDescending(g => g.Size))
                text.AppendLine($"  {Mb(group.Size),9}  {group.Kind}");

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, text.ToString());
            Debug.Log($"[Release] Build OK: {Mb(total)} in {BuildFolder}. Report: {ReportPath}\n{text}");
        }

        private static string AssetKind(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "built-in";
            string ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".png":
                case ".jpg":
                case ".spriteatlasv2":
                    return "textures";
                case ".ogg":
                case ".wav":
                case ".mp3":
                    return "audio";
                case ".ttf":
                case ".otf":
                    return "fonts";
                case ".asset":
                    return path.Contains("Font") || path.Contains("TextMesh Pro") ? "fonts" : "assets";
                case ".shader":
                    return "shaders";
                default:
                    return ext.Length > 0 ? ext : "other";
            }
        }

        private static string Mb(long bytes)
        {
            return (bytes / 1048576.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " MB";
        }
    }
}
