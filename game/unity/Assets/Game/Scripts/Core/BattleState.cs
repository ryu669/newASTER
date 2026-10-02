using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public sealed partial class BattleHero
    {
        public string Id { get; }
        public int HitPoints { get; private set; }
        public int MaxHitPoints { get; }
        public int BaseAttack { get; }
        public int Attack => (int)Math.Min(int.MaxValue,(long)BaseAttack*(100+EffectPercent("attack"))/100);
        public int Speed { get; }
        public int JobResource { get; private set; }
        public int JobResourceMax { get; }

        public BattleHero(string id, int hitPoints, int attack, int jobResourceMax, int speed = 100)
        {
            if(speed<=0) throw new ArgumentOutOfRangeException(nameof(speed));
            Speed=speed;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            MaxHitPoints = hitPoints;
            HitPoints = hitPoints;
            BaseAttack = attack;
            JobResourceMax = jobResourceMax;
        }

        public void GainResource(int amount) => JobResource = Math.Min(JobResourceMax, JobResource + Math.Max(0, amount));
        public bool SpendResource(int amount)
        {
            if (amount < 0 || JobResource < amount) return false;
            JobResource -= amount;
            return true;
        }
        public bool IsAlive => HitPoints > 0;
        public void TakeDamage(int amount) { HitPoints = Math.Max(0, HitPoints - Math.Max(0, amount)); if(!IsAlive) timedEffects.Clear(); }
        public void Heal(int amount) { if (IsAlive) HitPoints = (int)Math.Min(MaxHitPoints, (long)HitPoints + Math.Max(0, amount)); }
    }

    public sealed class BattlePart
    {
        public string Id { get; }
        public int HitPoints { get; private set; }
        public bool IsBroken => HitPoints == 0;
        public string BreakEffectId { get; }

        public BattlePart(string id, int hitPoints, string breakEffectId)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            HitPoints = Math.Max(1, hitPoints);
            BreakEffectId = breakEffectId ?? string.Empty;
        }

        public bool ApplyDamage(int damage)
        {
            if (IsBroken) return false;
            HitPoints = Math.Max(0, HitPoints - Math.Max(0, damage));
            return IsBroken;
        }
    }

    public readonly struct ChainModifier
    {
        public string HeroId { get; }
        public decimal AdditiveRate { get; }
        public ChainModifier(string heroId, decimal additiveRate) { HeroId = heroId; AdditiveRate = additiveRate; }
    }

    /// <summary>描画とUnity APIに依存しない、正式版縦切りの戦闘状態。</summary>
    public sealed class BattleState
    {
        public const int MinimumLevel = 1;
        public const int MaximumLevel = 50;
        public const int UltimateUnlockLevel = 45;
        public const int PartySize = 5;
        public const int VerticalSlicePartCount = 4;

        public int SelectedLevel { get; }
        public bool UltimateUnlocked => SelectedLevel >= UltimateUnlockLevel;
        public int BossGauge { get; private set; }
        public int BossGaugeMax { get; }
        public int BossHitPoints { get; private set; }
        public int BossMaxHitPoints { get; }
        public bool IsVictory => BossHitPoints == 0;
        public IReadOnlyList<BattleHero> Heroes { get; }
        public IReadOnlyList<BattlePart> Parts { get; }
        public IReadOnlyList<ChainModifier> TurnChainModifiers { get; private set; }

        public BattleState(int selectedLevel, IEnumerable<BattleHero> heroes, IEnumerable<BattlePart> parts, int bossHitPoints, int bossGaugeMax)
        {
            if (selectedLevel < MinimumLevel || selectedLevel > MaximumLevel) throw new ArgumentOutOfRangeException(nameof(selectedLevel));
            Heroes = heroes?.ToArray() ?? throw new ArgumentNullException(nameof(heroes));
            Parts = parts?.ToArray() ?? throw new ArgumentNullException(nameof(parts));
            if (Heroes.Count != PartySize || Heroes.Select(hero => hero.Id).Distinct().Count() != PartySize)
                throw new ArgumentException("Battle requires exactly five unique heroes.", nameof(heroes));
            if (Parts.Count != VerticalSlicePartCount || Parts.Select(part => part.Id).Distinct().Count() != VerticalSlicePartCount)
                throw new ArgumentException("Vertical slice requires exactly four unique parts.", nameof(parts));
            SelectedLevel = selectedLevel;
            BossMaxHitPoints = Math.Max(1, bossHitPoints);
            BossHitPoints = BossMaxHitPoints;
            BossGaugeMax = Math.Max(1, bossGaugeMax);
            TurnChainModifiers = Array.Empty<ChainModifier>();
        }

        public void BeginTurn(int seed, decimal turnBonusChance)
        {
            if (turnBonusChance < 0m || turnBonusChance > 1m) throw new ArgumentOutOfRangeException(nameof(turnBonusChance));
            var random = new Random(seed);
            var modifiers = Heroes.Select(hero => new ChainModifier(hero.Id, random.NextDouble() < (double)turnBonusChance ? 0.10m : 0m)).ToArray();
            foreach (var index in Enumerable.Range(0, Heroes.Count).OrderBy(_ => random.Next()).Take(2))
                modifiers[index] = new ChainModifier(modifiers[index].HeroId, modifiers[index].AdditiveRate + 0.05m);
            TurnChainModifiers = modifiers;
        }

        public bool BreakPart(string partId, int damage)
        {
            var part = Parts.SingleOrDefault(item => item.Id == partId) ?? throw new ArgumentException("Unknown part.", nameof(partId));
            var brokenNow = part.ApplyDamage(damage);
            if (brokenNow && part.BreakEffectId == "gauge-down") BossGauge = Math.Max(0, BossGauge - 1);
            return brokenNow;
        }

        public int ApplyBossDamage(int damage)
        {
            var applied = Math.Min(BossHitPoints, Math.Max(0, damage));
            BossHitPoints -= applied;
            return applied;
        }

        public void AdvanceBossGauge(int amount) => BossGauge = Math.Min(BossGaugeMax, BossGauge + Math.Max(0, amount));
        public void ReduceBossGauge(int amount) => BossGauge = Math.Max(0, BossGauge - Math.Max(0, amount));
        public bool TryConsumeUltimateGauge()
        {
            if (!UltimateUnlocked || BossGauge < BossGaugeMax) return false;
            return TryConsumeMajorGauge();
        }

        public bool TryConsumeMajorGauge()
        {
            if (BossGauge < BossGaugeMax) return false;
            BossGauge = 0;
            return true;
        }
    }
}
