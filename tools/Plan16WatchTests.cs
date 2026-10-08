using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan16WatchTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};var combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10-shangrila.json")),json);var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-shangrila-story-content.json")),json);var home=ProductionStoryCatalog.Home(combat,story);var collection=ProductionStoryCatalog.Collection(combat,story);
        var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion),collection=new FormalCollectionLedger{contentVersion=collection.contentVersion}};
        save.world.unlockedGardenIds=GardenLifeCatalog.GardenIds.ToArray();GardenLifeCatalog.Migrate(save);AffectionSaveAdapter.Migrate(save,home);WeaponGrowthRules.EnsureRoots(save,home);
        var runtime=new GardenLifeRuntime(save,home,home.gardens[0].id);var target=runtime.Agents[0];runtime.SetWatchTarget(target.heroineId);check(runtime.WatchAgents.Count==4 && runtime.WatchAgents[0]==target,"WM-01/WM-03 target and three companions");
        var outside=runtime.Agents.Except(runtime.WatchAgents).ToArray();var frozen=outside.Select(a=>new{a.x,a.y,a.remaining,a.kind}).ToArray();string before=JsonSerializer.Serialize(save,json);for(int i=0;i<600;i++)runtime.Tick(.1f);
        check(frozen.SequenceEqual(outside.Select(a=>new{a.x,a.y,a.remaining,a.kind})),"WM-03 offscreen AI remains frozen");check(runtime.GardenId==home.gardens[0].id,"WM-02 watch remains in the same garden");check(JsonSerializer.Serialize(save,json)==before,"WM-09 watching life never changes affection or progression");
        runtime.Paused=true;float time=target.remaining;runtime.Tick(1);check(time==target.remaining,"WM-08 minimization suspension pauses life");runtime.Paused=false;runtime.Tick(.1f);check(runtime.WatchAgents.Count==4,"WM-08 life resumes without replacing target");runtime.SetWatchTarget(null);check(runtime.WatchAgents.Count==runtime.Agents.Count,"WM-05 normal AI roster restored");
        string unplaced=combat.HeroineIds.Last();save.gardenLife.assignments=new[]{new GardenLifeAssignment{heroineId=unplaced,mode="Hidden"}};string settings=JsonSerializer.Serialize(save.gardenLife.assignments,json);runtime=new GardenLifeRuntime(save,home,home.gardens[0].id,displayHeroineId:unplaced);runtime.SetWatchTarget(unplaced);check(runtime.WatchAgents.First().heroineId==unplaced && JsonSerializer.Serialize(save.gardenLife.assignments,json)==settings,"WM-01 unplaced target uses temporary display without changing assignments");
        var notices=new BookNotificationService();string hero=combat.HeroineIds[0];var affinity=AffectionService.State(save,home,hero);affinity.level=20;notices.Refresh(save,home,collection);bool unread=AffectionEventResolver.ForPerson(save,home,hero).Any(e=>e.requiredAffectionLevel<=20);check(!unread || (notices.For(hero)&BookNotice.Unread)!=0,"BK-09 unlocked unread events notify");affinity.readEventIds=AffectionEventResolver.ForPerson(save,home,hero).Where(e=>e.requiredAffectionLevel<=20).Select(e=>e.id).ToArray();save.revision++;notices.Refresh(save,home,collection);check((notices.For(hero)&BookNotice.Unread)==0,"BK-09 reading removes unread notification");
        save.growth.nectar=10000;save.revision++;notices.Refresh(save,home,collection);check((notices.For(hero)&BookNotice.Upgrade)!=0,"BK-09 affordable level upgrade notifies");save.growth.nectar=0;save.revision++;notices.Refresh(save,home,collection);check((notices.For(hero)&BookNotice.Upgrade)==0,"BK-09 resource consumption clears upgrade notification");
        var terra=TerraformRules.Migrate(save.world);TerraformRules.Victory(terra,TerraformRules.ColossusIds[0],1);terra.totalTp=1000;save.revision++;notices.Refresh(save,home,collection);check((notices.For("terraform.domain.life")&BookNotice.Develop)!=0,"BK-09 Terraform record plus TP creates development notification");terra.totalTp=0;save.revision++;notices.Refresh(save,home,collection);check((notices.For("terraform.domain.life")&BookNotice.Develop)==0,"BK-09 insufficient TP clears development notification");
        Console.WriteLine("PLAN16_WATCH_PASS target / four actors / frozen offscreen / no affection / temporary placement / notices");
    }
}
