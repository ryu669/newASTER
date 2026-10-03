using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan8StoryIntegrationTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat,string json)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        var story=JsonSerializer.Deserialize<TrialStoryContent>(json,options);
        var collection=TrialStoryCatalog.Collection(combat,story);var home=TrialStoryCatalog.Home(combat,story);
        check(collection.contentVersion!=CollectionCatalog.FixtureVersion && home.contentVersion!=HomeExperienceCatalog.FixtureVersion,"Original content has distinct collection and home versions");
        var old=CollectionContractFixture.Create(combat);
        check(story.chapters.All(ch=>!old.chapters.Any(c=>c.id==TrialStoryCatalog.Id(ch.id))),"Old fixture chapter IDs cannot represent new original read completion");
        var initial=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion)};
        initial.world.unlockedStoryIds=new[]{story.chapters[0].id};initial.world.readStoryIds=new[]{story.chapters[0].id};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var journal=new FormalCampaignJournal(initial,encode,decode);
        var session=new BattleCollectionSession(collection,"story-integration",story.colossusId,1,0,combat.FormationIds,8);
        foreach(var poem in story.chapters.Take(3).SelectMany(c=>c.poems))session.RecordCompletedSinging(poem.id);
        var request=new FormalBattleEndRequest(session.Finish(BattleEndReason.Retreat,initial.world.poemIds,initial.world.unlockedStoryIds),0);
        string saved=null;
        check(journal.CommitBattleEnd(request,collection,null,s=>{saved=encode(s);return true;},home)==GrowthCommitResult.Committed,"Completed singing joins authored correspondences and home conditions atomically");
        check(journal.Snapshot.world.unlockedStoryIds.Count(id=>id.StartsWith("trial.plan8."))==8,"Hearing the 24 authored source poems unlocks exactly eight trial chapters");
        check(journal.Snapshot.world.poemIds.Length==54,"Hearing authors' source poems acquires 24 enemy and 30 heroine poems");
        string battleSaved=saved;
        var chapter=home.chapters.Single(c=>c.id==TrialStoryCatalog.Id(story.chapters[0].id));
        check(!journal.Snapshot.world.readStoryIds.Contains(chapter.id),"Existing fixture read flag never marks new trial chapter read");
        var adv=new AdvSession(home,chapter.sceneId,chapter.id,false,null);
        adv.Advance();adv.Advance();string unread=adv.LineId;
        var resumed=new AdvSession(home,chapter.sceneId,chapter.id,false,adv.NewlyRead);resumed.ResumeAtFirstUnread();
        check(resumed.LineId==unread && resumed.NewlyRead.Count==0 && resumed.Backlog.Count==2,"Resume reconstructs first unread line and backlog without generating read writes");
        int steps=0;while(!adv.EndReached && steps++<500){adv.Advance();adv.Tick(10);}
        check(adv.EndReached && steps<500,"Original trial script is playable to explicit end");
        var read=new FormalHomeRequest("read-original","sceneEnd",journal.Snapshot.revision,home.contentVersion,adv.SourceId+"/"+adv.SceneId+"/"+adv.ScriptVersion);
        string before=encode(journal.Snapshot);
        check(journal.CommitAdvEnd(read,home,adv,s=>false)==GrowthCommitResult.SaveFailed && before==encode(journal.Snapshot),"Failed original read completion leaves every saved state unchanged");
        check(journal.CommitAdvEnd(read,home,adv,s=>{saved=encode(s);return true;})==GrowthCommitResult.Committed,"Retry preserves and publishes same original read transaction");
        journal=new FormalCampaignJournal(decode(saved),encode,decode);
        check(journal.Snapshot.world.readStoryIds.Contains(chapter.id) && journal.Snapshot.home.readLineKeys.All(l=>l.sceneId==chapter.sceneId),"Restart preserves original scene read keys");
        check(journal.CommitAdvEnd(read,null,adv,s=>throw new Exception("duplicate save"))==GrowthCommitResult.AlreadyCommitted,"Original read transaction cannot publish twice");
        var replay=new AdvSession(home,chapter.sceneId,chapter.id,true,journal.Snapshot.home.readLineKeys);
        while(!replay.EndReached){replay.Advance();replay.Tick(10);}replay.MarkCommitted();
        check(encode(journal.Snapshot)==saved,"Replay is read only including currency, affection and rewards");
        check(!journal.Snapshot.home.loverHeroineIds.Any(),"Reading original chapter does not create lover state");
        string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"newaster-story-store-"+Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);string path=System.IO.Path.Combine(directory,"campaign.json");
        try{
            var store=new FormalCampaignStore(path,encode,decode,text=>JsonSerializer.Deserialize<FormalCampaignHeader>(text,options));
            check(store.Save(initial) && store.Save(decode(battleSaved)) && store.Save(journal.Snapshot),"Original trial writes ordered revisions through physical formal store");
            check(store.Load(out var loaded)==FormalLoadResult.Loaded && encode(loaded)==saved,"Known original versions pass physical save header and payload gates");
            string future=saved.Replace(HomeExperienceCatalog.TrialVersion,"home-trial-future");System.IO.File.WriteAllText(path,future);
            check(store.Load(out loaded)==FormalLoadResult.Blocked && System.IO.File.ReadAllText(path)==future,"Unknown future trial header remains blocked and byte-preserved");
        }finally{
            foreach(var file in System.IO.Directory.GetFiles(directory))System.IO.File.Delete(file);
            System.IO.Directory.Delete(directory);
        }
        bool invalid=false;try{new FormalHomeRequest("future","talk",0,"home-trial-future","operation");}catch(ArgumentException){invalid=true;}
        check(invalid,"Known trial support never accepts unknown future content versions");
        var legacyEnemy=ColossusCombatCatalog.Get(story.colossusId);var trialEnemy=ColossusCombatCatalog.GetPlan8Trial(story.colossusId);
        check(legacyEnemy.hpPerLevel==120 && legacyEnemy.damagePerLevel==2 && legacyEnemy.contentVersion==ColossusCombatDef.Version,"Normal enemy baseline remains intact");
        check(trialEnemy.hpPerLevel==600 && trialEnemy.damagePerLevel==8 && trialEnemy.contentVersion==ColossusCombatDef.Plan8Version && trialEnemy.parts.Select(p=>p.hpPerLevel).SequenceEqual(legacyEnemy.parts.Select(p=>p.hpPerLevel)),"Named Plan8 enemy profile changes body HP and attack slopes only");
        var originalBattle=new BattleCollectionSession(collection,"plan8-curve",story.colossusId,45,0,combat.FormationIds,12,trialEnemy.contentVersion);
        check(originalBattle.Snapshot.colossusVersion==trialEnemy.contentVersion,"Battle receipt freezes actual enemy profile version");
        var mixed=journal.Snapshot;var frozen=originalBattle.Finish(BattleEndReason.Retreat,mixed.world.poemIds,mixed.world.unlockedStoryIds);mixed.collection.receipts=mixed.collection.receipts.Concat(new[]{frozen}).ToArray();mixed.collection.ValidateContent(collection);
        check(true,"Known old and adjusted enemy receipts coexist without recalculating old history");
        invalid=false;try{trialEnemy.contentVersion="colossus-plan8-future";trialEnemy.Validate();}catch(ArgumentException){invalid=true;}
        check(invalid,"Unknown future enemy profile remains rejected");
        var heard=new System.Collections.Generic.HashSet<string>();var random=new Random(4);
        for(int i=0;i<24;i++)heard.Add(TrialSingingSelector.Select(collection,story.colossusId,combat.FormationIds,Array.Empty<string>(),heard,random,true));
        check(heard.Count==24,"Trial selector avoids repeat source poems until available missing sources have sung");
        string ownedSource=story.chapters[3].poems[0].sourcePoemId;
        var allSources=story.chapters.Take(3).SelectMany(c=>c.poems).Select(p=>p.id).ToArray();
        string reSung=TrialSingingSelector.Select(collection,story.colossusId,new[]{story.chapters[3].ownerId},allSources,allSources.Where(id=>id!=ownedSource),random,true);
        check(reSung==ownedSource,"Already-owned enemy source stays eligible for missing starting-hero correspondence");
        var uniform=new Random(8);string uniformExpected=allSources[uniform.Next(allSources.Length)];
        check(TrialSingingSelector.Select(collection,story.colossusId,combat.FormationIds,allSources,allSources,new Random(8),false)==uniformExpected,"Ordinary uniform selection consumes the same one RNG draw");
    }
}
