using System;
using System.Linq;
namespace NewAster.Core
{
    public static class HeroineTraitRules
    {
        public static bool Mastered(FormalHeroineGrowth growth)=>growth!=null && Enumerable.Range(0,3).All(i=>growth.SkillLevel(i)==7);
        public static int MasteryBonusForHero(string heroineId,string stat,FormalHeroineGrowth growth)
        {
            if(!Mastered(growth))return 0;
            string key;
            switch(heroineId){
                case "heroine.slayer":case "heroine.annihilator":case "heroine.nighthawk":case "heroine.shangrila":key="critical";break;
                case "heroine.iconoclast":case "heroine.r":case "heroine.oriflamme":key="attack";break;
                case "heroine.undermine":case "heroine.annihilator-holy":key="hp";break;
                case "heroine.echidna":case "heroine.arcane-academy":key="magic-defense";break;
                case "heroine.excalipan":case "heroine.shell":case "heroine.slayer-swim":case "heroine.arcane":key="physical-defense";break;
                default:return 0;
            }
            return key==stat?(key=="critical"?500:5):0;
        }
        public static int MasteryBonus(string job,string stat,FormalHeroineGrowth growth)
        {
            if(!Mastered(growth))return 0;
            return (job=="job.fighter" || job=="job.chaser" || job=="job.sniper") && stat=="critical"?500:(job=="job.berserker" || job=="job.artist" || job=="job.alchemist") && stat=="attack"?5:(job=="job.defender" || job=="job.healer") && stat=="hp"?5:(job=="job.blaster" || job=="job.gambler") && stat=="magic-defense"?5:(job=="job.gunner" || job=="job.panzer" || job=="job.general") && stat=="physical-defense"?5:0;
        }
    }
}
