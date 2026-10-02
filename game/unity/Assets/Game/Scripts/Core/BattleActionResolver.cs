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

        public BattleSkill(string id, decimal power, int resourceCost,int selfHealingBaseAttackPercent=0,int selfDamageMaxHpPercent=0)
        {
            if (power <= 0m) throw new ArgumentOutOfRangeException(nameof(power));
            if (resourceCost < 0) throw new ArgumentOutOfRangeException(nameof(resourceCost));
            if(selfHealingBaseAttackPercent<0 || selfHealingBaseAttackPercent>1000 || selfDamageMaxHpPercent<0 || selfDamageMaxHpPercent>100) throw new ArgumentOutOfRangeException("Attack follow-up percentage");
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Power = power;
            ResourceCost = resourceCost;
            SelfHealingBaseAttackPercent=selfHealingBaseAttackPercent;
            SelfDamageMaxHpPercent=selfDamageMaxHpPercent;
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

        public BattleActionResult(bool accepted, int damage, bool partBroken, bool victory, string reason,int selfHealing=0,int selfDamage=0)
        {
            Accepted = accepted;
            Damage = damage;
            PartBroken = partBroken;
            Victory = victory;
            Reason = reason;
            SelfHealing=selfHealing; SelfDamage=selfDamage;
        }
    }

    /// <summary>スキルのコスト、対象、ダメージ、部位破壊、勝利を一度だけ解決する。</summary>
    public static class BattleActionResolver
    {
        public static BattleActionResult Resolve(BattleState battle, string heroId, BattleSkill skill, string targetId)
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

            if (!hero.SpendResource(skill.ResourceCost)) return new BattleActionResult(false, 0, false, false, "insufficient-resource");

            var damage = Math.Max(1, (int)Math.Floor(hero.Attack * skill.Power));
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
            return new BattleActionResult(true,appliedDamage,broken,battle.IsVictory,isBody?"body":"part",healed,recoil);
        }
    }
}
