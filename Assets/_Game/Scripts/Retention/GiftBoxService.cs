using System;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Подарок-ящик (GDD §8): раз в cooldown по серверному времени, за досмотренную рекламу — монеты на N минут дохода.
    /// Если часы ушли назад, «последнее открытие» прижимается к now (кулдаун не растягивается навсегда).
    /// Сам является участником сейва (SaveData.Retention.GiftLastClaimUtcTicks).
    /// </summary>
    public sealed class GiftBoxService : ISaveParticipant
    {
        private readonly TimeSpan _cooldown;
        private readonly double _incomeMinutes;
        private readonly Wallet _wallet;
        private readonly Func<double> _coinsPerMinute;
        private readonly IPlatformService _platform;
        private readonly Func<DateTime> _utcNow;
        private readonly Action _saveNow;

        public GiftBoxService(TimeSpan cooldown, double incomeMinutes, Wallet wallet, Func<double> coinsPerMinute, IPlatformService platform,
            Func<DateTime> utcNow, Action saveNow)
        {
            _cooldown = cooldown > TimeSpan.Zero ? cooldown : TimeSpan.Zero;
            _incomeMinutes = Math.Max(0, incomeMinutes);
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _coinsPerMinute = coinsPerMinute ?? throw new ArgumentNullException(nameof(coinsPerMinute));
            _platform = platform;
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            _saveNow = saveNow;
        }

        /// <summary>Аргумент — сколько монет выдано.</summary>
        public event Action<double> Opened;

        public long LastClaimUtcTicks { get; private set; }
        public bool IsShowing { get; private set; }
        public DateTime Now => _utcNow();

        /// <summary>Сколько монет даст подарок сейчас.</summary>
        public double Reward => RewardMath.CoinsForMinutes(_coinsPerMinute(), _incomeMinutes);

        public bool IsReady => TimeLeft <= TimeSpan.Zero;
        public bool CanOpen => _platform != null && !IsShowing && IsReady;

        public TimeSpan TimeLeft
        {
            get
            {
                DateTime now = _utcNow();
                if (LastClaimUtcTicks > now.Ticks)
                    LastClaimUtcTicks = now.Ticks;
                if (LastClaimUtcTicks == 0)
                    return TimeSpan.Zero;

                TimeSpan left = new DateTime(LastClaimUtcTicks, DateTimeKind.Utc) + _cooldown - now;
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        /// <returns>false — реклама не запускалась (кулдаун, уже показывается, нет платформы).</returns>
        public bool TryOpen(Action<bool> onDone)
        {
            if (!CanOpen)
                return false;

            IsShowing = true;
            _platform.ShowRewarded(completed =>
            {
                IsShowing = false;
                bool rewarded = completed && IsReady;
                if (rewarded)
                {
                    double coins = Reward;
                    LastClaimUtcTicks = _utcNow().Ticks;
                    _wallet.Add(coins);
                    _saveNow?.Invoke();
                    Opened?.Invoke(coins);
                }
                onDone?.Invoke(rewarded);
            });
            return true;
        }

        /// <summary>Сбросить кулдаун (отладка).</summary>
        public void ResetTimer()
        {
            LastClaimUtcTicks = 0;
        }

        public void Capture(SaveData data)
        {
            RetentionSave save = data.Retention ?? (data.Retention = new RetentionSave());
            save.GiftLastClaimUtcTicks = LastClaimUtcTicks;
        }

        public void Apply(SaveData data)
        {
            LastClaimUtcTicks = data.Retention != null ? Math.Max(0, data.Retention.GiftLastClaimUtcTicks) : 0;
        }
    }
}
