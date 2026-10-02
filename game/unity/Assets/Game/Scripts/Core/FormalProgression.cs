using System;
using System.Linq;

namespace NewAster.Core
{
    [Serializable] public sealed class FormalHeroineGrowth
    {
        public string heroineId;
        public int level = 1, awakeningStage, duplicateRank, fragments;
        public int LevelCap => awakeningStage == 0 ? 50 : awakeningStage == 1 ? 80 : 120;
        public FormalHeroineGrowth Copy() => (FormalHeroineGrowth)MemberwiseClone();
    }
    [Serializable] public sealed class GrowthReceipt
    {
        public string transactionId, signature;
        public GrowthReceipt Copy() => (GrowthReceipt)MemberwiseClone();
    }
    /// <summary>Independent formal payload. Legacy index-based saves are not interpreted as this format.</summary>
    [Serializable] public sealed class FormalGrowthSave
    {
        public const string ContentVersion = "growth-2026-10-02";
        public int version = 1;
        public string saveId, contentVersion = ContentVersion;
        public long revision;
        public int nectar, awakeningCrystals, overflow;
        public FormalHeroineGrowth[] heroines = Array.Empty<FormalHeroineGrowth>();
        public GrowthReceipt[] receipts = Array.Empty<GrowthReceipt>();
        public FormalGrowthSave Copy() => new FormalGrowthSave {
            version=version, saveId=saveId, contentVersion=contentVersion, revision=revision,
            nectar=nectar, awakeningCrystals=awakeningCrystals, overflow=overflow,
            heroines=heroines.Select(h=>h.Copy()).ToArray(), receipts=receipts.Select(r=>r.Copy()).ToArray()
        };
        public void Validate()
        {
            if(version!=1 || contentVersion!=ContentVersion || string.IsNullOrWhiteSpace(saveId) || revision<0 || nectar<0 || awakeningCrystals<0 || overflow<0)
                throw new ArgumentException("Unsupported or invalid formal growth save.");
            if(heroines==null || heroines.Any(h=>h==null || string.IsNullOrWhiteSpace(h.heroineId) || h.awakeningStage<0 || h.awakeningStage>2 || h.level<1 || h.level>h.LevelCap || h.duplicateRank<0 || h.duplicateRank>5 || h.fragments<0 || h.duplicateRank==5 && h.fragments!=0) || heroines.Select(h=>h.heroineId).Distinct().Count()!=heroines.Length)
                throw new ArgumentException("Invalid formal heroine growth.");
            if(receipts==null || receipts.Any(r=>r==null || string.IsNullOrWhiteSpace(r.transactionId) || string.IsNullOrEmpty(r.signature)) || receipts.Select(r=>r.transactionId).Distinct().Count()!=receipts.Length)
                throw new ArgumentException("Invalid growth receipts.");
        }
    }
    public enum GrowthOperation { Level, Awaken, Strengthen, ReceiveHeroine }
    public sealed class GrowthRequest
    {
        public string TransactionId { get; }
        public string HeroineId { get; }
        public string ContentVersion { get; }
        public long BaseRevision { get; }
        public int TargetLevel { get; }
        public GrowthOperation Operation { get; }
        public GrowthRequest(string transactionId,string heroineId,long baseRevision,GrowthOperation operation,int targetLevel=0,string contentVersion=FormalGrowthSave.ContentVersion)
        {
            if(string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(heroineId) || transactionId.Contains("|") || heroineId.Contains("|")) throw new ArgumentException("Operation IDs required.");
            TransactionId=transactionId;HeroineId=heroineId;BaseRevision=baseRevision;Operation=operation;TargetLevel=targetLevel;ContentVersion=contentVersion;
        }
        public string Signature => ContentVersion+"|"+HeroineId+"|"+(int)Operation+"|"+TargetLevel+"|"+BaseRevision;
    }
    public sealed class GrowthPreview
    {
        public int NectarCost { get; internal set; }
        public int CrystalCost { get; internal set; }
        public int FragmentCost { get; internal set; }
        public int OverflowCost { get; internal set; }
        public int OverflowGrant { get; internal set; }
        public FormalHeroineGrowth HeroineAfter { get; internal set; }
    }
    public enum GrowthCommitResult { Committed, AlreadyCommitted, SaveFailed }
    /// <summary>Single-writer growth transactions: detached preview, durable callback, exact retry, idempotent receipts.</summary>
    public sealed class FormalProgression
    {
        private FormalGrowthSave current, pending;
        private GrowthRequest pendingRequest;
        private bool saving;
        private readonly string[] knownIds;
        public FormalGrowthSave Snapshot => current.Copy();
        public bool HasPending => pending!=null;
        public FormalProgression(FormalGrowthSave save,string[] knownHeroineIds)
        {
            if(save==null) throw new ArgumentNullException(nameof(save));save.Validate();
            if(knownHeroineIds==null || knownHeroineIds.Any(string.IsNullOrWhiteSpace) || knownHeroineIds.Distinct().Count()!=knownHeroineIds.Length) throw new ArgumentException("Invalid heroine registry.");
            current=save.Copy();knownIds=(string[])knownHeroineIds.Clone();
            // Unknown owned IDs remain in payload but cannot be used as operation targets.
        }
        public static int LevelCost(int from,int to)
        {
            if(from<1 || to<=from || to>120) throw new ArgumentOutOfRangeException(nameof(to));
            long count=to-from;
            return checked((int)(10*count+count*(from+to-1)));
        }
        public GrowthPreview Preview(GrowthRequest request)
        {
            if(saving || HasPending) throw new InvalidOperationException("A save must finish or retry first.");
            return Prepare(request,out _);
        }
        private GrowthPreview Prepare(GrowthRequest r,out FormalGrowthSave candidate)
        {
            if(r==null) throw new ArgumentNullException(nameof(r));
            if(r.ContentVersion!=current.contentVersion || r.BaseRevision!=current.revision || !knownIds.Contains(r.HeroineId) || !Enum.IsDefined(typeof(GrowthOperation),r.Operation) || r.Operation!=GrowthOperation.Level && r.TargetLevel!=0)
                throw new ArgumentException("Stale or unsupported growth request.");
            candidate=current.Copy();var h=candidate.heroines.SingleOrDefault(x=>x.heroineId==r.HeroineId);var p=new GrowthPreview();
            if(r.Operation==GrowthOperation.ReceiveHeroine) {
                if(h==null) { h=new FormalHeroineGrowth {heroineId=r.HeroineId};candidate.heroines=candidate.heroines.Concat(new[]{h}).ToArray(); }
                else if(h.duplicateRank==5) {candidate.overflow=checked(candidate.overflow+100);p.OverflowGrant=100;}
                else h.fragments=checked(h.fragments+100);
            } else {
                if(h==null) throw new ArgumentException("Heroine not owned.");
                switch(r.Operation) {
                    case GrowthOperation.Level:
                        if(r.TargetLevel>h.LevelCap) throw new ArgumentException("Awakening cap exceeded.");
                        p.NectarCost=LevelCost(h.level,r.TargetLevel);
                        if(candidate.nectar<p.NectarCost) throw new ArgumentException("Not enough nectar.");
                        candidate.nectar-=p.NectarCost;h.level=r.TargetLevel;break;
                    case GrowthOperation.Awaken:
                        if(h.awakeningStage==2 || h.level!=h.LevelCap) throw new ArgumentException("Awakening requirement not met.");
                        p.CrystalCost=h.awakeningStage==0?20:60;
                        if(candidate.awakeningCrystals<p.CrystalCost) throw new ArgumentException("Not enough crystals.");
                        candidate.awakeningCrystals-=p.CrystalCost;h.awakeningStage++;break;
                    case GrowthOperation.Strengthen:
                        if(h.duplicateRank==5) throw new ArgumentException("Duplicate rank at cap.");
                        p.FragmentCost=Math.Min(100,h.fragments);p.OverflowCost=100-p.FragmentCost;
                        if(candidate.overflow<p.OverflowCost) throw new ArgumentException("Not enough fragments.");
                        h.fragments-=p.FragmentCost;candidate.overflow-=p.OverflowCost;h.duplicateRank++;
                        if(h.duplicateRank==5) {p.OverflowGrant=h.fragments;candidate.overflow=checked(candidate.overflow+h.fragments);h.fragments=0;}
                        break;
                }
            }
            p.HeroineAfter=h.Copy();candidate.revision=checked(candidate.revision+1);
            candidate.receipts=candidate.receipts.Concat(new[]{new GrowthReceipt {transactionId=r.TransactionId,signature=r.Signature}}).ToArray();
            candidate.Validate();return p;
        }
        public GrowthCommitResult Commit(GrowthRequest request,Func<FormalGrowthSave,bool> durableSave)
        {
            if(request==null || durableSave==null) throw new ArgumentNullException();
            if(saving) throw new InvalidOperationException("Concurrent save rejected.");
            var receipt=current.receipts.SingleOrDefault(r=>r.transactionId==request.TransactionId);
            if(receipt!=null) {
                if(receipt.signature!=request.Signature) throw new ArgumentException("Transaction ID reused with other contents.");
                return GrowthCommitResult.AlreadyCommitted;
            }
            if(pending!=null) {
                if(request.TransactionId!=pendingRequest.TransactionId || request.Signature!=pendingRequest.Signature) throw new InvalidOperationException("Retry the pending operation first.");
            } else { Prepare(request,out var candidate);pending=candidate;pendingRequest=request; }
            bool saved=false;saving=true;
            try { saved=durableSave(pending.Copy()); }
            finally { saving=false; }
            if(!saved) return GrowthCommitResult.SaveFailed;
            current=pending;pending=null;pendingRequest=null;return GrowthCommitResult.Committed;
        }
    }
    public static class FormalGrowthMath
    {
        public static int Stat(int jobBase,int modifierBp,int level,int duplicateRank)
        {
            if(jobBase<1 || jobBase>1000000 || modifierBp<9000 || modifierBp>11000 || level<1 || level>120 || duplicateRank<0 || duplicateRank>5) throw new ArgumentOutOfRangeException();
            long basis=Math.Max(1,(long)jobBase*modifierBp*(100+3*(level-1))/1000000);
            return checked((int)(basis*(100+2*duplicateRank)/100));
        }
        public static int Speed(int jobSpeed,int modifierBp)
        {
            if(jobSpeed<1 || jobSpeed>10000 || modifierBp<9500 || modifierBp>10500) throw new ArgumentOutOfRangeException();
            return Math.Max(1,(int)((long)jobSpeed*modifierBp/10000));
        }
        public static int TraitAmount(int baseAmount,int rank)
        {
            if(baseAmount<1 || rank<0 || rank>5) throw new ArgumentOutOfRangeException();
            return checked((int)((long)baseAmount*(100+2*rank)/100));
        }
    }
}
