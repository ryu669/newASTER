using System;
using System.Linq;
namespace NewAster.Core
{
    public static class WeaponGrowthRules
    {
        public static string[] EffectKinds(HomeWeaponNode n)
        {
            var effects=new System.Collections.Generic.List<string>();
            if(n.initial)effects.Add("root");
            if(n.attackBonus>0)effects.Add("attack");if(n.skillPower>1)effects.Add("power");
            if(n.physicalDefenseBonus>0)effects.Add("physical");if(n.magicDefenseBonus>0)effects.Add("magic");
            if(n.speedBonus>0)effects.Add("speed");if(n.criticalBonusBp>0)effects.Add("critical");if(n.criticalDamageBonus>0)effects.Add("critical-damage");
            if(!string.IsNullOrEmpty(n.weaponTraitName))effects.Add("trait");return effects.ToArray();
        }
        public static int Attack(HomeWeaponNode n,int level) {HeroineSkillRules.Multiplier(level);return checked(n.attackBonus+(n.attackBonus>0?2*(level-1):0));}
        public static float Power(HomeWeaponNode n,int level)=>n.skillPower+.03f*(level-1);
        public static int Physical(HomeWeaponNode n,int level)=>n.physicalDefenseBonus+(n.physicalDefenseBonus>0?3*(level-1):0);
        public static int Magic(HomeWeaponNode n,int level)=>n.magicDefenseBonus+(n.magicDefenseBonus>0?3*(level-1):0);
        public static int Speed(HomeWeaponNode n,int level)=>n.speedBonus+(n.speedBonus>0?level-1:0);
        public static int Critical(HomeWeaponNode n,int level)=>n.criticalBonusBp+(n.criticalBonusBp>0?50*(level-1):0);
        public static int CriticalDamage(HomeWeaponNode n,int level)=>n.criticalDamageBonus+(n.criticalDamageBonus>0?2*(level-1):0);
        public static string Trait(HomeWeaponNode n)=>string.IsNullOrEmpty(n.weaponTraitName)?"特性：最終神器で解放":n.weaponTraitName+" ／ "+(n.traitAttackPercent>0?"攻撃＋"+n.traitAttackPercent+"%":n.traitDefensePercent>0?"物理・魔法防御＋"+n.traitDefensePercent+"%":"速度＋"+n.traitSpeedPercent+"%");
        public static string Summary(HomeWeaponNode n,int level){var parts=new System.Collections.Generic.List<string>{"攻撃＋"+Attack(n,level)};
            if(n.physicalDefenseBonus>0)parts.Add("物防＋"+Physical(n,level)+" 魔防＋"+Magic(n,level));
            if(n.speedBonus>0)parts.Add("速度＋"+Speed(n,level));if(n.criticalBonusBp>0)parts.Add("会心＋"+(Critical(n,level)/100f).ToString("0.#")+"%");
            if(n.criticalDamageBonus>0)parts.Add("会心威力＋"+CriticalDamage(n,level)+"%");return string.Join(" ／ ",parts);}
        public static HomeCost[] Costs(HomeWeaponNode n,int level,HomeExperienceCatalog c)
        {
            if(level<1 || level>=7)throw new ArgumentOutOfRangeException(nameof(level));
            var costs=n.costs;
            if(costs.Length==0)costs=c.weaponNodes.First(x=>x.heroineId==n.heroineId && !x.initial).costs;
            return costs.Select(x=>new HomeCost{resourceId=x.resourceId,amount=checked(Math.Max(2,x.amount/3)*level)}).ToArray();
        }
    }
}
