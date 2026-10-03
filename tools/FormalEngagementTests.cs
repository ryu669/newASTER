using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class FormalEngagementTests
{
    public static void Run(Action<bool,string> check)
    {
        var options=new JsonSerializerOptions {IncludeFields=true};Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=t=>JsonSerializer.Deserialize<FormalCampaignSave>(t,options);
        var seed=new FormalCampaignSave {world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave {saveId="newaster.formal-growth",heroines=new[]{new FormalHeroineGrowth {heroineId="heroine.slayer"}}}};
        var journal=new FormalCampaignJournal(seed,encode,decode);var rules=new FormalEngagementRules();var utc=new DateTime(2026,10,2,15,0,0,DateTimeKind.Utc);
        Action<Action,string> reject=(action,name)=>{bool threw=false;try{action();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is OverflowException){threw=true;}check(threw,name);};
        check(FormalEngagementRules.Day(utc)==20261003 && FormalEngagementRules.Day(utc.AddSeconds(-1))==20261002,"Japanese midnight boundary exact");
        var login=new FormalEngagementRequest(true,20261003,0,0);var before=encode(journal.Snapshot);
        check(journal.PreviewEngagement(login,rules,utc)==300 && encode(journal.Snapshot)==before,"Login preview read-only");
        check(journal.CommitEngagement(login,rules,utc,s=>false)==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==before,"Failed daily claim publishes neither stone nor day");
        reject(()=>journal.CommitActiveSeconds(60,s=>true),"Pending reward blocks time checkpoint");reject(()=>journal.CommitWorld(seed.world,s=>true),"Pending reward blocks world save");
        reject(()=>journal.CommitVictory(new FormalVictoryRequest("other",WorldCatalog.ColossusIds[0],1,0),w=>w,s=>true),"Pending reward cannot be replaced by victory");
        FormalCampaignSave disk=null;Func<FormalCampaignSave,bool> writer=s=>{disk=decode(encode(s));return true;};
        check(journal.CommitEngagement(login,rules,utc.AddDays(1),writer)==GrowthCommitResult.Committed,"Daily save retry crossing midnight uses exact prior candidate");
        check(disk.growth.stones==300 && disk.engagement.lastLoginDay==20261003 && disk.growth.receipts.Length==1,"Daily wallet and receipt durable together");
        journal=new FormalCampaignJournal(disk,encode,decode);check(journal.CommitEngagement(login,rules,utc,s=>throw new Exception("duplicate"))==GrowthCommitResult.AlreadyCommitted,"Restart cannot grant daily twice");
        reject(()=>journal.PreviewEngagement(new FormalEngagementRequest(true,20261002,0,journal.Snapshot.revision),rules,utc.AddDays(-1)),"Clock rollback cannot reclaim an earlier day");
        reject(()=>journal.PreviewEngagement(new FormalEngagementRequest(true,20261005,0,journal.Snapshot.revision),rules,utc),"Arbitrary future day not accepted");
        var nextLogin=new FormalEngagementRequest(true,20261004,0,journal.Snapshot.revision);journal.CommitEngagement(nextLogin,rules,utc.AddDays(1),writer);check(journal.Snapshot.growth.stones==600,"Next actual day claim allowed once");
        before=encode(journal.Snapshot);check(!journal.CommitActiveSeconds(1799,s=>false) && encode(journal.Snapshot)==before,"Failed active checkpoint does not create time");
        journal.CommitActiveSeconds(1799,writer);reject(()=>journal.PreviewEngagement(new FormalEngagementRequest(false,0,1,journal.Snapshot.revision),rules,utc),"One second short rejects time reward");
        journal.CommitActiveSeconds(1,writer);var time=new FormalEngagementRequest(false,0,1,journal.Snapshot.revision);
        check(journal.PreviewEngagement(time,rules,utc)==100,"Thirty saved active minutes earns one period");
        before=encode(journal.Snapshot);journal.CommitEngagement(time,rules,utc,s=>{s.growth.stones=0;s.engagement.claimedPeriods=99;return false;});check(encode(journal.Snapshot)==before,"Writer mutation cannot corrupt pending time reward");
        journal.CommitEngagement(time,rules,utc,writer);check(disk.growth.stones==700 && disk.engagement.claimedPeriods==1 && disk.engagement.activeSeconds==1800,"Time receipt stone and counter atomic");
        check(journal.CommitEngagement(time,rules,utc,s=>false)==GrowthCommitResult.AlreadyCommitted,"Repeated time reward no duplicate stone");
        journal.CommitActiveSeconds(3600,writer);var multiple=new FormalEngagementRequest(false,0,3,journal.Snapshot.revision);check(journal.PreviewEngagement(multiple,rules,utc)==200,"Unclaimed periods bundle exact amount");journal.CommitEngagement(multiple,rules,utc,writer);
        check(journal.Snapshot.growth.stones==900 && journal.Snapshot.engagement.claimedPeriods==3,"Bundled time periods each paid once");
        var progression=new FormalProgression(journal.Snapshot.growth,new[]{"heroine.slayer"});var next=progression.Snapshot;next.nectar=20000;
        // Existing progression writers must retain engagement through the same envelope.
        progression=new FormalProgression(next,new[]{"heroine.slayer"});progression.Commit(new GrowthRequest("level.after-reward","heroine.slayer",next.revision,GrowthOperation.Level,2),s=>journal.CommitGrowth(s,writer));
        check(journal.Snapshot.engagement.activeSeconds==5400 && journal.Snapshot.engagement.lastLoginDay==20261004,"Growth commit retains engagement history");
        var invalid=decode(encode(disk));invalid.engagement.claimedPeriods=4;reject(invalid.Validate,"Impossible paid period count rejected");invalid=decode(encode(disk));invalid.engagement.lastLoginDay=20260230;reject(invalid.Validate,"Invalid calendar date rejected");
        string dir=Path.Combine(Path.GetTempPath(),"newaster-engagement-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"formal.json");
        try {
            var store=new FormalCampaignStore(path,encode,decode,t=>JsonSerializer.Deserialize<FormalCampaignHeader>(t,options));store.Save(seed);
            var real=new FormalCampaignJournal(seed,encode,decode);real.CommitActiveSeconds(1800,store.Save);var r=new FormalEngagementRequest(false,0,1,real.Snapshot.revision);real.CommitEngagement(r,rules,utc,store.Save);
            check(store.Load(out var loaded)==FormalLoadResult.Loaded && loaded.growth.stones==100 && loaded.engagement.claimedPeriods==1,"Real file stores reward and active clock together");
            var restart=new FormalCampaignJournal(loaded,encode,decode);check(restart.CommitEngagement(r,rules,utc,s=>false)==GrowthCommitResult.AlreadyCommitted,"Real save reload blocks double time claim");
        }finally{foreach(var file in Directory.GetFiles(dir))File.Delete(file);Directory.Delete(dir);}
        RunEconomyLoop(check,options);
    }
    private static void RunEconomyLoop(Action<bool,string> check,JsonSerializerOptions options)
    {
        var ids=new[]{"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna","heroine.excalipan"};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=t=>JsonSerializer.Deserialize<FormalCampaignSave>(t,options);
        var seed=new FormalCampaignSave {world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave {saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=ids.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()}};
        var banner=new FormalKinderBanner {id="kinder.initial-five",contentVersion="kinder-trial-2026-10-02",status="trial",heroineIds=ids,materials=new[]{new KinderMaterialEntry {kind="nectar",weight=9000,amount=200},new KinderMaterialEntry {kind="crystal",weight=700,amount=2}}};
        string dir=Path.Combine(Path.GetTempPath(),"newaster-plan4-loop-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"formal.json");
        try {
            var store=new FormalCampaignStore(path,encode,decode,t=>JsonSerializer.Deserialize<FormalCampaignHeader>(t,options));store.Save(seed);var journal=new FormalCampaignJournal(seed,encode,decode);
            var progression=new FormalProgression(journal.Snapshot.growth,ids);Func<FormalGrowthSave,bool> growthWrite=s=>journal.CommitGrowth(s,store.Save);
            progression.CommitKinder(new KinderRequest("grant.plan4-kinder-introduction",0,KinderOperation.IntroGrant),banner,null,growthWrite);
            for(int i=0;i<233;i++){
                var c=WorldCatalog.Colossi[0];var request=new FormalVictoryRequest("loop.victory."+i,c.Id,50,journal.Snapshot.revision);
                journal.CommitVictory(request,w=>{var state=new CampaignState(WorldCatalog.ColossusIds,w);state.ClaimColossusVictory(c.Id,c.EnvironmentTags,new VictoryReward(request.BattleId,50,10,4,Array.Empty<string>()),GreenReturnDragonVerticalSlice.StoryChapters,Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return state.CreateSave();},store.Save);
            }
            check(journal.Snapshot.world.claimedBattleIds.Length==233 && journal.Snapshot.growth.stones==33290,"Loop repeat victories and intro stone are durable once each");
            progression=new FormalProgression(journal.Snapshot.growth,ids);int sequence=0;
            Action<string,GrowthOperation,int> grow=(id,op,to)=>progression.Commit(new GrowthRequest("loop.growth."+sequence++,id,progression.Snapshot.revision,op,to),growthWrite);
            foreach(string id in ids){grow(id,GrowthOperation.Level,50);grow(id,GrowthOperation.Awaken,0);grow(id,GrowthOperation.Level,80);grow(id,GrowthOperation.Awaken,0);grow(id,GrowthOperation.Level,120);}
            check(progression.Snapshot.heroines.All(h=>h.level==120 && h.awakeningStage==2) && progression.Snapshot.nectar==56070 && progression.Snapshot.awakeningCrystals==785,"Five complete level and awakening costs verified through real saves");
            for(int i=0;i<10;i++)progression.CommitKinder(new KinderRequest("loop.draw."+i,progression.Snapshot.revision,KinderOperation.StoneDraw,10),banner,max=>0,growthWrite);
            check(progression.Snapshot.totalKinderDraws==100 && progression.Snapshot.kinderPoints==100 && progression.Snapshot.stones==3290,"One hundred actual draw operations charge thirty thousand stone and earn hundred points");
            progression.CommitKinder(new KinderRequest("loop.exchange",progression.Snapshot.revision,KinderOperation.Exchange,heroineId:ids[1]),banner,null,growthWrite);
            check(progression.Snapshot.heroines[1].fragments==0 && progression.Snapshot.tickets[0].count==1 && progression.Snapshot.kinderPoints==0,"Exchange creates ticket not premature heroine grant");
            progression.CommitKinder(new KinderRequest("loop.ticket",progression.Snapshot.revision,KinderOperation.TicketDraw,heroineId:ids[1]),banner,null,growthWrite);
            for(int i=0;i<5;i++)grow(ids[0],GrowthOperation.Strengthen,0);
            foreach(string id in ids.Skip(1))for(int i=0;i<5;i++)grow(id,GrowthOperation.Strengthen,0);
            grow(ids[0],GrowthOperation.ReceiveHeroine,0);
            check(progression.Snapshot.heroines.All(h=>h.duplicateRank==5 && h.fragments==0) && progression.Snapshot.overflow==7700,"Dedicated fragments then generic overflow support five capped heroes and later duplicates");
            check(store.Load(out var reloaded)==FormalLoadResult.Loaded && encode(reloaded)==encode(journal.Snapshot),"Complete economy loop reload equals all durable state");
            check(reloaded.growth.totalKinderDraws==100 && reloaded.growth.kinderPoints==0 && reloaded.growth.tickets[0].count==0,"Reload retains draw history and consumed exchange ticket");
            check(FormalProgression.LevelCost(1,120)*5==77350 && FormalProgression.LevelCost(1,50)*5==14700,"Balance budget total costs independently calculated");
        }finally{foreach(var file in Directory.GetFiles(dir))File.Delete(file);Directory.Delete(dir);}
    }
}
