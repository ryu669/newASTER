using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class ObservedHeroineStats
    {
        public int level,skillLevel,affinity,hp,attack,defense,magicDefense,speed,criticalPercent,damageCap;
    }
    [Serializable] public sealed class ObservedSkillEffect
    {
        public string kind,target,basis;
        public int amount,turns;
    }
    [Serializable] public sealed class ObservedHeroineSkill
    {
        public string id,name,target,damageType,recovery,casting;
        public int attackPercent;
        public string[] tags;
        public ObservedSkillEffect[] effects;
    }
    [Serializable] public sealed class ObservedHeroine
    {
        public string id,name,jobId,sourceFile;
        public int sourceSecond;
        public ObservedHeroineStats observedStats;
        public ObservedHeroineSkill[] skills;
        public string[] unresolved;
    }
    // Evidence is deliberately a different type from executable combat definitions.
    // Equipment-adjusted snapshots must never be accepted as level-one base stats.
    [Serializable] public sealed class HeroineReferenceCatalog
    {
        public int schemaVersion;
        public string status,statContext;
        public string[] formation;
        public ObservedHeroine[] heroines;
        public ObservedHeroine Hero(string id) => heroines.Single(h=>h.id==id);
        public void Validate()
        {
            if(schemaVersion!=1 || status!="reference-only" || statContext!="equipment-adjusted-snapshot")
                throw new ArgumentException("Heroine evidence must remain reference-only with explicit stat context.");
            string[] ids={"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna","heroine.excalipan"};
            string[] jobs={"job.fighter","job.berserker","job.defender","job.blaster","job.gunner"};
            if(formation==null || !formation.SequenceEqual(ids) || heroines==null || heroines.Length!=5 || heroines.Any(h=>h==null) || heroines.Select(h=>h.id).Distinct().Count()!=5)
                throw new ArgumentException("Formal reference formation must contain the five selected heroines.");
            for(int i=0;i<5;i++) {
                var h=heroines.SingleOrDefault(x=>x.id==ids[i]);
                if(h==null || h.jobId!=jobs[i] || string.IsNullOrWhiteSpace(h.name) || string.IsNullOrWhiteSpace(h.sourceFile) || !h.sourceFile.EndsWith(".mkv",StringComparison.OrdinalIgnoreCase) || h.sourceSecond<0 || h.skills==null || h.skills.Length!=3 || h.unresolved==null || h.unresolved.Length==0)
                    throw new ArgumentException("Missing heroine ownership, source or unresolved conversion rules.");
                var s=h.observedStats;
                if(s==null || s.level<1 || s.level>120 || s.skillLevel<1 || s.affinity<0 || s.hp<=0 || s.attack<=0 || s.defense<=0 || s.magicDefense<=0 || s.speed<=0 || s.criticalPercent<0 || s.criticalPercent>100 || (s.damageCap!=-1 && s.damageCap<=0))
                    throw new ArgumentException("Invalid observed stats; -1 is allowed only for an unread damage cap.");
                if(h.skills.Select(x=>x?.id).Distinct().Count()!=3) throw new ArgumentException("Duplicate observed skill IDs.");
                foreach(var skill in h.skills) {
                    if(skill==null || skill.id==null || !skill.id.StartsWith(h.id+".",StringComparison.Ordinal) || string.IsNullOrWhiteSpace(skill.name) || !OneOf(skill.target,"enemy.single","enemy.all","enemy.range","self") || !OneOf(skill.damageType,"physical","magic","none") || !OneOf(skill.recovery,"unspecified","short","somewhat-long","long") || !OneOf(skill.casting,"none","somewhat-short","normal","somewhat-long") || skill.attackPercent<0 || skill.attackPercent>1000 || (skill.damageType=="none")!=(skill.attackPercent==0) || skill.tags==null || skill.effects==null)
                        throw new ArgumentException("Invalid observed skill or evidence timing category.");
                    foreach(var effect in skill.effects)
                        if(effect==null || !OneOf(effect.kind,"critical","critical-damage","attack","damage-cap","wt-add","wt-reduce-percent","self-damage","stun","burn","regen","physical-protection","forced-target","charge-consume-max","ignore-defense","bleed","poison","sickness","fracture","self-heal","special-weapon-damage") || !OneOf(effect.target,"self","enemy","unresolved") || (effect.target=="unresolved" && effect.kind!="wt-add") || !OneOf(effect.basis,"points","percent","max-hp-percent","attack-percent","base-attack-percent","count","flag") || effect.amount<0 || effect.turns<0)
                            throw new ArgumentException("Unknown observed effect, target or basis.");
                }
            }
        }
        private static bool OneOf(string value,params string[] allowed) => allowed.Contains(value);
    }
}
