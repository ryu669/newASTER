using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan9StoryTests
{
    public static readonly string[] Heroes={"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna","heroine.excalipan"};
    public static void Run(Action<bool,string> check,string json,CombatDefinitionCatalog combat)
    {
        Func<ProductionStoryContent> fresh=()=>JsonSerializer.Deserialize<ProductionStoryContent>(json,new JsonSerializerOptions{IncludeFields=true});
        var pack=fresh();pack.Validate(Heroes,WorldCatalog.ColossusIds.ToArray());
        check(pack.chapters.Length==60 && pack.chapters.Sum(c=>c.poems.Length)==450 && pack.events.Length==25,"Complete authored production story passes core validation");
        check(pack.chapters.All(c=>!c.id.StartsWith("trial.")) && pack.events.All(e=>!e.id.StartsWith("trial.")),"Production identities remain separate from diagnostic profiles");
        foreach(var hero in Heroes){
            check(pack.chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.poems).Select(p=>p.sourcePoemId).Distinct().Count()==18,"Each heroine has eighteen individually selected correspondences");
            check(pack.events.Count(e=>e.ownerId==hero && e.establishesLover)==1 && pack.events.Single(e=>e.ownerId==hero && e.establishesLover).id==hero+".event.2","Only the mutual confession establishes the lover relationship");
        }
        Action<Action<ProductionStoryContent>> reject=mutate=>{var value=fresh();mutate(value);bool failed=false;try{value.Validate(Heroes,WorldCatalog.ColossusIds.ToArray());}catch(ArgumentException){failed=true;}check(failed,"Malformed production narrative is rejected before use");};
        reject(p=>p.schemaVersion=2);reject(p=>p.contentVersion="future");reject(p=>p.provenance="unknown");
        reject(p=>p.chapters=null);reject(p=>p.chapters=p.chapters.Take(59).ToArray());reject(p=>p.chapters[0]=null);
        reject(p=>p.chapters[0].id=p.chapters[1].id);reject(p=>p.chapters[0].ownerId="unknown");
        reject(p=>p.chapters[0].poems=null);reject(p=>p.chapters[0].poems[0]=null);
        reject(p=>p.chapters[0].poems[0].body="No quoted poem");reject(p=>p.chapters[0].introduction="【動作検証用】");
        reject(p=>p.chapters[0].poems[0].id="foreign.poem");
        reject(p=>p.chapters.First(c=>c.ownerId==Heroes[0]).poems[0].sourcePoemId="missing");
        reject(p=>p.chapters.First(c=>c.ownerId==Heroes[0]).poems[0].reason="");
        reject(p=>{var c=p.chapters.First(v=>v.ownerId==Heroes[0]);c.poems[1].sourcePoemId=c.poems[0].sourcePoemId;});
        reject(p=>{var c=p.chapters.First(v=>v.ownerId==WorldCatalog.ColossusIds[0]);c.poems[0].sourcePoemId="unexpected";});
        reject(p=>p.events=null);reject(p=>p.events=p.events.Take(24).ToArray());reject(p=>p.events[0]=null);
        reject(p=>p.events[0].id=p.events[1].id);reject(p=>p.events[0].ownerId="unknown");
        reject(p=>p.events[0].affectionRequired=999);reject(p=>p.events[0].establishesLover=!p.events[0].establishesLover);
        reject(p=>p.events[0].paragraphs=new[]{"too short"});reject(p=>p.events[0].paragraphs=null);
        reject(p=>p.events[0].paragraphs[0]=p.events[1].paragraphs[0]);
        reject(p=>p.events[0].expressions=new[]{"normal"});reject(p=>p.events[0].expressions[0]="unknown");
        reject(p=>p.events[0].cgResourcePath=p.events[1].cgResourcePath);reject(p=>p.events[0].backgroundResourcePath="");
        bool invalidOwners=false;try{pack.Validate(new[]{Heroes[0],Heroes[0],Heroes[2],Heroes[3],Heroes[4]},WorldCatalog.ColossusIds.ToArray());}catch(ArgumentException){invalidOwners=true;}
        check(invalidOwners,"Duplicated production roster is rejected");
        check(HeroineRosterCatalog.InitialFive(combat).Page(0,20).Select(e=>e.id).OrderBy(id=>id).SequenceEqual(combat.FormationIds.OrderBy(id=>id)),"The production roster exposes exactly the five completed heroines while reserving eight jobs");
        var collection=ProductionStoryCatalog.Collection(combat,pack);var home=ProductionStoryCatalog.Home(combat,pack);
        System.IO.Directory.CreateDirectory("tmp");System.IO.File.WriteAllText("tmp/plan9-home-catalog.json",JsonSerializer.Serialize(home,new JsonSerializerOptions{IncludeFields=true}));
        ValidateGardens(check,combat,collection,home);
        ValidateEconomy(check,combat,collection,home);
        BookRedesignTests.Run(check,combat,collection,home);
        var legacy=CollectionContractFixture.Create(combat);
        check(collection.poems.Select(p=>p.id).SequenceEqual(legacy.poems.Select(p=>p.id)) && collection.chapters.Select(c=>c.id).SequenceEqual(legacy.chapters.Select(c=>c.id)),"Production collection preserves every existing canonical poem and chapter ID");
        check(collection.links.Length==90 && collection.chapters.All(c=>c.textId.StartsWith("text.production.")),"All ninety authored correspondences and sixty chapter texts are connected");
        check(home.scripts.Length==85 && home.texts.All(t=>!t.text.Contains("【動作検証用") && !t.text.Contains("【オリジナル試遊本文】")),"Production home contains only the eighty-five authored scenes");
        foreach(var chapter in pack.chapters){
            var target=home.chapters.Single(c=>c.id==chapter.id);
            check(target.requiredPoemIds.SequenceEqual(chapter.poems.Select(p=>p.id)),"Authored chapter requires its own poems");
            var lines=home.scripts.Single(s=>s.id==target.sceneId).commands.Where(c=>c.kind=="line").Select(c=>home.texts.Single(t=>t.id==c.textId).text).ToArray();
            check(lines.SequenceEqual(new[]{chapter.introduction}.Concat(chapter.poems.Select(p=>p.body)).Concat(new[]{chapter.conclusion})),"Chapter intro poem bodies and conclusion retain authored order");
        }
        foreach(var script in home.scripts){
            var session=new AdvSession(home,script.id,"validation.scene",true,null);int steps=0;
            while(!session.EndReached && steps++<200){session.Advance();session.Tick(10);}
            check(session.EndReached && steps<200,"Every production scene reaches explicit end without fixture commands");
        }
        foreach(var e in pack.events){
            var target=home.events.Single(v=>v.id==e.id);int index=int.Parse(e.id.Substring(e.id.LastIndexOf('.')+1));
            check(target.unlockCondition.items.Any(c=>c.domain=="affection" && c.value==e.affectionRequired) && (index==0 || target.unlockCondition.items.Any(c=>c.domain=="eventRead" && c.id==e.ownerId+".event."+(index-1))),"Production event combines affection and preceding read completion");
            check(index<3 || target.unlockCondition.items.Any(c=>c.domain=="lover" && c.id==e.ownerId),"Post-confession events require actual lover state");
            var script=home.scripts.Single(s=>s.id==target.sceneId);
            check(script.commands.Count(c=>c.kind=="cg")==1 && script.commands.Count(c=>c.kind=="hideCg")==1 && script.commands.Where(c=>c.kind=="actor").Select(c=>c.expressionId).SequenceEqual(e.expressions.Select(x=>"expression."+x)),"Each event owns one CG and preserves all expression cues and return");
        }
        home.Validate(true);var unaccepted=JsonSerializer.Deserialize<HomeExperienceCatalog>(JsonSerializer.Serialize(home,new JsonSerializerOptions{IncludeFields=true}),new JsonSerializerOptions{IncludeFields=true});unaccepted.assets[0].placeholder=true;bool releaseRejected=false;try{unaccepted.Validate(true);}catch(HomeDefinitionException){releaseRejected=true;}
        check(releaseRejected,"An individually unaccepted asset blocks the otherwise complete release catalog");
        var options=new JsonSerializerOptions{IncludeFields=true};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<FormalCampaignSave,FormalCampaignSave> clone=s=>JsonSerializer.Deserialize<FormalCampaignSave>(encode(s),options);
        var priorHome=HomeExperienceFixture.Create(combat);
        string oldChapter=pack.chapters[0].id,hero0=Heroes[0];
        var prior=new FormalCampaignSave{
            world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),
            growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=123,stones=7,heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},
            collection=new FormalCollectionLedger{materials=new[]{new CollectionMaterial{id=legacy.owners[0].materialIds[0],sourceColossusId=legacy.owners[0].id,amount=17}}},
            home=FormalHomeProgress.Empty(HomeExperienceCatalog.FixtureVersion)};
        prior.world.unlockedStoryIds=new[]{oldChapter};prior.world.readStoryIds=new[]{oldChapter};prior.world.poemIds=pack.chapters[0].poems.Select(p=>p.id).ToArray();
        prior.home.affections=new[]{new HomeAffection{heroineId=hero0,value=20}};
        prior.home.unlockedEventIds=new[]{hero0+".event.0",hero0+".event.1",hero0+".event.2"};prior.home.readEventIds=prior.home.unlockedEventIds.ToArray();prior.home.loverHeroineIds=new[]{hero0};
        prior.home.readLineKeys=new[]{new HomeReadLine{sceneId=priorHome.events.Single(e=>e.id==hero0+".event.0").sceneId,lineId="line.one",scriptVersion=1}};
        string bytes=encode(prior);var migrated=ProductionStoryMigration.Prepare(prior,combat,pack,clone);
        check(encode(prior)==bytes && migrated.revision==prior.revision+1,"Narrative migration prepares a separate revision without touching original save");
        check(encode(migrated).Contains("previousNarrative") && migrated.previousNarrative.readStoryIds.Contains(oldChapter) && migrated.previousNarrative.readEventIds.SequenceEqual(prior.home.readEventIds) && migrated.previousNarrative.readLineKeys[0].sceneId==prior.home.readLineKeys[0].sceneId,"Diagnostic read flags and scene keys remain archived");
        check(!migrated.world.readStoryIds.Contains(oldChapter) && !migrated.home.readEventIds.Any() && !migrated.home.loverHeroineIds.Any() && !migrated.home.readLineKeys.Any(),"Diagnostic reads and relationship never imply authored production completion");
        check(migrated.world.poemIds.SequenceEqual(prior.world.poemIds) && migrated.world.unlockedStoryIds.Contains(oldChapter) && migrated.growth.nectar==123 && migrated.growth.stones==7 && migrated.collection.materials[0].amount==17 && migrated.home.affections[0].value==20,"Migration preserves poems unlocks wallet inventory and affection");
        check(migrated.home.unlockedEventIds.SequenceEqual(new[]{hero0+".event.0"}),"High affection alone cannot skip the authored event chain");
        check(!ProductionStoryMigration.Required(migrated) && encode(ProductionStoryMigration.Prepare(migrated,combat,pack,clone))==encode(migrated),"Completed migration is idempotent without a new revision");
        bool trialRejected=false;var trial=clone(prior);trial.home.contentVersion=HomeExperienceCatalog.TrialVersion;
        try{ProductionStoryMigration.Prepare(trial,combat,pack,clone);}catch(ArgumentException){trialRejected=true;}
        check(trialRejected,"Separate trial profile cannot silently become a normal production save");
        bool cloneRejected=false;try{ProductionStoryMigration.Prepare(prior,combat,pack,s=>s);}catch(ArgumentException){cloneRejected=true;}
        check(cloneRejected,"Migration rejects shared campaign objects");
        var noLedger=clone(prior);noLedger.collection=null;var without=ProductionStoryMigration.Prepare(noLedger,combat,pack,clone);
        HomeRules.Grant(without,home,Array.Empty<HomeCost>());
        check(without.collection.contentVersion==CollectionCatalog.ProductionVersion,"First production read on a pre-collection save creates the correct ledger version");
        Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var journal=new FormalCampaignJournal(migrated,encode,decode);
        for(int index=0;index<3;index++){
            var e=home.events.Single(v=>v.id==hero0+".event."+index);
            check(HomeConditions.Evaluate(e.unlockCondition,journal.Snapshot),"Authored event becomes readable only after preceding completion");
            var session=new AdvSession(home,e.sceneId,e.id,false,null);int steps=0;
            while(!session.EndReached && steps++<200){session.Advance();session.Tick(10);}
            var request=new FormalHomeRequest("production-event-read-"+index,"sceneEnd",journal.Snapshot.revision,home.contentVersion,session.SourceId+"/"+session.SceneId+"/"+session.ScriptVersion);
            string before=encode(journal.Snapshot);
            check(journal.CommitAdvEnd(request,home,session,s=>false)==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==before,"Failed production ADV save publishes no read affection relationship or wallet change");
            check(journal.CommitAdvEnd(request,home,session,s=>true)==GrowthCommitResult.Committed,"Retry publishes the same production ADV completion");
            check(journal.Snapshot.home.loverHeroineIds.Contains(hero0)==(index==2),"Only completed mutual confession grants lover state");
            journal=new FormalCampaignJournal(decode(encode(journal.Snapshot)),encode,decode);
        }
        check(journal.Snapshot.growth.nectar==123 && journal.Snapshot.collection.materials[0].amount==17 && journal.Snapshot.home.unlockedEventIds.Contains(hero0+".event.3"),"Restart retains event chain without unconfigured rewards");
        var collected=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion)};
        var collectionJournal=new FormalCampaignJournal(collected,encode,decode);
        foreach(string enemy in WorldCatalog.ColossusIds){
            var session=new BattleCollectionSession(collection,"production-source-"+enemy,enemy,1,collectionJournal.Snapshot.revision,combat.FormationIds,31,ColossusCombatCatalog.Get(enemy).contentVersion);
            foreach(string poem in collection.owners.Single(o=>o.id==enemy).poemIds)session.RecordCompletedSinging(poem);
            var receipt=session.Finish(BattleEndReason.Retreat,collectionJournal.Snapshot.world.poemIds,collectionJournal.Snapshot.world.unlockedStoryIds);
            var request=new FormalBattleEndRequest(receipt,collectionJournal.Snapshot.revision);
            check(collectionJournal.CommitBattleEnd(request,collection,null,s=>true,home)==GrowthCommitResult.Committed,"Production singing collection atomically applies authored correspondences");
        }
        check(collectionJournal.Snapshot.world.poemIds.Length==450 && collectionJournal.Snapshot.world.unlockedStoryIds.Length==60,"All fifteen source collections unlock exactly 450 poems and sixty authored chapters");
        check(collectionJournal.Snapshot.home.readEventIds.Length==0 && collectionJournal.Snapshot.home.loverHeroineIds.Length==0 && collectionJournal.Snapshot.world.readStoryIds.Length==0,"Collecting poems never grants story reads or romantic relationship");
        check(collectionJournal.Snapshot.collection.materials.Length==0 && collectionJournal.Snapshot.world.firstClearIds.Length==0,"Retreat collection never grants materials or world clears");
        string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"newaster-production-story-"+Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);string path=System.IO.Path.Combine(directory,"campaign.json");
        try{
            var store=new FormalCampaignStore(path,encode,decode,s=>JsonSerializer.Deserialize<FormalCampaignHeader>(s,options));
            check(store.Save(prior) && store.Save(migrated),"Physical store atomically commits one migration revision");
            check(System.IO.File.ReadAllText(path+".bak")==bytes,"Migration backup retains exact prior formal envelope");
            check(store.Load(out var loaded)==FormalLoadResult.Loaded && encode(loaded)==encode(migrated),"Production versions and narrative archive survive physical reload");
            loaded.collection.ValidateContent(collection);loaded.home.ValidateContent(home,loaded);
            var future=clone(migrated);future.previousNarrative.version=2;string unknown=encode(future);System.IO.File.WriteAllText(path,unknown);
            check(store.Load(out loaded)==FormalLoadResult.Blocked && System.IO.File.ReadAllText(path)==unknown,"Unknown archive version cannot fall back to older valid backup");
            check(store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Recovery respects unknown narrative archive version");
            unknown=encode(migrated).Replace(HomeExperienceCatalog.ProductionVersion,"home-production-future");System.IO.File.WriteAllText(path,unknown);
            check(store.Load(out loaded)==FormalLoadResult.Blocked && System.IO.File.ReadAllText(path)==unknown,"Future production home version preserves primary bytes");
            unknown=encode(migrated).Replace(CollectionCatalog.ProductionVersion,"collection-production-future");System.IO.File.WriteAllText(path,unknown);
            check(store.Load(out loaded)==FormalLoadResult.Blocked && System.IO.File.ReadAllText(path)==unknown,"Future production collection version preserves primary bytes");
        }finally{
            foreach(string file in System.IO.Directory.GetFiles(directory))System.IO.File.Delete(file);
            System.IO.Directory.Delete(directory);
        }
    }
    private static void ValidateEconomy(Action<bool,string> check,CombatDefinitionCatalog combat,CollectionCatalog collection,HomeExperienceCatalog home)
    {
        var banner=ProductionEconomyCatalog.Kinder(Heroes);banner.Validate(Heroes);
        for(int i=0;i<5;i++){int call=0;var outcome=banner.Draw(max=>call++==0?299:i);check(outcome.kind=="heroine" && outcome.heroineId==Heroes[i],"All five equally weighted heroine outcomes resolve");}
        int draw=0;check(banner.Draw(max=>draw++==0?300:8999).kind=="nectar","Production category boundary grants nectar");
        draw=0;check(banner.Draw(max=>draw++==0?9999:9000).kind=="crystal","Production material boundary grants crystal");
        var growth=new FormalGrowthSave{saveId="newaster.formal-growth",stones=3000,heroines=Heroes.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()};
        var journal=new FormalProgression(growth,Heroes);var request=new KinderRequest("production.draw",0,KinderOperation.StoneDraw,10,bannerVersion:banner.contentVersion);int calls=0;
        check(journal.CommitKinder(request,banner,max=>{calls++;return max==10000?9999:0;},s=>false)==GrowthCommitResult.SaveFailed && journal.Snapshot.stones==3000,"Production ten-draw save failure retains wallet");int once=calls;
        check(journal.CommitKinder(request,banner,max=>{throw new Exception("Retry must not draw again");},s=>true)==GrowthCommitResult.Committed && calls==once && journal.Snapshot.stones==0 && journal.Snapshot.kinderPoints==10 && journal.Snapshot.nectar==2000,"Production ten-draw retry persists the same outcomes and exact cost");
        foreach(string hero in Heroes){var nodes=home.weaponNodes.Where(n=>n.heroineId==hero).ToArray();check(nodes.Length==13 && nodes.All(n=>n.abilityId=="ability.production.weapon-attack") && nodes.Where(n=>!n.initial).Select(n=>n.terminal).Distinct().Count()==12,"Every heroine owns thirteen distinct authored tree nodes");check(nodes.Where(n=>!n.initial).All(n=>n.costs.All(c=>collection.resources.Any(r=>r.id==c.resourceId && r.kind=="material"))),"Tree materials come from reachable colossi");}
        var ledger=new FormalCollectionLedger{contentVersion=collection.contentVersion};
        var baseline=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:growth);
        var heroDef=combat.Hero(Heroes[0]);var job=combat.Job(heroDef.jobId);int rawAttack=FormalGrowthMath.Stat(job.attack,heroDef.attackBp,1,0),rawHp=FormalGrowthMath.Stat(job.hp,heroDef.hpBp,1,0);
        foreach(var relic in collection.relics.Where(r=>!r.id.StartsWith("relic.",StringComparison.Ordinal))){
            ledger.relics=new[]{new CollectionRelic{id=relic.id,contentVersion=collection.contentVersion,attackRoll=100,hpRoll=1000}};ledger.equipment=new[]{new CollectionEquipment{heroineId=Heroes[0],relicId=relic.id}};
            var battle=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:growth,collectionGrowth:ledger,relicCatalog:collection);
            check(battle.State.Heroes[0].Attack==((rawAttack+110)*(100+relic.attackPercent)/100)*(100+heroDef.traitAttackPercent)/100,"Own relic attack ability reaches battle projection before own trait");
            check(battle.State.Heroes[0].MaxHitPoints==((rawHp+1050)*(100+relic.hpPercent)/100)*(100+heroDef.traitHpPercent)/100,"Own relic HP ability reaches battle projection before own trait");
            check(Enumerable.Range(0,5).All(i=>battle.ChainRate(i)==baseline.ChainRate(i)),"Production equipment preserves common chain probability");
        }
        var copy=collection.Copy();check(copy.relics.Select(r=>r.attackPercent+":"+r.hpPercent).SequenceEqual(collection.relics.Select(r=>r.attackPercent+":"+r.hpPercent)),"Catalog copy preserves every own relic ability");
        copy.relics[0].hpPercent=101;bool rejected=false;try{copy.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Unsupported production relic percentage is rejected");
        ProductionEconomyCatalog.Engagement().Validate();
    }
    private static void ValidateGardens(Action<bool,string> check,CombatDefinitionCatalog combat,CollectionCatalog collection,HomeExperienceCatalog home)
    {
        check(home.gardens.Length==9 && home.gardens.All(g=>!g.unmade && g.middleAssetIds.Length==1 && g.foregroundAssetIds.Length==1),"All nine production garden layouts have ordered three-layer scenery");
        check(home.furniture.Length==10 && home.furniture.Take(3).Select(f=>f.id).SequenceEqual(new[]{"furniture.fixture.0","furniture.fixture.1","furniture.fixture.2"}),"Ten production recipes preserve all preexisting furniture identities");
        var options=new JsonSerializerOptions{IncludeFields=true};Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion),collection=new FormalCollectionLedger{contentVersion=collection.contentVersion,materials=home.materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=100}).ToArray()}};
        foreach(var d in save.world.terraform.domains){d.currentLevel=5;d.maxReachedLevel=5;}TerraformRules.RefreshUnlocks(save.world);
        save.world.unlockedGardenIds=home.gardens.Select(g=>g.id).ToArray();HomeConditions.Refresh(save,home);var journal=new FormalCampaignJournal(save,encode,decode);
        Action<HomeOperation> commit=op=>{string kind=op.Kind=="use"?"occupant":op.Kind=="remove"?"place":op.Kind;var request=new FormalHomeRequest("garden-"+journal.Snapshot.revision,kind,journal.Snapshot.revision,home.contentVersion,op.Key);string before=encode(journal.Snapshot);check(journal.CommitHomeOperation(request,home,op,s=>false)==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==before,"Production home operation is atomic on save failure");check(journal.CommitHomeOperation(request,home,op,s=>true)==GrowthCommitResult.Committed,"Production home retries the exact prepared operation");};
        for(int i=0;i<home.furniture.Length;i++){
            var f=home.furniture[i];string instance="production-furniture-"+i;int before=HomeRules.Balance(journal.Snapshot,f.costs[0].resourceId);
            commit(new HomeOperation("craft",f.id,instance));check(HomeRules.Balance(journal.Snapshot,f.costs[0].resourceId)==before-f.costs[0].amount,"Production recipe consumes its own world material exactly once");
            commit(new HomeOperation("place",instance,garden:home.gardens[0].id,zone:"zone.ground",x:.13f+.23f*(i%4),y:.5f+.14f*(i/4)));
            string hero=combat.FormationIds[i%5];commit(new HomeOperation("occupant",hero,garden:home.gardens[0].id,x:.5f,y:.7f));commit(new HomeOperation("use",hero,instance));
            check(journal.Snapshot.home.occupants.Single(o=>o.heroineId==hero).furnitureInstanceId==instance,"Every production furniture item supports real heroine use");
        }
        var snapshot=decode(encode(journal.Snapshot));snapshot.Validate();snapshot.home.ValidateContent(home,snapshot);snapshot.collection.ValidateContent(collection);
        check(snapshot.home.furnitureInstances.Length==10 && snapshot.home.furniturePlacements.Length==10,"All ten furniture items and placements survive roundtrip");
        string bytes=encode(snapshot);bool overlap=false;try{HomeRules.Apply(snapshot,home,new HomeOperation("place","production-furniture-1",garden:home.gardens[0].id,zone:"zone.ground",x:.13f,y:.5f));}catch(ArgumentException){overlap=true;}check(overlap && encode(snapshot)==bytes,"Colliding production placement is rejected without changing inventory");
        bool locked=false;snapshot.world.unlockedGardenIds=Array.Empty<string>();bytes=encode(snapshot);try{HomeRules.Apply(snapshot,home,new HomeOperation("place","production-furniture-0",garden:home.gardens[0].id,zone:"zone.ground",x:.5f,y:.5f));}catch(ArgumentException){locked=true;}check(locked && encode(snapshot)==bytes,"Production furniture cannot be placed into a locked garden");
        var poor=decode(encode(save));poor.collection.materials=Array.Empty<CollectionMaterial>();bytes=encode(poor);bool insufficient=false;try{HomeRules.Apply(poor,home,new HomeOperation("craft",home.furniture[9].id,"poor-furniture"));}catch(ArgumentException){insufficient=true;}check(insufficient && encode(poor)==bytes,"Unavailable world materials never create a production furniture item");
    }
}
