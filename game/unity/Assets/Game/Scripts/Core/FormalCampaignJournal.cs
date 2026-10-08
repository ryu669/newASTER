using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed class FormalVictoryRequest
    {
        public string BattleId {get;}
        public string ColossusId {get;}
        public int Level {get;}
        public long Revision {get;}
        public int Nectar=>60+10*Level;
        public int Crystals=>Math.Min(5,1+Level/10);
        public int Stones=>30+2*Level;
        public string Signature=>"victory|rewards-trial-2026-10-02|"+ColossusId+"|"+Level;
        public FormalVictoryRequest(string battleId,string colossusId,int level,long revision)
        {
            if(string.IsNullOrWhiteSpace(battleId)||battleId.Contains("|")||string.IsNullOrWhiteSpace(colossusId)||colossusId.Contains("|")||level<1||level>50||revision<0)throw new ArgumentException("Invalid victory request.");
            BattleId=battleId;ColossusId=colossusId;Level=level;Revision=revision;
        }
    }
    /// <summary>Build rewards on detached world+growth. Publish only after a single durable write.</summary>
    public sealed partial class FormalCampaignJournal
    {
        private FormalCampaignSave current,pending;
        private FormalVictoryRequest pendingVictory;
        private readonly Func<FormalCampaignSave,string> encode;
        private readonly Func<string,FormalCampaignSave> decode;
        private bool writing;
        public bool HasPending=>pending!=null;
        public long Revision=>current.revision;
        public bool HasAffection=>current.affection!=null;
        public FormalCampaignSave Snapshot=>Copy(current);
        public FormalCampaignJournal(FormalCampaignSave save,Func<FormalCampaignSave,string> encode,Func<string,FormalCampaignSave> decode)
        {save.Validate();this.encode=encode;this.decode=decode;current=Copy(save);TerraformRules.Synchronize(current);}
        private FormalCampaignSave Copy(FormalCampaignSave s)=>decode(encode(s));
        private void Ready(){if(writing || HasPending)throw new InvalidOperationException("Finish pending campaign save first.");}
        public bool CommitGrowth(FormalGrowthSave growth,Func<FormalCampaignSave,bool> save,HomeExperienceCatalog home=null)
        {
            Ready();growth.Validate();var old=current.growth;
            if(growth.revision!=checked(old.revision+1)||growth.receipts.Length!=old.receipts.Length+1 || old.receipts.AnyMissingFrom(growth))throw new ArgumentException("Invalid growth transition.");
            var next=Snapshot;next.revision=checked(next.revision+1);next.growth=growth.Copy();if(home!=null)HomeConditions.Refresh(next,home);return Persist(next,save);
        }
        public bool CommitWorld(CampaignSaveV2 world,Func<FormalCampaignSave,bool> save)
        {
            Ready();FormalCampaignSave.ValidateWorld(world);
            if(!world.claimedBattleIds.OrderBy(x=>x).SequenceEqual(current.world.claimedBattleIds.OrderBy(x=>x)))throw new ArgumentException("Battle rewards require a victory transaction.");
            var next=Snapshot;next.world=world;next=Copy(next);next.revision=checked(next.revision+1);return Persist(next,save);
        }
        public GrowthCommitResult CommitVictory(FormalVictoryRequest request,Func<CampaignSaveV2,CampaignSaveV2> rewardWorld,Func<FormalCampaignSave,bool> save)
        {
            if(request==null||save==null)throw new ArgumentNullException();if(writing)throw new InvalidOperationException("Reentrant campaign write.");
            var receipt=current.growth.receipts.SingleOrDefault(r=>r.transactionId==request.BattleId);
            if(receipt!=null){if(receipt.signature!=request.Signature)throw new ArgumentException("Battle ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){if(pendingVictory==null || request.BattleId!=pendingVictory.BattleId||request.Signature!=pendingVictory.Signature||request.Revision!=pendingVictory.Revision)throw new InvalidOperationException("Retry the same victory.");}
            else{
                if(request.Revision!=current.revision || current.world.claimedBattleIds.Contains(request.BattleId) || rewardWorld==null)throw new ArgumentException("Stale or already-claimed world victory.");
                var next=Snapshot;TerraformRules.RequireIntegration(next.world,request.ColossusId);writing=true;try{next.world=rewardWorld(next.world);}finally{writing=false;}
                if(next.world==null || !next.world.claimedBattleIds.Contains(request.BattleId))throw new ArgumentException("World callback must claim this battle.");
                next=Copy(next);next.growth.nectar=checked(next.growth.nectar+request.Nectar);next.growth.awakeningCrystals=checked(next.growth.awakeningCrystals+request.Crystals);next.growth.stones=checked(next.growth.stones+request.Stones);
                next.growth.revision=checked(next.growth.revision+1);next.growth.receipts=next.growth.receipts.Concat(new[]{new GrowthReceipt {transactionId=request.BattleId,signature=request.Signature}}).ToArray();next.revision=checked(next.revision+1);next.Validate();pending=next;pendingVictory=request;
            }
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;
            pending=null;pendingVictory=null;return GrowthCommitResult.Committed;
        }
        private bool Persist(FormalCampaignSave next,Func<FormalCampaignSave,bool> save)
        {
            next.Validate();bool success;writing=true;
            try{success=save(Copy(next));}finally{writing=false;}
            if(success)current=Copy(next);return success;
        }
    }
}
