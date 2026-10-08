using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan11TerraformTests
{
    public static void Run(Action<bool,string> check)
    {
        var old=new CampaignSaveV2 {terraformingExperience=1234,unlockedGardenIds=new[]{"unknown.garden"}};
        var s=TerraformRules.Migrate(old);TerraformRules.Migrate(old);
        check(s.totalTp==1234 && old.migration.plan11TerraformCompleted && old.unlockedGardenIds.Contains("unknown.garden"),"Plan11 migration once and preserve unlocks");
        var options=new JsonSerializerOptions {IncludeFields=true};
        var round=JsonSerializer.Deserialize<CampaignSaveV2>(JsonSerializer.Serialize(old,options),options);
        check(TerraformRules.Migrate(round).totalTp==1234,"Plan11 reload no double conversion");
        var reward=new TerraformSave();
        check(TerraformRules.Victory(reward,TerraformRules.ColossusIds[0],1)==150,"Plan11 first level band reward");
        check(TerraformRules.Victory(reward,TerraformRules.ColossusIds[0],1)==50,"Plan11 repeated band base reward");
        check(TerraformRules.Victory(reward,TerraformRules.ColossusIds[0],50)==3300,"Plan11 skipped bands awarded once");
        check(TerraformRules.Victory(reward,TerraformRules.ColossusIds[0],50)==600,"Plan11 repeat level50 reward");
        reward.totalTp=200000;
        for(int l=1;l<=3;l++)TerraformRules.SetLevel(reward,"life",l);
        check(TerraformRules.BlockReason(reward,"life",4)!=null,"Plan11 one colossus cannot reach level4");
        for(int i=1;i<14;i++)TerraformRules.Victory(reward,TerraformRules.ColossusIds[i],50);
        var catalog=CollectionContractFixture.Create(new[]{"heroine.placeholder-01","heroine.placeholder-02","heroine.placeholder-03","heroine.placeholder-04","heroine.placeholder-05"});
        TerraformRules.RefreshDeep(reward,catalog.owners.Where(o=>o.kind=="colossus").SelectMany(o=>o.chapterIds).ToArray(),catalog);
        check(reward.acquiredDeepRecords.Length==7,"Plan11 all world deep records including W07");
        check(TerraformRules.MissingIntegration(reward).Length==6,"Plan11 integration domain gate");
        foreach(var d in reward.domains)for(int l=d.maxReachedLevel+1;l<=5;l++)TerraformRules.SetLevel(reward,d.domainId,l);
        check(TerraformRules.BlockReason(reward,"life",6,TerraformRules.DeepIds[0],null,true)!=null,"Plan11 level6 integration gate");
        TerraformRules.Victory(reward,TerraformRules.ColossusIds[14],50);
        check(TerraformRules.BlockReason(reward,"life",6,TerraformRules.DeepIds[0])!=null,"Plan11 overgrowth confirmation required");
        int before=reward.totalTp;
        foreach(var d in reward.domains){string deep=TerraformRules.DeepFor(reward,d.domainId)[0];TerraformRules.SetLevel(reward,d.domainId,6,deep,null,true);TerraformRules.SetLevel(reward,d.domainId,7,deep,TerraformRules.Fusions[Array.IndexOf(TerraformRules.DomainIds,d.domainId)][0]);}
        check(reward.sevenExtremeGenesis && before-reward.totalTp==84000,"Plan11 seven extreme genesis and costs");
        before=reward.totalTp;TerraformRules.SetLevel(reward,"life",5);TerraformRules.SetLevel(reward,"life",7,TerraformRules.DeepIds[0],"night");
        check(reward.totalTp==before && reward.domains[0].maxReachedLevel==7,"Plan11 stabilization and discovered extreme free");
        TerraformRules.SetLevel(reward,"life",7,TerraformRules.DeepIds[0],"light");
        check(before-reward.totalTp==1000 && reward.discoveredExtremes.Last().sequence==8,"Plan11 rebuild cost and ordered discovery");
        TerraformRules.UnlockPhenomenon(reward,"rain");check(reward.unlockedWorldPhenomena.Contains("rain"),"Plan11 phenomenon unlock");
        var c=new CampaignState(WorldCatalog.ColossusIds);var first=WorldCatalog.Colossi[0];var r=new VictoryReward("plan11.once",1,0,0,Array.Empty<string>());
        c.ClaimColossusVictory(first.Id,first.EnvironmentTags,r,null,null,Array.Empty<GardenRequirement>());c.ClaimColossusVictory(first.Id,first.EnvironmentTags,r,null,null,Array.Empty<GardenRequirement>());
        check(c.Terraform.totalTp==150,"Plan11 battle claim remains idempotent");
        check(TerraformRules.Costs.Take(6).Sum()==4200 && TerraformRules.Costs.Sum()==16200,"Plan11 fixed initial costs");
        Transactions(check,reward,catalog,options);
    }
    private static void Transactions(Action<bool,string> check,TerraformSave state,CollectionCatalog catalog,JsonSerializerOptions options)
    {
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        Action<Action,string> reject=(action,message)=>{bool rejected=false;try{action();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException){rejected=true;}check(rejected,message);};
        var world=new CampaignState(WorldCatalog.ColossusIds).CreateSave();world.terraform=TerraformRules.Copy(state);
        var journal=new FormalCampaignJournal(new FormalCampaignSave {world=world,growth=new FormalGrowthSave {saveId="newaster.formal-growth"}},encode,decode);
        var request=new FormalTerraformRequest("terraform.atomic",journal.Snapshot.revision,TerraformOperation.SetLevel,"earth",7,TerraformRules.DeepFor(state,"earth")[0],"light");
        string original=encode(journal.Snapshot);int balance=journal.Snapshot.world.terraform.totalTp;
        check(journal.CommitTerraform(request,catalog,s=>false)==GrowthCommitResult.SaveFailed && journal.HasPending && encode(journal.Snapshot)==original,"Plan11 failed operation keeps wallet scenery discoveries unchanged");
        reject(()=>journal.CommitTerraform(new FormalTerraformRequest("other",request.Revision,TerraformOperation.SetLevel,"life",5),catalog,s=>true),"Plan11 pending candidate cannot be replaced");
        check(journal.CommitTerraform(request,catalog,s=>{s.world.terraform.totalTp=0;return false;})==GrowthCommitResult.SaveFailed,"Plan11 writer mutation cannot alter retry candidate");
        string durable=null;check(journal.CommitTerraform(request,catalog,s=>{durable=encode(s);return true;})==GrowthCommitResult.Committed && !journal.HasPending,"Plan11 exact candidate retry commits");
        check(journal.Snapshot.world.terraform.totalTp==balance-1000,"Plan11 retry consumes reconstruction TP once");
        journal=new FormalCampaignJournal(decode(durable),encode,decode);
        check(journal.CommitTerraform(request,catalog,s=>throw new Exception("duplicate write"))==GrowthCommitResult.AlreadyCommitted,"Plan11 replay after restart never spends TP twice");
        reject(()=>journal.CommitTerraform(new FormalTerraformRequest(request.Id,request.Revision,TerraformOperation.SetLevel,"earth",5),catalog,s=>true),"Plan11 changed request cannot reuse receipt ID");
        foreach(var extreme in TerraformCatalog.Extremes){
            var snapshot=journal.Snapshot;var s=snapshot.world.terraform;string deep=TerraformRules.DeepFor(s,extreme.mainDomainId)[0];
            var discover=new FormalTerraformRequest("discover."+extreme.id,snapshot.revision,TerraformOperation.SetLevel,extreme.mainDomainId,7,deep,extreme.fusionDomainId);
            journal.CommitTerraform(discover,catalog,saved=>true);
        }
        var all=journal.Snapshot.world.terraform;
        check(all.discoveredExtremes.Length==28 && all.discoveredExtremes.Select(e=>e.sequence).OrderBy(x=>x).SequenceEqual(Enumerable.Range(1,28)),"Plan11 all 28 extreme definitions have stable discovery order");
        int tp=all.totalTp;
        foreach(var extreme in TerraformCatalog.Extremes){var snap=journal.Snapshot;journal.CommitTerraform(new FormalTerraformRequest("switch."+extreme.id,snap.revision,TerraformOperation.SetLevel,extreme.mainDomainId,7,TerraformRules.DeepFor(snap.world.terraform,extreme.mainDomainId)[0],extreme.fusionDomainId),catalog,saved=>true);}
        check(journal.Snapshot.world.terraform.totalTp==tp,"Plan11 all discovered extreme switches are free");
        var frozen=journal.Snapshot;var detached=new CampaignState(WorldCatalog.ColossusIds,frozen.world);detached.Terraform.totalTp=0;
        check(frozen.world.terraform.totalTp==tp,"Plan11 campaign construction detaches terraform wallet");
        var exported=detached.CreateSave();exported.terraform.domains[0].currentLevel=0;
        check(detached.Terraform.domains[0].currentLevel==7,"Plan11 exported save detaches domain state");
        var lower=TerraformRules.Copy(all);TerraformRules.SetLevel(lower,"life",0);TerraformRules.SetLevel(lower,"life",4);
        check(lower.domains[0].currentLevel==4 && lower.domains[0].maxReachedLevel==7 && lower.totalTp==tp,"Plan11 all reached levels may be restored without first TP");
        var incomplete=new CampaignState(WorldCatalog.ColossusIds).CreateSave();incomplete.firstClearIds=TerraformRules.ColossusIds.Take(14).ToArray();
        reject(()=>TerraformRules.RequireIntegration(incomplete,TerraformRules.ColossusIds[14]),"Plan11 authoritative Asteria gate rejects unformed domains");
        foreach(var d in incomplete.terraform.domains){d.currentLevel=3;d.maxReachedLevel=3;}TerraformRules.RequireIntegration(incomplete,TerraformRules.ColossusIds[14]);
        check(true,"Plan11 authoritative Asteria gate accepts seven settled domains");
        var future=TerraformRules.Copy(all);future.acquiredDeepRecords=future.acquiredDeepRecords.Concat(new[]{"future.deep"}).ToArray();future.domains=future.domains.Concat(new[]{new TerraformDomainState {domainId="future.domain"}}).ToArray();future.unlockedFurnitureIds=new[]{"future.furniture"};
        var futureWorld=new CampaignSaveV2 {terraform=future,migration=new TerraformMigration {plan11TerraformCompleted=true}};
        var serialized=JsonSerializer.Deserialize<CampaignSaveV2>(JsonSerializer.Serialize(futureWorld,options),options);TerraformRules.Migrate(serialized);
        check(serialized.terraform.domains.Any(d=>d.domainId=="future.domain") && serialized.terraform.acquiredDeepRecords.Contains("future.deep") && serialized.terraform.unlockedFurnitureIds.Contains("future.furniture"),"Plan11 unknown future IDs survive load and migration");
        check(TerraformCatalog.Domains.Length==7 && TerraformCatalog.Extremes.Length==28 && TerraformCatalog.WorldRecords.Length==75 && TerraformCatalog.DeepRecords.All(d=>d.modifierTags.Length>0),"Plan11 typed definitions and deep modifier tags complete");
        var unlocked=new CampaignState(WorldCatalog.ColossusIds).CreateSave();unlocked.terraform.domains[0].currentLevel=2;unlocked.terraform.domains[0].maxReachedLevel=2;TerraformRules.RefreshUnlocks(unlocked);TerraformRules.SetLevel(unlocked.terraform,"life",0);TerraformRules.RefreshUnlocks(unlocked);
        check(unlocked.unlockedGardenIds.Contains("garden.grassland-forest") && TerraformRules.FurnitureUnlocked(unlocked,"furniture.production.planter"),"Plan11 garden and furniture unlocks remain after stabilization");
        reject(()=>TerraformRules.SelectPhenomenon(new TerraformSave(),"snow"),"Plan11 unowned phenomenon cannot be selected");
        reject(()=>TerraformRules.RenameWorld(new TerraformSave(),"星"),"Plan11 world name requires genesis");
        reject(()=>TerraformRules.RenameWorld(all,new string('x',41)),"Plan11 world name length enforced in core");
        var deepWorld=new CampaignState(WorldCatalog.ColossusIds).CreateSave();string phoenix=WorldCatalog.ColossusIds[13];
        deepWorld.firstClearIds=WorldCatalog.ColossusIds.Take(13).ToArray();deepWorld.readStoryIds=catalog.owners.Single(o=>o.id==phoenix).chapterIds;deepWorld.unlockedStoryIds=deepWorld.readStoryIds.ToArray();
        var deepJournal=new FormalCampaignJournal(new FormalCampaignSave {world=deepWorld,growth=new FormalGrowthSave {saveId="newaster.formal-growth"}},encode,decode);
        var session=new BattleCollectionSession(catalog,"terraform.deep-on-victory",phoenix,50,0,catalog.owners.Where(o=>o.kind=="heroine").Select(o=>o.id).ToArray(),7);
        var battleEnd=new FormalBattleEndRequest(session.Finish(BattleEndReason.Victory,deepWorld.poemIds,deepWorld.unlockedStoryIds),0);
        deepJournal.CommitBattleEnd(battleEnd,catalog,w=>{
            var campaign=new CampaignState(WorldCatalog.ColossusIds,w);campaign.ClaimColossusVictory(phoenix,WorldCatalog.Colossi[13].EnvironmentTags,new VictoryReward(battleEnd.Id,50,0,0,Array.Empty<string>()),null,null,Array.Empty<GardenRequirement>());return campaign.CreateSave();
        },saved=>true);
        check(deepJournal.Snapshot.world.terraform.acquiredDeepRecords.Contains("frozen_cycle"),"Plan11 final Lv50 victory permanently acquires deep record before opening terraforming UI");
        check(deepJournal.Snapshot.collection.receipts.Single().terraformingTp==3400,"Plan11 receipt preserves exact TP including first reached thresholds");
        var partial=new TerraformSave();TerraformRules.Victory(partial,phoenix,50);string[] chapters=catalog.owners.Single(o=>o.id==phoenix).chapterIds;
        TerraformRules.RefreshDeep(partial,chapters.Take(2).ToArray(),catalog);check(partial.acquiredDeepRecords.Length==0,"Plan11 two read chapters do not grant W07 deep record");
        TerraformRules.RefreshDeep(partial,chapters,catalog);TerraformRules.RefreshDeep(partial,Array.Empty<string>(),catalog);
        check(partial.acquiredDeepRecords.SequenceEqual(new[]{"frozen_cycle"}),"Plan11 complete W07 deep record stays permanently acquired");
        var previousPlan11=new CampaignSaveV2 {terraform=new TerraformSave(),migration=new TerraformMigration {plan11TerraformCompleted=true}};
        TerraformRules.Migrate(previousPlan11);check(previousPlan11.terraform.unlockedFurnitureIds.Length==7,"Plan11 previous version's available recipes stay unlocked");
        var fresh=new CampaignState(WorldCatalog.ColossusIds).CreateSave();check(!TerraformRules.FurnitureUnlocked(fresh,"furniture.production.planter") && fresh.migration.plan11ContentUnlocksCompleted,"Plan11 fresh campaign requires domain development for new recipes");
    }
}
