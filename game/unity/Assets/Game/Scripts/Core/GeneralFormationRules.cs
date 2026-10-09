using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class GeneralSlotEffect
    {
        public int slot,attackPercent,physicalDefensePercent,magicDefensePercent,speedPercent,criticalBp;
        public string label;
    }
    [Serializable] public sealed class GeneralFormationDef
    {
        public string ownerId;
        public int strengthenPercent=100,durationClock=300;
        public GeneralSlotEffect[] slots=Array.Empty<GeneralSlotEffect>();
        public GeneralSlotEffect[] enhancedSlots=Array.Empty<GeneralSlotEffect>();
    }
    public sealed partial class CombatDefinitionCatalog
    {
        public GeneralFormationDef[] generalFormations=Array.Empty<GeneralFormationDef>();
        public void ValidateGeneralFormations()
        {
            var profiles=generalFormations??Array.Empty<GeneralFormationDef>();
            if(profiles.Any(p=>p==null) || profiles.Select(p=>p.ownerId).Distinct().Count()!=profiles.Length)throw new ArgumentException("Duplicate general profile.");
            foreach(var p in profiles){
                if(!heroines.Any(h=>h.id==p.ownerId && h.jobId=="job.general") || p.strengthenPercent<1 || p.strengthenPercent>200 || p.durationClock<100 || p.durationClock>1000 || p.slots==null || p.slots.Length!=5 || p.slots.Any(s=>s==null) || !p.slots.Select(s=>s.slot).OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,5)))throw new ArgumentException("Invalid general formation profile.");
                bool current=!string.IsNullOrEmpty(Hero(p.ownerId).secondTraitId);
                var enhanced=p.enhancedSlots??Array.Empty<GeneralSlotEffect>();
                if(current && (enhanced.Length!=5 || enhanced.Any(s=>s==null) || !enhanced.Select(s=>s.slot).OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,5))))throw new ArgumentException("General requires five independent enhanced slots.");
                foreach(var s in p.slots.Concat(enhanced))if(s==null || string.IsNullOrWhiteSpace(s.label) || new[]{s.attackPercent,s.physicalDefensePercent,s.magicDefensePercent,s.speedPercent}.Any(x=>x<0 || x>100) || s.criticalBp<0 || s.criticalBp>2000)throw new ArgumentException("Invalid authored general slot effect.");
            }
            if(heroines.Where(h=>h.jobId=="job.general").Any(h=>!profiles.Any(p=>p.ownerId==h.id)))throw new ArgumentException("General heroine requires a personal formation profile.");
        }
    }
    public sealed partial class BattleHero
    {
        public int GeneralAttackPercent {get;internal set;}
        public int GeneralPhysicalDefensePercent {get;internal set;}
        public int GeneralMagicDefensePercent {get;internal set;}
        public int GeneralSpeedPercent {get;internal set;}
        public int GeneralCriticalBp {get;internal set;}
    }
    public sealed class BattleDeployment
    {
        public string CommanderId {get;}
        private readonly HomeSniperSupport[] sniperSupports;
        public HomeSniperSupport[] SniperSupports=>sniperSupports.Select(s=>new HomeSniperSupport{heroineId=s.heroineId,targetId=s.targetId}).ToArray();
        public BattleDeployment(string commanderId=null,HomeSniperSupport[] sniperSupports=null)
        {CommanderId=commanderId;this.sniperSupports=(sniperSupports??Array.Empty<HomeSniperSupport>()).Select(s=>s==null?throw new ArgumentException("Null support deployment."):new HomeSniperSupport{heroineId=s.heroineId,targetId=s.targetId}).ToArray();}
    }
    public sealed partial class PlayableBattle
    {
        private GeneralFormationDef[] generalProfiles=Array.Empty<GeneralFormationDef>();
        public int CommanderActor {get;private set;}=-1;
        private GeneralFormationDef activeGeneral;
        private int generalOopartBoost;
        private void InitializeGeneral(BattleDeployment deployment,CombatDefinitionCatalog definitions)
        {
            if(!UsesJobRulesV2)return;
            generalProfiles=definitions.generalFormations??Array.Empty<GeneralFormationDef>();
            string selected=deployment?.CommanderId;
            if(!string.IsNullOrEmpty(selected)){
                CommanderActor=Array.IndexOf(formationIds,selected);
                if(CommanderActor<0 || !Job(CommanderActor,"general"))throw new ArgumentException("Commander must be a deployed general.");
            }else CommanderActor=Array.FindIndex(jobStates,j=>j.Id=="job.general");
            if(CommanderActor>=0)activeGeneral=generalProfiles.Single(p=>p.ownerId==formationIds[CommanderActor]);
            for(int i=0;i<5;i++)UpdateGeneralFormationStats(i);
        }
        public string GeneralSlotDescription(int slot)
        {
            if(slot<0 || slot>=5)throw new ArgumentOutOfRangeException(nameof(slot));
            return activeGeneral==null?"指揮官なし":(JobEmpowered(CommanderActor) && activeGeneral.enhancedSlots?.Length==5?activeGeneral.enhancedSlots:activeGeneral.slots).Single(s=>s.slot==slot).label+(JobEmpowered(CommanderActor)?" ／ 指揮強化中":"");
        }
        private void UpdateGeneralFormationStats(int actor)
        {
            var h=State.Heroes[actor];h.GeneralAttackPercent=h.GeneralPhysicalDefensePercent=h.GeneralMagicDefensePercent=h.GeneralSpeedPercent=h.GeneralCriticalBp=0;
            if(activeGeneral==null || !State.Heroes[CommanderActor].IsAlive)return;
            var s=activeGeneral.slots.Single(e=>e.slot==actor);int scale=UsesOoparts?100+generalOopartBoost+(State.Heroes[CommanderActor].OopartBuffs.FirstOrDefault(b=>b.kind=="job.empowered")?.value??0):100+(JobEmpowered(CommanderActor)?activeGeneral.strengthenPercent:0);
            if(activeGeneral.enhancedSlots?.Length==5){
                s=(JobEmpowered(CommanderActor)?activeGeneral.enhancedSlots:activeGeneral.slots).Single(e=>e.slot==actor);
                scale=100+State.Heroes[CommanderActor].TraitEffect("formation");
            }
            h.GeneralAttackPercent=s.attackPercent*scale/100;h.GeneralPhysicalDefensePercent=s.physicalDefensePercent*scale/100;h.GeneralMagicDefensePercent=s.magicDefensePercent*scale/100;h.GeneralSpeedPercent=s.speedPercent*scale/100;h.GeneralCriticalBp=s.criticalBp*scale/100;
        }
        public bool ActivateGeneralCommand(int actor)
        {
            if(!JobReady(actor) || !Job(actor,"general") || actor!=CommanderActor || activeGeneral==null)return false;
            var h=State.Heroes[actor];if(h.JobResource<h.JobResourceMax || !h.SpendResource(h.JobResourceMax))return false;
            jobStates[actor].EmpoweredUntil=Clock+activeGeneral.durationClock+h.TraitEffect("empowered-clock");
            if(UsesOoparts && activeGeneral.enhancedSlots?.Length!=5){State.Heroes[actor].AddOopartBuff(new BuffInstance{sourceActorId=h.Id,sourceId="general.command",kind="job.empowered",initialValue=activeGeneral.strengthenPercent*(100+OopartBonus(actor,"buff-power"))/100,value=activeGeneral.strengthenPercent*(100+OopartBonus(actor,"buff-power"))/100,expiresAt=Clock+activeGeneral.durationClock});}
            for(int i=0;i<5;i++)UpdateGeneralFormationStats(i);
            RecordPresentation(BattlePresentationKind.Support,actor,"body","夏の指揮：5枠の効果を強化 ／ "+activeGeneral.durationClock+" Clock",standalone:true);return true;
        }
    }
}
