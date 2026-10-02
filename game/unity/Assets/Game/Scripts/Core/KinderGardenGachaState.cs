using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public sealed class KinderGardenBanner
    {
        public const decimal HeroineTotalRate = 0.03m;
        public const int ExchangeThreshold = 100;
        public string Id { get; }
        public IReadOnlyList<string> HeroineIds { get; }
        public IReadOnlyList<string> NonHeroineRewardIds { get; }
        public decimal HeroineRatePerTarget => HeroineTotalRate / HeroineIds.Count;

        public KinderGardenBanner(string id, IEnumerable<string> heroineIds, IEnumerable<string> nonHeroineRewardIds)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A banner id is required.", nameof(id));
            HeroineIds = (heroineIds ?? throw new ArgumentNullException(nameof(heroineIds))).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
            NonHeroineRewardIds = (nonHeroineRewardIds ?? throw new ArgumentNullException(nameof(nonHeroineRewardIds))).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
            if (HeroineIds.Count == 0 || NonHeroineRewardIds.Count == 0) throw new ArgumentException("Both reward pools must contain entries.");
            Id = id;
        }
    }

    public sealed class GachaDrawResult
    {
        public string RewardId { get; }
        public bool IsHeroine { get; }
        public GachaDrawResult(string rewardId, bool isHeroine) { RewardId = rewardId; IsHeroine = isHeroine; }
    }

    /// <summary>キンダーガーデンの回数・任意交換状態。石の所持と消費は経済台帳から原子的に接続する。</summary>
    public sealed class KinderGardenGachaState
    {
        private readonly List<GachaDrawResult> _history = new List<GachaDrawResult>();
        public int TotalDrawCount => _history.Count;
        public int ExchangedHeroineCount { get; private set; }
        public int AvailableExchanges => TotalDrawCount / KinderGardenBanner.ExchangeThreshold - ExchangedHeroineCount;
        public IReadOnlyList<GachaDrawResult> History => _history;

        /// <summary>0以上1未満の乱数を受け取ることで、確率表示と実抽選を一致させ、試験も再現可能にする。</summary>
        public GachaDrawResult Draw(KinderGardenBanner banner, decimal roll)
        {
            if (banner == null) throw new ArgumentNullException(nameof(banner));
            if (roll < 0m || roll >= 1m) throw new ArgumentOutOfRangeException(nameof(roll));
            var heroine = roll < KinderGardenBanner.HeroineTotalRate;
            IReadOnlyList<string> pool = heroine ? banner.HeroineIds : banner.NonHeroineRewardIds;
            var normalized = heroine ? roll / KinderGardenBanner.HeroineTotalRate
                : (roll - KinderGardenBanner.HeroineTotalRate) / (1m - KinderGardenBanner.HeroineTotalRate);
            var index = Math.Min(pool.Count - 1, (int)(normalized * pool.Count));
            var result = new GachaDrawResult(pool[index], heroine);
            _history.Add(result);
            return result;
        }

        public bool TryExchange(KinderGardenBanner banner, string heroineId)
        {
            if (banner == null) throw new ArgumentNullException(nameof(banner));
            if (AvailableExchanges <= 0 || !banner.HeroineIds.Contains(heroineId)) return false;
            ExchangedHeroineCount++;
            return true;
        }
    }
}
