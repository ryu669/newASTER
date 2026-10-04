using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
public static class Plan9ColossusTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        Func<int,PlayableBattle> fresh=level=>new PlayableBattle(level,new PlayableProgress(),71,combatDefinitions:combat,colossusDefinition:ColossusCombatCatalog.Get("colossus.red-crystal-tyrant"));
        Action<PlayableBattle> next=b=>{int before=b.EnemyActionCount;for(int i=0;i<100 && b.EnemyActionCount==before && !b.Ended;i++)b.Pass();check(b.EnemyActionCount==before+1,"Exactly one authored enemy action reached");};
        Action<Action,string> reject=(f,name)=>{bool failed=false;try{f();}catch(ArgumentException){failed=true;}check(failed,name);};
        var b=fresh(1);check(b.NextEnemyAction=="岩砕の顎" && b.NextEnemyTargets.Length==1,"Tyrant opens with single-target jaw");
        foreach(var h in b.State.Heroes)h.GainResource(3);
        var resources=b.State.Heroes.Select(h=>h.JobResource).ToArray();next(b);
        check(b.State.Heroes.Select((h,i)=>h.JobResource>=resources[i]).All(x=>x),"Jaw does not drain resources");
        check(b.NextEnemyAction=="掘削爪の掃射" && b.NextEnemyTargets.Length==5,"Intact mining claw changes next action to all living heroes");
        next(b);check(b.NextEnemyAction=="鉱脈吸収","Cycle advances to tail absorption");
        foreach(var h in b.State.Heroes)h.SpendResource(h.JobResource);
        b.State.Heroes[4].GainResource(b.State.Heroes[4].JobResourceMax);
        check(b.NextEnemyTargets.SequenceEqual(new[]{4}),"Tail targets highest resource deterministically");
        foreach(var h in b.State.Heroes.Take(4))h.GainResource(3);
        var noDrain=ColossusCombatCatalog.Get("colossus.red-crystal-tyrant");noDrain.actionCycle[2].drainAmount=0;
        var control=new PlayableBattle(1,new PlayableProgress(),71,combatDefinitions:combat,colossusDefinition:noDrain);next(control);next(control);
        foreach(var h in control.State.Heroes){h.SpendResource(h.JobResource);h.GainResource(3);}control.State.Heroes[4].GainResource(control.State.Heroes[4].JobResourceMax);
        next(b);next(control);
        check(b.State.BossGauge==4 && b.State.Heroes.Take(4).Select((h,i)=>control.State.Heroes[i].JobResource-h.JobResource==2).All(x=>x),"Tail adds two gauge and drains two resources beyond owner-command regeneration");
        b=fresh(1);next(b);next(b);
        b.State.BreakPart("tyrant.tail",int.MaxValue);
        check(b.NextEnemyAction=="岩砕の顎" && b.NextEnemyTargets.Length==1,"Destroyed tail replaces absorption with fallback jaw");
        b=fresh(1);next(b);b.State.BreakPart("tyrant.claw",int.MaxValue);
        check(b.NextEnemyAction=="岩砕の顎" && b.NextEnemyTargets.Length==1,"Destroyed claw removes sweeping action");
        check(b.PreviewEnemyDamage(0)<16,"Destroyed attack part reduces replacement jaw damage");
        foreach(int level in new[]{1,9,10,19,20,29,30,39,40,44,45,49,50}){
            b=fresh(level);b.State.AdvanceBossGauge(4);
            check(b.NextAttackIsMajor && b.NextEnemyAction==(level>=45?"極大技：渓谷断裂":"大技：鉱晶崩落"),"Tyrant level boundary uses its own named major");
            check(b.NextEnemyTargets.Length==5,"Major targets all living heroes");
            check(b.State.BreakPart("tyrant.crown",int.MaxValue) && b.State.BossGauge==3 && !b.NextAttackIsMajor,"Crown break cancels telegraph and stops accumulation");
            check(!b.State.BreakPart("tyrant.crown",int.MaxValue) && b.State.BossGauge==3,"Repeated crown break is inert");
            check(b.NextEnemyAction=="岩砕の顎","Interrupted major returns to authored cycle");
        }
        b=fresh(1);b.State.ApplyBossDamage(b.State.BossMaxHitPoints*59/100);check(!b.IsEnraged,"Tyrant remains calm above forty percent HP");
        b.State.ApplyBossDamage(b.State.BossMaxHitPoints/100+1);check(b.IsEnraged && b.NextEnemyAction.Contains("赤熱の咆哮"),"Tyrant enrage boundary differs from dragon");
        var source=ColossusCombatCatalog.Get("colossus.red-crystal-tyrant");b=new PlayableBattle(1,new PlayableProgress(),71,combatDefinitions:combat,colossusDefinition:source);source.actionCycle[0].name="mutated";source.actionCycle[0].gaugeGain=3;
        check(b.NextEnemyAction=="岩砕の顎","Encounter freezes its action cycle independently of source mutation");
        source=ColossusCombatCatalog.Get("colossus.red-crystal-tyrant");source.actionCycle[0].requiredPartId="unknown.part";reject(source.Validate,"Unresolved dependency is rejected");
        source=ColossusCombatCatalog.Get("colossus.red-crystal-tyrant");source.actionCycle[0].requiredPartId="tyrant.crown";reject(source.Validate,"A cycle without unconditional fallback is rejected");
        source=ColossusCombatCatalog.Get("colossus.red-crystal-tyrant");source.actionCycle[0].gaugeGain=4;reject(source.Validate,"Invalid gauge increments are rejected");
        Func<PlayableBattle> memory=()=>new PlayableBattle(1,new PlayableProgress(),72,combatDefinitions:combat,colossusDefinition:ColossusCombatCatalog.Get("colossus.memory-crystal-dragon"));
        b=memory();b.State.Heroes[3].TakeDamage(b.State.Heroes[3].MaxHitPoints/2);
        check(b.NextEnemyAction=="結晶砲撃" && b.NextEnemyTargets.SequenceEqual(new[]{3}),"Memory cannon selects lowest HP ratio");
        b.State.BreakPart("memory.cannon",int.MaxValue);
        check(b.NextEnemyAction=="機殻の踏撃","Destroyed memory cannon substitutes its own fallback");
        b=memory();next(b);check(b.NextEnemyAction=="記憶走査","Memory cycle advances to resource scan");
        b.State.AdvanceBossGauge(2);check(b.State.BossGauge==3 && !b.NextAttackIsMajor,"Zero-gain scan does not prematurely trigger major");
        next(b);check(b.State.BossGauge==3 && b.NextAttackIsMajor,"Scan preserves gauge and next fast step telegraphs major");
        b=memory();next(b);b.State.BreakPart("memory.antenna",int.MaxValue);
        check(b.NextEnemyAction=="機殻の踏撃","Antenna destruction removes scan and its drain");
        b=memory();next(b);next(b);
        check(b.NextEnemyAction=="高速照射" && b.NextEnemyTargets.Length==5,"Memory fast beam targets all living heroes");
        Func<PlayableBattle> sky=()=>new PlayableBattle(1,new PlayableProgress(),74,combatDefinitions:combat,colossusDefinition:ColossusCombatCatalog.Get("colossus.sky-tower-machine"));
        b=sky();check(b.NextEnemyAction=="気圧弾" && b.NextEnemyTargets.Length==1,"Sky opens with pressure cannon");
        next(b);check(b.NextEnemyAction=="圧力蓄積","Sky advances to pressure accumulation");
        next(b);check(b.State.BossGauge==4 && b.NextAttackIsMajor,"Pressure accumulation adds three gauge and prepares release");
        b=sky();next(b);b.State.BreakPart("sky.turbine",int.MaxValue);
        check(b.NextEnemyAction=="塔影の衝撃","Broken turbine removes pressure accumulation");
        b=sky();next(b);next(b);b.State.BreakPart("sky.ring",int.MaxValue);
        check(!b.NextAttackIsMajor && b.NextEnemyAction=="暴風放出" && b.NextEnemyTargets.Length==5,"Broken ring interrupts major while retaining authored all-target release");
        b.State.BreakPart("sky.cannon",int.MaxValue);check(b.NextEnemyAction=="塔影の衝撃","Broken sky cannon removes wind release");
        foreach(string id in ColossusCombatCatalog.AuthoredIds){
            var definition=ColossusCombatCatalog.Get(id);check(definition.id==id && definition.parts.Select(p=>p.id).Distinct().Count()==definition.parts.Length,"Every authored ID owns its combat and parts");
            check(!string.IsNullOrEmpty(ColossusCombatCatalog.IllustrationResource(id)),"Every authored enemy owns an illustration binding");
            if(definition.contentVersion==ColossusCombatDef.Plan9Version)foreach(int level in new[]{44,45,49,50}){
                b=new PlayableBattle(level,new PlayableProgress(),73,combatDefinitions:combat,colossusDefinition:definition);
                b.State.AdvanceBossGauge(definition.gaugeMax-1);
                check(b.NextAttackIsMajor && b.NextEnemyAction==(level>=45?definition.ultimateAction:definition.majorAction),"Every new enemy retains its own level-boundary telegraph");
            }
        }
    }
}
