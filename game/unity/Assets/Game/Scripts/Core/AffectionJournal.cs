using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed class AffectionRequest
    {
        public string Id{get;} public string Kind{get;} private readonly string[] forms; public string[] Forms=>(string[])forms.Clone(); public string Content{get;} public long Revision{get;}
        public string Signature=>"affection.v1|"+Kind+"|"+string.Join(",",Forms)+"|"+Content;
        public AffectionRequest(string id,long revision,string kind,string[] forms,string content)
        {if(!HomeExperienceCatalog.Id(id) || revision<0 || !new[]{"interaction","activity","ring-purchase","ring-use","max-display"}.Contains(kind) || forms==null || forms.Any(f=>!HomeExperienceCatalog.Id(f)) || string.IsNullOrWhiteSpace(content))throw new ArgumentException("Invalid affection request.");Id=id;Revision=revision;Kind=kind;this.forms=(string[])forms.Clone();Content=content;}
        public void Apply(FormalCampaignSave s,HomeExperienceCatalog c)
        {
            AffectionSaveAdapter.Migrate(s,c);
            if(Kind=="ring-purchase"){if(Forms.Length!=0)throw new ArgumentException();AffectionRingService.Purchase(s);}
            else if(Kind=="ring-use"){if(Forms.Length!=1)throw new ArgumentException();AffectionRingService.Use(s,c,Forms[0]);}
            else if(Kind=="max-display"){
                if(Forms.Length!=1)throw new ArgumentException();var a=AffectionService.State(s,c,Forms[0]);if(a.level!=99 || !s.affection.pendingMaxLevelPersonIds.Contains(a.personId))throw new ArgumentException("Milestone already shown or unavailable.");s.affection.pendingMaxLevelPersonIds=s.affection.pendingMaxLevelPersonIds.Where(id=>id!=a.personId).ToArray();s.affection.shownMaxLevelPersonIds=s.affection.shownMaxLevelPersonIds.Union(new[]{a.personId}).ToArray();
            }else foreach(string form in Forms.GroupBy(c.PersonId).Select(g=>g.First()))AffectionRewardService.Interaction(s,c,form,Content);
            AffectionEventResolver.Refresh(s,c);
        }
    }
    public sealed partial class FormalCampaignJournal
    {
        private AffectionRequest pendingAffection;
        public GrowthCommitResult CommitAffection(AffectionRequest request,HomeExperienceCatalog c,Func<FormalCampaignSave,bool> save)
        {
            if(request==null || c==null || save==null)throw new ArgumentNullException();if(writing)throw new InvalidOperationException("Reentrant affection save.");
            var receipt=current.affection?.receipts.SingleOrDefault(r=>r.id==request.Id);if(receipt!=null){if(receipt.signature!=request.Signature)throw new ArgumentException("Affection ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){if(pendingAffection==null || pendingAffection.Id!=request.Id || pendingAffection.Signature!=request.Signature || pendingAffection.Revision!=request.Revision)throw new InvalidOperationException("Retry the same affection operation.");}
            else{
                if(request.Revision!=current.revision || request.Forms.Any(id=>!current.growth.heroines.Any(h=>h.heroineId==id)))throw new ArgumentException("Stale operation or unowned person.");
                if(current.growth.receipts.Any(r=>r.transactionId==request.Id) || (current.home?.receipts.Any(r=>r.transactionId==request.Id)??false) || (current.gardenLife?.receipts.Any(r=>r.transactionId==request.Id)??false))throw new ArgumentException("Transaction ID already used.");
                var next=Snapshot;request.Apply(next,c);next.affection.receipts=next.affection.receipts.Concat(new[]{new AffectionReceipt{id=request.Id,signature=request.Signature}}).ToArray();
                if(request.Kind=="ring-purchase" || request.Kind=="ring-use")next.growth.revision=checked(next.growth.revision+1);
                next.revision=checked(next.revision+1);AffectionSaveAdapter.ValidateContent(next,c);next.Validate();pending=Copy(next);pendingAffection=request;
            }
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;pending=null;pendingAffection=null;return GrowthCommitResult.Committed;
        }
    }
}
