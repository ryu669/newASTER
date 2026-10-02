using System;
using System.Linq;
using System.Text.RegularExpressions;
namespace NewAster.Core
{
    [Serializable] public sealed class HeroineCombatDef
    {
        public string id,name,jobId,chainActionId;
        public int baseRarity;
        public string[] skills;
    }
    [Serializable] public sealed class SkillCombatDef
    {
        public string id,ownerId,name,effectRuleId,targetRuleId;
        public int resourceCost,recoveryPercent,castPercent,targetCount,baseHealing;
        public float powerScale,partScale;
        public bool chainEligible;
    }
    [Serializable] public sealed class ChainCombatDef
    {
        public string id,heroineId,effectRuleId,targetRuleId,resourcePolicy,timelinePolicy,commandInteractionPolicy,presentationId;
        public float powerScale;
    }
    // A versioned combat-only slice, not the complete story/trait/weapon HeroineDef.
    [Serializable] public sealed class CombatDefinitionCatalog
    {
        public int schemaVersion;
        public string status;
        public HeroineCombatDef[] heroines;
        public SkillCombatDef[] skills;
        public ChainCombatDef[] chainActions;
        private static bool Id(string id) => id!=null && Regex.IsMatch(id,"^[a-z0-9._-]{1,96}$");
        private static bool Scale(float value,bool positive=false) => !float.IsNaN(value) && !float.IsInfinity(value) && (positive?value>0:value>=0) && value<=100;
        public HeroineCombatDef Hero(string id) => heroines.Single(h=>h.id==id);
        public SkillCombatDef Skill(string heroId,int slot) => skills.Single(s=>s.id==Hero(heroId).skills[slot]);
        public void Validate()
        {
            if(schemaVersion!=1 || status!="placeholder") throw new ArgumentException("Combat slice requires version 1 and explicit placeholder status.");
            if(heroines==null || heroines.Length!=5 || skills==null || skills.Length!=15 || chainActions==null || chainActions.Length!=5)
                throw new ArgumentException("Combat slice requires 5 heroines, 15 skills and 5 chain actions.");
            if(heroines.Any(h=>h==null || !Id(h.id) || !Id(h.jobId) || string.IsNullOrWhiteSpace(h.name) || h.baseRarity!=6 || h.skills==null || h.skills.Length!=3 || h.skills.Any(s=>!Id(s)) || h.skills.Distinct().Count()!=3 || !Id(h.chainActionId)) || heroines.Select(h=>h.id).Distinct().Count()!=5)
                throw new ArgumentException("Invalid heroine combat definition.");
            if(skills.Any(s=>s==null || !Id(s.id) || !Id(s.ownerId) || string.IsNullOrWhiteSpace(s.name) || s.resourceCost<0 || s.resourceCost>10 || s.recoveryPercent<=0 || s.recoveryPercent>1000 || s.castPercent<0 || s.castPercent>1000 || !Scale(s.powerScale) || !Scale(s.partScale,true) || s.baseHealing<0 || s.baseHealing>1000000) || skills.Select(s=>s.id).Distinct().Count()!=15)
                throw new ArgumentException("Invalid skill definition.");
            if(chainActions.Any(a=>a==null || !Id(a.id) || !Id(a.heroineId) || !Id(a.presentationId) || a.effectRuleId!="effect.damage" || a.targetRuleId!="target.boss-body" || a.resourcePolicy!="none" || a.timelinePolicy!="preserve" || a.commandInteractionPolicy!="none" || !Scale(a.powerScale,true)) || chainActions.Select(a=>a.id).Distinct().Count()!=5)
                throw new ArgumentException("Unsupported or invalid chain definition.");
            for(int actor=0;actor<5;actor++) {
                string heroId="hero-"+actor;
                var h=heroines.SingleOrDefault(x=>x.id==heroId);
                if(h==null) throw new ArgumentException("Combat formation IDs must be hero-0 through hero-4.");
                var a=chainActions.SingleOrDefault(x=>x.id==h.chainActionId);
                if(a==null || a.heroineId!=heroId) throw new ArgumentException("Chain ownership mismatch.");
                for(int slot=0;slot<3;slot++) {
                    var s=skills.SingleOrDefault(x=>x.id==h.skills[slot]);
                    if(s==null || s.ownerId!=heroId) throw new ArgumentException("Skill ownership mismatch.");
                    if(s.effectRuleId=="effect.heal") {
                        if(s.castPercent!=0 || s.chainEligible || s.targetCount<1 || s.targetCount>5 || (s.targetRuleId!="target.self" && s.targetRuleId!="target.selected-allies" && s.targetRuleId!="target.all-living-allies") || (s.targetRuleId=="target.self" && s.targetCount!=1) || (s.targetRuleId=="target.all-living-allies" && s.targetCount!=5)) throw new ArgumentException("Unsupported healing definition.");
                    } else if(slot<2) {
                        if(s.effectRuleId!="effect.damage" || s.targetRuleId!="target.selected-enemy" || !Scale(s.powerScale,true)) throw new ArgumentException("Unsupported attack definition.");
                    } else {
                        // These three effects still use the existing trial resolver. Reject
                        // unsupported combinations rather than silently changing their meaning.
                        string effect=actor==1?"effect.gauge-down":actor==2?"effect.guard":actor==4?"effect.resource-to-others":"";
                        string target=actor==1?"target.boss-body":actor==2?"target.all-living-allies":"target.other-living-allies";
                        if(effect=="" || s.effectRuleId!=effect || s.targetRuleId!=target || s.castPercent!=0 || s.chainEligible) throw new ArgumentException("Unsupported support definition.");
                    }
                }
            }
        }
        public SkillTimingDefinition[,] Timings()
        {
            Validate();var result=new SkillTimingDefinition[5,3];
            for(int i=0;i<5;i++) for(int slot=0;slot<3;slot++) {var s=Skill("hero-"+i,slot);result[i,slot]=new SkillTimingDefinition(s.recoveryPercent,s.castPercent);}
            return result;
        }
        public HealingSkillDefinition[] Healing()
        {
            Validate();return Enumerable.Range(0,5).SelectMany(i=>Enumerable.Range(0,3).Select(slot=>new {actor=i,slot,skill=Skill("hero-"+i,slot)}))
                .Where(x=>x.skill.effectRuleId=="effect.heal").Select(x=>new HealingSkillDefinition(x.actor,x.slot,x.skill.name,
                    x.skill.targetRuleId=="target.self"?HealingTargetRule.Self:x.skill.targetRuleId=="target.selected-allies"?HealingTargetRule.SelectedAllies:HealingTargetRule.AllLivingAllies,
                    x.skill.targetCount,x.skill.resourceCost,x.skill.baseHealing,(decimal)x.skill.powerScale)).ToArray();
        }
        public HeroineChainAction[] Chain()
        {
            Validate();return Enumerable.Range(0,5).Select(i=>{var h=Hero("hero-"+i);var a=chainActions.Single(x=>x.id==h.chainActionId);return new HeroineChainAction(h.id,a.id,(decimal)a.powerScale);}).ToArray();
        }
    }
}
