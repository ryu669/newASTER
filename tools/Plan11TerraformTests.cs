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
    }
}
