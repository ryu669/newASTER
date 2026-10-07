using System;
using System.Linq;

namespace NewAster.Core
{
    public enum TerraformOperation { SetLevel, UnlockPhenomenon, SelectPhenomenon, RenameWorld }
    [Serializable] public sealed class TerraformOperationReceipt { public string id, signature; }
    public sealed class FormalTerraformRequest
    {
        public string Id { get; }
        public long Revision { get; }
        public TerraformOperation Operation { get; }
        public string DomainId { get; }
        public int Level { get; }
        public string DeepRecordId { get; }
        public string FusionDomainId { get; }
        public string Value { get; }
        public bool Confirmed { get; }
        private static string Part(string value) => value==null?"-1:":value.Length+":"+value;
        public string Signature => "terraform.v1|"+Operation+"|"+Part(DomainId)+Level+"|"+Part(DeepRecordId)+Part(FusionDomainId)+Part(Value)+Confirmed;
        public FormalTerraformRequest(string id,long revision,TerraformOperation operation,string domainId=null,int level=0,string deepRecordId=null,string fusionDomainId=null,string value=null,bool confirmed=false)
        {
            if(string.IsNullOrWhiteSpace(id) || revision<0 || !Enum.IsDefined(typeof(TerraformOperation),operation))throw new ArgumentException("Invalid terraforming request.");
            Id=id;Revision=revision;Operation=operation;DomainId=domainId;Level=level;DeepRecordId=deepRecordId;FusionDomainId=fusionDomainId;Value=value;Confirmed=confirmed;
        }
    }
    public sealed partial class FormalCampaignJournal
    {
        private FormalTerraformRequest pendingTerraform;
        public GrowthCommitResult CommitTerraform(FormalTerraformRequest request,CollectionCatalog catalog,Func<FormalCampaignSave,bool> save)
        {
            if(request==null || catalog==null || save==null)throw new ArgumentNullException();
            if(writing)throw new InvalidOperationException("Concurrent campaign write.");
            var receipt=current.world.terraform.operationReceipts.SingleOrDefault(r=>r.id==request.Id);
            if(receipt!=null){if(receipt.signature!=request.Signature)throw new ArgumentException("Terraform transaction ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){
                if(pendingTerraform==null || pendingTerraform.Id!=request.Id || pendingTerraform.Signature!=request.Signature || pendingTerraform.Revision!=request.Revision)throw new InvalidOperationException("Retry the pending terraforming operation first.");
            }else{
                if(request.Revision!=current.revision)throw new ArgumentException("Stale terraforming request.");
                catalog.Validate();var next=Snapshot;TerraformRules.Synchronize(next,catalog);var s=next.world.terraform;
                switch(request.Operation){
                    case TerraformOperation.SetLevel:TerraformRules.SetLevel(s,request.DomainId,request.Level,request.DeepRecordId,request.FusionDomainId,request.Confirmed);break;
                    case TerraformOperation.UnlockPhenomenon:TerraformRules.UnlockPhenomenon(s,request.Value);break;
                    case TerraformOperation.SelectPhenomenon:TerraformRules.SelectPhenomenon(s,request.Value);break;
                    case TerraformOperation.RenameWorld:TerraformRules.RenameWorld(s,request.Value);break;
                }
                TerraformRules.RefreshUnlocks(next.world,next.home);
                s.operationReceipts=s.operationReceipts.Concat(new[]{new TerraformOperationReceipt {id=request.Id,signature=request.Signature}}).ToArray();
                next.revision=checked(next.revision+1);next.Validate();pending=Copy(next);pendingTerraform=request;
            }
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;
            pending=null;pendingTerraform=null;return GrowthCommitResult.Committed;
        }
    }
}
