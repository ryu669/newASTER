using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool mediaWarmCapture,mediaWarmComplete;
        private void PrepareRuntimeImprovementsCapture(string[] args)
        {
            if(!args.Contains("-validateRuntimeImprovements"))return;
            AcceptanceCheck(formalDiagnostic && capturePath!=null,"Performance cache test requires isolated capture");
            if(args.Contains("-validateSaveHistory"))MeasureSaveHistory();
            string id=book.SubjectId;var before=GrowthSnapshot();var preview=HeroineDetailPreview(id,before);
            AcceptanceCheck(ReferenceEquals(before,GrowthSnapshot()) && ReferenceEquals(preview,HeroineDetailPreview(id,before)),"Unchanged growth and preview are reused");
            var growth=before.heroines.Single(h=>h.heroineId==id);
            var request=new GrowthRequest(Guid.NewGuid().ToString("N"),id,before.revision,GrowthOperation.Level,growth.level+1);
            AcceptanceCheck(formalProgression.Commit(request,s=>false)==GrowthCommitResult.SaveFailed,"Cache fixture save failure");
            AcceptanceCheck(ReferenceEquals(before,GrowthSnapshot()) && ReferenceEquals(preview,HeroineDetailPreview(id,before)),"Failed save retains current preview and growth");
            AcceptanceCheck(formalProgression.Commit(request,SaveFormalGrowth)==GrowthCommitResult.Committed,"Cache fixture retries same real save");
            var after=GrowthSnapshot();var updated=HeroineDetailPreview(id,after);
            AcceptanceCheck(!ReferenceEquals(before,after) && !ReferenceEquals(preview,updated) && after.heroines.Single(h=>h.heroineId==id).level==growth.level+1 && updated.MaxHitPoints>preview.MaxHitPoints,"Committed growth invalidates caches and changes actual stats");
            AcceptanceCheck(ReferenceEquals(after,GrowthSnapshot()) && ReferenceEquals(updated,HeroineDetailPreview(id,after)),"Updated caches are stable");
            Debug.Log("RUNTIME_IMPROVEMENTS_CACHE_PASS reuse=true failedSave=unchanged retry=durable stats=updated");
            if(args.Contains("-validateMediaWarmup")){mediaWarmCapture=true;StartCoroutine(ValidateMediaWarmup());}
        }
        private System.Collections.IEnumerator ValidateMediaWarmup()
        {
            string source="heroine.april.event.0";
            ReadProductionDiagnosticScene(source,false);BeginAdv(source,true);CloseAdv();BeginAdv(source,true);
            string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);int steps=0;double deadline=Time.realtimeSinceStartupAsDouble+30;
            while(adv.CgId==null && !adv.EndReached){
                AcceptanceCheck(Time.realtimeSinceStartupAsDouble<deadline && steps<100,"Async ADV reaches CG without stale request deadlock");
                if(AdvVisualReady()){adv.Resume();adv.Tick(2);AdvanceAdv();steps++;}
                yield return null;
            }
            while(!AdvVisualReady()){AcceptanceCheck(Time.realtimeSinceStartupAsDouble<deadline,"ADV image load completes");yield return null;}
            AcceptanceCheck(adv.CgId!=null && AdvTexture(adv.CgId)!=null && advImageFailures.Count==0,"Authored CG is loaded, not a placeholder");
            AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"Async replay remains read-only");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(capturePath),"adv-async-cg.png"));
            yield return null;CloseAdv();yield return illustrationView.WarmHeroes(CurrentFormation());
            AcceptanceCheck(illustrationView.CachedLayerCount<=36,"Battle prefetch respects existing cache capacity");
            Debug.Log("MEDIA_WARM_PLAYER_PASS async-adv / cancel-reopen / authored-cg / replay-readonly / battle-prefetch-cache");mediaWarmComplete=true;
        }
        private void MeasureSaveHistory()
        {
            string root=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(capturePath),"save-history-"+Guid.NewGuid().ToString("N"));
            int decodes=0;
            var validation=new SaveValidationService(1,Application.version,UnityFormalCampaignJson.Encode,text=>{decodes++;return UnityFormalCampaignJson.Decode(text);},s=>JsonUtility.ToJson(s,true),s=>JsonUtility.FromJson<GameSave>(s),ValidateSlotContent);
            var slots=new SaveSlotManager(new SaveTransactionService(root,validation));
            var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=GrowthSnapshot()};
            // Own the fixture: never add synthetic receipts to the live journal or normal save directory.
            save=validation.Copy(save);
            save.growth.receipts=Enumerable.Range(0,10000).Select(i=>new GrowthReceipt{transactionId="measurement."+i,signature="measurement|"+i}).ToArray();
            for(int i=0;i<3;i++)slots.Save(1,save,DateTime.UtcNow);
            for(int i=0;i<3;i++){
                decodes=0;var clock=System.Diagnostics.Stopwatch.StartNew();var loaded=slots.Load(1);clock.Stop();
                AcceptanceCheck(loaded.growth.receipts.Length==10000 && decodes==1,"Large history reload validates once");
                Debug.Log("SAVE_HISTORY_PLAYER operation=load sample="+i+" ms="+clock.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" decodes="+decodes);
                decodes=0;clock.Restart();slots.Save(1,save,DateTime.UtcNow);clock.Stop();
                AcceptanceCheck(decodes==7,"Large history backups retain validation without duplicate decoding");
                Debug.Log("SAVE_HISTORY_PLAYER operation=save sample="+i+" ms="+clock.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" decodes="+decodes);
            }
            AcceptanceCheck(slots.Inspect(1,1).Status==SaveSlotStatus.Ready && slots.Inspect(1,2).Status==SaveSlotStatus.Ready,"Large history backups readable");
            Debug.Log("SAVE_HISTORY_PLAYER_PASS receipts=10000 backups=2 isolated="+root);
        }
    }
}
