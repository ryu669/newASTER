using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class CombatTraitTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        Func<CombatDefinitionCatalog> load=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan11-7.json")),options);
        var c=load();c.Validate();
        check(CombatTraitCatalog.Ids.Length==30,"Thirteen masteries and seventeen second traits");
        foreach(string id in CombatTraitCatalog.Ids){
            var d=CombatTraitCatalog.Get(id);check(d.effects.All(e=>e.rankValues.Length==5 && e.rankValues.All(n=>n>=0)),"Five rank values: "+id);
            int original=d.effects[0].Value(1);d.effects[0].rankValues[0]=9999;check(CombatTraitCatalog.Get(id).effects[0].Value(1)==original,"Catalog cannot leak mutable values: "+id);
        }
        check(Enumerable.Range(0,6).Select(CombatTraitCatalog.Rank).SequenceEqual(new[]{1,2,3,4,5,5}),"Existing duplicate stages map to rank one through five without save migration");
        var saved=new FormalGrowthSave{saveId="traits.old-save",heroines=c.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id,duplicateRank=4}).ToArray()};
        string json=JsonSerializer.Serialize(saved,options);var restored=JsonSerializer.Deserialize<FormalGrowthSave>(json,options);restored.Validate();
        var ranked=new PlayableBattle(1,new PlayableProgress(),29,combatDefinitions:c,formalGrowth:restored,useJobRulesV2:true);
        check(ranked.State.Heroes.All(h=>h.CombatTraitRank==5),"Unchanged old save payload resolves common rank five");restored.heroines[0].duplicateRank=0;
        check(ranked.State.Heroes[0].CombatTraitRank==5,"In-progress battle freezes the rank at departure");
        Action<Action,string> rejects=(action,label)=>{bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}check(rejected,label);};
        rejects(()=>CombatTraitCatalog.Get("missing"),"Unknown trait is rejected");
        foreach(var h in c.heroines){
            var set=CombatTraitCatalog.Resolve(h);var growth=new FormalHeroineGrowth{heroineId=h.id};
            var cards=HeroineIdentityCatalog.Traits(h,growth);
            check(cards[0].id==CombatTraitCatalog.Mastery(h.jobId) && cards[1].id==h.secondTraitId && cards.Length<=8,"Fixed first and second slots: "+h.id);
            check(set.VisibleIds.SequenceEqual(InteractionTraitCatalog.VisibleIds(h)),"UI and condition IDs share layout: "+h.id);
            var ids=new[]{h.id}.Concat(c.FormationIds.Where(id=>c.PersonId(id)!=c.PersonId(h.id))).Take(5).ToArray();
            var battle=new PlayableBattle(1,new PlayableProgress(),27,combatDefinitions:c.WithFormation(ids),useJobRulesV2:true);
            check(battle.State.Heroes[0].HasVisibleTrait(set.masteryTraitId) && battle.State.Heroes[0].CombatTraitRank==1,"Actual battle receives common traits: "+h.id);
            int commands=0;
            while(!battle.Ended && commands++<20){int a=battle.AvailableHero;battle.State.Heroes[a].GainResource(15);if(battle.RequiresPanzerDefense(a))battle.DefendPanzer(a);else if(battle.JobState(a).Id=="job.gambler")battle.SpinGamblerSlot(a,"body");else if(battle.PreviewDamage(a,0,"body")>0)check(battle.Act(a,0,"body"),"Actual command with common traits: "+battle.State.Heroes[a].Id);else battle.Pass();}
            check(commands>0,"Timeline runs for job: "+h.jobId);
        }
        var bad=load();bad.heroines[0].secondTraitId="nanomachine-armor";rejects(()=>bad.Validate(),"Wrong-job second trait is rejected");
        bad=load();bad.heroines[0].secondTraitId=null;rejects(()=>bad.Validate(),"Versioned content cannot silently omit common slots");
        bad=load();bad.Hero("heroine.slayer-swim").interactionTraitIds=new[]{"hair.blonde","personality.active","outfit.swimsuit","taste.books"};rejects(()=>bad.Validate(),"Five combat plus four interaction traits exceeds eight");
        bad=load();bad.generalFormations[0].enhancedSlots=null;rejects(()=>bad.Validate(),"New general requires independent enhanced definitions");
        var flexible=load();var flexibleHero=flexible.Hero("heroine.slayer-swim");flexibleHero.uniqueTraitIds=new[]{flexibleHero.traitId};flexibleHero.interactionTraitIds=new[]{"hair.blonde","personality.active","outfit.swimsuit","taste.books","appearance.glasses"};flexible.Validate();
        check(HeroineIdentityCatalog.Traits(flexibleHero,new FormalHeroineGrowth()).Length==8,"Authoring may allocate one unique and five interaction slots");
        flexibleHero.uniqueTraitIds=new[]{"missing"};rejects(()=>flexible.Validate(),"Undefined unique card is rejected");
        var fighter=c.Hero("heroine.annihilator");
        var hero=new BattleHero(fighter.id,1000,100,10);hero.InitializeCombatTraits(fighter,null,()=>0);
        var enemy=new BattleState(1,new[]{hero}.Concat(Enumerable.Range(0,4).Select(i=>new BattleHero("ally"+i,1000,100,10))),Enumerable.Range(0,4).Select(i=>new BattlePart("part"+i,10000,"")),100000,10);
        var ordinary=new BattleSkill("test",1,0);var spent=new BattleSkill("test",1,0,traitResourceSpent:true);
        check(BattleActionResolver.CalculateDamage(enemy,hero,ordinary,"body")==100,"No resource damage bonus on an ordinary hit");
        check(BattleActionResolver.CalculateDamage(enemy,hero,spent,"body")==105,"Resource attack receives mastery once");
        hero.InitializeCombatTraits(fighter,new FormalHeroineGrowth{duplicateRank=4},()=>0);
        check(BattleActionResolver.CalculateDamage(enemy,hero,spent,"body")==118,"Rank five damage is applied through actual resolver");
        hero.TraitDraw=max=>0;hero.GainResource(1);check(hero.JobResource==2,"Boost fighter grants an extra resource rather than damage");
        var sniperDef=c.Hero("heroine.shangrila");var sniper=new BattleHero(sniperDef.id,1000,100,10);sniper.InitializeCombatTraits(sniperDef,null,()=>0);check(BattleActionResolver.CalculateDamage(enemy,sniper,spent,"body")==110,"Sniper mastery and second trait stack once even on a resource-consuming pursuit");
        var healthy=c.Hero("heroine.annihilator");healthy.secondTraitId="full-power";
        var hp=new BattleHero(healthy.id,1000,100,10);hp.InitializeCombatTraits(healthy,null,()=>0);
        check(hp.Attack==105,"Full power is active at full health");hp.TakeDamage(200);check(hp.Attack==105,"Full power includes exactly eighty percent");hp.TakeDamage(1);check(hp.Attack==100,"Full power turns off below eighty percent");
        var chaser=c.Hero("heroine.nighthawk");long clock=299;var fast=new BattleHero(chaser.id,1000,100,10,100);fast.InitializeCombatTraits(chaser,null,()=>clock);
        check(fast.Speed==115,"Opening speed before three hundred clock");clock=300;check(fast.Speed==102,"Opening speed transitions to sustained speed");
        var shell=c.Hero("heroine.shell");var panzer=new BattleHero(shell.id,1000,100,0);panzer.InitializeCombatTraits(shell,null,()=>0);panzer.InitializePanzer(()=>0);
        check(panzer.ArmorMaxHitPoints==3150 && panzer.FleshMaxHitPoints==200,"Panzer mastery increases armor but never flesh HP");
        panzer.TakeDamage(200);int before=panzer.HitPoints;panzer.TickCombatTraits();check(panzer.HitPoints==before+31,"Nanomachines repair armor once per turn");
        panzer.TakeDamage(int.MaxValue);before=panzer.HitPoints;panzer.TickCombatTraits();check(panzer.HitPoints==before,"Nanomachines never heal flesh after armor breaks");
        var general=c.Hero("heroine.slayer-swim");var formation=c.generalFormations.Single(g=>g.ownerId==general.id);formation.enhancedSlots[0].attackPercent=0;formation.enhancedSlots[0].speedPercent=37;
        var team=new[]{general.id}.Concat(c.FormationIds.Where(id=>c.PersonId(id)!=c.PersonId(general.id))).Take(5).ToArray();
        var command=new PlayableBattle(1,new PlayableProgress(),71,combatDefinitions:c.WithFormation(team),useJobRulesV2:true);
        int limit=0;while(command.AvailableHero!=0 && !command.Ended && limit++<100)command.Pass();
        command.State.Heroes[0].GainResource(15);check(command.ActivateGeneralCommand(0),"General enters enhanced mode");
        check(command.State.Heroes[0].GeneralAttackPercent==0 && command.State.Heroes[0].GeneralSpeedPercent==40,"Enhanced slots are independent of normal formation and receive leader once");
        check(!command.State.Heroes[0].OopartBuffs.Any(b=>b.kind=="job.empowered"),"New general mode is not an extendable buff");
        long until=command.JobState(0).EmpoweredUntil;command.State.Heroes[0].ExtendTimedEffects();check(command.JobState(0).EmpoweredUntil==until,"Buff extension cannot extend command mode");
        var healerDef=c.Hero("heroine.annihilator-holy");
        var healerTeam=new[]{healerDef.id}.Concat(c.FormationIds.Where(id=>c.PersonId(id)!=c.PersonId(healerDef.id))).Take(5).ToArray();
        var emergency=new PlayableBattle(1,new PlayableProgress(),83,combatDefinitions:c.WithFormation(healerTeam),useJobRulesV2:true);
        var nurse=emergency.State.Heroes[0];nurse.InitializeCombatTraits(healerDef,new FormalHeroineGrowth{duplicateRank=4},()=>emergency.Clock);
        var patient=emergency.State.Heroes[1];patient.TakeDamage(patient.HitPoints-patient.MaxHitPoints/4-1);long normalWait=emergency.NextAt(0);
        emergency.EmergencyHealerReaction();check(emergency.NextAt(0)==normalWait,"Emergency does not trigger above twenty-five percent");
        patient.TakeDamage(1);emergency.EmergencyHealerReaction();check(emergency.NextAt(0)==emergency.Clock,"Rank five first emergency grants an immediate action at twenty-five percent");
        int steps=0;while(emergency.AvailableHero!=0 && !emergency.Ended && steps++<100)emergency.Pass();
        nurse.GainResource(15);check(emergency.Act(0,1,"body",1) && emergency.NextAt(0)>emergency.Clock,"Emergency action installs ordinary recovery WT afterward");
        var alchemistDef=c.Hero("heroine.oriflamme");var chemist=new BattleHero(alchemistDef.id,1000,100,15);chemist.InitializeCombatTraits(alchemistDef,null,()=>0);chemist.TickCombatTraits();check(chemist.JobResource==1,"Alchemy gains resource once at each battle-turn hook");
        var artistTeam=new[]{"heroine.r"}.Concat(c.FormationIds.Where(id=>id!="heroine.r")).Take(5).ToArray();var artist=new PlayableBattle(1,new PlayableProgress(),87,combatDefinitions:c.WithFormation(artistTeam),useJobRulesV2:true);
        check(Enumerable.Range(0,20).Sum(_=>artist.SongResourceCost(0))==38,"Song maintenance carries fractions instead of turning five percent reduction into fifty percent");
        var blasterDef=c.Hero("heroine.echidna");var blaster=new BattleHero(blasterDef.id,1000,100,10);blaster.InitializeCombatTraits(blasterDef,null,()=>0);
        enemy.AttributeResistances=new[]{new AttributeResistanceDef{attribute="火",resistanceBp=5000}};
        var fire=new BattleSkill("fire",1,0,attributes:new[]{"火"});check(BattleActionResolver.CalculateDamage(enemy,blaster,fire,"body")==52,"Blaster penetrates only a portion of positive attribute resistance");
        enemy.AttributeResistances[0].resistanceBp=-5000;check(BattleActionResolver.CalculateDamage(enemy,blaster,fire,"body")==150,"Resistance penetration preserves weaknesses");
        var operativeDef=c.Hero("heroine.arcane-academy");var operative=new BattleHero(operativeDef.id,1000,100,0);operative.InitializeCombatTraits(operativeDef,null,()=>0);
        enemy.BossStatus.Add(new EnemyStatusDef{kind="poison",amount=100});enemy.BossStatus.Add(new EnemyStatusDef{kind="burn",amount=100});
        check(BattleActionResolver.CalculateDamage(enemy,operative,ordinary,"body")==109,"Operative counts distinct active statuses once alongside gambler mastery");
        var resistantDef=c.Hero("heroine.annihilator-holy");resistantDef.secondTraitId="miracle-body";var resistant=new BattleHero(resistantDef.id,1000,100,10);resistant.InitializeCombatTraits(resistantDef,null,()=>0);
        resistant.AddStatus(new EnemyStatusDef{kind="stun",amount=100});check(!resistant.Status.Active("stun") && resistant.Status.Meter("stun")==95,"Miracle body feeds the existing accumulation resistance pipeline");
        Console.WriteLine("COMBAT_TRAITS_PASS fixed slots / rank tables / all jobs / actual damage / armor / independent formations");
    }
}
