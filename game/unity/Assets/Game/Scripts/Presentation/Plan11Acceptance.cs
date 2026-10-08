using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void PreparePlan11Capture(string[] args)
        {
            if(!args.Contains("-capturePlan11") || capturePath==null)return;
            int at=Array.IndexOf(args,"-terraformView");string view=at>=0 && at+1<args.Length?args[at+1]:"domains";
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"terraform-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var snapshot=formalCampaign.Snapshot;var s=TerraformRules.Migrate(snapshot.world);s.totalTp=180000;
            for(int i=0;i<14;i++)TerraformRules.Victory(s,WorldCatalog.ColossusIds[i],50);
            snapshot.world.firstClearIds=WorldCatalog.ColossusIds.Take(14).ToArray();
            snapshot.world.readStoryIds=CollectionData().owners.Where(o=>o.kind=="colossus").SelectMany(o=>o.chapterIds).ToArray();
            snapshot.world.unlockedStoryIds=snapshot.world.readStoryIds.ToArray();
            TerraformRules.RefreshDeep(s,snapshot.world.readStoryIds,CollectionData());
            foreach(var d in s.domains)for(int l=1;l<=5;l++)TerraformRules.SetLevel(s,d.domainId,l);
            TerraformRules.Victory(s,WorldCatalog.ColossusIds[14],50);snapshot.world.firstClearIds=WorldCatalog.ColossusIds.ToArray();
            foreach(var d in s.domains){string deep=TerraformRules.DeepFor(s,d.domainId)[0];TerraformRules.SetLevel(s,d.domainId,6,deep,null,true);TerraformRules.SetLevel(s,d.domainId,7,deep,TerraformRules.Fusions[Array.IndexOf(TerraformRules.DomainIds,d.domainId)][0]);}
            TerraformRules.Synchronize(snapshot,CollectionData());snapshot.Validate();AcceptanceCheck(acceptanceStore.Save(snapshot),"Plan11 isolated initial save");BindFormalCampaign(snapshot);
            title=false;encounter=null;result=null;collectionOpen=false;book.ChangeBookmark(BookBookmark.NewWorld);book.CompleteTransition();
            if(view=="journey")ValidatePlan11PlayerJourney();
            else if(view=="warning"){
                var saved=formalCampaign.Snapshot;TerraformRules.SetLevel(saved.world.terraform,"life",5);saved.world.terraform.overgrowthWarningAccepted=false;BindFormalCampaign(saved);
                book.ChangeBookmark(BookBookmark.NewWorld);
                SelectTerraformDomain(saved.world.terraform,0);terraformTargetLevel=6;ProposeTerraform(TerraformOperation.SetLevel,"life",6,TerraformRules.DeepFor(saved.world.terraform,"life")[0]);
            }else if(view=="blocked"){
                var saved=formalCampaign.Snapshot;foreach(var d in saved.world.terraform.domains){d.currentLevel=0;d.maxReachedLevel=0;d.activeDeepRecordId=d.activeExtremeId=null;}
                saved.world.terraform.worldIntegrated=false;saved.world.terraform.sevenExtremeGenesis=false;saved.world.firstClearIds=WorldCatalog.ColossusIds.Take(14).ToArray();BindFormalCampaign(saved);book.RequestSubject(BookBookmark.Colossi,WorldCatalog.ColossusIds[14]);book.CompleteTransition();
                AcceptanceCheck(TerraformRules.MissingIntegration(campaign.Terraform).Length==7,"Asteria page remains visible while all seven domains block deployment");
            }else if(view=="extremes" || view=="deep" || view=="phenomena" || view=="name"){
                book.ChangeBookmark(BookBookmark.PossibleWorlds);possibleWorldTab=view=="deep"?1:view=="phenomena"?2:view=="name"?3:0;
                if(view=="name"){var saved=formalCampaign.Snapshot;TerraformRules.RenameWorld(saved.world.terraform,"星幽のアステル");BindFormalCampaign(saved);book.ChangeBookmark(BookBookmark.PossibleWorlds);}
            }else if(view.StartsWith("weather-",StringComparison.Ordinal)){
                string id=view.Substring(8);var saved=formalCampaign.Snapshot;
                TerraformRules.UnlockPhenomenon(saved.world.terraform,id);TerraformRules.SelectPhenomenon(saved.world.terraform,id);TerraformRules.RefreshUnlocks(saved.world,saved.home);BindFormalCampaign(saved);
                book.RequestSubject(BookBookmark.Gardens,"garden.integrated-world");book.CompleteTransition();
            }else if(view!="domains")throw new ArgumentException("Unknown Plan11 capture view.");
            Debug.Log("PLAN11_PLAYER_PASS view="+view+" / "+acceptanceChecks+" checks");
        }
        private void ValidatePlan11PlayerJourney()
        {
            var catalog=CollectionData();var snapshot=formalCampaign.Snapshot;
            var request=new FormalTerraformRequest("plan11.player.rebuild",snapshot.revision,TerraformOperation.SetLevel,"life",7,TerraformRules.DeepFor(snapshot.world.terraform,"life")[0],"light");
            string before=UnityFormalCampaignJson.Encode(snapshot);int tp=snapshot.world.terraform.totalTp;
            AcceptanceCheck(formalCampaign.CommitTerraform(request,catalog,saved=>false)==GrowthCommitResult.SaveFailed && before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"Player failed save retains prior landscape and TP");
            AcceptanceCheck(formalCampaign.CommitTerraform(request,catalog,SaveDiagnosticCampaign)==GrowthCommitResult.Committed,"Player retries frozen terraform candidate");
            AcceptanceCheck(formalCampaign.Snapshot.world.terraform.totalTp==tp-1000,"Player retry spends reconstruction TP once");
            AcceptanceCheck(acceptanceStore.Load(out var loaded)==FormalLoadResult.Loaded,"Player physical unified save loads");BindFormalCampaign(loaded);
            AcceptanceCheck(formalCampaign.CommitTerraform(request,catalog,saved=>throw new Exception("No duplicate write"))==GrowthCommitResult.AlreadyCommitted,"Player restart suppresses duplicate operation");
            foreach(var extreme in TerraformCatalog.Extremes){
                snapshot=formalCampaign.Snapshot;formalCampaign.CommitTerraform(new FormalTerraformRequest("player.discover."+extreme.id,snapshot.revision,TerraformOperation.SetLevel,extreme.mainDomainId,7,TerraformRules.DeepFor(snapshot.world.terraform,extreme.mainDomainId)[0],extreme.fusionDomainId),catalog,SaveDiagnosticCampaign);
            }
            AcceptanceCheck(formalCampaign.Snapshot.world.terraform.discoveredExtremes.Length==28,"Player collects all 28 extreme states");
            snapshot=formalCampaign.Snapshot;formalCampaign.CommitTerraform(new FormalTerraformRequest("player.name",snapshot.revision,TerraformOperation.RenameWorld,value:"七極のアステル"),catalog,SaveDiagnosticCampaign);
            AcceptanceCheck(acceptanceStore.Load(out loaded)==FormalLoadResult.Loaded && loaded.world.terraform.customWorldName=="七極のアステル" && loaded.world.terraform.discoveredExtremes.Length==28,"Player JsonUtility roundtrip retains name and encyclopedia");
            BindFormalCampaign(loaded);book.ChangeBookmark(BookBookmark.PossibleWorlds);possibleWorldTab=0;
        }
    }
}
