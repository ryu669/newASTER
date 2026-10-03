using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
public static partial class PlayableBuild
{
    private static void ValidatePlan5()
    {
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-formal").text);
        var catalog=CollectionContractFixture.Create(combat);
        var restored=JsonUtility.FromJson<CollectionCatalog>(JsonUtility.ToJson(catalog));restored.Validate();
        Check(restored.poems.Length==450 && restored.chapters.Length==60 && restored.weaponNodes.Length==15,"Unity collection definition counts and nested weapon JSON");
        Func<FormalCampaignSave,string> encode=s=>JsonUtility.ToJson(s,true);
        Func<string,FormalCampaignSave> decode=s=>JsonUtility.FromJson<FormalCampaignSave>(s);
        foreach(var reason in new[]{BattleEndReason.Victory,BattleEndReason.Defeat,BattleEndReason.Retreat}){
            var initial=new FormalCampaignSave {world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave {saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()}};
            var journal=new FormalCampaignJournal(initial,encode,decode);string before=encode(journal.Snapshot);
            var session=new BattleCollectionSession(catalog,"unity.end."+reason,WorldCatalog.ColossusIds[0],50,0,combat.FormationIds,8);
            for(int i=0;i<24;i++)session.RecordCompletedSinging(catalog.owners[0].poemIds[i]);
            var req=new FormalBattleEndRequest(session.Finish(reason,initial.world.poemIds,initial.world.unlockedStoryIds),0);
            Func<CampaignSaveV2,CampaignSaveV2> world=w=>{var state=new CampaignState(WorldCatalog.ColossusIds,w);state.ClaimColossusVictory(WorldCatalog.ColossusIds[0],WorldCatalog.Colossi[0].EnvironmentTags,new VictoryReward(req.Id,50,10,8,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return state.CreateSave();};
            Check(journal.CommitBattleEnd(req,catalog,world,s=>false)==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==before,"Unity all end reasons atomically retain pending");
            Check(journal.CommitBattleEnd(req,null,null,s=>{decode(encode(s)).Validate();return true;})==GrowthCommitResult.Committed,"Unity all end JSON retry");
            var save=decode(encode(journal.Snapshot));save.Validate();
            Check(save.world.poemIds.Length==114 && save.world.unlockedStoryIds.Length==18,"Unity complete enemy and party collections survive every ending");
            Check(save.collection.receipts[0].battle.seed==8 && save.collection.receipts[0].battle.level==50 && save.collection.receipts[0].reason==reason,"Unity battle provenance and enum survive JSON");
            Check(save.growth.stones==(reason==BattleEndReason.Victory?130:0) && save.collection.materials.Length==(reason==BattleEndReason.Victory?1:0),"Unity victory-only resources");
            Check(save.collection.receipts[0].relicDrawCount==(reason==BattleEndReason.Victory?5:0),"Unity level band draw count");
            var restarted=new FormalCampaignJournal(save,encode,decode);Check(restarted.CommitBattleEnd(req,null,null,s=>false)==GrowthCommitResult.AlreadyCommitted,"Unity restart prevents end replay");
            if(reason==BattleEndReason.Victory){
                Check(save.collection.receipts[0].relicDrops.Length>0,"Unity relic outcome fixture");
                var item=save.collection.relics[0];var equip=new FormalRelicRequest("unity.equip",item.id,combat.FormationIds[0],restarted.Snapshot.revision,RelicOperation.Equip);
                Check(restarted.CommitRelic(equip,catalog,s=>false)==GrowthCommitResult.SaveFailed,"Unity relic save failure");
                Check(restarted.CommitRelic(equip,null,s=>{decode(encode(s)).Validate();return true;})==GrowthCommitResult.Committed,"Unity relic retry with same result");
                var armed=new PlayableBattle(50,new PlayableProgress(),8,combatDefinitions:combat,formalGrowth:save.growth,colossusDefinition:ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]),collectionGrowth:restarted.Snapshot.collection);
                var naked=new PlayableBattle(50,new PlayableProgress(),8,combatDefinitions:combat,formalGrowth:save.growth,colossusDefinition:ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]));
                Check(armed.State.Heroes[0].Attack>naked.State.Heroes[0].Attack && armed.ChainRate(0)==naked.ChainRate(0),"Unity equip changes stats without chain bonus");
            }
        }
        foreach(int level in new[]{44,45,49,50}){
            var definition=JsonUtility.FromJson<ColossusCombatDef>(JsonUtility.ToJson(ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0])));definition.Validate();
            var battle=new PlayableBattle(level,new PlayableProgress(),4,combatDefinitions:combat,colossusDefinition:definition);
            battle.State.AdvanceBossGauge(3);Check(battle.NextAttackIsMajor && battle.State.UltimateUnlocked==(level>=45),"Unity major ultimate level boundary");
            Check(battle.State.BreakPart("crystal-horn-crown",int.MaxValue) && !battle.NextAttackIsMajor,"Unity destruction cancels imminent major");
        }
        Check(WorldCatalog.ColossusIds.Skip(1).All(id=>!ColossusCombatCatalog.CanSummon(id)),"Unity all unmade enemies cannot summon");
    }
}
