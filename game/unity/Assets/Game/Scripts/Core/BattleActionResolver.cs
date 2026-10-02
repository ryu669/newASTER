using System;
using System.Linq;

namespace NewAster.Core
{
    public sealed class BattleSkill
    {
        public string Id { get; }
        public decimal Power { get; }
        public int ResourceCost { get; }
        public int SelfHealingBaseAttackPercent { get; }
        public int SelfDamageMaxHpPercent { get; }
        public int? AttackSnapshot { get; }
        public int CriticalChanceBp { get; }
        public int CriticalMultiplierPercent { get; }
        public int DamageCap { get; }
        public string DamageType { get; }
        public int IgnoreDefenseBp { get; }

        public BattleSkill(string id, decimal power, int resourceCost,int selfHealingBaseAttackPercent=0,int selfDamageMaxHpPercent=0,int? attackSnapshot=null,int criticalChanceBp=0,int criticalMultiplierPercent=150,int damageCap=0,string damageType="physical",int ignoreDefenseBp=0)
        {
            if(damageType!="physical" && damageType!="magic") throw new ArgumentException("Unknown damage type.");
            if(ignoreDefenseBp<0 || ignoreDefenseBp>10000) throw new ArgumentOutOfRangeException(nameof(ignoreDefenseBp));
            DamageType=damageType;IgnoreDefenseBp=ignoreDefenseBp;
            if (power <= 0m) throw new ArgumentOutOfRangeException(nameof(power));
            if (resourceCost < 0) throw new ArgumentOutOfRangeException(nameof(resourceCost));
            if(selfHealingBaseAttackPercent<0 || selfHealingBaseAttackPercent>1000 || selfDamageMaxHpPercent<0 || selfDamageMaxHpPercent>100) throw new ArgumentOutOfRangeException("Attack follow-up percentage");
            if(attackSnapshot.HasValue && attackSnapshot.Value<=0) throw new ArgumentOutOfRangeException(nameof(attackSnapshot));
            if(criticalChanceBp<0 || criticalChanceBp>10000 || criticalMultiplierPercent<100 || criticalMultiplierPercent>2000 || damageCap<0) throw new ArgumentOutOfRangeException("Invalid critical profile.");
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Power = power;
            ResourceCost = resourceCost;
            SelfHealingBaseAttackPercent=selfHealingBaseAttackPercent;
            SelfDamageMaxHpPercent=selfDamageMaxHpPercent;
            AttackSnapshot=attackSnapshot;
            CriticalChanceBp=criticalChanceBp;CriticalMultiplierPercent=criticalMultiplierPercent;DamageCap=damageCap;
        }
    }

    public sealed class BattleActionResult
    {
        public bool Accepted { get; }
        public int Damage { get; }
        public bool PartBroken { get; }
        public bool Victory { get; }
        public string Reason { get; }
        public int SelfHealing { get; }
        public int SelfDamage { get; }
        public bool Critical { get; }
        public int CriticalRoll { get; }

        public BattleActionResult(bool accepted, int damage, bool partBroken, bool victory, string reason,int selfHealing=0,int selfDamage=0,bool critical=false,int criticalRoll=-1)
        {
            Accepted = accepted;
            Damage = damage;
            PartBroken = partBroken;
            Victory = victory;
            Reason = reason;
            SelfHealing=selfHealing; SelfDamage=selfDamage;
            Critical=critical;CriticalRoll=criticalRoll;
        }
    }

    /// <summary>スキルのコスト、対象、ダメージ、部位破壊、勝利を一度だけ解決する。</summary>
    public static class BattleActionResolver
    {
        // Shared by preview and resolution. This is a newASTER rule, not a source-game formula.
        public static int CalculateDamage(BattleState battle,BattleHero hero,BattleSkill skill,string targetId,bool critical=false)
        {
            if(battle==null || hero==null || skill==null) throw new ArgumentNullException("Damage inputs");
            var part=targetId=="body"?null:battle.Parts.SingleOrDefault(p=>p.Id==targetId);
            if(targetId!="body" && (part==null || part.IsBroken)) throw new ArgumentException("Invalid damage target.");
            int defense=skill.DamageType=="magic"?(part?.MagicDefense??battle.BossMagicDefense):(part?.PhysicalDefense??battle.BossPhysicalDefense);
            decimal effectiveDefense=(decimal)defense*(10000-skill.IgnoreDefenseBp)/10000m;
            decimal raw=(skill.AttackSnapshot??hero.Attack)*skill.Power*(critical?skill.CriticalMultiplierPercent/100m:1m)*1000m/(1000m+effectiveDefense);
            return (int)Math.Max(1m,Math.Min(skill.DamageCap>0?skill.DamageCap:int.MaxValue,Math.Floor(raw)));
        }

        public static BattleActionResult Resolve(BattleState battle, string heroId, BattleSkill skill, string targetId,Func<int,int> draw=null)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (battle.IsVictory) return new BattleActionResult(false, 0, false, true, "battle-ended");
            var hero = battle.Heroes.SingleOrDefault(item => item.Id == heroId);
            if (hero == null) return new BattleActionResult(false, 0, false, false, "unknown-hero");
            if (!hero.IsAlive) return new BattleActionResult(false, 0, false, false, "hero-defeated");

            var isBody = targetId == "body";
            var part = isBody ? null : battle.Parts.SingleOrDefault(item => item.Id == targetId);
            if (!isBody && (part == null || part.IsBroken))
                return new BattleActionResult(false, 0, false, false, "invalid-part");

            if(hero.JobResource<skill.ResourceCost) return new BattleActionResult(false,0,false,false,"insufficient-resource");
            if(skill.CriticalChanceBp>0 && skill.CriticalChanceBp<10000 && draw==null) throw new ArgumentException("Probabilistic critical requires an explicit RNG.");
            int roll=skill.CriticalChanceBp>0 && skill.CriticalChanceBp<10000?draw(10000):-1;
            if(roll<-1 || roll>=10000 || (skill.CriticalChanceBp>0 && skill.CriticalChanceBp<10000 && roll<0)) throw new ArgumentException("Invalid critical RNG result.");
            bool critical=skill.CriticalChanceBp==10000 || roll>=0 && roll<skill.CriticalChanceBp;
            hero.SpendResource(skill.ResourceCost);
            int damage=CalculateDamage(battle,hero,skill,targetId,critical);
            int appliedDamage=isBody?battle.ApplyBossDamage(damage):Math.Min(part.HitPoints,damage);
            bool broken=!isBody && battle.BreakPart(targetId,damage);
            // One accepted command: enemy damage, self healing, then self recoil.
            // Follow-ups are not a second command, consume no additional resource,
            // and occur even when this attack delivers the killing blow.
            int hp=hero.HitPoints;
            hero.Heal((int)Math.Min(int.MaxValue,(long)hero.BaseAttack*skill.SelfHealingBaseAttackPercent/100));
            int healed=hero.HitPoints-hp;hp=hero.HitPoints;
            hero.TakeDamage((int)((long)hero.MaxHitPoints*skill.SelfDamageMaxHpPercent/100));
            int recoil=hp-hero.HitPoints;
            return new BattleActionResult(true,appliedDamage,broken,battle.IsVictory,isBody?"body":"part",healed,recoil,critical,roll);
        }
    }
}
