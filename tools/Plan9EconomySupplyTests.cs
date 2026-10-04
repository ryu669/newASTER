using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan9EconomySupplyTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat,string storyJson)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(storyJson,options);var catalog=ProductionStoryCatalog.Collection(combat,story);var home=ProductionStoryCatalog.Home(combat,story);
        var initial=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion),collection=new FormalCollectionLedger{contentVersion=catalog.contentVersion}};
        var journal=new FormalCampaignJournal(initial,encode,decode);
        foreach(string hero in combat.FormationIds){var growth=new FormalProgression(journal.Snapshot.growth,combat.FormationIds);check(growth.Commit(new GrowthRequest("supply.train."+hero,hero,growth.Snapshot.revision,GrowthOperation.Level,10),s=>journal.CommitGrowth(s,p=>true))==GrowthCommitResult.Committed,"Introductory nectar supports actual initial-party training");}
        foreach(var entry in WorldCatalog.Colossi){
            var saved=journal.Snapshot;var world=new CampaignState(WorldCatalog.ColossusIds,saved.world);check(world.ColossusUnlocks.IsUnlocked(entry.Id),"Every next enemy unlocks through prior earned clears");
            var def=ProductionEconomyCatalog.Enemy(entry.Id);int seed=3;var battle=new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:combat,formalGrowth:saved.growth,colossusDefinition:def);
            var session=new BattleCollectionSession(catalog,"supply."+entry.Id,entry.Id,1,saved.revision,combat.FormationIds,seed,def.contentVersion);var poems=catalog.owners.Single(o=>o.id==entry.Id).poemIds;var rng=new Random(seed^0x534F4E47);
            battle.CompletedEnemyAction=()=>session.RecordCompletedSinging(poems[rng.Next(poems.Length)]);
            int commands=0;while(!battle.Ended && commands++<4000){if(!battle.Act(battle.AvailableHero,0,battle.State.Parts.FirstOrDefault(p=>!p.IsBroken)?.Id??"body"))battle.Pass();battle.DrainPresentationEvents();}
            check(battle.State.IsVictory && commands<4000,"Trained introductory party actually clears each unlocked production enemy");
            var receipt=session.Finish(BattleEndReason.Victory,saved.world.poemIds,saved.world.unlockedStoryIds);var request=new FormalBattleEndRequest(receipt,saved.revision);
            Func<CampaignSaveV2,CampaignSaveV2> grant=w=>{var campaign=new CampaignState(WorldCatalog.ColossusIds,w);campaign.ClaimColossusVictory(entry.Id,entry.EnvironmentTags,new VictoryReward(request.Id,1,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return campaign.CreateSave();};
            check(journal.CommitBattleEnd(request,catalog,grant,p=>true,home)==GrowthCommitResult.Committed,"Earned victory supplies own materials and growth without diagnostic grants");
            check(catalog.owners.Single(o=>o.id==entry.Id).materialIds.All(id=>journal.Snapshot.collection.materials.Single(m=>m.id==id).amount==11),"Initial clear supplies eleven of each own material");
        }
        check(journal.Snapshot.world.firstClearIds.Length==15 && journal.Snapshot.growth.nectar==3090 && journal.Snapshot.growth.awakeningCrystals==35 && journal.Snapshot.growth.stones==480,"Complete introductory campaign reconciles every wallet and first clear");
        foreach(var tree in home.weaponNodes.Where(n=>!n.initial))check(tree.costs.All(c=>catalog.resources.Any(r=>r.id==c.resourceId && r.kind=="material")),"All branch costs have a reachable noncircular enemy source");
        int capCost=Enumerable.Range(1,119).Sum(level=>FormalRelicRules.UpgradeCost(new CollectionRelic{level=level},RelicOperation.LevelUp,catalog.contentVersion));
        check(capCost==7140 && FormalRelicRules.UpgradeCost(new CollectionRelic(),RelicOperation.AttackUp,catalog.contentVersion)==30,"Production relic progression requires 7140 own materials and thirty per direct step");
        check(FormalRelicRules.UpgradeCost(new CollectionRelic(),RelicOperation.HpUp,CollectionCatalog.FixtureVersion)==1000,"Historical trial upgrade costs remain compatible");
        var costFixture=decode(encode(journal.Snapshot));var relicDef=catalog.relics[0];
        costFixture.collection.relics=new[]{new CollectionRelic{id=relicDef.id,contentVersion=catalog.contentVersion,attackRoll=80,hpRoll=800}};
        foreach(var material in costFixture.collection.materials)material.amount=10000;
        var costs=new FormalCampaignJournal(costFixture,encode,decode);
        foreach(var operation in new[]{RelicOperation.LevelUp,RelicOperation.AttackUp,RelicOperation.HpUp}){
            var before=costs.Snapshot;var relic=before.collection.relics[0];int expected=FormalRelicRules.UpgradeCost(relic,operation,catalog.contentVersion);
            var request=new FormalRelicRequest("supply.relic."+operation,relic.id,null,before.revision,operation,catalog.contentVersion);
            check(costs.CommitRelic(request,catalog,s=>false)==GrowthCommitResult.SaveFailed && encode(costs.Snapshot)==encode(before),"Production relic cost fixture stays unchanged on failed write");
            check(costs.CommitRelic(request,catalog,s=>true)==GrowthCommitResult.Committed && relicDef.materialIds.All(id=>costs.Snapshot.collection.materials.Single(m=>m.id==id).amount==before.collection.materials.Single(m=>m.id==id).amount-expected),"Production relic retry consumes the displayed versioned cost exactly once");
        }
        check(costs.Snapshot.collection.relics[0].level==2 && costs.Snapshot.collection.relics[0].attackRoll==81 && costs.Snapshot.collection.relics[0].hpRoll==810,"All production relic operations apply their distinct effect after persistence");
        var old=decode(encode(journal.Snapshot));old.home.contentVersion=HomeExperienceCatalog.CandidateVersion;old.collection.contentVersion=CollectionCatalog.CandidateVersion;foreach(var relic in old.collection.relics)relic.contentVersion=CollectionCatalog.CandidateVersion;
        string oldText=encode(old);var migrated=ProductionStoryMigration.Prepare(old,combat,story,s=>decode(encode(s)));
        check(encode(old)==oldText && migrated.home.contentVersion==home.contentVersion && migrated.collection.relics.All(r=>r.contentVersion==catalog.contentVersion),"Candidate migration preserves the original and updates every owned relic version");
        check(migrated.world.poemIds.SequenceEqual(old.world.poemIds) && migrated.growth.stones==old.growth.stones && migrated.home.readEventIds.SequenceEqual(old.home.readEventIds) && migrated.collection.receipts.Length==15,"Candidate migration retains earned story wallets and historical battle receipts");
        var report=new{schemaVersion=1,definition=ProductionEconomyCatalog.Version,scope="earned fifteen initial clears plus deterministic supply/cost reachability; not performance",initialNectar=2940,partyLevel=10,initialTrainingCost=900,clears=15,nectar=journal.Snapshot.growth.nectar,crystals=journal.Snapshot.growth.awakeningCrystals,stones=journal.Snapshot.growth.stones,materialPerLevelOneWin=11,materialPerLevelFiftyWin=60,relicCapCost=capCost,relicCapLevelFiftyWins=119,directAttack80To100Cost=600,directHp800To1000Cost=600,treeFullCost=52,treeLevelOneWins=5,heroineOneTo120Cost=FormalProgression.LevelCost(1,120),fiveHeroineAwakeningCrystals=400,exchangeDraws=100,exchangeStoneCost=30000,loginOnlyDaysAfterIntro=90,adoption="relic costs reduced from trial 71400/20000 to 7140/600; initial enemy progression requires no random heroine acquisition"};
        File.WriteAllText("tmp/plan9-economy-supply.json",JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
    }
}
