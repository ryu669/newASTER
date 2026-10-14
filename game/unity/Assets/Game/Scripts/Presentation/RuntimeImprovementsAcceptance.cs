using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
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
