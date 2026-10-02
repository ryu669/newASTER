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