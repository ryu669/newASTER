using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class Plan10RTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};
        Func<string,CombatDefinitionCatalog> combat=path=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,path)),json);
        var all=combat("Combat/battle-plan10.json");all.Validate();
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-story-content.json")),json);
        story.Validate(all.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        check(all.HeroineIds.Length==6 && all.FormationIds.Length==5 && all.Skill("heroine.r",1).effectRuleId=="effect.allies-buff","R expands roster, preserves five battle slots, and has a party buff");
        var old=combat("Combat/battle-formal.json");
        foreach(var h in old.heroines){
            check(JsonSerializer.Serialize(h,json)==JsonSerializer.Serialize(all.Hero(h.id),json),"R leaves existing heroine definitions intact: "+h.id);
            foreach(var id in h.skills)check(JsonSerializer.Serialize(old.skills.Single(s=>s.id==id),json)==JsonSerializer.Serialize(all.skills.Single(s=>s.id==id),json),"R leaves existing skills intact: "+id);
        }
        var home=ProductionStoryCatalog.Home(all,story);var collection=ProductionStoryCatalog.Collection(all,story);
        check(home.heroineIds.Length==6 && home.weaponNodes.Count(n=>n.heroineId=="heroine.r")==13 && home.assets.All(a=>!a.placeholder),"R has complete home, weapon tree and adopted art bindings");
        check(story.chapters.Where(c=>c.ownerId=="heroine.r").Sum(c=>c.pages.Length)==30 && story.events.Count(e=>e.ownerId=="heroine.r")==5,"R has thirty narrative pages and five events");
        var growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=4321,stones=1234,heroines=old.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=20}).ToArray()};
        var save=new FormalCampaignSave{growth=growth,world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(home.contentVersion),collection=new FormalCollectionLedger{contentVersion=collection.contentVersion}};
        HomeConditions.Refresh(save,home);save.Validate();save.home.ValidateContent(home,save);save.collection.ValidateContent(collection);
        check(!save.growth.heroines.Any(h=>h.heroineId=="heroine.r") && save.growth.stones==1234,"Initial-five save validates against additive R content with no gifts or reset");
        var progression=new FormalProgression(growth,all.HeroineIds);
        var request=new GrowthRequest("test.r-recruit","heroine.r",growth.revision,GrowthOperation.ReceiveHeroine);
        check(progression.Commit(request,s=>false)==GrowthCommitResult.SaveFailed && !progression.Snapshot.heroines.Any(h=>h.heroineId=="heroine.r"),"Failed R recruitment grants nothing");
        FormalGrowthSave received=null;
        check(progression.Commit(request,s=>{received=s.Copy();return true;})==GrowthCommitResult.Committed,"Same R recruitment retry persists once");
        check(progression.Commit(request,s=>{throw new Exception("unexpected repeat save");})==GrowthCommitResult.AlreadyCommitted && received.heroines.Length==6 && received.nectar==4321 && received.stones==1234,"R recruitment is idempotent and preserves old balances");
        var notOwned=false;try{new PlayableBattle(1,new PlayableProgress(),combatDefinitions:all.WithFormation(new[]{"heroine.r"}.Concat(old.FormationIds.Skip(1)).ToArray()),formalGrowth:growth);}catch(ArgumentException){notOwned=true;}
        check(notOwned,"Unowned R cannot sortie");
        Func<int,PlayableBattle> fixture=slot=>{
            var ids=old.FormationIds;ids[slot]="heroine.r";
            var c=combat("Combat/battle-plan10.json");foreach(var j in c.jobs)j.hp=100000;
            var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=1000000;enemy.hpPerLevel=1;
            return new PlayableBattle(1,new PlayableProgress(),991,combatDefinitions:c.WithFormation(ids),formalGrowth:received,colossusDefinition:enemy,useJobRulesV2:true);
        };
        Action<PlayableBattle,int> ready=(b,actor)=>{int n=0;while(!b.Ended && b.AvailableHero!=actor && n++<500){int a=b.AvailableHero;if(a>=0 && b.JobState(a).Singing)b.ContinueSong(a);else b.Pass();}check(!b.Ended && b.AvailableHero==actor,"Artist reaches READY");};
        for(int slot=0;slot<5;slot++){var b=fixture(slot);ready(b,slot);check(b.JobState(slot).Id=="job.artist" && b.HeroineName(slot)=="R","Artist uses heroine job at every formation slot");}
        var battle=fixture(0);ready(battle,0);long clock=battle.Clock;
        check(!battle.StartSong(0) && battle.Clock==clock,"Insufficient song gauge rejects without time movement");
        check(battle.Act(0,1,"body"),"Party buff can be selected as Skill2");
        check(battle.State.Heroes.All(h=>h.TimedEffects.Any(e=>e.Kind=="attack" && e.Percent==20) && h.TimedEffects.Any(e=>e.Kind=="critical" && e.Percent==10)),"Party support reaches all five living allies");
        check(battle.State.Heroes[0].JobResource==2,"Exactly one ordinary R command earns two song units");
        ready(battle,0);battle.State.Heroes[0].GainResource(8);clock=battle.Clock;
        check(battle.StartSong(0) && battle.JobState(0).Singing,"Song starts at READY and schedules recovery");
        check(battle.State.Heroes.All(h=>h.JobAttackPercent>=5 && h.SongCriticalBonusBp>=100),"Active song supports allies independently of timed Skill2 effects");
        ready(battle,0);clock=battle.Clock;int gauge=battle.State.Heroes[0].JobResource;
        check(!battle.Act(0,0,"body") && !battle.Act(0,1,"body") && !battle.Act(0,2,"body"),"Singing blocks all three ordinary skills");
        battle.Pass();check(battle.Clock==clock && battle.State.Heroes[0].JobResource==gauge,"Singing cannot use PASS to bypass utility and gains no READY resource");
        check(battle.StopSong(0) && battle.Clock==clock && battle.AvailableHero==0 && battle.State.Heroes.All(h=>h.SongCriticalBonusBp==0),"Stop song restores same READY and removes only song effects");
        check(battle.Act(0,0,"body"),"Skill selectable immediately after stopping song");
        var empty=fixture(0);ready(empty,0);empty.State.Heroes[0].GainResource(3);check(empty.StartSong(0),"Low gauge song starts");
        int guard=0;while(empty.JobState(0).Singing && guard++<500){int a=empty.AvailableHero;if(a==0)empty.ContinueSong(0);else empty.Pass();}
        check(!empty.JobState(0).Singing && empty.State.Heroes.All(h=>h.SongCriticalBonusBp==0) && empty.State.Heroes[0].JobResource<=1,"Song drains at Battle Turn boundaries and ends without replenishing itself");
        var dead=fixture(0);ready(dead,0);dead.State.Heroes[0].GainResource(10);dead.StartSong(0);dead.State.Heroes[0].TakeDamage(dead.State.Heroes[0].MaxHitPoints);dead.Pass();
        check(!dead.JobState(0).Singing && dead.State.Heroes.All(h=>h.SongCriticalBonusBp==0),"Singer death ends party support");
        var reversed=all.WithFormation(all.FormationIds.Reverse().ToArray());
        var homeReordered=ProductionStoryCatalog.Home(reversed,story);
        check(home.weaponNodes.Select(n=>n.id).SequenceEqual(homeReordered.weaponNodes.Select(n=>n.id)),"Weapon and story catalogs are independent of selected formation order");
        Console.WriteLine("PLAN10_R_PASS roster / mixed party / singing / party buffs / additive saves / original story");
    }
}
