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
        public string traitId,weaponTreeId;
        public string[] poemChapters,poemLinks,affinityEventIds,loverEventIds;
        public int hpBp,attackBp,defenseBp,speedBp,traitHpPercent,traitAttackPercent;
    }
    [Serializable] public sealed class SkillCombatDef
    {
        public string id,ownerId,name,effectRuleId,targetRuleId;
        public string sourceSkillId,sourceFile,ruleOrigin;
        public int sourceSecond,observedSkillLevel;
        public int resourceCost,recoveryPercent,castPercent,targetCount,baseHealing;
        public int selfHealingBaseAttackPercent,selfDamageMaxHpPercent;
        public int criticalBonusBp,damageCap;
        public string damageType;
        public int ignoreDefenseBp;
        public EnemyStatusDef[] statusEffects;
        public int enemyWaitAdd,selfWaitReductionPercent,chargeConsumeMax,chargeBonusPercent,specialWeaponBonusPercent;
        public SkillConditionDef[] conditions;
        public float powerScale,partScale;
        public bool chainEligible;
        public TimedSelfEffectDef[] selfEffects;
    }
    [Serializable] public sealed class ChainCombatDef
    {
        public string id,heroineId,effectRuleId,targetRuleId,resourcePolicy,timelinePolicy,commandInteractionPolicy,presentationId;
        public string ruleOrigin;
        public float powerScale;
        public int baseHealing;
        public string damageType;
        public int ignoreDefenseBp;
    }
    // Versioned execution catalog. v3 includes full heroine reference boundaries;
    // reserved story/weapon content is not falsely presented as playable content.
    [Serializable] public sealed partial class CombatDefinitionCatalog
    {
        public int schemaVersion;
        public string status;
        public int enemyPhysicalDefense,enemyMagicDefense;
        public EnemyStatusResistanceDef[] enemyStatusResistances;
        public string[] formation;
        public HeroineCombatDef[] heroines;
        public SkillCombatDef[] skills;
        public ChainCombatDef[] chainActions;
        private static bool Id(string id) => id!=null && Regex.IsMatch(id,"^[a-z0-9._-]{1,96}$");
        private static bool Scale(float value,bool positive=false) => !float.IsNaN(value) && !float.IsInfinity(value) && (positive?value>0:value>=0) && value<=100;
        public HeroineCombatDef Hero(string id) => heroines.Single(h=>h.id==id);
        public SkillCombatDef Skill(string heroId,int slot) => skills.Single(s=>s.id==Hero(heroId).skills[slot]);
        public string[] FormationIds => schemaVersion==1?Enumerable.Range(0,5).Select(i=>"hero-"+i).ToArray():(string[])formation.Clone();
        public string HeroIdAt(int index) => FormationIds[index];
        public void Validate()
        {
            if(enemyPhysicalDefense<0 || enemyMagicDefense<0) throw new ArgumentException("Enemy defense cannot be negative.");
            new EnemyStatusState(enemyStatusResistances);
            if(!((schemaVersion==1 && status=="placeholder" && (formation==null || formation.Length==0)) || ((schemaVersion==2 && status=="integration-trial" || IsFormal) && formation!=null && formation.Length==5 && formation.All(Id) && formation.Distinct().Count()==5))) throw new ArgumentException("Unsupported combat version/status/formation.");
            if(heroines==null || heroines.Length!=5 || skills==null || skills.Length!=15 || chainActions==null || chainActions.Length!=5)
                throw new ArgumentException("Combat slice requires 5 heroines, 15 skills and 5 chain actions.");
            if(heroines.Any(h=>h==null || !Id(h.id) || !Id(h.jobId) || string.IsNullOrWhiteSpace(h.name) || h.baseRarity!=6 || h.skills==null || h.skills.Length!=3 || h.skills.Any(s=>!Id(s)) || h.skills.Distinct().Count()!=3 || !Id(h.chainActionId)) || heroines.Select(h=>h.id).Distinct().Count()!=5)
                throw new ArgumentException("Invalid heroine combat definition.");
            if(skills.Any(s=>s==null || !Id(s.id) || !Id(s.ownerId) || string.IsNullOrWhiteSpace(s.name) || s.resourceCost<0 || s.resourceCost>10 || s.recoveryPercent<=0 || s.recoveryPercent>1000 || s.castPercent<0 || s.castPercent>1000 || !Scale(s.powerScale) || !Scale(s.partScale,true) || s.baseHealing<0 || s.baseHealing>1000000) || skills.Select(s=>s.id).Distinct().Count()!=15)
                throw new ArgumentException("Invalid skill definition.");
            if(skills.Any(s=>s.selfHealingBaseAttackPercent<0 || s.selfHealingBaseAttackPercent>1000 || s.selfDamageMaxHpPercent<0 || s.selfDamageMaxHpPercent>100 || (s.effectRuleId!="effect.damage" && (s.selfHealingBaseAttackPercent!=0 || s.selfDamageMaxHpPercent!=0))))
                throw new ArgumentException("Attack follow-up percentages require a damage skill and valid ranges.");
            foreach(var skill in skills) {
                var statuses=skill.statusEffects??Array.Empty<EnemyStatusDef>();
                if(statuses.Any(e=>e==null) || statuses.Select(e=>e.kind).Distinct().Count()!=statuses.Length) throw new ArgumentException("Invalid status effect list.");
                foreach(var e in statuses) e.Validate();
                if(!IsFormal && (statuses.Length>0 || skill.enemyWaitAdd!=0 || skill.selfWaitReductionPercent!=0 || skill.chargeConsumeMax!=0 || skill.chargeBonusPercent!=0 || skill.specialWeaponBonusPercent!=0)) throw new ArgumentException("Extended status/job command effects require original formal rules.");
                if(skill.enemyWaitAdd<0 || skill.enemyWaitAdd>1000 || skill.selfWaitReductionPercent<0 || skill.selfWaitReductionPercent>90 || skill.chargeConsumeMax<0 || skill.chargeConsumeMax>9 || skill.chargeBonusPercent<0 || skill.chargeBonusPercent>100 || skill.specialWeaponBonusPercent<0 || skill.specialWeaponBonusPercent>1000 || (skill.effectRuleId!="effect.damage" && (statuses.Length>0 || skill.enemyWaitAdd!=0 || skill.selfWaitReductionPercent!=0 || skill.chargeConsumeMax!=0 || skill.chargeBonusPercent!=0 || skill.specialWeaponBonusPercent!=0)) || (skill.chargeConsumeMax==0 && skill.chargeBonusPercent!=0)) throw new ArgumentException("Invalid command-only effect.");
                if((!string.IsNullOrEmpty(skill.damageType) && skill.damageType!="physical" && skill.damageType!="magic") || skill.ignoreDefenseBp<0 || skill.ignoreDefenseBp>10000 || (skill.effectRuleId!="effect.damage" && (!string.IsNullOrEmpty(skill.damageType) || skill.ignoreDefenseBp!=0))) throw new ArgumentException("Invalid attack defense profile.");
                SkillConditionDef.ValidateAll(skill.conditions);
                if(skill.criticalBonusBp<0 || skill.criticalBonusBp>10000 || skill.damageCap<0 || (skill.effectRuleId!="effect.damage" && (skill.criticalBonusBp!=0 || skill.damageCap!=0))) throw new ArgumentException("Critical bonus and damage cap require an attack.");
                TimedSelfEffectDef.ValidateAll(skill.selfEffects);
                if(skill.effectRuleId=="effect.self-buff") {
                    if(skill.selfEffects==null || skill.selfEffects.Length==0 || skill.targetRuleId!="target.self" || skill.castPercent!=0 || skill.chainEligible || skill.powerScale!=0)
                        throw new ArgumentException("Self-buff requires nonempty supported effects and self target without casting or chain.");
                } else if(skill.effectRuleId!="effect.damage" && skill.selfEffects!=null && skill.selfEffects.Length>0) throw new ArgumentException("Timed effects require a self-buff or attack command.");
            }
            if(chainActions.Any(a=>a==null || !Id(a.id) || !Id(a.heroineId) || !Id(a.presentationId) || a.resourcePolicy!="none" || a.timelinePolicy!="preserve" || a.commandInteractionPolicy!="none" || !Scale(a.powerScale)) || chainActions.Select(a=>a.id).Distinct().Count()!=5)
                throw new ArgumentException("Unsupported or invalid chain definition.");
            foreach(var action in chainActions) MakeChain(action);
            for(int actor=0;actor<5;actor++) {
                string heroId=HeroIdAt(actor);
                var h=heroines.SingleOrDefault(x=>x.id==heroId);
                if(h==null) throw new ArgumentException("Combat formation references a missing heroine.");
                var a=chainActions.SingleOrDefault(x=>x.id==h.chainActionId);
                if(a==null || a.heroineId!=heroId) throw new ArgumentException("Chain ownership mismatch.");
                for(int slot=0;slot<3;slot++) {
                    var s=skills.SingleOrDefault(x=>x.id==h.skills[slot]);
                    if(s==null || s.ownerId!=heroId) throw new ArgumentException("Skill ownership mismatch.");
                    if(s.effectRuleId=="effect.self-buff") {
                        // Validated above; unlike legacy support, any heroine slot may own it.
                    } else if(s.effectRuleId=="effect.heal") {
                        if(s.castPercent!=0 || s.chainEligible || s.targetCount<1 || s.targetCount>5 || (s.targetRuleId!="target.self" && s.targetRuleId!="target.selected-allies" && s.targetRuleId!="target.all-living-allies") || (s.targetRuleId=="target.self" && s.targetCount!=1) || (s.targetRuleId=="target.all-living-allies" && s.targetCount!=5)) throw new ArgumentException("Unsupported healing definition.");
                    } else if(slot<2 || s.effectRuleId=="effect.damage") {
                        if(s.effectRuleId!="effect.damage" || (s.targetRuleId!="target.selected-enemy" && s.targetRuleId!="target.enemy-range" && s.targetRuleId!="target.all-enemies") || !Scale(s.powerScale,true)) throw new ArgumentException("Unsupported attack definition.");
                    } else {
                        // These three effects still use the existing trial resolver. Reject
                        // unsupported combinations rather than silently changing their meaning.
                        string effect=actor==1?"effect.gauge-down":actor==2?"effect.guard":actor==4?"effect.resource-to-others":"";
                        string target=actor==1?"target.boss-body":actor==2?"target.all-living-allies":"target.other-living-allies";
                        if(effect=="" || s.effectRuleId!=effect || s.targetRuleId!=target || s.castPercent!=0 || s.chainEligible) throw new ArgumentException("Unsupported support definition.");
                    }
                }
            }
            ValidateFormal();
        }
        public SkillTimingDefinition[,] Timings()
        {
            Validate();var result=new SkillTimingDefinition[5,3];
            for(int i=0;i<5;i++) for(int slot=0;slot<3;slot++) {var s=Skill(HeroIdAt(i),slot);result[i,slot]=new SkillTimingDefinition(s.recoveryPercent,s.castPercent);}
            return result;
        }
        public HealingSkillDefinition[] Healing()
        {
            Validate();return Enumerable.Range(0,5).SelectMany(i=>Enumerable.Range(0,3).Select(slot=>new {actor=i,slot,skill=Skill(HeroIdAt(i),slot)}))
                .Where(x=>x.skill.effectRuleId=="effect.heal").Select(x=>new HealingSkillDefinition(x.actor,x.slot,x.skill.name,
                    x.skill.targetRuleId=="target.self"?HealingTargetRule.Self:x.skill.targetRuleId=="target.selected-allies"?HealingTargetRule.SelectedAllies:HealingTargetRule.AllLivingAllies,
                    x.skill.targetCount,x.skill.resourceCost,x.skill.baseHealing,(decimal)x.skill.powerScale)).ToArray();
        }
        public HeroineChainAction[] Chain()
        {
            Validate();return Enumerable.Range(0,5).Select(i=>{var h=Hero(HeroIdAt(i));return MakeChain(chainActions.Single(x=>x.id==h.chainActionId));}).ToArray();
        }
        private static HeroineChainAction MakeChain(ChainCombatDef a)
        {
            if(a.effectRuleId!="effect.damage" && (!string.IsNullOrEmpty(a.damageType) || a.ignoreDefenseBp!=0)) throw new ArgumentException("Healing fixed action cannot carry an attack defense profile.");
            ChainEffect effect;
            if(a.effectRuleId=="effect.damage") effect=ChainEffect.Damage;
            else if(a.effectRuleId=="effect.heal") effect=ChainEffect.Heal;
            else throw new ArgumentException("Unsupported fixed chain effect.");
            ChainTarget target;
            switch(a.targetRuleId) {
                case "target.boss-body": target=ChainTarget.BossBody;break;
                case "target.lowest-hp-part": target=ChainTarget.LowestHpPart;break;
                case "target.self": target=ChainTarget.Self;break;
                case "target.lowest-hp-ally": target=ChainTarget.LowestHpAlly;break;
                case "target.all-living-allies": target=ChainTarget.AllLivingAllies;break;
                default: throw new ArgumentException("Unsupported fixed chain target.");
            }
            return new HeroineChainAction(a.heroineId,a.id,(decimal)a.powerScale,effect,target,a.baseHealing,a.presentationId,string.IsNullOrEmpty(a.damageType)?"physical":a.damageType,a.ignoreDefenseBp);
        }
    }
}
