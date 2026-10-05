using System;
using System.Linq;
namespace NewAster.Core
{
    public static class WeaponGrowthRules
    {
        public static int Attack(HomeWeaponNode n,int level) {HeroineSkillRules.Multiplier(level);return checked(n.attackBonus+2*(level-1));}
        public static float Power(HomeWeaponNode n,int level)=>n.skillPower+.03f*(level-1);
        public static HomeCost[] Costs(HomeWeaponNode n,int level,HomeExperienceCatalog c)
        {
            if(level<1 || level>=7)throw new ArgumentOutOfRangeException(nameof(level));
            var costs=n.costs;
            if(costs.Length==0)costs=c.weaponNodes.First(x=>x.heroineId==n.heroineId && !x.initial).costs;
            return costs.Select(x=>new HomeCost{resourceId=x.resourceId,amount=checked(Math.Max(2,x.amount/3)*level)}).ToArray();
        }
    }
}
