using System;
using System.Linq;
namespace NewAster.Core
{
    public static class HeroineTraitRules
    {
        public static bool Mastered(FormalHeroineGrowth growth)=>growth!=null && Enumerable.Range(0,3).All(i=>growth.SkillLevel(i)==7);
        public static int MasteryBonus(string job,string stat,FormalHeroineGrowth growth)
        {
            if(!Mastered(growth))return 0;
            return job=="job.fighter" && stat=="critical"?500:job=="job.berserker" && stat=="attack"?5:job=="job.defender" && stat=="hp"?5:job=="job.blaster" && stat=="magic-defense"?5:job=="job.gunner" && stat=="physical-defense"?5:0;
        }
    }
}
