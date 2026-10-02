using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class FormalCampaignTests
{
    public static void Run(Action<bool,string> check)
    {
        var options=new JsonSerializerOptions {IncludeFields=true};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=t=>{try{return JsonSerializer.Deserialize<FormalCampaignSave>(t,options);}catch(JsonException e){throw new ArgumentException("JSON",e);}};
        Func<FormalCampaignSave> fresh=()=>new FormalCampaignSave {world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave {saveId="newaster.formal-growth",nectar=100,stones=300}};
        Action<Action,string> reject=(action,name)=>{bool threw=false;try{action();}catch(Exception e)when(e is ArgumentException||e is InvalidOperationException||e is OverflowException){threw=true;}check(threw,name);};
        Func<CampaignSaveV2,string,int,CampaignSaveV2> worldReward=(world,battle,level)=>{
            var campaign=new CampaignState(WorldCatalog.ColossusIds,world);var c=WorldCatalog.Colossi[0];
            campaign.ClaimColossusVictory(c.Id,c.EnvironmentTags,new VictoryReward(battle,level,10,4,GreenReturnDragonVerticalSlice.PoemIds.Take(4)),GreenReturnDragonVerticalSlice.StoryChapters,Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
            campaign.Playable.RecordVictory(level);return campaign.CreateSave();
        };
        var state=new FormalCampaignJournal(fresh(),encode,decode);var original=encode(state.Snapshot);
        var request=new FormalVictoryRequest("battle.atomic",WorldCatalog.ColossusIds[0],10,0);int builds=0,writes=0;FormalCampaignSave disk=null;
        Func<CampaignSaveV2,CampaignSaveV2> build=w=>{builds++;return worldReward(w,request.BattleId,request.Level);};
        Func<FormalCampaignSave,bool> write=s=>{writes++;disk=decode(encode(s));return true;};
        check(state.CommitVictory(request,build,s=>false)==GrowthCommitResult.SaveFailed,"Victory save failure reported");
        check(state.HasPending && encode(state.Snapshot)==original,"Failed victory leaves page garden materials and wallets unchanged");
        reject(()=>state.CommitWorld(state.Snapshot.world,write),"World writes blocked during pending victory");
        reject(()=>state.CommitGrowth(state.Snapshot.growth,write),"Growth writes blocked during pending victory");
        reject(()=>state.CommitVictory(new FormalVictoryRequest("battle.other",request.ColossusId,10,0),build,write),"Different victory cannot replace pending");
        check(state.CommitVictory(request,w=>throw new Exception("Must not rebuild"),s=>{s.world.materials=0;s.growth.stones=0;return false;})==GrowthCommitResult.SaveFailed,"Writer mutations cannot change pending result");
        check(state.CommitVictory(request,null,write)==GrowthCommitResult.Committed && builds==1 && writes==1,"Retry saves same world candidate without rebuild");
        check(disk.world.materials>0 && disk.world.firstClearIds.Contains(request.ColossusId) && disk.world.unlockedGardenIds.Length>0 && disk.world.poemIds.Length==4,"World reward and page garden poems durable");
        check(disk.growth.nectar==260 && disk.growth.awakeningCrystals==2 && disk.growth.stones==350 && disk.revision==1 && disk.growth.revision==1,"Formal resources durable in same envelope");
        var detached=state.Snapshot;detached.world.firstClearIds[0]="mutated";detached.growth.nectar=0;
        check(state.Snapshot.world.firstClearIds[0]==request.ColossusId && state.Snapshot.growth.nectar==260,"Unified snapshot deeply detached");
        var restarted=new FormalCampaignJournal(disk,encode,decode);
        check(restarted.CommitVictory(request,null,s=>throw new Exception("No duplicate write"))==GrowthCommitResult.AlreadyCommitted,"Restart replay no duplicate grant");
        reject(()=>restarted.CommitVictory(new FormalVictoryRequest(request.BattleId,request.ColossusId,11,0),build,write),"Same battle changed level rejected");
        reject(()=>restarted.CommitVictory(new FormalVictoryRequest("battle.stale",request.ColossusId,10,0),build,write),"Stale envelope request rejected");
        var newRequest=new FormalVictoryRequest("battle.repeat",request.ColossusId,10,1);
        restarted.CommitVictory(newRequest,w=>worldReward(w,newRequest.BattleId,10),write);
        check(disk.world.claimedBattleIds.Length==2 && disk.world.firstClearIds.Length==1 && disk.growth.stones==400,"Replay of same boss with different battle grants once without repeating page");
        var bad=fresh();bad.growth.nectar=int.MaxValue;var overflow=new FormalCampaignJournal(bad,encode,decode);
        reject(()=>overflow.CommitVictory(request,build,write),"Reward wallet overflow rejected");
        check(!overflow.HasPending && overflow.Snapshot.world.claimedBattleIds.Length==0 && overflow.Snapshot.growth.nectar==int.MaxValue,"Overflow does not partially unlock world");
        var incomplete=new FormalCampaignJournal(fresh(),encode,decode);
        reject(()=>incomplete.CommitVictory(request,w=>w,write),"Victory callback without battle claim rejected");
        check(!incomplete.HasPending,"Invalid callback leaves no pending victory");
        reject(()=>incomplete.CommitVictory(request,w=>{incomplete.CommitWorld(w,write);return worldReward(w,request.BattleId,10);},write),"Reentrant world change during reward build rejected");
        check(encode(incomplete.Snapshot)==original && !incomplete.HasPending,"Reentrant rejection leaves full state unchanged");
        bool exception=false;try{incomplete.CommitVictory(request,build,s=>throw new IOException("simulated"));}catch(IOException){exception=true;}
        check(exception && incomplete.HasPending && encode(incomplete.Snapshot)==original,"IO exception preserves whole pending reward");
        incomplete.CommitVictory(request,null,write);check(!incomplete.HasPending,"IO failure retry completes");
        for(int level=1;level<=50;level++){
            var r=new FormalVictoryRequest("level."+level,request.ColossusId,level,0);
            check(r.Nectar==60+10*level,"Trial nectar scales with summon level");check(r.Crystals==Math.Min(5,1+level/10),"Crystal bands match specification");check(r.Stones==30+2*level,"Trial stone scales with summon level");
        }
        var growthState=new FormalProgression(fresh().growth,new[]{"heroine.slayer"});
        var whole=new FormalCampaignJournal(fresh(),encode,decode);
        // Normal growth path: acquire a known hero on the same durable envelope.
        var growthRequest=new GrowthRequest("growth.unified","heroine.slayer",0,GrowthOperation.ReceiveHeroine);
        check(growthState.Commit(growthRequest,s=>whole.CommitGrowth(s,p=>false))==GrowthCommitResult.SaveFailed,"Growth failure leaves envelope and wallet unchanged");
        check(whole.Snapshot.revision==0 && whole.Snapshot.growth.heroines.Length==0,"No split growth publication");
        check(growthState.Commit(growthRequest,s=>whole.CommitGrowth(s,write))==GrowthCommitResult.Committed && disk.revision==1 && disk.growth.heroines.Length==1,"Growth retry writes unified payload");
        var worldChange=whole.Snapshot.world;worldChange.materials+=5;
        check(!whole.CommitWorld(worldChange,s=>false) && whole.Snapshot.world.materials==fresh().world.materials,"Failed world-only change rolls back");
        check(whole.CommitWorld(worldChange,write) && disk.growth.heroines.Length==1,"World save preserves formal growth");
        var changedReceipt=whole.Snapshot.growth;changedReceipt.revision++;changedReceipt.receipts[0].kinderOutcomes=new[]{new KinderOutcome {kind="nectar",amount=1}};
        changedReceipt.receipts=changedReceipt.receipts.Concat(new[]{new GrowthReceipt {transactionId="new",signature="new"}}).ToArray();
        reject(()=>whole.CommitGrowth(changedReceipt,write),"Existing reward receipt results cannot be changed by a later growth save");
        var roster=new[]{"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna","heroine.excalipan"};
        var kinder=new FormalProgression(whole.Snapshot.growth,roster);
        var banner=new FormalKinderBanner {id="kinder.initial-five",contentVersion="kinder-trial-2026-10-02",status="trial",heroineIds=roster,materials=new[]{new KinderMaterialEntry {kind="nectar",amount=200,weight=1}}};
        check(kinder.CommitKinder(new KinderRequest("grant.plan4-kinder-introduction",1,KinderOperation.IntroGrant),banner,null,s=>whole.CommitGrowth(s,write))==GrowthCommitResult.Committed,"Intro stones use unified writer");
        var draw=new KinderRequest("kinder.unified",2,KinderOperation.StoneDraw,10);
        check(kinder.CommitKinder(draw,banner,max=>max==10000?300:0,s=>whole.CommitGrowth(s,write))==GrowthCommitResult.Committed,"Ten draw rewards use unified writer");
        check(disk.world.materials==worldChange.materials && disk.growth.stones==300 && disk.growth.nectar==2100 && disk.growth.kinderPoints==10 && disk.growth.receipts.Last().kinderOutcomes.Length==10,"Gacha preserves world and records all rewards in same file");
        var forged=whole.Snapshot.world;forged.claimedBattleIds=new[]{"forged"};reject(()=>whole.CommitWorld(forged,write),"World-only save cannot add battle reward");
        var invalid=fresh();invalid.world=null;reject(()=>invalid.Validate(),"Missing world rejected");
        invalid=fresh();invalid.world.materials=-1;reject(()=>invalid.Validate(),"Negative world wallet rejected");
        invalid=fresh();invalid.growth.receipts=new[]{new GrowthReceipt {transactionId="orphan",signature="victory|fake"}};reject(()=>invalid.Validate(),"Growth battle receipt needs world claim");
        invalid=fresh();invalid.world.heroineLevels=new[]{1};reject(()=>invalid.Validate(),"Truncated world array rejected");
        invalid=fresh();invalid.world.poemIds=new[]{"p","p"};reject(()=>invalid.Validate(),"Duplicate world IDs rejected");
        string dir=Path.Combine(Path.GetTempPath(),"newaster-campaign-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"formal.json");
        try{
            var store=new FormalCampaignStore(path,encode,decode);check(store.Load(out _)==FormalLoadResult.Missing,"Empty unified slot missing");
            store.Save(fresh());var durable=new FormalCampaignJournal(fresh(),encode,decode);durable.CommitVictory(request,build,store.Save);
            check(store.Load(out var loaded)==FormalLoadResult.Loaded && loaded.growth.stones==350 && loaded.world.claimedBattleIds.Contains(request.BattleId),"Real file contains both sides of victory");
            check(store.Save(loaded),"Exact durable candidate retry acknowledged");
            check(decode(File.ReadAllText(path+".bak")).revision==0 && !File.Exists(path+".tmp"),"Atomic replace keeps previous snapshot backup");
            string normal=File.ReadAllText(path);loaded.version=2;File.WriteAllText(path,encode(loaded));
            check(store.Load(out _)==FormalLoadResult.Blocked,"Future envelope not hidden by backup");reject(()=>store.Save(fresh()),"Future file protected against write");
            File.WriteAllText(path,normal);loaded=decode(normal);loaded.growth.version=3;File.WriteAllText(path,encode(loaded));check(store.Load(out _)==FormalLoadResult.Blocked,"Future embedded growth protected");
            File.WriteAllText(path,"corrupt");check(store.Load(out var backup)==FormalLoadResult.RecoveredBackup && backup.revision==0,"Whole backup recoverable read-only");reject(()=>store.Save(fresh()),"Corrupt primary cannot be implicitly overwritten");
            check(File.ReadAllText(path)=="corrupt","Corrupt primary retained");
            File.WriteAllText(path,normal);File.WriteAllText(path+".tmp","uncommitted");check(store.Load(out loaded)==FormalLoadResult.Loaded && loaded.revision==1,"Uncommitted tmp ignored");
            File.Delete(path);check(store.Load(out _)==FormalLoadResult.Blocked,"Orphan backup not treated as new game");
        }finally{foreach(var file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);Directory.Delete(dir);}
    }
}
