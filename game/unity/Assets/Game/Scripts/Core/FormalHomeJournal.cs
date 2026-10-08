using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NewAster.Core
{
    public sealed class FormalHomeRequest
    {
        public string Id {get;} public string Kind {get;} public long Revision {get;} public string ContentVersion {get;} public string OperationKey {get;}
        public string Signature=>"home|"+Kind+"|"+ContentVersion+"|"+OperationKey;
        public FormalHomeRequest(string id,string kind,long revision,string contentVersion,string operationKey)
        {
            if(!HomeExperienceCatalog.Id(id) || !new[]{"garden","event","advRead","homeInit","weapon","weapon-level","panzer-loadout","commander","sniper-support","formation","craft","place","occupant","talk","sceneEnd"}.Contains(kind) || revision<0 || !HomeExperienceCatalog.Id(operationKey) || !HomeExperienceCatalog.SupportedVersion(contentVersion))throw new ArgumentException("Invalid home request.");
            Id=id;Kind=kind;Revision=revision;ContentVersion=contentVersion;OperationKey=operationKey;
        }
    }
    public sealed partial class FormalCampaignJournal
    {
        private string pendingHomeId,pendingHomeKind,pendingHomeVersion,pendingHomeSignature;
        private long pendingHomeRevision;
        // Build on a detached home snapshot. Future costed operations add wallet changes
        // through dedicated typed requests; this boundary cannot mutate world or growth.
        public GrowthCommitResult CommitHome(FormalHomeRequest request,HomeExperienceCatalog catalog,
            Func<FormalHomeProgress,FormalHomeProgress> build,Func<FormalCampaignSave,bool> save)
        {return CommitHomeCandidate(request,catalog,build==null?null:(Func<FormalCampaignSave,FormalCampaignSave>)(s=>{s.home=build(s.home);return s;}),save);}
        private GrowthCommitResult CommitHomeCandidate(FormalHomeRequest request,HomeExperienceCatalog catalog,
            Func<FormalCampaignSave,FormalCampaignSave> build,Func<FormalCampaignSave,bool> save)
        {
            if(request==null || save==null)throw new ArgumentException("Invalid home transaction.");
            string transactionId=request.Id,kind=request.Kind;long revision=request.Revision;
            if(writing)throw new InvalidOperationException("Reentrant campaign write.");
            var receipt=current.home?.receipts.SingleOrDefault(r=>r.transactionId==transactionId);
            if(receipt!=null){if(receipt.signature!=request.Signature || catalog!=null && catalog.contentVersion!=request.ContentVersion)throw new ArgumentException("Home transaction ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){
                if(pendingHomeId!=transactionId || pendingHomeSignature!=request.Signature || pendingHomeKind!=kind || pendingHomeRevision!=revision || catalog!=null && pendingHomeVersion!=catalog.contentVersion)throw new InvalidOperationException("Retry the same home transaction first.");
            }else{
                if(revision!=current.revision || catalog==null || build==null)throw new ArgumentException("Stale or missing home definition.");
                catalog.Validate();if(catalog.contentVersion!=request.ContentVersion)throw new ArgumentException("Unsupported home content.");
                if(current.growth.receipts.Any(r=>r.transactionId==transactionId) || current.world.claimedBattleIds.Contains(transactionId))throw new ArgumentException("Transaction identity already used.");
                var next=Snapshot;next.home=next.home??FormalHomeProgress.Empty(catalog.contentVersion);
                var before=next.home;next=Copy(next);writing=true;try{next=build(next);}finally{writing=false;}
                if(next.home==null)throw new ArgumentException("Missing home result.");
                next=Copy(next);next.home.ValidateContent(catalog,next);
                if((before.weaponLevels??Array.Empty<HomeWeaponLevel>()).Any(w=>!catalog.weaponNodes.Any(n=>n.initial && n.id==w.nodeId) && next.home.WeaponLevel(w.nodeId)<w.level))throw new ArgumentException("神器Lvは下げられません。");
                if(!before.receipts.Select(r=>r.transactionId+"|"+r.kind+"|"+r.signature+"|"+r.resultHash).SequenceEqual(next.home.receipts.Select(r=>r.transactionId+"|"+r.kind+"|"+r.signature+"|"+r.resultHash)) ||
                    before.weaponNodeIds.Any(id=>!next.home.weaponNodeIds.Contains(id)) || before.unlockedEventIds.Any(id=>!next.home.unlockedEventIds.Contains(id)) || before.readEventIds.Any(id=>!next.home.readEventIds.Contains(id)) || before.claimedRewardIds.Any(id=>!next.home.claimedRewardIds.Contains(id)) || before.loverHeroineIds.Any(id=>!next.home.loverHeroineIds.Contains(id)) ||
                    before.readLineKeys.Any(l=>!next.home.readLineKeys.Any(n=>n.sceneId==l.sceneId && n.scriptVersion==l.scriptVersion && n.lineId==l.lineId)) || before.furnitureInstances.Any(i=>!next.home.furnitureInstances.Any(n=>n.instanceId==i.instanceId && n.defId==i.defId)) || before.affections.Any(a=>!next.home.affections.Any(n=>n.heroineId==a.heroineId && n.value>=a.value)))throw new ArgumentException("Home transaction cannot remove durable progress or replace receipts.");
                string resultHash;using(var hash=SHA256.Create())resultHash=Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(encode(next))));
                next.home.receipts=next.home.receipts.Concat(new[]{new HomeReceipt{transactionId=transactionId,kind=kind,signature=request.Signature,resultHash=resultHash}}).ToArray();
                next.revision=checked(next.revision+1);next.Validate();pending=next;pendingHomeId=transactionId;pendingHomeKind=kind;pendingHomeRevision=revision;pendingHomeVersion=catalog.contentVersion;pendingHomeSignature=request.Signature;
            }
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;
            pending=null;pendingHomeId=null;pendingHomeKind=null;pendingHomeVersion=null;pendingHomeSignature=null;return GrowthCommitResult.Committed;
        }
    }
}
