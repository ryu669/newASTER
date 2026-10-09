using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed partial class BattleHero
    {
        private HeroineTraitSet combatTraits;
        private System.Collections.Generic.Dictionary<string,int> combatEffects;
        public int CombatTraitRank {get;private set;}=1;
        internal Func<long> TraitClock;
        internal Func<int,int> TraitDraw;
        internal Func<int> TraitGear;
        internal void InitializeCombatTraits(HeroineCombatDef def,FormalHeroineGrowth growth,Func<long> clock)
        {
            if(string.IsNullOrEmpty(def.secondTraitId))return;
            combatTraits=CombatTraitCatalog.Resolve(def);CombatTraitRank=CombatTraitCatalog.Rank(growth?.duplicateRank??0);TraitClock=clock;
            combatEffects=new[]{combatTraits.masteryTraitId,combatTraits.secondTraitId}.SelectMany(id=>CombatTraitCatalog.Get(id).effects).GroupBy(e=>e.kind).ToDictionary(g=>g.Key,g=>g.Sum(e=>e.Value(CombatTraitRank)));
            PermanentGaugePercent+=TraitEffect("resource-gain");PermanentRegenPercent+=TraitEffect("turn-regen");
            Status.AddResistance("stun",TraitEffect("stun-resistance-bp"));
        }
        public int TraitEffect(string kind)=>combatEffects!=null && combatEffects.TryGetValue(kind,out var value)?value:0;
        internal int TraitAttackPercent=>TraitEffect("attack")+((long)HitPoints*100>=80L*MaxHitPoints?TraitEffect("healthy-attack"):0);
        internal int TraitSpeedPercent=>(TraitClock?.Invoke()??0)<300?TraitEffect("opening-speed"):TraitEffect("speed");
        internal int TraitDamage(BattleState state,BattleSkill skill,string target)
        {
            if(combatTraits==null)return 0;
            bool spent=skill.TraitResourceSpent;
            int value=(spent?TraitEffect("resource-damage")+TraitEffect("resource-effect"):0)+TraitEffect(target=="body"?"body-damage":"part-damage");
            value+=EnemyStatusState.Kinds.Count(k=>state.EnemyStatus(target).Active(k))*TraitEffect("status-damage");
            if(skill.Id=="reaction.defender")value+=TraitEffect("counter-damage");
            if(spent || skill.Id=="job.sniper.support")value+=TraitEffect("sniper-damage");
            if((TraitGear?.Invoke()??1)>=2)value+=TraitEffect("gear-damage");
            if(skill.Id.Contains("ignition"))value+=TraitEffect("ignition-damage");
            if(skill.Id!="reaction.defender" && skill.Id!="job.sniper.support")value+=TraitEffect("skill-damage");
            return value;
        }
        internal void TickCombatTraits()
        {
            GainResource(TraitEffect("turn-resource"));
            if(IsPanzer && ArmorActive)RepairArmor((int)((long)ArmorMaxHitPoints*TraitEffect("armor-regen")/100));
        }
    }
    public sealed partial class PlayableBattle
    {
        private readonly bool[] emergencyUsed=new bool[5];
        private void InitializeCombatTraits()
        {
            if(!IsFormal)return;
            for(int i=0;i<5;i++){
                int actor=i;var h=State.Heroes[i];
                if(string.IsNullOrEmpty(combatDefinitionsForTraits.Hero(h.Id).secondTraitId))continue;
                h.InitializeCombatTraits(combatDefinitionsForTraits.Hero(h.Id),formalGrowthForTraits?.heroines.SingleOrDefault(g=>g.heroineId==h.Id),()=>Clock);
                h.TraitDraw=max=>random.Next(max);
                h.TraitGear=()=>UsesJobRulesV2?jobStates[actor].Gear:1;
                h.GainResource(h.TraitEffect("initial-resource"));
                if(Job(i,"panzer"))for(int tool=0;tool<2;tool++)jobStates[i].ToolUses[tool]+=h.TraitEffect("tool-uses");
                h.InitializeOopartHitPoints();
            }
            for(int i=0;i<5;i++)UpdateJobStatsIfAvailable(i);
        }
        private CombatDefinitionCatalog combatDefinitionsForTraits;
        private FormalGrowthSave formalGrowthForTraits;
        internal void EmergencyHealerReaction()
        {
            if(!UsesTimeline || !UsesJobRulesV2 || Ended || !State.Heroes.Any(h=>h.IsAlive && (long)h.HitPoints*4<=h.MaxHitPoints))return;
            for(int i=0;i<5;i++){
                var h=State.Heroes[i];if(!Job(i,"healer") || !h.IsAlive || IsCasting(i) || h.Status.Active("stun") || h.Status.Active("absent"))continue;
                int chance=h.TraitEffect(emergencyUsed[i]?"emergency-repeat-bp":"emergency-first-bp");
                if(chance<=0)continue;emergencyUsed[i]=true;
                if(random.Next(10000)>=chance)continue;
                readyAt[i]=Clock;
                RecordPresentation(BattlePresentationKind.Support,i,"body","緊急処置：即時行動",standalone:true);
                break;
            }
        }
    }
}
