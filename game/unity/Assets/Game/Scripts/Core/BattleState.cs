using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public sealed partial class BattleHero
    {
        public EnemyStatusState Status {get;}
        public int StatusWaitPenalty {get;private set;}
        public int ConsumeStatusActivationWait(){int n=StatusWaitPenalty;StatusWaitPenalty=0;return n;}
        public bool AddStatus(EnemyStatusDef effect){if(IsPanzer && ArmorActive && PanzerFireResistance && effect.kind=="burn"){effect=effect.Copy();effect.amount=Math.Max(1,effect.amount/2);}bool activated=Status.Add(effect);if(activated){TakeDamage(Status.ActivationDamage(effect.kind,MaxHitPoints));if(effect.kind=="electrified")StatusWaitPenalty+=20;}return activated;}
        public int FinishStatusAction(bool attacked){int wait=StatusWaitPenalty+(Status.Active("electrified")?20:0);StatusWaitPenalty=0;if(Status.Active("absent"))Heal((int)((long)MaxHitPoints*5/100));TakeDamage(Status.Dot(MaxHitPoints)+(attacked && Status.Active("fracture")?(int)((long)MaxHitPoints*5/100):0));if(!UsesBattleTurnDuration)Status.Tick();return wait;}
        public bool UsesBattleTurnDuration {get;set;}
        public int JobAllStatsPercent {get;set;}
        public int JobAttackPercent {get;set;}
        public int SongCriticalBonusBp {get;set;}
        public int JobSpeedPercent {get;set;}
        public int RelicAttackPercent {get;set;}
        public int RelicDefensePercent {get;set;}
        public int RelicSpeedPercent {get;set;}
        public int JobIncomingPercent {get;set;}=100;
        public int LifeMaxHpPercent {get;internal set;}
        public int OverhealLimitPercent {get;internal set;}
        internal void ClampJobHitPoints(){HitPoints=Math.Min(HitPoints,(int)Math.Min(int.MaxValue,(long)MaxHitPoints*(100+OverhealLimitPercent)/100));}
        public void TickBattleTurn(){TickTimedEffects();Status.Tick();RegenerateAtOwnerReady();}
        public string Id { get; }
        public int HitPoints { get; private set; }
        private readonly int baseMaxHitPoints;
        public int MaxHitPoints => IsPanzer?(ArmorActive?ArmorMaxHitPoints:FleshMaxHitPoints):OrdinaryMaxHp;
        public int BaseAttack { get; }
        public int BaseCriticalChanceBp { get; }
        private readonly int basePhysicalDefense,baseMagicDefense;
        public int PhysicalDefense => (int)Math.Min(int.MaxValue,(long)basePhysicalDefense*(100+JobAllStatsPercent+GeneralPhysicalDefensePercent+RelicDefensePercent+OopartStatPercent("physical-defense"))/100)/(IsPanzer && !ArmorActive?5:1);
        public int MagicDefense => (int)Math.Min(int.MaxValue,(long)baseMagicDefense*(100+JobAllStatsPercent+GeneralMagicDefensePercent+RelicDefensePercent+OopartStatPercent("magic-defense"))/100)/(IsPanzer && !ArmorActive?5:1);
        public int WeaponCriticalDamageBonus { get; }
        public string TraitId { get; }
        private readonly string[] visibleTraitIds;
        public bool HasVisibleTrait(string id) => id!=null && visibleTraitIds.Contains(id);
        public int Attack => (int)Math.Min(int.MaxValue,(long)BaseAttack*(Math.Max(1,100+EffectPercent("attack")-EffectPercent("attack-reduction")+JobAllStatsPercent+JobAttackPercent+GeneralAttackPercent+RelicAttackPercent+OopartStatPercent("attack")))/100*(Status.Active("burn")?80:100)/100*(Status.Active("sickness")?80:100)/100);
        private readonly int baseSpeed;
        public int Speed => Math.Max(1,baseSpeed*(100+JobAllStatsPercent+JobSpeedPercent+TimedSpeedPercent+GeneralSpeedPercent+RelicSpeedPercent+OopartStatPercent("speed"))/100*(Status.Active("frostbite")?80:100)/100);
        public int JobResource { get; private set; }
        public int JobResourceMax { get; internal set; }

        public BattleHero(string id, int hitPoints, int attack, int jobResourceMax, int speed = 100,int criticalChanceBp=0,int physicalDefense=0,int magicDefense=0,string traitId=null,int weaponCriticalDamageBonus=0,EnemyStatusResistanceDef[] statusResistances=null,string[] visibleTraits=null)
        {
            Status=new EnemyStatusState(statusResistances);TraitId=traitId;WeaponCriticalDamageBonus=weaponCriticalDamageBonus;
            visibleTraitIds=(visibleTraits??Array.Empty<string>()).Concat(string.IsNullOrEmpty(traitId)?Array.Empty<string>():new[]{traitId}).Distinct(StringComparer.Ordinal).ToArray();
            if(visibleTraitIds.Length>8 || visibleTraitIds.Any(string.IsNullOrEmpty))throw new ArgumentException("Invalid visible battle traits.");
            if(weaponCriticalDamageBonus<0)throw new ArgumentOutOfRangeException(nameof(weaponCriticalDamageBonus));
            if(hitPoints<=0 || attack<=0 || jobResourceMax<0 || physicalDefense<0 || magicDefense<0) throw new ArgumentOutOfRangeException("Invalid heroine stats.");
            basePhysicalDefense=physicalDefense;baseMagicDefense=magicDefense;
            if(speed<=0) throw new ArgumentOutOfRangeException(nameof(speed));
            if(criticalChanceBp<0 || criticalChanceBp>10000) throw new ArgumentOutOfRangeException(nameof(criticalChanceBp));
            BaseCriticalChanceBp=criticalChanceBp;
            baseSpeed=speed;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            baseMaxHitPoints = hitPoints;
            HitPoints = hitPoints;
            BaseAttack = attack;
            JobResourceMax = jobResourceMax;
        }

        private int permanentResourceRemainder; public int PermanentGaugePercent,PermanentRegenPercent,PermanentReductionPercent,PermanentDamagePercent;
        public void GainResource(int amount) {if(ResourceGainBlocked?.Invoke()==true)return;long bonus=(long)Math.Max(0,amount)*PermanentGaugePercent+permanentResourceRemainder;permanentResourceRemainder=(int)(bonus%100);JobResource=(int)Math.Min(JobResourceMax,bonus/100+(long)JobResource+Math.Max(0,amount)*(100L+(OopartValue?.Invoke("gauge")??0))/100);}
        public bool SpendResource(int amount)
        {
            if (amount < 0 || JobResource < amount) return false;
            JobResource -= amount;
            return true;
        }
        public bool IsAlive => HitPoints > 0;
        public void TakeDamage(int amount) { if(amount>0)Status.Remove("absent"); HitPoints = Math.Max(0, HitPoints - Math.Max(0, amount)); if(IsPanzer && ArmorActive && HitPoints==0)PanzerBreak();if(IsPanzer && !ArmorActive)FleshHitPoints=HitPoints;if(!IsAlive) timedEffects.Clear(); }
        public void Heal(int amount) { if (IsAlive && !(IsPanzer && ArmorActive) && !Status.Active("bleed")) {HitPoints = (int)Math.Min(Math.Max(MaxHitPoints,HitPoints), (long)HitPoints + Math.Max(0, amount));if(IsPanzer)FleshHitPoints=HitPoints;} }
        internal void Overheal(int amount){if(IsAlive && !(IsPanzer && ArmorActive) && !Status.Active("bleed")){OverhealLimitPercent=25;HitPoints=(int)Math.Min(int.MaxValue,Math.Min((long)MaxHitPoints*125/100,(long)HitPoints+Math.Max(0,amount)));if(IsPanzer)FleshHitPoints=Math.Min(FleshMaxHitPoints,HitPoints);}}
        internal void Revive(){if(!IsAlive){Status.ClearAll();HitPoints=Math.Max(1,MaxHitPoints/4);}}
    }

    public sealed class BattlePart
    {
        public string Id { get; }
        public int HitPoints { get; private set; }
        public bool IsBroken => HitPoints == 0;
        public void Heal(int amount){if(!IsBroken && !Status.Active("bleed"))HitPoints=(int)Math.Min(MaxHitPoints,(long)HitPoints+Math.Max(0,amount));}
        public string BreakEffectId { get; }
        public string Role { get; }
        public int PhysicalDefense { get; }
        public int MagicDefense { get; }
        public int WeaponCriticalDamageBonus { get; }
        public int MaxHitPoints { get; }
        public EnemyStatusState Status { get; }

        public BattlePart(string id, int hitPoints, string breakEffectId,int physicalDefense=0,int magicDefense=0,EnemyStatusResistanceDef[] resistances=null,string role=null)
        {
            Role=role;
            Status=new EnemyStatusState(resistances);
            if(physicalDefense<0 || magicDefense<0) throw new ArgumentOutOfRangeException("Defense cannot be negative.");
            PhysicalDefense=physicalDefense;MagicDefense=magicDefense;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            HitPoints = Math.Max(1, hitPoints);
            MaxHitPoints=HitPoints;
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
        public int BossPhysicalDefense { get; }
        public int BossMagicDefense { get; }
        public EnemyStatusState BossStatus { get; }
        public bool ReferenceStatusRules {get;set;}
        public int EnemyWaitPenalty {get;set;}
        public void HealBoss(int amount){if(!IsVictory && !BossStatus.Active("bleed"))BossHitPoints=(int)Math.Min(BossMaxHitPoints,(long)BossHitPoints+Math.Max(0,amount));}
        public AttributeResistanceDef[] AttributeResistances {get;set;}=Array.Empty<AttributeResistanceDef>();
        public EnemyStatusState EnemyStatus(string target) => target=="body"?BossStatus:Parts.Single(p=>p.Id==target).Status;
        public bool IsVictory => BossHitPoints == 0;
        public IReadOnlyList<BattleHero> Heroes { get; }
        public IReadOnlyList<BattlePart> Parts { get; }
        public IReadOnlyList<ChainModifier> TurnChainModifiers { get; private set; }

        public BattleState(int selectedLevel, IEnumerable<BattleHero> heroes, IEnumerable<BattlePart> parts, int bossHitPoints, int bossGaugeMax,int bossPhysicalDefense=0,int bossMagicDefense=0,EnemyStatusResistanceDef[] bossResistances=null)
        {
            BossStatus=new EnemyStatusState(bossResistances);
            if(bossPhysicalDefense<0 || bossMagicDefense<0) throw new ArgumentOutOfRangeException("Defense cannot be negative.");
            BossPhysicalDefense=bossPhysicalDefense;BossMagicDefense=bossMagicDefense;
            if (selectedLevel < MinimumLevel || selectedLevel > MaximumLevel) throw new ArgumentOutOfRangeException(nameof(selectedLevel));
            Heroes = heroes?.ToArray() ?? throw new ArgumentNullException(nameof(heroes));
            Parts = parts?.ToArray() ?? throw new ArgumentNullException(nameof(parts));
            if (Heroes.Count != PartySize || Heroes.Select(hero => hero.Id).Distinct().Count() != PartySize)
                throw new ArgumentException("Battle requires exactly five unique heroes.", nameof(heroes));
            if (Parts.Count < 4 || Parts.Count > 6 || Parts.Select(part => part.Id).Distinct().Count() != Parts.Count)
                throw new ArgumentException("Battle requires four to six unique parts.", nameof(parts));
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
            if(ReferenceStatusRules && applied>0)BossStatus.Remove("absent");
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
