using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class BattleEndTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        var options=new JsonSerializerOptions {IncludeFields=true};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=t=>JsonSerializer.Deserialize<FormalCampaignSave>(t,options);
        var catalog=CollectionContractFixture.Create(combat.FormationIds);
        var dragon=catalog.owners[0];
        check(!FormalCampaignJsonShape.HasRootMember("{\"nested\":{\"collection\":{}}}","collection"),"Nested collection does not masquerade as envelope field");
        check(!FormalCampaignJsonShape.HasRootMember("{\"text\":\"\\\"collection\\\":{}\"}","collection"),"Quoted collection text is not an envelope member");
        check(FormalCampaignJsonShape.HasRootMember("{\"collect\\u0069on\":{\"version\":2}}","collection"),"Unicode escaped collection member remains detectable");
        check(FormalCampaignJsonShape.HasRootMember("{\"x\":[{\"text\":\"} \\\"\"}],\"collection\":{}}","collection"),"Escaped strings and nested arrays preserve root depth");
        check(FormalCampaignJsonShape.RootMemberIsNull("{\"collection\" : null}","collection") && !FormalCampaignJsonShape.RootMemberIsNull("{\"collection\":\"null\"}","collection"),"Explicit optional null distinguished from quoted payload");
        Func<FormalCampaignSave> fresh=()=>new FormalCampaignSave {world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave {saveId="newaster.formal-growth",stones=987,nectar=654}};
        Action<Action,string> reject=(f,name)=>{bool failed=false;try{f();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException){failed=true;}check(failed,name);};
        foreach(var reason in new[]{BattleEndReason.Victory,BattleEndReason.Defeat,BattleEndReason.Retreat}){
            var initial=fresh();initial.world.poemIds=new[]{"previous.legacy.poem"};
            var journal=new FormalCampaignJournal(initial,encode,decode);string before=encode(journal.Snapshot);
            var session=new BattleCollectionSession(catalog,"end."+reason,dragon.id,10,0,combat.FormationIds);
            for(int i=0;i<8;i++)session.RecordCompletedSinging(dragon.poemIds[i]);
            var r=new FormalBattleEndRequest(session.Finish(reason,initial.world.poemIds,initial.world.unlockedStoryIds),0);
            int builds=0;Func<CampaignSaveV2,CampaignSaveV2> victory=w=>{
                builds++;var state=new CampaignState(WorldCatalog.ColossusIds,w);
                state.ClaimColossusVictory(dragon.id,WorldCatalog.Colossi[0].EnvironmentTags,new VictoryReward(r.Id,10,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
                state.Playable.RecordVictory(10);return state.CreateSave();
            };
            check(journal.CommitBattleEnd(r,catalog,victory,s=>false)==GrowthCommitResult.SaveFailed,"End save failure reported");
            check(journal.HasPending && encode(journal.Snapshot)==before,"Every ending is atomic on failed save");
            reject(()=>journal.CommitActiveSeconds(60,s=>true),"Pending end blocks time checkpoint");
            reject(()=>journal.CommitWorld(journal.Snapshot.world,s=>true),"Pending end blocks world navigation");
            reject(()=>journal.CommitVictory(new FormalVictoryRequest("another",dragon.id,10,0),victory,s=>true),"Pending end blocks old victory transaction");
            var other=new FormalBattleEndRequest(new BattleCollectionSession(catalog,"other",dragon.id,10,0,combat.FormationIds).Finish(reason,initial.world.poemIds,initial.world.unlockedStoryIds),0);
            reject(()=>journal.CommitBattleEnd(other,catalog,victory,s=>true),"Different end cannot replace pending candidate");
            bool failed=false;try{journal.CommitBattleEnd(r,null,null,s=>throw new System.IO.IOException("test"));}catch(System.IO.IOException){failed=true;}
            check(failed && journal.HasPending && encode(journal.Snapshot)==before,"Exception retains same pending result");
            check(journal.CommitBattleEnd(r,null,null,s=>{s.growth.stones=0;s.collection.receipts[0].battle.formationIds[0]="changed";return false;})==GrowthCommitResult.SaveFailed,"Untrusted writer cannot mutate candidate");
            FormalCampaignSave disk=null;
            check(journal.CommitBattleEnd(r,null,null,s=>{disk=decode(encode(s));return true;})==GrowthCommitResult.Committed,"Retry commits frozen result without rebuilding");
            check(builds==(reason==BattleEndReason.Victory?1:0),"Only victory builds world reward once");
            check(disk.world.poemIds.Length==49 && disk.world.poemIds.Contains("previous.legacy.poem") && disk.world.unlockedStoryIds.Length==6,"Heard plus all starting party poems and chapters persist without deleting history");
            check(disk.collection.receipts[0].reason==reason && disk.world.claimedBattleIds.Contains(r.Id),"Every end records same battle identity");
            check(disk.growth.stones==987+(reason==BattleEndReason.Victory?50:0) && disk.growth.nectar==654+(reason==BattleEndReason.Victory?160:0),"Victory alone grants stones and nectar");
            check((disk.world.firstClearIds.Length>0)==(reason==BattleEndReason.Victory) && (disk.world.materials>0)==(reason==BattleEndReason.Victory),"Defeat and retreat never grant clear or materials");
            var restarted=new FormalCampaignJournal(disk,encode,decode);var final=encode(restarted.Snapshot);
            check(restarted.CommitBattleEnd(r,null,null,s=>throw new Exception("Duplicate write"))==GrowthCommitResult.AlreadyCommitted && encode(restarted.Snapshot)==final,"Restart/replay grants nothing twice");
            var repeat=new BattleCollectionSession(catalog,"repeat."+reason,dragon.id,10,1,combat.FormationIds);repeat.RecordCompletedSinging(dragon.poemIds[0]);
            var repeatReceipt=repeat.Finish(reason,disk.world.poemIds,disk.world.unlockedStoryIds);
            check(repeatReceipt.acquiredPoemIds.Length==0 && repeatReceipt.battle.heardPoemIds.Length==1,"Hearing and new acquisition are distinct");
        }
        foreach(var reason in new[]{BattleEndReason.Victory,BattleEndReason.Defeat,BattleEndReason.Retreat}) {
            var initial=fresh();initial.growth.heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id,level=reason==BattleEndReason.Victory?10:1}).ToArray();
            int level=reason==BattleEndReason.Defeat?50:1;
            var battle=new PlayableBattle(level,new PlayableProgress(),23,combatDefinitions:combat,formalGrowth:initial.growth,colossusDefinition:ColossusCombatCatalog.Get(dragon.id));
            var session=new BattleCollectionSession(catalog,"played."+reason,dragon.id,level,0,combat.FormationIds,23);
            var singing=new Random(23 ^ 0x534F4E47);
            battle.CompletedEnemyAction=()=>session.RecordCompletedSinging(dragon.poemIds[singing.Next(24)]);
            int steps=0;
            while(!battle.Ended && session.Snapshot.heardPoemIds.Length==0 && steps++<100)battle.Pass();
            while(reason!=BattleEndReason.Retreat && !battle.Ended && steps++<2000) {
                if(reason==BattleEndReason.Defeat || !battle.Act(battle.AvailableHero,0,"body"))battle.Pass();
                battle.DrainPresentationEvents();
            }
            check(steps<2000 && session.Snapshot.heardPoemIds.Length>0,"Real encounter records singing before bounded ending");
            check(reason==BattleEndReason.Retreat?!battle.Ended:reason==BattleEndReason.Victory?battle.State.IsVictory:!battle.State.Heroes.Any(h=>h.IsAlive),"Actions reach requested end without injected HP or songs");
            var receipt=session.Finish(reason,initial.world.poemIds,initial.world.unlockedStoryIds);
            var request=new FormalBattleEndRequest(receipt,0);
            var journal=new FormalCampaignJournal(initial,encode,decode);string before=encode(journal.Snapshot),durable=null;
            Func<CampaignSaveV2,CampaignSaveV2> victory=w=>{var state=new CampaignState(WorldCatalog.ColossusIds,w);state.ClaimColossusVictory(dragon.id,WorldCatalog.Colossi[0].EnvironmentTags,new VictoryReward(request.Id,level,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return state.CreateSave();};
            check(journal.CommitBattleEnd(request,catalog,victory,s=>false)==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==before,"Played ending preserves all state on failed save");
            check(journal.CommitBattleEnd(request,null,null,s=>{durable=encode(s);return true;})==GrowthCommitResult.Committed,"Played ending retries original result");
            var restarted=new FormalCampaignJournal(decode(durable),encode,decode);var restored=restarted.Snapshot;
            check(restored.collection.receipts.Single().battle.heardPoemIds.SequenceEqual(receipt.battle.heardPoemIds) && receipt.acquiredPoemIds.All(p=>restored.world.poemIds.Contains(p)),"Played singing and collection survive serialized restart");
            check(restored.collection.receipts.Single().battle.formationIds.SequenceEqual(combat.FormationIds),"Played ending retains original party including defeated heroes");
            check(restarted.CommitBattleEnd(request,null,null,s=>throw new Exception("Duplicate played write"))==GrowthCommitResult.AlreadyCommitted,"Played ending cannot grant twice after restart");
        }
        for(int seed=0;seed<20;seed++){
            var a=new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:combat);
            var b=new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:combat);
            var session=new BattleCollectionSession(catalog,"rng."+seed,dragon.id,1,0,combat.FormationIds);
            var singing=new Random(seed ^ 0x534F4E47);int completions=0;
            a.CompletedEnemyAction=()=>{completions++;session.RecordCompletedSinging(dragon.poemIds[singing.Next(24)]);};
            for(int step=0;step<30 && !a.Ended && !b.Ended;step++){
                a.Pass();b.Pass();
                check(a.Clock==b.Clock && a.State.BossHitPoints==b.State.BossHitPoints && a.State.Heroes.Select(h=>h.HitPoints).SequenceEqual(b.State.Heroes.Select(h=>h.HitPoints)),"Singing RNG never changes combat outcome or timeline");
            }
            check(completions>0 && session.Snapshot.heardPoemIds.Length>0,"Completed enemy command generates hearing without presentation");
            a.DrainPresentationEvents();
            check(session.Snapshot.heardPoemIds.Length>0,"Discarding presentation never discards heard songs");
        }
    }
}
