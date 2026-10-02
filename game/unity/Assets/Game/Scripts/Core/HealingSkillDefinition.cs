using System;
namespace NewAster.Core
{
    public enum HealingTargetRule { Self, SelectedAllies, AllLivingAllies }
    public sealed class HealingSkillDefinition
    {
        public int Actor { get; }
        public int Slot { get; }
        public string Name { get; }
        public HealingTargetRule TargetRule { get; }
        public int TargetCount { get; }
        public int ResourceCost { get; }
        public int BaseHealing { get; }
        public decimal AttackScale { get; }
        public HealingSkillDefinition(int actor, int slot, string name, HealingTargetRule rule, int count, int cost, int baseHealing, decimal attackScale)
        {
            if(actor<0 || actor>=5 || slot<0 || slot>=3 || string.IsNullOrWhiteSpace(name) || !Enum.IsDefined(typeof(HealingTargetRule),rule) || count<1 || count>5 || cost<0 || baseHealing<0 || attackScale<0 || (rule==HealingTargetRule.Self && count!=1) || (rule==HealingTargetRule.AllLivingAllies && count!=5)) throw new ArgumentException("Invalid healing skill definition.");
            Actor=actor; Slot=slot; Name=name; TargetRule=rule; TargetCount=count; ResourceCost=cost; BaseHealing=baseHealing; AttackScale=attackScale;
        }
    }
}
