using System;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_EDITOR
using UnityEngine.Networking;
#endif

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Музыка и петли эмбиента — не в стартовом файле билда, а MP3 в StreamingAssets/Audio (меню Encode Web Audio):
    /// игра открывается без них, клип догружается при первом запросе. Unity на WebGL кодирует любой клип в AAC ~128 кбит/с,
    /// свои MP3 (музыка 64 кбит/с, эмбиент 40 кбит/с моно) в 2–3 раза легче. Клип создаётся распакованным — играет через
    /// Web Audio, без HTML5-плеера браузера (требования 1.6.1.6, 1.6.2.5). В редакторе клип берётся из Assets/_Game/Audio.
    /// </summary>
    public sealed class AudioClipStore : ITickable
    {
        /// <summary>Папка MP3 внутри StreamingAssets.</summary>
        public const string WebFolder = "Audio";

        /// <summary>Расширение файлов в StreamingAssets/Audio.</summary>
        public const string WebExtension = ".mp3";

        /// <summary>Папки клипов, которые идут отдельными файлами (остальные звуки — в стартовом файле).</summary>
        public static readonly string[] DeferredFolders = { "Assets/_Game/Audio/Music", "Assets/_Game/Audio/Ambient" };

        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>(8);
        private readonly Dictionary<string, List<Action<AudioClip>>> _pending = new Dictionary<string, List<Action<AudioClip>>>(4);
        private readonly List<KeyValuePair<string, AudioClip>> _decoding = new List<KeyValuePair<string, AudioClip>>(2);

        /// <summary>Имя файла клипа в StreamingAssets/Audio.</summary>
        public static string WebFileName(string clipName)
        {
            return clipName + WebExtension;
        }

        /// <summary>
        /// Клип по имени файла без расширения (music_cozy_01). onLoaded вызывается один раз: сразу, если клип уже есть,
        /// иначе после загрузки; null — имени нет или загрузить не удалось (слой молчит, игра продолжается).
        /// </summary>
        public void Get(string clipName, Action<AudioClip> onLoaded)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                onLoaded?.Invoke(null);
                return;
            }

            if (_clips.TryGetValue(clipName, out AudioClip clip) && clip != null)
            {
                onLoaded?.Invoke(clip);
                return;
            }

            if (_pending.TryGetValue(clipName, out List<Action<AudioClip>> waiting))
            {
                waiting.Add(onLoaded);
                return;
            }

            _pending[clipName] = new List<Action<AudioClip>>(2) { onLoaded };
            Load(clipName);
        }

        /// <summary>
        /// Клип больше не играет (музыка прежнего города): в билде он удаляется целиком — распакованный трек занимает ~50 МБ,
        /// а данные скачанного клипа заново не загрузить; при следующем Get файл скачается снова (обычно из кэша браузера).
        /// В редакторе клип — ассет проекта: выгружаются только его данные.
        /// </summary>
        public void Release(AudioClip clip)
        {
            if (clip == null)
                return;
#if UNITY_EDITOR
            clip.UnloadAudioData();
#else
            if (_clips.TryGetValue(clip.name, out AudioClip cached) && cached == clip)
                _clips.Remove(clip.name);
            UnityEngine.Object.Destroy(clip);
#endif
        }

        /// <summary>
        /// Скачанный клип браузер распаковывает асинхронно (decodeAudioData); Play до конца распаковки молча не играет.
        /// Поэтому клип отдаётся только когда он загружен.
        /// </summary>
        public void Tick(float deltaTime)
        {
            for (int i = _decoding.Count - 1; i >= 0; i--)
            {
                AudioClip clip = _decoding[i].Value;
                AudioDataLoadState state = clip != null ? clip.loadState : AudioDataLoadState.Failed;
                if (state == AudioDataLoadState.Loading || state == AudioDataLoadState.Unloaded)
                    continue;

                string clipName = _decoding[i].Key;
                _decoding.RemoveAt(i);
                if (state == AudioDataLoadState.Failed)
                {
                    Debug.LogWarning($"[Audio] Can't decode '{clipName}'.");
                    if (clip != null)
                        UnityEngine.Object.Destroy(clip);
                    clip = null;
                }
                Finish(clipName, clip);
            }
        }

        private void Finish(string clipName, AudioClip clip)
        {
            if (clip != null)
            {
                clip.name = clipName;
                _clips[clipName] = clip;
            }

            if (!_pending.TryGetValue(clipName, out List<Action<AudioClip>> waiting))
                return;
            _pending.Remove(clipName);
            for (int i = 0; i < waiting.Count; i++)
            {
                try
                {
                    waiting[i]?.Invoke(clip);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

#if UNITY_EDITOR
        private void Load(string clipName)
        {
            AudioClip clip = null;
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets(clipName + " t:AudioClip", DeferredFolders))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == clipName)
                {
                    clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    break;
                }
            }

            if (clip == null)
                Debug.LogWarning($"[Audio] Clip '{clipName}' not found in {string.Join(", ", DeferredFolders)}.");
            Finish(clipName, clip);
        }
#else
        private void Load(string clipName)
        {
            string url = Application.streamingAssetsPath + "/" + WebFolder + "/" + WebFileName(clipName);
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
            var handler = (DownloadHandlerAudioClip)request.downloadHandler;
            // Не потоковый и не сжатый в памяти: только так клип распаковывается через Web Audio, без HTML5 <audio>.
            handler.streamAudio = false;
            handler.compressed = false;
            request.SendWebRequest().completed += _ =>
            {
                AudioClip clip = null;
                if (request.result == UnityWebRequest.Result.Success)
                    clip = DownloadHandlerAudioClip.GetContent(request);
                else
                    Debug.LogWarning($"[Audio] Can't load '{url}': {request.error}");
                request.Dispose();
                if (clip == null)
                {
                    Finish(clipName, null);
                    return;
                }

                if (clip.loadState == AudioDataLoadState.Unloaded)
                    clip.LoadAudioData();
                _decoding.Add(new KeyValuePair<string, AudioClip>(clipName, clip));
            };
        }
#endif
    }
}
