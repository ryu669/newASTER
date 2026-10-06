using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
public static class BattleV2Tests
{
    public static void Run(Action<bool,string> check,Func<CombatDefinitionCatalog> fresh)
    {
        Func<int,PlayableBattle> fixture=seed=>{
            var c=fresh();foreach(var s in c.skills)s.chainEligible=false;
            foreach(var j in c.jobs){j.hp=100000;j.attack=30;j.speed=100;j.initialResource=0;j.gainAtReady=3;}
            var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=1000000;enemy.enemySpeed=1;
            return new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:c,colossusDefinition:enemy,useJobRulesV2:true);
        };
        Action<PlayableBattle,int> ready=(b,a)=>{int guard=0;while(b.AvailableHero!=a && !b.Ended && guard++<100)b.Pass();check(b.AvailableHero==a,"v2 actor becomes READY");};
        var b=fixture(100);int actor=b.AvailableHero;long clock=b.Clock;int balance=b.State.Heroes[actor].JobResource;
        check(b.UsesJobRulesV2 && !b.SelectBoostUnits((actor+1)%5,1) && b.Clock==clock,"Other actor cannot issue job controls / READY freezes clock");
        check(!b.Act(actor,0,"missing") && b.Clock==clock,"Invalid command leaves frozen time unchanged");
        b.Pass();check(b.NextAt(actor)==clock+SkillTimingDefinition.Delay(b.State.Heroes[actor].Speed,25),"PASS uses WT 25");
        ready(b,actor);check(b.State.Heroes[actor].JobResource==balance,"Repeated READY/PASS generates no resources");
        var h=b.State.Heroes[actor];h.ApplySelfEffects(new[]{new TimedSelfEffectDef{kind="attack",percent=25,turns=3}});
        long created=b.Clock;int guard2=0;while(b.BattleTurn==created/100 && guard2++<100)b.Pass();
        check(h.TimedEffects.Single().RemainingCommands==2,"Crossing Clock 100 ticks timed effect once regardless of commands");
        var f=fixture(101);ready(f,0);f.State.Heroes[0].GainResource(15);int originalSpeed=f.State.Heroes[0].Speed;
        clock=f.Clock;
        check(f.ToggleReckless(0) && f.State.Heroes[0].Speed>originalSpeed && f.State.Heroes[0].JobIncomingPercent==130,"Reckless raises speed and incoming damage");
        check(f.Clock==clock,"Job mode choice leaves READY clock frozen");
        check(f.SelectBoostUnits(0,Math.Min(3,f.State.Heroes[0].JobResource)),"Arbitrary reachable orb count accepted");
        int orbs=f.SkillResourceCost(0,0),stock=f.State.Heroes[0].JobResource;
        check(orbs==3 && f.Act(0,0,"body"),"Fighter invests selected orbs in skill");
        var first=f.DrainPresentationEvents().First(e=>e.Actor==0 && e.Kind==BattlePresentationKind.Attack);
        check(first.Resources[0]==stock-3 && f.JobState(0).Gauge==30,"Orb cost charged once / BOOST gained per orb");
        for(int i=0;i<2;i++){ready(f,0);f.State.Heroes[0].GainResource(15);f.SelectBoostUnits(0,5);f.Act(0,0,"body");}
        ready(f,0);int attackBefore=f.State.Heroes[0].Attack;
        check(f.ActivateJobGauge(0) && f.State.Heroes[0].Attack>attackBefore && f.JobState(0).Gauge==0,"Fighter MAX consumes BOOST and increases stats");
        long expires=f.JobState(0).EmpoweredUntil;int expiryGuard=0;while(f.Clock<expires && expiryGuard++<300)f.Pass();
        check(!f.JobState(0).Empowered(f.Clock) && f.State.Heroes[0].JobAllStatsPercent==0,"BOOST expires on logical time independently of owner commands");
        var berserk=fixture(102);ready(berserk,1);berserk.State.Heroes[1].GainResource(15);
        check(berserk.ActivateJobGauge(1),"Berserker enters double activation at max gauge");clock=berserk.Clock;
        check(berserk.Act(1,0,"body"),"Double skill command accepted");
        check(berserk.DrainPresentationEvents().Count(e=>e.Actor==1 && e.Kind==BattlePresentationKind.Attack)==2 && berserk.JobState(1).Predation==2,"Two hits / two predation checks");
        check(berserk.NextAt(1)==clock+Math.Max(1,berserk.RecoveryDelay(1,0)*(100-fresh().Skill(fresh().formation[1],0).selfWaitReductionPercent)/100),"Two hits share one Recovery WT including skill reduction");
        var cannon=fixture(103);ready(cannon,3);cannon.State.Heroes[3].GainResource(15);
        check(cannon.SelectBlaster(3,200,2),"Blaster reserves double cast / double release");
        stock=cannon.State.Heroes[3].JobResource;long start=cannon.Clock,castAt=start+cannon.CastDelay(3,0);
        check(cannon.Act(3,0,"body") && cannon.IsCasting(3) && cannon.NextAt(3)==castAt,"Cast scheduled separately from Recovery");
        check(cannon.State.Heroes[3].JobResource==stock-3,"Cast cost paid once at selection");
        int guard=0;while(cannon.IsCasting(3) && !cannon.Ended && guard++<100)cannon.Pass();
        var casts=cannon.DrainPresentationEvents();
        check(casts.Count(e=>e.Actor==3 && (e.Kind==BattlePresentationKind.CastRelease || e.Kind==BattlePresentationKind.Attack))==2,"Reserved cast releases two attacks");
        check(cannon.NextAt(3)==castAt+cannon.RecoveryDelay(3,0),"Multi-cast shares one recovery");
        var gun=fixture(104);ready(gun,4);gun.State.Heroes[4].GainResource(15);clock=gun.Clock;
        check(gun.SelectMagazine(4,1) && gun.FullVolley(4,"body"),"Gunner chooses magazine for full volley");
        check(gun.JobState(4).Magazines[0]==6 && gun.JobState(4).Magazines[1]==0,"Full volley empties only selected magazine");
        ready(gun,4);check(!gun.ConditionsSatisfied(4,0) && gun.Reload(4),"Empty magazine requires reload action");
        check(gun.JobState(4).Magazines.All(n=>n==6),"Reload refills all magazines");
        var protect=fresh();foreach(var j in protect.jobs)j.hp=100000;
        var enemy2=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy2.enemySpeed=100;enemy2.actionCycle=new[]{new ColossusActionCombatDef{name="護衛検証の単体攻撃",damageType="physical",targetRule="single",damagePercent=100,gaugeGain=1,waitPercent=100},new ColossusActionCombatDef{name="護衛検証の全体攻撃",damageType="physical",targetRule="all",damagePercent=100,gaugeGain=0,waitPercent=100}};
        var shield=new PlayableBattle(1,new PlayableProgress(),112,combatDefinitions:protect,colossusDefinition:enemy2,useJobRulesV2:true,protectedSlot:0);
        guard=0;while(shield.EnemyActionCount==0 && guard++<100)shield.Pass();
        var hit=shield.DrainPresentationEvents().First(e=>e.Kind==BattlePresentationKind.Enemy);
        check(hit.TargetIds.Contains(shield.State.Heroes[2].Id) && !hit.TargetIds.Contains(shield.State.Heroes[0].Id),"Defender automatically covers selected ally");
        ready(shield,2);shield.State.Heroes[2].GainResource(15);check(shield.ActivateJobGauge(2),"Defender MAX activates");
        shield.State.Heroes[3].TakeDamage(shield.State.Heroes[3].MaxHitPoints);
        guard=0;while(shield.EnemyActionCount<2 && guard++<100)shield.Pass();
        var volley=shield.DrainPresentationEvents();var all=volley.First(e=>e.Kind==BattlePresentationKind.Enemy);
        check(all.TargetIds.Distinct().Count()==4 && all.TargetIds.Contains(shield.State.Heroes[0].Id),"All-target attack is not covered even with one dead ally");
        check(volley.Count(e=>e.Message.Contains("護衛反撃"))==1,"One counter for defender's own hit; reactions do not recursively cover all-target hits");
        var shuffled=protect.WithFormation(new[]{protect.formation[4],protect.formation[2],protect.formation[0],protect.formation[3],protect.formation[1]});
        var reorder=new PlayableBattle(1,new PlayableProgress(),113,combatDefinitions:shuffled,useJobRulesV2:true);
        check(reorder.JobState(0).Id=="job.gunner" && reorder.JobState(1).Id=="job.defender","Job rules follow heroine IDs after formation swap");
    }
}
