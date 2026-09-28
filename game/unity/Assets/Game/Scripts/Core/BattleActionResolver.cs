using System;
using System.Linq;

namespace NewAster.Core
{
    public sealed class BattleSkill
    {
        public string Id { get; }
        public decimal Power { get; }
        public int ResourceCost { get; }

        public BattleSkill(string id, decimal power, int resourceCost)
        {
            if (power <= 0m) throw new ArgumentOutOfRangeException(nameof(power));
            if (resourceCost < 0) throw new ArgumentOutOfRangeException(nameof(resourceCost));
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Power = power;
            ResourceCost = resourceCost;
        }
    }

    public sealed class BattleActionResult
    {
        public bool Accepted { get; }
        public int Damage { get; }
        public bool PartBroken { get; }
        public bool Victory { get; }
        public string Reason { get; }

        public BattleActionResult(bool accepted, int damage, bool partBroken, bool victory, string reason)
        {
            Accepted = accepted;
            Damage = damage;
            PartBroken = partBroken;
            Victory = victory;
            Reason = reason;
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

            var isBody = targetId == "body";
            var part = isBody ? null : battle.Parts.SingleOrDefault(item => item.Id == targetId);
            if (!isBody && (part == null || part.IsBroken))
                return new BattleActionResult(false, 0, false, false, "invalid-part");

            if (!hero.SpendResource(skill.ResourceCost)) return new BattleActionResult(false, 0, false, false, "insufficient-resource");

            var damage = Math.Max(1, (int)Math.Floor(hero.Attack * skill.Power));
            if (isBody)
                return new BattleActionResult(true, battle.ApplyBossDamage(damage), false, battle.IsVictory, "body");

            var broken = battle.BreakPart(targetId, damage);
            return new BattleActionResult(true, damage, broken, battle.IsVictory, "part");
        }
    }
}
