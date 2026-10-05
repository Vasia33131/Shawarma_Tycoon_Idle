using NUnit.Framework;
using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Tests
{
    public class SettingsTests
    {
        [Test]
        public void NewSettings_HaveSoundAndMusicOn()
        {
            var settings = new GameSettings();

            Assert.IsTrue(settings.SoundOn);
            Assert.IsTrue(settings.MusicOn);
        }

        [Test]
        public void Set_RaisesChangedOnlyWhenValueChanges()
        {
            var settings = new GameSettings();
            int changed = 0;
            settings.Changed += () => changed++;

            settings.SetSound(true);
            settings.SetMusic(true);
            Assert.AreEqual(0, changed);

            settings.SetSound(false);
            settings.SetMusic(false);
            Assert.AreEqual(2, changed);
            Assert.IsFalse(settings.SoundOn);
            Assert.IsFalse(settings.MusicOn);
        }

        [Test]
        public void Participant_RoundTrip_KeepsValues()
        {
            var source = new GameSettings();
            source.SetSound(false);
            var data = new SaveData();
            new SettingsSaveParticipant(source).Capture(data);

            var restored = new GameSettings();
            new SettingsSaveParticipant(restored).Apply(data);

            Assert.IsFalse(restored.SoundOn);
            Assert.IsTrue(restored.MusicOn);
        }

        [Test]
        public void OldSave_WithoutSettings_GetsEverythingOn()
        {
            var data = new SaveData { Settings = null };

            SaveData migrated = SaveMigrator.Migrate(data);
            var settings = new GameSettings();
            settings.SetMusic(false);
            new SettingsSaveParticipant(settings).Apply(migrated);

            Assert.IsNotNull(migrated.Settings);
            Assert.IsTrue(settings.SoundOn);
            Assert.IsTrue(settings.MusicOn);
            Assert.IsTrue(settings.EffectsOn);
        }

        [Test]
        public void Effects_OnByDefault_AndSurviveRoundTrip()
        {
            var source = new GameSettings();
            Assert.IsTrue(source.EffectsOn);
            int changed = 0;
            source.Changed += () => changed++;

            source.SetEffects(false);
            source.SetEffects(false);
            Assert.AreEqual(1, changed);

            var data = new SaveData();
            new SettingsSaveParticipant(source).Capture(data);
            var restored = new GameSettings();
            new SettingsSaveParticipant(restored).Apply(data);

            Assert.IsFalse(restored.EffectsOn);
            Assert.IsTrue(restored.SoundOn);
        }
    }
}
