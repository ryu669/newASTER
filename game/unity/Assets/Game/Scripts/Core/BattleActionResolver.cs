using System;
using System.Linq;

namespace NewAster.Core
{
    public sealed class BattleSkill
    {
        public string Id { get; }
        public decimal Power { get; }
        private readonly string[] attributes;
        public string[] Attributes => attributes.ToArray();
        public int ResourceCost { get; }
        public int SelfHealingBaseAttackPercent { get; }
        public int SelfDamageMaxHpPercent { get; }
        public int? AttackSnapshot { get; }
        public int CriticalChanceBp { get; }
        public int CriticalMultiplierPercent { get; }
        public int DamageCap { get; }
        public string DamageType { get; }
        public int IgnoreDefenseBp { get; }
        public int BodyDamageBonusPercent {get;}
        public string TargetRule { get; }
        public decimal PerTargetPartScale { get; }
        public bool BodyPartProtection { get; }
        private readonly EnemyStatusDef[] statusEffects;
        public System.Collections.Generic.IReadOnlyList<EnemyStatusDef> StatusEffects => Array.AsReadOnly(statusEffects.Select(e=>e.Copy()).ToArray());

        public BattleSkill(string id, decimal power, int resourceCost,int selfHealingBaseAttackPercent=0,int selfDamageMaxHpPercent=0,int? attackSnapshot=null,int criticalChanceBp=0,int criticalMultiplierPercent=150,int damageCap=0,string damageType="physical",int ignoreDefenseBp=0,string targetRule="target.selected-enemy",EnemyStatusDef[] statusEffects=null,decimal perTargetPartScale=1m,bool bodyPartProtection=false,string[] attributes=null,int bodyDamageBonusPercent=0)
        {
            if(perTargetPartScale<=0 || perTargetPartScale>100) throw new ArgumentOutOfRangeException(nameof(perTargetPartScale));
            CombatAttributeRules.Validate(attributes);this.attributes=(attributes??Array.Empty<string>()).ToArray();
            if(bodyDamageBonusPercent<0 || bodyDamageBonusPercent>200)throw new ArgumentOutOfRangeException(nameof(bodyDamageBonusPercent));
            BodyDamageBonusPercent=bodyDamageBonusPercent;PerTargetPartScale=perTargetPartScale;BodyPartProtection=bodyPartProtection;
            if(targetRule!="target.selected-enemy" && targetRule!="target.enemy-range" && targetRule!="target.all-enemies") throw new ArgumentException("Unknown attack target rule.");
            var effects=(statusEffects??Array.Empty<EnemyStatusDef>()).ToArray();
            if(effects.Any(e=>e==null) || effects.Select(e=>e.kind).Distinct().Count()!=effects.Length) throw new ArgumentException("Invalid attack statuses.");
            foreach(var effect in effects) effect.Validate();
            this.statusEffects=effects.Select(e=>e.Copy()).ToArray();TargetRule=targetRule;
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
        public System.Collections.Generic.IReadOnlyList<string> TargetIds { get; }

        public BattleActionResult(bool accepted, int damage, bool partBroken, bool victory, string reason,int selfHealing=0,int selfDamage=0,bool critical=false,int criticalRoll=-1,string[] targetIds=null)
        {
            TargetIds=Array.AsReadOnly((targetIds??Array.Empty<string>()).ToArray());
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
            if(!battle.ReferenceStatusRules && battle.EnemyStatus(targetId).Active("fracture")) effectiveDefense*=.7m;
            decimal raw=CombatAttributeRules.Multiplier(skill.Attributes,battle.AttributeResistances)*(skill.AttackSnapshot??hero.Attack)*skill.Power*(critical?skill.CriticalMultiplierPercent/100m:1m)*1000m/(1000m+effectiveDefense);
            if(skill.Attributes.Contains("火"))raw*=1m+(hero.FireAmplificationPercent+battle.EnemyStatus(targetId).FireVulnerabilityPercent)/100m;
            if(battle.ReferenceStatusRules){var status=battle.EnemyStatus(targetId);if(status.Active("sickness"))raw*=1.25m;if(status.Active("electrified") && skill.Attributes.Contains("雷"))raw*=1.25m;}
            if(targetId=="body")raw*=1m+skill.BodyDamageBonusPercent/100m;
            if(part!=null) raw*=skill.PerTargetPartScale;
            else if(skill.BodyPartProtection) {
                var armor=battle.Parts.Any(p=>!string.IsNullOrEmpty(p.Role))
                    ?battle.Parts.Single(p=>p.Role=="armor"):battle.Parts[2];
                if(!armor.IsBroken && !armor.Status.Active("stun")) raw*=.7m;
            }
            return (int)Math.Max(1m,Math.Min(skill.DamageCap>0?skill.DamageCap:int.MaxValue,Math.Floor(raw)));
        }

        private static int partHp(BattleState battle,string target)=>battle.Parts.Single(p=>p.Id==target).MaxHitPoints;
        public static BattleActionResult Resolve(BattleState battle, string heroId, BattleSkill skill, string targetId,Func<int,int> draw=null)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (battle.IsVictory) return new BattleActionResult(false, 0, false, true, "battle-ended");
            var hero = battle.Heroes.SingleOrDefault(item => item.Id == heroId);
            if (hero == null) return new BattleActionResult(false, 0, false, false, "unknown-hero");
            if (!hero.IsAlive) return new BattleActionResult(false, 0, false, false, "hero-defeated");

            var targets=EnemyAttackTargets.Resolve(battle,skill.TargetRule,targetId);
            if (targets.Length==0)
                return new BattleActionResult(false, 0, false, false, "invalid-part");

            if(hero.JobResource<skill.ResourceCost) return new BattleActionResult(false,0,false,false,"insufficient-resource");
            int chance=Math.Min(10000,skill.CriticalChanceBp+(battle.ReferenceStatusRules && targets.Any(t=>battle.EnemyStatus(t).Active("absent"))?2500:0));
            if(chance>0 && chance<10000 && draw==null) throw new ArgumentException("Probabilistic critical requires an explicit RNG.");
            int roll=chance>0 && chance<10000?draw(10000):-1;
            if(roll<-1 || roll>=10000 || (chance>0 && chance<10000 && roll<0)) throw new ArgumentException("Invalid critical RNG result.");
            bool critical=chance==10000 || roll>=0 && roll<chance;
            hero.SpendResource(skill.ResourceCost);
            // Precompute the entire simultaneous hit before mutation; one cost and one critical draw.
            var damages=targets.Select(t=>CalculateDamage(battle,hero,skill,t,critical)).ToArray();
            long total=0;bool broken=false;
            for(int i=0;i<targets.Length;i++) {
                string t=targets[i];int damage=damages[i];
                if(t=="body") total+=battle.ApplyBossDamage(damage);
                else {var part=battle.Parts.Single(p=>p.Id==t);total+=Math.Min(part.HitPoints,damage);broken=battle.BreakPart(t,damage)||broken;}
                bool alive=t=="body"?!battle.IsVictory:!battle.Parts.Single(p=>p.Id==t).IsBroken;
                if(battle.ReferenceStatusRules && damage>0)battle.EnemyStatus(t).Remove("absent");
                if(alive) foreach(var effect in skill.StatusEffects) {bool active=battle.EnemyStatus(t).Add(effect);if(active && battle.ReferenceStatusRules){if(effect.kind=="electrified")battle.EnemyWaitPenalty+=20;int dot=battle.EnemyStatus(t).ActivationDamage(effect.kind,t=="body"?battle.BossMaxHitPoints:partHp(battle,t));if(t=="body")total+=battle.ApplyBossDamage(dot);else {var p=battle.Parts.Single(p=>p.Id==t);total+=Math.Min(p.HitPoints,dot);broken=battle.BreakPart(t,dot)||broken;}}}
            }
            int appliedDamage=(int)Math.Min(int.MaxValue,total);
            // One accepted command: enemy damage, self healing, then self recoil.
            // Follow-ups are not a second command, consume no additional resource,
            // and occur even when this attack delivers the killing blow.
            int hp=hero.HitPoints;
            hero.Heal((int)Math.Min(int.MaxValue,(long)hero.BaseAttack*skill.SelfHealingBaseAttackPercent/100));
            int healed=hero.HitPoints-hp;hp=hero.HitPoints;
            hero.TakeDamage((int)((long)hero.MaxHitPoints*skill.SelfDamageMaxHpPercent/100));
            int recoil=hp-hero.HitPoints;
            return new BattleActionResult(true,appliedDamage,broken,battle.IsVictory,targetId=="body"?"body":"part",healed,recoil,critical,roll,targets);
        }
    }
}
