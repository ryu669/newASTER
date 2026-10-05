using System;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using NewAster.Data;
public static class CombatExpansionTests
{
    public static void Run(Action<bool,string> check,Func<CombatDefinitionCatalog> fresh,Func<CombatDefinitionCatalog> healingFixture)
    {
        check(fresh().optionalResourceBoost,"Current production enables optional resources");
        foreach(var kind in CombatAttributeRules.Kinds){
            var weak=new[]{new AttributeResistanceDef{attribute=kind,resistanceBp=-2500}};
            var resist=new[]{new AttributeResistanceDef{attribute=kind,resistanceBp=3000}};
            check(CombatAttributeRules.Multiplier(new[]{kind},weak)==1.25m && CombatAttributeRules.Multiplier(new[]{kind},resist)==.7m,"Attribute weakness/resistance "+kind);
        }
        check(CombatAttributeRules.Multiplier(null,null)==1,"Zero attributes are neutral");
        check(CombatAttributeRules.Multiplier(new[]{"火","水"},new[]{new AttributeResistanceDef{attribute="火",resistanceBp=-5000},new AttributeResistanceDef{attribute="水",resistanceBp=5000}})==1,"Mixed attributes average rather than compound");
        bool rejected=false;try{CombatAttributeRules.Validate(CombatAttributeRules.Kinds.Take(6).ToArray());}catch(ArgumentException){rejected=true;}check(rejected,"Six attributes rejected");
        rejected=false;try{CombatAttributeRules.Validate(new[]{"雷","雷"});}catch(ArgumentException){rejected=true;}check(rejected,"Duplicate attributes rejected");
        CombatAttributeRules.Validate(CombatAttributeRules.Kinds.Take(5).ToArray());
        foreach(var id in WorldCatalog.ColossusIds){var c=ColossusCombatCatalog.Get(id);check(c.attributeResistances.Length==12 && c.attributeResistances.Any(a=>a.resistanceBp<0) && c.attributeResistances.Any(a=>a.resistanceBp>0),"Each beast has all 12 attributes plus weakness and resistance "+id);var copy=c.Copy();copy.attributeResistances[0].resistanceBp=9999;check(c.attributeResistances[0].resistanceBp!=9999,"Enemy profile is deep copied");}
        // All current skills are reachable without resource; cast release remains paid zero exactly once.
        for(int actor=0;actor<5;actor++)for(int slot=0;slot<3;slot++){
            var defs=fresh();foreach(var s in defs.skills)s.chainEligible=false;
            var b=new PlayableBattle(1,new PlayableProgress(),113+actor*3+slot,combatDefinitions:defs);
            int guard=0;while(b.AvailableHero!=actor && !b.Ended && guard++<100)b.Pass();
            b.State.Heroes[actor].SpendResource(b.State.Heroes[actor].JobResource);
            check(b.SkillResourceCost(actor,slot)==0 && b.ConditionsSatisfied(actor,slot),"No resource prerequisite "+actor+"/"+slot);
            check(b.Act(actor,slot,"body"),"Zero resource executes actual skill "+actor+"/"+slot);
        }
        Func<bool,PlayableBattle> boostBattle=boost=>{var c=fresh();foreach(var s in c.skills)s.chainEligible=false;var b=new PlayableBattle(1,new PlayableProgress(),991,combatDefinitions:c);b.State.Heroes[b.AvailableHero].GainResource(15);b.ResourceBoostSelected=boost;return b;};
        var standard=boostBattle(false);var enhanced=boostBattle(true);int who=enhanced.AvailableHero,cost=enhanced.SkillResourceCost(who,0),balance=enhanced.State.Heroes[who].JobResource;
        check(cost>0 && enhanced.PreviewDamage(who,0,"body")>standard.PreviewDamage(who,0,"body"),"Opt-in resource improves damage preview");
        check(enhanced.Act(who,0,"body") && !enhanced.ResourceBoostSelected,"Boost choice resets after accepted skill");
        var events=enhanced.DrainPresentationEvents();var hit=events.First(e=>e.Actor==who && e.Kind==BattlePresentationKind.Attack);
        check(hit.Resources[who]==balance-cost,"Optional resource charged once at attack snapshot");
        var invalid=boostBattle(true);int hp=invalid.State.BossHitPoints,resource=invalid.State.Heroes[invalid.AvailableHero].JobResource;long clock=invalid.Clock;
        check(!invalid.Act(invalid.AvailableHero,0,"missing") && hp==invalid.State.BossHitPoints && resource==invalid.State.Heroes[invalid.AvailableHero].JobResource && clock==invalid.Clock && invalid.ResourceBoostSelected,"Invalid target preserves boost choice, time and resource");
        foreach(var kind in EnemyStatusState.Kinds){
            var h=new BattleHero("status-test",1000,100,10);
            h.AddStatus(new EnemyStatusDef{kind=kind,amount=99});check(!h.Status.Active(kind),"Status accumulates before threshold "+kind);
            h.AddStatus(new EnemyStatusDef{kind=kind,amount=1});check(h.Status.Active(kind) && h.Status.Meter(kind)==0,"Threshold activates status "+kind);
            check(!string.IsNullOrEmpty(EnemyStatusState.EffectDescription(kind)),"Every status explains effects "+kind);
            if(kind=="poison" || kind=="burn" || kind=="bleed" || kind=="frostbite"){
                int expected=kind=="poison"?30:20;check(h.HitPoints==1000-expected,"Damage on activation "+kind);h.FinishStatusAction(false);check(h.HitPoints==1000-2*expected,"Damage at action end "+kind);
            }
            if(kind=="burn" || kind=="sickness")check(h.Attack==80,"Attack down "+kind);
            if(kind=="frostbite")check(h.Speed==80,"Frostbite speed down");
            if(kind=="bleed"){int before=h.HitPoints;h.Heal(100);check(h.HitPoints==before,"Bleed prevents healing");}
            if(kind=="fracture"){h.FinishStatusAction(false);check(h.HitPoints==1000,"Nonattack does not trigger fracture");h.FinishStatusAction(true);check(h.HitPoints==950,"Attack triggers fracture recoil");}
            if(kind=="absent"){h.TakeDamage(100);check(!h.Status.Active(kind),"Damage clears absent");h.AddStatus(new EnemyStatusDef{kind=kind,amount=100});h.FinishStatusAction(false);check(h.HitPoints==950,"Absent heals at skipped action end");}
            if(kind=="electrified")check(h.FinishStatusAction(false)==40 && h.FinishStatusAction(false)==20,"Electrified activation and subsequent action delays");
        }
        var resistant=new BattleHero("resistant",1000,100,10,statusResistances:new[]{new EnemyStatusResistanceDef{kind="poison",resistanceBp=5000}});resistant.AddStatus(new EnemyStatusDef{kind="poison",amount=100});check(!resistant.Status.Active("poison") && resistant.Status.Meter("poison")==50,"Hero status susceptibility changes buildup");
        var state=new BattleState(1,Enumerable.Range(0,5).Select(i=>new BattleHero("h"+i,1000,100,10)),Enumerable.Range(0,4).Select(i=>new BattlePart("p"+i,1000,"")),10000,4){ReferenceStatusRules=true};
        var lightning=new BattleSkill("lightning",1,0,attributes:new[]{"雷"});state.BossStatus.Add(new EnemyStatusDef{kind="electrified",amount=100});check(BattleActionResolver.CalculateDamage(state,state.Heroes[0],lightning,"body")==125,"Electrified amplifies lightning");
        check(BattleActionResolver.CalculateDamage(state,state.Heroes[0],new BattleSkill("plain",1,0),"body")==100,"Electrified does not amplify unrelated attributes");
        state.BossStatus.Add(new EnemyStatusDef{kind="sickness",amount=100});check(BattleActionResolver.CalculateDamage(state,state.Heroes[0],lightning,"body")==156,"Sickness increases final damage");
        state.BossStatus.Add(new EnemyStatusDef{kind="absent",amount=100});int draws=0;var outcome=BattleActionResolver.Resolve(state,"h0",lightning,"body",n=>{draws++;return 0;});check(outcome.Critical && draws==1 && !state.BossStatus.Active("absent"),"Absent adds critical opportunity then clears from hit");
        var skipped=boostBattle(false);int a=skipped.AvailableHero;skipped.State.Heroes[(a+1)%5].AddStatus(new EnemyStatusDef{kind="stun",amount=100});skipped.Pass();check(skipped.DrainPresentationEvents().Any(e=>e.Message.Contains("状態異常により行動をスキップ")),"Hero stun is automatically skipped");
        var castDefs=fresh();foreach(var s in castDefs.skills)s.chainEligible=false;
        var cast=new PlayableBattle(1,new PlayableProgress(),714,combatDefinitions:castDefs);
        while(cast.AvailableHero!=3 && !cast.Ended)cast.Pass();cast.State.Heroes[3].GainResource(15);cast.ResourceBoostSelected=true;
        int castCost=cast.SkillResourceCost(3,2),castBalance=cast.State.Heroes[3].JobResource;
        check(cast.Act(3,2,"body"),"Enhanced cast accepts current skill");
        var castEvents=cast.DrainPresentationEvents();
        check(castEvents.First(e=>e.Kind==BattlePresentationKind.CastStart && e.Actor==3).Resources[3]==castBalance-castCost,"Cast reserves optional resource exactly once");
        cast.State.Heroes[3].AddStatus(new EnemyStatusDef{kind="stun",amount=150});int castGuard=0;var releases=new List<BattlePresentationEvent>();
        while(cast.IsCasting(3) && !cast.Ended && castGuard++<100){cast.Pass();releases.AddRange(cast.DrainPresentationEvents());}
        check(releases.Any(e=>e.Actor==3 && e.Kind==BattlePresentationKind.CastCanceled) && !releases.Any(e=>e.Actor==3 && e.Kind==BattlePresentationKind.CastRelease),"Stun skips pending release without a duplicate attack");
        Func<int,string> replay=seed=>{
            var c=fresh();var b=new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:c,colossusDefinition:ProductionEconomyCatalog.Enemy(WorldCatalog.ColossusIds[seed%WorldCatalog.ColossusIds.Count]));
            var trace=new List<string>();int steps=0;
            while(!b.Ended && steps++<500){int actor=b.AvailableHero;if(actor<0)break;b.ResourceBoostSelected=steps%2==0;bool acted=false;for(int i=0;i<3;i++){if(b.Act(actor,(steps+i)%3,"body")){acted=true;break;}}if(!acted)b.Pass();foreach(var e in b.DrainPresentationEvents())trace.Add(e.Clock+":"+e.Actor+":"+e.Kind+":"+e.Damage+":"+e.BossHp+":"+string.Join(",",e.HeroHp)+":"+string.Join(",",e.HeroStatuses));}
            check(b.Ended && steps<500,"Current combat with beast status attacks terminates seed "+seed);return string.Join(";",trace);
        };
        for(int seed=0;seed<15;seed++)check(replay(seed)==replay(seed),"Current resource/attribute/status battle deterministic replay "+seed);
        var healDefs=healingFixture();healDefs.optionalResourceBoost=true;foreach(var s in healDefs.skills)s.chainEligible=false;
        var jammedHeal=new PlayableBattle(1,new PlayableProgress(),71,combatDefinitions:healDefs);
        while(jammedHeal.AvailableHero!=3 && !jammedHeal.Ended)jammedHeal.Pass();
        jammedHeal.State.Heroes[0].TakeDamage(30);jammedHeal.State.Heroes[1].TakeDamage(30);jammedHeal.State.Heroes[3].AddStatus(new EnemyStatusDef{kind="jamming",amount=100});
        check(jammedHeal.ActWithAllies(3,1,"body",Array.Empty<int>()) && jammedHeal.LastHealingTargets.Count==1,"Jamming chooses eligible healing target without manual selection");
        var frozen=new BattleSkill("frozen",1,0,attributes:new[]{"火"});frozen.Attributes[0]="水";check(frozen.Attributes[0]=="火","Reserved attack attributes cannot be mutated through a getter");
    }
}
