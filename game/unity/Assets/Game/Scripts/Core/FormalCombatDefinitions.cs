using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class JobCombatDef
    {
        public string id,resourceName;
        public int resourceMax,initialResource,gainAtReady,gainOnAttack,gainOnHit;
        public int hp,attack,defense,magicDefense,speed,criticalBp;
        public void Validate()
        {
            if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(resourceName) || resourceMax<1 || resourceMax>15 || initialResource<0 || initialResource>resourceMax || gainAtReady<0 || gainAtReady>15 || gainOnAttack<0 || gainOnAttack>15 || gainOnHit<0 || gainOnHit>15 || hp<1 || hp>1000000 || attack<1 || attack>1000000 || defense<0 || defense>1000000 || magicDefense<0 || magicDefense>1000000 || speed<1 || speed>10000 || criticalBp<0 || criticalBp>10000) throw new ArgumentException("Invalid formal job.");
        }
        public JobCombatDef Copy() => (JobCombatDef)MemberwiseClone();
    }
    [Serializable] public sealed class HeroineContentRef
    {
        public string id,ownerId,kind,status;
    }
    public sealed partial class CombatDefinitionCatalog
    {
        public JobCombatDef[] jobs;
        public string designOrigin;
        public HeroineContentRef[] contentReferences;
        public bool IsFormal => schemaVersion==3 && status=="newaster-original";
        public JobCombatDef Job(string id) => jobs.Single(j=>j.id==id);
        private void ValidateFormal()
        {
            if(!IsFormal) return;
            if(designOrigin!="user-authorized-newaster-rules-2026-10-02") throw new ArgumentException("Formal original rules need explicit provenance.");
            if(jobs==null || jobs.Length!=5 || jobs.Any(j=>j==null) || jobs.Select(j=>j.id).Distinct().Count()!=5) throw new ArgumentException("Formal roster needs five distinct job definitions.");
            foreach(var job in jobs) {job.Validate();if(!Id(job.id)) throw new ArgumentException("Invalid job ID.");}
            var kinds=new[]{"trait","weapon-tree","poem-chapter","poem-link","affinity-event","lover-event"};
            if(contentReferences==null || contentReferences.Any(r=>r==null || !Id(r.id) || !Id(r.ownerId) || !kinds.Contains(r.kind) || (r.status!="reserved" && r.status!="implemented")) || contentReferences.Select(r=>r.id).Distinct().Count()!=contentReferences.Length) throw new ArgumentException("Invalid heroine content registry.");
            foreach(var hero in heroines) {
                var job=jobs.SingleOrDefault(j=>j.id==hero.jobId);if(job==null) throw new ArgumentException("Missing heroine job.");
                if(hero.hpBp<9000 || hero.hpBp>11000 || hero.attackBp<9000 || hero.attackBp>11000 || hero.defenseBp<9000 || hero.defenseBp>11000 || hero.hpBp+hero.attackBp+hero.defenseBp!=30000 || hero.speedBp<9500 || hero.speedBp>10500 || (long)job.speed*hero.speedBp/10000<1) throw new ArgumentException("Invalid character stat construction.");
                ValidateReference(hero,hero.traitId,"trait");ValidateReference(hero,hero.weaponTreeId,"weapon-tree");
                ValidateReferences(hero,hero.poemChapters,"poem-chapter",3);ValidateReferences(hero,hero.poemLinks,"poem-link",18);
                ValidateReferences(hero,hero.affinityEventIds,"affinity-event",3);ValidateReferences(hero,hero.loverEventIds,"lover-event",2);
                if(hero.traitHpPercent<0 || hero.traitHpPercent>100 || hero.traitAttackPercent<0 || hero.traitAttackPercent>100) throw new ArgumentException("Invalid trait stats.");
                if((long)job.hp*hero.hpBp*(100+hero.traitHpPercent)/1000000<1 || (long)job.attack*hero.attackBp*(100+hero.traitAttackPercent)/1000000<1) throw new ArgumentException("Character stats cannot round down to zero.");
                if(contentReferences.Single(r=>r.id==hero.traitId).status!="implemented") throw new ArgumentException("Combat trait must be implemented.");
                for(int slot=0;slot<3;slot++) {
                    var skill=Skill(hero.id,slot);
                    if(skill.ruleOrigin!="video-observation-plus-newaster-original" || skill.sourceSkillId!=skill.id || string.IsNullOrEmpty(skill.sourceFile) || !skill.sourceFile.EndsWith(".mkv",StringComparison.OrdinalIgnoreCase) || skill.sourceSecond<0 || skill.observedSkillLevel!=7) throw new ArgumentException("Missing skill evidence provenance.");
                    if(skill.resourceCost>job.resourceMax || (skill.effectRuleId=="effect.damage" && string.IsNullOrEmpty(skill.damageType))) throw new ArgumentException("Formal skill needs explicit attack profile and reachable resource cost.");
                    if(skill.effectRuleId=="effect.damage" && skill.damageCap<=0) throw new ArgumentException("Formal damage cap must be explicit.");
                    if((skill.conditions??Array.Empty<SkillConditionDef>()).Any(c=>c.kind=="trait-equipped" && c.referenceId!=hero.traitId || c.kind=="job-resource-at-least" && c.threshold>job.resourceMax)) throw new ArgumentException("Unreachable or cross-owner formal skill condition.");
                }
            }
            if(chainActions.Any(a=>a.ruleOrigin!="newaster-original")) throw new ArgumentException("Fixed actions must be explicitly authored original definitions.");
            if(contentReferences.Any(r=>!heroines.Any(h=>h.id==r.ownerId))) throw new ArgumentException("Orphan heroine content.");
        }
        private void ValidateReference(HeroineCombatDef hero,string id,string kind)
        {
            var r=contentReferences.SingleOrDefault(x=>x.id==id);
            if(!Id(id) || r==null || r.ownerId!=hero.id || r.kind!=kind) throw new ArgumentException("Missing or cross-owner heroine content reference.");
        }
        private void ValidateReferences(HeroineCombatDef hero,string[] ids,string kind,int count)
        {
            if(ids==null || ids.Length!=count || ids.Distinct().Count()!=count) throw new ArgumentException("Invalid heroine content count.");
            foreach(var id in ids) ValidateReference(hero,id,kind);
        }
    }
}
