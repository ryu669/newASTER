using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class Plan119QualityTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};
        T Load<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(resources,path)),json);
        var combat=Load<CombatDefinitionCatalog>("Combat/battle-plan11-7.json");
        var story=Load<ProductionStoryContent>("Story/plan10-shangrila-story-content.json");
        var portraits=Load<HeroinePortraitCatalog>("UI/heroine-portrait-framing.json");
        var trees=Load<HeroineWeaponTreeCatalog>("UI/heroine-weapon-trees.json");
        var errors=HeroineProductionValidation.Errors(combat,story,portraits,trees);
        check(errors.Length==0,"11-9A production audit: "+string.Join("; ",errors));
        check(combat.heroines.Length==15 && combat.heroines.Select(h=>h.jobId).Distinct().Count()==13,"11-9A fifteen forms cover thirteen jobs");
        check(InteractionTraitCatalog.Ids.Length==27 && CombatTraitCatalog.Ids.Length==30,"11-9B interaction and combat catalogs have separate counts");
        foreach(var hero in combat.heroines){var set=CombatTraitCatalog.Resolve(hero);check(set.uniqueTraitIds.Length==3 && set.interactionTraitIds.Length==3 && set.VisibleIds.Length==8,"11-9A standard job 2 / character 3 / interaction 3: "+hero.id);}
        var originalInteractions=combat.heroines[0].interactionTraitIds;combat.heroines[0].interactionTraitIds=originalInteractions.Take(2).ToArray();
        check(HeroineProductionValidation.Errors(combat,story,portraits,trees).Any(e=>e.Contains(combat.heroines[0].id+" / traits /")),"11-9A missing interaction slot reports the form");combat.heroines[0].interactionTraitIds=originalInteractions;
        var original=combat.heroines[0].reactionStyleMain;combat.heroines[0].reactionStyleMain="unknown";
        check(HeroineProductionValidation.Errors(combat,story,portraits,trees).Any(e=>e.Contains(combat.heroines[0].id+" / reactionStyle /")),"11-9A errors identify form and field");combat.heroines[0].reactionStyleMain=original;
        int evictions=0;var cache=new BoundedCache<string,object>(24,()=>evictions++);
        var retained=new object();cache["keep"]=retained;
        for(int i=0;i<320;i++){cache["form."+i]=new object();check(cache.TryGetValue("keep",out var value) && ReferenceEquals(value,retained),"11-9E recent image stays available");}
        check(cache.Count==24 && evictions==297 && !cache.TryGetValue("form.0",out _),"11-9E cache remains bounded across 320 forms");
        string[] jobs=combat.heroines.Select(h=>h.jobId).Distinct().ToArray();
        var entries=Enumerable.Range(0,320).Select(i=>new HeroineRosterEntry{id="heroine.load."+i.ToString("D3"),name="Load heroine "+i,jobId=jobs[i%jobs.Length],stage="available",originalStats=true,originalSkills=true}).ToArray();
        var timer=Stopwatch.StartNew();var roster=new HeroineRoster(entries,jobs,jobs);double openMs=timer.Elapsed.TotalMilliseconds;
        var times=new double[100];for(int i=0;i<times.Length;i++){timer.Restart();var result=roster.Search("Load",null,null);times[i]=timer.Elapsed.TotalMilliseconds;check(result.Length==320,"11-9E all 320 forms searchable");}
        var personByForm=entries.Select((e,i)=>new{e.id,personId=entries[i<256?i:i-256].id}).ToDictionary(e=>e.id,e=>e.personId);
        check(personByForm.Values.Distinct().Count()==256,"11-9E 256 people and 320 forms are distinct test dimensions");
        var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=entries.Select(e=>new FormalHeroineGrowth{heroineId=e.id}).ToArray()},affection=new AffectionProgress{states=personByForm.Values.Distinct().Select(id=>new AffectionState{personId=id,level=10}).ToArray()}};
        var home=ProductionStoryCatalog.Home(combat,story);home.personLinks=home.personLinks.Concat(personByForm.Select(p=>new HomePersonLink{heroineId=p.Key,personId=p.Value})).ToArray();
        save.home=FormalHomeProgress.Empty(home.contentVersion);GardenLifeCatalog.Migrate(save);
        timer.Restart();var garden=new GardenLifeRuntime(save,home,home.gardens[0].id,119);for(int i=0;i<600;i++)garden.Tick(1f/60);double gardenMs=timer.Elapsed.TotalMilliseconds;
        check(garden.Agents.Count==8 && garden.Agents.Select(a=>a.personId).Distinct().Count()==8,"11-9E 320-form garden limits actors and excludes duplicate people");
        timer.Restart();foreach(var entry in entries){var context=new DailyInteractionContext{subject=new DailyActorContext{form=new HeroineCombatDef{id=entry.id,traitId=entry.id+".trait",interactionTraitIds=new[]{"taste.books"}},affectionLevel=10},garden=home.gardens[0].id,time="day",weather="clear",participantCount=1,participantIds=new[]{entry.id}};check(DailyInteractionSelector.Choose(DailyInteractionContent.Definitions,context,new DailyInteractionHistory(),n=>0)!=null,"11-9E daily candidate selected for every form");}double dailyMs=timer.Elapsed.TotalMilliseconds;
        string root=Path.Combine(Path.GetTempPath(),"newaster-plan119-"+Guid.NewGuid().ToString("N"));
        double saveMs,loadMs;try{
            var validation=new SaveValidationService(0,"0.9.5-rc.2",s=>JsonSerializer.Serialize(s,json),s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,json),s=>JsonSerializer.Serialize(s,json),s=>JsonSerializer.Deserialize<GameSave>(s,json));
            var slots=new SaveSlotManager(new SaveTransactionService(root,validation));timer.Restart();slots.Save(1,save,DateTime.UtcNow);saveMs=timer.Elapsed.TotalMilliseconds;timer.Restart();var loaded=slots.Load(1);loadMs=timer.Elapsed.TotalMilliseconds;
            check(loaded.growth.heroines.Length==320 && loaded.affection.states.Length==256,"11-9E 320-form/256-person durable save roundtrip");
            check(ReferenceEquals(loaded.affection.states.Single(s=>s.personId==personByForm[entries[0].id]),loaded.affection.states.Single(s=>s.personId==personByForm[entries[256].id])),"11-9E alternate form resolves the same person state");
            var next=entries.Concat(new[]{new HeroineRosterEntry{id="heroine.load.added",name="Added",jobId=jobs[0],stage="available",originalStats=true,originalSkills=true}}).ToArray();
            var progress=new FormalProgression(loaded.growth,next.Select(e=>e.id).ToArray());check(progress.Snapshot.heroines.Length==320 && slots.Load(1).growth.heroines.Length==320,"11-9E adding a definition preserves existing save");
        }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        string output=Path.GetFullPath(Path.Combine(resources,"../../../../../tmp/plan11-9"));Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output,"core-performance.json"),JsonSerializer.Serialize(new{forms=320,persons=256,personMapping="forms 0..255 canonical; 256..319 variants of 0..63 (test-only)",productionForms=combat.heroines.Length,productionPersons=combat.heroines.Select(h=>combat.PersonId(h.id)).Distinct().Count(),catalogOpenMs=openMs,searchMaxMs=times.Max(),saveMs,loadMs,garden600TicksMs=gardenMs,gardenVisibleActors=garden.Agents.Count,daily320SelectionsMs=dailyMs,portraitCacheLimit=24,treeCacheLimit=3,scope="Core roster/search/garden/daily/save timings; excludes Unity rendering, texture residency, and book opening"},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PLAN11_9_QUALITY_PASS forms=15 jobs=13 stressForms=320 searchMaxMs="+times.Max().ToString("F2")+" saveMs="+saveMs.ToString("F2")+" loadMs="+loadMs.ToString("F2")+" report="+output);
    }
}
