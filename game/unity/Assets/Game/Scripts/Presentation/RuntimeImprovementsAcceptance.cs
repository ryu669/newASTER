using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void PrepareRuntimeImprovementsCapture(string[] args)
        {
            if(!args.Contains("-validateRuntimeImprovements"))return;
            AcceptanceCheck(formalDiagnostic && capturePath!=null,"Performance cache test requires isolated capture");
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
    }
}
