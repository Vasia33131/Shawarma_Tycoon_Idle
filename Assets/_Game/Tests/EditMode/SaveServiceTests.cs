using System;
using System.Globalization;
using NUnit.Framework;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Tests
{
    public class SaveServiceTests
    {
        /// <summary>Хранилище в памяти.</summary>
        private sealed class MemoryStorage : ISaveStorage
        {
            public string Json;
            public int Writes;

            public void Save(string json, Action<bool> onComplete)
            {
                Json = json;
                Writes++;
                onComplete?.Invoke(true);
            }

            public void Load(Action<string> onLoaded)
            {
                onLoaded?.Invoke(Json);
            }
        }

        /// <summary>Простой формат «coins=N» вместо JsonUtility (тесты без Unity).</summary>
        private sealed class CoinsSerializer : ISaveSerializer
        {
            private const string Prefix = "coins=";

            public string Serialize(SaveData data)
            {
                return Prefix + data.Coins.ToString(CultureInfo.InvariantCulture);
            }

            public SaveData Deserialize(string json)
            {
                if (!json.StartsWith(Prefix, StringComparison.Ordinal))
                    throw new FormatException("bad save");
                return new SaveData { Coins = double.Parse(json.Substring(Prefix.Length), CultureInfo.InvariantCulture) };
            }
        }

        private sealed class CoinsParticipant : ISaveParticipant
        {
            public double Coins;
            public int Applies;

            public void Capture(SaveData data)
            {
                data.Coins = Coins;
            }

            public void Apply(SaveData data)
            {
                Coins = data.Coins;
                Applies++;
            }
        }

        private static readonly DateTime Now = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

        private MemoryStorage _storage;
        private SaveService _service;

        [SetUp]
        public void SetUp()
        {
            _storage = new MemoryStorage();
            _service = new SaveService(_storage, new CoinsSerializer(), () => Now);
        }

        [Test]
        public void Load_NoSave_StartsNewGame()
        {
            bool loaded = false;
            _service.Load(() => loaded = true);

            Assert.IsTrue(loaded);
            Assert.IsTrue(_service.IsLoaded);
            Assert.AreEqual(0, _service.Data.Coins);
        }

        [Test]
        public void Register_AppliesLoadedData()
        {
            _storage.Json = "coins=250";
            _service.Load(null);
            var participant = new CoinsParticipant();

            _service.Register(participant);

            Assert.AreEqual(250, participant.Coins);
            Assert.AreEqual(1, participant.Applies);
        }

        [Test]
        public void SaveNow_CapturesParticipants_AndStampsTime()
        {
            _service.Load(null);
            var participant = new CoinsParticipant();
            _service.Register(participant);
            participant.Coins = 777;

            _service.SaveNow();

            Assert.AreEqual("coins=777", _storage.Json);
            Assert.AreEqual(Now.Ticks, _service.Data.SavedAtUtcTicks);
        }

        [Test]
        public void SaveNow_BeforeLoad_DoesNothing()
        {
            _service.SaveNow();

            Assert.AreEqual(0, _storage.Writes);
        }

        [Test]
        public void Tick_SavesEveryAutosaveInterval()
        {
            _service.Load(null);

            _service.Tick(SaveService.AutosaveInterval - 0.5f);
            Assert.AreEqual(0, _storage.Writes);

            _service.Tick(1f);
            Assert.AreEqual(1, _storage.Writes);

            _service.Tick(SaveService.AutosaveInterval - 0.5f);
            Assert.AreEqual(1, _storage.Writes);
        }

        [Test]
        public void RequestSoon_SavesAfterShortDelay_AndMergesBursts()
        {
            _service.Load(null);

            _service.RequestSoon();
            _service.Tick(SaveService.SoonDelay * 0.5f);
            _service.RequestSoon();
            Assert.AreEqual(0, _storage.Writes, "Not instantly: a burst of purchases is one write.");

            _service.Tick(SaveService.SoonDelay * 0.6f);
            Assert.AreEqual(1, _storage.Writes);

            _service.Tick(SaveService.AutosaveInterval - 0.5f);
            Assert.AreEqual(1, _storage.Writes, "The regular autosave timer restarts after the write.");
        }

        [Test]
        public void FocusLost_Pause_Quit_SaveImmediately()
        {
            _service.Load(null);

            _service.OnApplicationFocusChanged(true);
            _service.OnApplicationPaused(false);
            Assert.AreEqual(0, _storage.Writes);

            _service.OnApplicationFocusChanged(false);
            _service.OnApplicationPaused(true);
            _service.OnApplicationQuitting();
            Assert.AreEqual(3, _storage.Writes);
        }

        [Test]
        public void CorruptSave_RaisesLoadFailed_AndStartsNewGame()
        {
            _storage.Json = "{ garbage";
            Exception error = null;
            _service.LoadFailed += e => error = e;

            _service.Load(null);

            Assert.IsNotNull(error);
            Assert.IsTrue(_service.IsLoaded);
            Assert.AreEqual(0, _service.Data.Coins);
        }

        [Test]
        public void Unregister_CapturesLastState()
        {
            _service.Load(null);
            var participant = new CoinsParticipant();
            _service.Register(participant);
            participant.Coins = 99;

            _service.Unregister(participant);

            Assert.AreEqual(99, _service.Data.Coins);
        }
    }
}
