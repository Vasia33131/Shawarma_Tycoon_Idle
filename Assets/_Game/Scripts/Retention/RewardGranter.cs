using System;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Prestige;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Выдаёт награды календаря и заданий: монеты (минуты дохода), купюры, бонусную репутацию.
    /// Монеты из наград не считаются заработком города (как мешки монет из магазина).
    /// </summary>
    public sealed class RewardGranter
    {
        private readonly Wallet _wallet;
        private readonly BillWallet _bills;
        private readonly PrestigeState _prestige;
        private readonly Func<double> _coinsPerMinute;

        /// <param name="coinsPerMinute">Монет за «минуту дохода» сейчас (см. RewardMath.CoinsPerMinute).</param>
        public RewardGranter(Wallet wallet, BillWallet bills, PrestigeState prestige, Func<double> coinsPerMinute)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _bills = bills ?? throw new ArgumentNullException(nameof(bills));
            _prestige = prestige;
            _coinsPerMinute = coinsPerMinute ?? throw new ArgumentNullException(nameof(coinsPerMinute));
        }

        /// <summary>Сколько выдаст награда прямо сейчас (монеты пересчитываются по текущему доходу).</summary>
        public RewardGrant Preview(RewardDefinition reward)
        {
            switch (reward.Kind)
            {
                case RewardKind.Coins:
                    return new RewardGrant(RewardKind.Coins, RewardMath.CoinsForMinutes(_coinsPerMinute(), reward.Amount));
                case RewardKind.Bills:
                case RewardKind.Reputation:
                    return new RewardGrant(reward.Kind, Math.Max(0, Math.Floor(reward.Amount)));
                default:
                    return new RewardGrant(reward.Kind, 0);
            }
        }

        /// <summary>Начисляет монеты, купюры или репутацию. Скин выдаёт вызывающий.</summary>
        public RewardGrant Grant(RewardDefinition reward)
        {
            RewardGrant grant = Preview(reward);
            switch (grant.Kind)
            {
                case RewardKind.Coins:
                    _wallet.Add(grant.Amount);
                    break;
                case RewardKind.Bills:
                    _bills.Add((long)grant.Amount, BillSource.Reward);
                    break;
                case RewardKind.Reputation:
                    _prestige?.AddBonusReputation((long)grant.Amount);
                    break;
            }
            return grant;
        }
    }
}
