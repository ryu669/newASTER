using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using NewAster.Presentation;
using System.IO;
using UnityEngine;
public static partial class PlayableBuild
{
    private static void ValidatePlan5()
    {
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-formal").text);
        var catalog=CollectionContractFixture.Create(combat);
        var restored=JsonUtility.FromJson<CollectionCatalog>(JsonUtility.ToJson(catalog));restored.Validate();
        Check(restored.poems.Length==450 && restored.chapters.Length==60 && restored.weaponNodes.Length==15,"Unity collection definition counts and nested weapon JSON");
        Func<FormalCampaignSave,string> encode=NewAster.Presentation.UnityFormalCampaignJson.Encode;
        Func<string,FormalCampaignSave> decode=UnityFormalCampaignJson.Decode;
        var preCollection=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",stones=321,heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()}};
        string oldJson="{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"revision\":0,\"world\":"+JsonUtility.ToJson(preCollection.world)+",\"growth\":"+JsonUtility.ToJson(preCollection.growth)+"}";
        Check(UnityFormalCampaignJson.DecodeHeader(oldJson).collection==null,"Unity omitted optional collection header remains absent");
        var oldSave=decode(oldJson);oldSave.Validate();Check(oldSave.collection==null && oldSave.engagement==null && oldSave.growth.stones==321,"Unity old envelope retains absent additions and balances");
        string explicitNull=oldJson.Substring(0,oldJson.Length-1)+",\"collection\":null,\"engagement\":null}";
        var nullSave=decode(explicitNull);nullSave.Validate();Check(nullSave.collection==null && nullSave.engagement==null && UnityFormalCampaignJson.DecodeHeader(explicitNull).collection==null,"Unity explicit null additions remain optional");
        string oldPath=Path.Combine(Application.temporaryCachePath,"plan5-old-"+Guid.NewGuid().ToString("N")+".json");
        try {
            File.WriteAllText(oldPath,oldJson);var store=new FormalCampaignStore(oldPath,encode,decode,UnityFormalCampaignJson.DecodeHeader);
            Check(store.Load(out var loaded)==FormalLoadResult.Loaded && loaded.growth.stones==321,"Unity actual old formal file loads without backup rollback");
            string future=oldJson.Substring(0,oldJson.Length-1)+",\"collection\":{\"version\":2,\"contentVersion\":\"future\",\"relics\":\"changed-shape\"}}";
            File.WriteAllText(oldPath+".bak",oldJson);File.WriteAllText(oldPath,future);
            Check(store.Load(out _) == FormalLoadResult.Blocked && store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Unity future collection shape blocks old backup");
        }finally{if(File.Exists(oldPath))File.Delete(oldPath);if(File.Exists(oldPath+".bak"))File.Delete(oldPath+".bak");}
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
        Check(ColossusCombatCatalog.CanSummon(WorldCatalog.ColossusIds[1]) && WorldCatalog.ColossusIds.Skip(2).All(id=>!ColossusCombatCatalog.CanSummon(id)),"Unity authored tyrant can summon and remaining unmade enemies cannot");
        foreach(int level in new[]{44,45,49,50}){
            var definition=JsonUtility.FromJson<ColossusCombatDef>(JsonUtility.ToJson(ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[1])));definition.Validate();
            Check(definition.actionCycle.Length==3 && definition.actionCycle[1].requiredPartId=="tyrant.claw" && definition.actionCycle[2].drainAmount==2,"Unity tyrant action cycle survives serialization");
            var battle=new PlayableBattle(level,new PlayableProgress(),4,combatDefinitions:combat,colossusDefinition:definition);
            battle.State.AdvanceBossGauge(4);Check(battle.NextAttackIsMajor && battle.NextEnemyAction==(level>=45?"極大技：渓谷断裂":"大技：鉱晶崩落"),"Unity tyrant own major boundary");
            Check(battle.State.BreakPart("tyrant.crown",int.MaxValue) && !battle.NextAttackIsMajor,"Unity tyrant crown interrupts major");
        }
    }
}
