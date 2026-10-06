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
            return (job=="job.fighter" || job=="job.chaser" || job=="job.sniper") && stat=="critical"?500:(job=="job.berserker" || job=="job.artist" || job=="job.alchemist") && stat=="attack"?5:(job=="job.defender" || job=="job.healer") && stat=="hp"?5:(job=="job.blaster" || job=="job.gambler") && stat=="magic-defense"?5:(job=="job.gunner" || job=="job.panzer" || job=="job.general") && stat=="physical-defense"?5:0;
        }
    }
}
