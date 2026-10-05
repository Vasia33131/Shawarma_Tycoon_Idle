using System;
using System.Collections.Generic;
using ShaurmaTycoon.Core;

namespace ShaurmaTycoon.Save
{
    /// <summary>
    /// Загрузка и сохранение игры. Участники (ISaveParticipant) получают данные при регистрации
    /// и отдают своё состояние перед каждой записью.
    /// Автосейв — каждые AutosaveInterval секунд игрового времени, а также при потере фокуса, паузе и выходе
    /// (методы OnApplication* подключаются к GameEvents в GameBootstrap).
    /// </summary>
    public sealed class SaveService : ITickable
    {
        public const float AutosaveInterval = 10f;

        private readonly ISaveStorage _storage;
        private readonly ISaveSerializer _serializer;
        private readonly Func<DateTime> _utcNow;
        private readonly List<ISaveParticipant> _participants = new List<ISaveParticipant>(4);
        private float _timer;
        private bool _wiped;

        public SaveService(ISaveStorage storage, ISaveSerializer serializer, Func<DateTime> utcNow)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        }

        /// <summary>Сейв не разобрался — игра начинается заново. Аргумент — причина.</summary>
        public event Action<Exception> LoadFailed;

        /// <summary>Хранилище не смогло записать сейв.</summary>
        public event Action SaveFailed;

        public SaveData Data { get; private set; } = new SaveData();
        public bool IsLoaded { get; private set; }

        /// <summary>Сколько раз сейв отправлен в хранилище (для отладки и тестов).</summary>
        public int SaveCount { get; private set; }

        public void Load(Action onLoaded)
        {
            _storage.Load(json =>
            {
                Data = Parse(json);
                IsLoaded = true;
                _timer = 0f;
                onLoaded?.Invoke();
            });
        }

        /// <summary>Подключает участника и сразу восстанавливает его состояние из загруженных данных.</summary>
        public void Register(ISaveParticipant participant)
        {
            if (participant == null || _participants.Contains(participant))
                return;

            _participants.Add(participant);
            participant.Apply(Data);
        }

        /// <summary>Отключает участника, предварительно забрав его последнее состояние в Data.</summary>
        public void Unregister(ISaveParticipant participant)
        {
            if (!_participants.Remove(participant))
                return;
            participant.Capture(Data);
        }

        public void SaveNow()
        {
            if (!IsLoaded || _wiped)
                return;

            for (int i = 0; i < _participants.Count; i++)
                _participants[i].Capture(Data);

            Data.Version = SaveData.CurrentVersion;
            Data.SavedAtUtcTicks = _utcNow().Ticks;
            _timer = 0f;
            SaveCount++;

            _storage.Save(_serializer.Serialize(Data), ok =>
            {
                if (!ok)
                    SaveFailed?.Invoke();
            });
        }

        /// <summary>Сейв стёрт отладочным окном — до перезапуска игра больше ничего не записывает.</summary>
        public bool IsWiped => _wiped;

        /// <summary>
        /// Отладка (этап 11): записывает пустой сейв и выключает дальнейшие записи, чтобы текущее состояние
        /// не перезаписало его. Новая игра начнётся после перезапуска.
        /// </summary>
        public void Wipe()
        {
            _wiped = true;
            var empty = new SaveData { SavedAtUtcTicks = _utcNow().Ticks };
            _storage.Save(_serializer.Serialize(empty), ok =>
            {
                if (!ok)
                    SaveFailed?.Invoke();
            });
        }

        public void Tick(float deltaTime)
        {
            if (!IsLoaded)
                return;

            _timer += deltaTime;
            if (_timer >= AutosaveInterval)
                SaveNow();
        }

        public void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                SaveNow();
        }

        public void OnApplicationPaused(bool paused)
        {
            if (paused)
                SaveNow();
        }

        public void OnApplicationQuitting()
        {
            SaveNow();
        }

        private SaveData Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new SaveData();

            try
            {
                return SaveMigrator.Migrate(_serializer.Deserialize(json));
            }
            catch (Exception e)
            {
                LoadFailed?.Invoke(e);
                return new SaveData();
            }
        }
    }
}
