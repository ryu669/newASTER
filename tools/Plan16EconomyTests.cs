using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan16EconomyTests
{
    public static void Run(Action<bool,string> check)
    {
        var r=new PlayRewardState();var boundary=new DateTime(2026,10,7,20,0,0,DateTimeKind.Utc);
        check(DailyRewardService.Day(boundary.AddSeconds(-1))=="2026-10-07" && DailyRewardService.Day(boundary)=="2026-10-08","EC-01 Japan 05:00 boundary");
        check(DailyRewardService.Apply(r,boundary)==300 && DailyRewardService.Apply(r,boundary.AddHours(2))==0 && DailyRewardService.Apply(r,boundary.AddDays(-1))==0,"EC-01 one daily grant; clock rollback never regrants");
        check(PlayTimeRewardService.Apply(r,1799.75)==0 && PlayTimeRewardService.Apply(r,.25)==100,"EC-02 exact thirty-minute threshold");
        check(PlayTimeRewardService.Apply(r,3600)==200 && r.totalPlaySeconds==5400 && r.rewardRemainderSeconds==0,"EC-02 unlimited periods");
        var options=new JsonSerializerOptions{IncludeFields=true};PlayTimeRewardService.Apply(r,37.25);r=JsonSerializer.Deserialize<PlayRewardState>(JsonSerializer.Serialize(r,options),options);check(r.rewardRemainderSeconds==37.25,"EC-06 fractional carry survives restart");
        var world=new CampaignState(WorldCatalog.ColossusIds).CreateSave();var save=new FormalCampaignSave{world=world,growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=10000}};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var journal=new FormalCampaignJournal(save,encode,decode);string before=encode(journal.Snapshot);
        check(!journal.CommitPlayRewards(1800,boundary,s=>false) && encode(journal.Snapshot)==before,"EC-07 save failure grants nothing");
        check(journal.CommitPlayRewards(1800,boundary,s=>true) && journal.Snapshot.growth.stones==400,"EC-07 retry grants daily and time exactly once");
        check(journal.CommitPlayRewards(0,boundary,s=>true) && journal.Snapshot.growth.stones==400,"EC-07 repeated daily check never duplicates");
        var low=new CollectionResourceDef{id="material.low",ownerId=WorldCatalog.ColossusIds[0],kind="material",rarity=1,minDropLevel=1};var mid=new CollectionResourceDef{id="material.mid",ownerId=low.ownerId,kind="material",rarity=2,minDropLevel=10};var high=new CollectionResourceDef{id="material.high",kind="material",rarity=3,minDropLevel=20};
        save=journal.Snapshot;save.collection=new FormalCollectionLedger{materials=new[]{new CollectionMaterial{id=low.id,sourceColossusId=low.ownerId,amount=1},new CollectionMaterial{id=mid.id,sourceColossusId=mid.ownerId,amount=1}}};
        check(MaterialExchangeService.UnitCost(low)==500 && MaterialExchangeService.UnitCost(mid)==2000 && MaterialExchangeService.UnitCost(high)==0,"EC-08 material band costs");
        var partOnly=new CollectionResourceDef{id="material.named-normal",kind="material",rarity=1,minDropLevel=1,partBreakOnly=true};check(MaterialExchangeService.UnitCost(partOnly)==0,"EC-08 explicit part-break-only materials are never exchanged");
        check(MaterialExchangeService.Maximum(save,low)==20 && MaterialExchangeService.Maximum(save,high)==0,"EC-08 MAX and prohibited high band");MaterialExchangeService.Apply(save,low,5);check(save.growth.nectar==7500 && save.collection.materials[0].amount==6,"EC-08 five-item exchange");save.collection.materials[0].amount=0;check(MaterialExchangeService.Unlocked(save,low.id),"EC-08 obtained exchange remains unlocked at zero balance");
        journal=new FormalCampaignJournal(save,encode,decode);var catalog=new CollectionCatalog{resources=new[]{low,mid,high}};before=encode(journal.Snapshot);check(!journal.CommitMaterialExchange(low.id,10,journal.Revision,catalog,s=>false) && encode(journal.Snapshot)==before,"EC-09 failed save preserves cost and material");
        long rev=journal.Revision;check(journal.CommitMaterialExchange(low.id,10,rev,catalog,s=>true),"EC-09 successful retry");bool stale=false;try{journal.CommitMaterialExchange(low.id,10,rev,catalog,s=>true);}catch(ArgumentException){stale=true;}check(stale && journal.Snapshot.growth.nectar==2500,"EC-07 exchange replay rejected");
        var legacy=new FormalCampaignSave{world=world,growth=new FormalGrowthSave{saveId="newaster.formal-growth",stones=500},engagement=new FormalEngagementState{activeSeconds=3700,claimedPeriods=1,lastLoginDay=20261008}};journal=new FormalCampaignJournal(legacy,encode,decode);check(journal.CommitPlayRewards(0,boundary,s=>true) && journal.Snapshot.growth.stones==600 && journal.Snapshot.playRewards.rewardRemainderSeconds==100 && journal.Snapshot.playRewards.totalPlaySeconds==3700,"EC-06/BK-15 legacy unclaimed time migrates without repeating claimed daily or time rewards");
        Console.WriteLine("PLAN16_ECONOMY_PASS daily / fractions / atomic rewards / exchange");
    }
}
