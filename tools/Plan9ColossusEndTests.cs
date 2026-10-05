using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan9ColossusEndTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var catalog=CollectionContractFixture.Create(combat);
        foreach(string id in ColossusCombatCatalog.AuthoredIds)foreach(var reason in new[]{BattleEndReason.Victory,BattleEndReason.Defeat,BattleEndReason.Retreat}){
            var world=new CampaignState(WorldCatalog.ColossusIds);var entry=WorldCatalog.Colossi.Single(c=>c.Id==id);
            // Fixture prerequisites isolate this encounter; tested ending itself uses real battle commands.
            foreach(var previous in WorldCatalog.Colossi.TakeWhile(c=>c.Id!=id))world.ClaimColossusVictory(previous.Id,previous.EnvironmentTags,new VictoryReward("setup."+previous.Id,1,0,0,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
            var initial=new FormalCampaignSave{world=world.CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=reason==BattleEndReason.Victory?10:1}).ToArray()},collection=new FormalCollectionLedger()};
            int level=reason==BattleEndReason.Defeat?50:1;var definition=ColossusCombatCatalog.Get(id);
            var battle=new PlayableBattle(level,new PlayableProgress(),79,combatDefinitions:combat,formalGrowth:initial.growth,colossusDefinition:definition);
            var owner=catalog.owners.Single(o=>o.id==id);
            var session=new BattleCollectionSession(catalog,"plan9."+id+"."+reason,id,level,0,combat.FormationIds,79,definition.contentVersion);
            var songs=new Random(79 ^ 0x534F4E47);battle.CompletedEnemyAction=()=>session.RecordCompletedSinging(owner.poemIds[songs.Next(owner.poemIds.Length)]);
            int commands=0;
            while(!battle.Ended && session.Snapshot.heardPoemIds.Length==0 && commands++<100)battle.Pass();
            while(reason!=BattleEndReason.Retreat && !battle.Ended && commands++<2000){
                if(reason==BattleEndReason.Defeat || !battle.Act(battle.AvailableHero,0,"body"))battle.Pass();
                battle.DrainPresentationEvents();
            }
            check(commands<2000 && session.Snapshot.heardPoemIds.Length>0,"Every authored encounter hears its own song through enemy actions");
            check(reason==BattleEndReason.Retreat?!battle.Ended:reason==BattleEndReason.Victory?battle.State.IsVictory:!battle.State.Heroes.Any(h=>h.IsAlive),"Every authored encounter reaches requested ending without injected HP");
            var receipt=session.Finish(reason,initial.world.poemIds,initial.world.unlockedStoryIds);var request=new FormalBattleEndRequest(receipt,0);
            var journal=new FormalCampaignJournal(initial,encode,decode);string before=encode(journal.Snapshot),durable=null;
            Func<CampaignSaveV2,CampaignSaveV2> victory=s=>{var restored=new CampaignState(WorldCatalog.ColossusIds,s);restored.ClaimColossusVictory(id,entry.EnvironmentTags,new VictoryReward(request.Id,level,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return restored.CreateSave();};
            check(journal.CommitBattleEnd(request,catalog,victory,s=>false)==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==before,"Every enemy ending is atomic on failed save");
            check(journal.CommitBattleEnd(request,null,null,s=>{durable=encode(s);return true;})==GrowthCommitResult.Committed,"Every enemy ending retries the original rolled outcome");
            var restart=new FormalCampaignJournal(decode(durable),encode,decode);var saved=restart.Snapshot;
            check(saved.collection.receipts.Single().battle.colossusId==id && saved.collection.receipts.Single().battle.colossusVersion==definition.contentVersion && receipt.acquiredPoemIds.All(saved.world.poemIds.Contains),"Enemy ID version and acquired songs survive restart");
            check(reason==BattleEndReason.Victory?saved.world.firstClearIds.Contains(id) && entry.EnvironmentTags.All(saved.world.environmentTags.Contains):!saved.world.firstClearIds.Contains(id),"Only victory commits this enemy first clear and its own environments");
            check(restart.CommitBattleEnd(request,null,null,s=>throw new Exception("Duplicate write"))==GrowthCommitResult.AlreadyCommitted,"Every enemy receipt prevents duplicate grants after restart");
        }
    }
}
