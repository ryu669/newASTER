using System;
using System.Linq;
using System.Security.Cryptography;
namespace NewAster.Core
{
    [Serializable] public sealed class HeroineTicket
    {public string heroineId;public int count;public HeroineTicket Copy()=>(HeroineTicket)MemberwiseClone();}
    [Serializable] public sealed class KinderOutcome
    {
        public string kind,heroineId,grantKind;
        public int amount;
        public void Validate(){if(amount<1 || kind!="heroine" && kind!="nectar" && kind!="crystal" || kind=="heroine" && (amount!=1 || string.IsNullOrWhiteSpace(heroineId)) || kind!="heroine" && heroineId!=null || grantKind!=null && grantKind!="owned" && grantKind!="fragments" && grantKind!="overflow")throw new ArgumentException("Invalid kinder reward.");}
        public KinderOutcome Copy()=>(KinderOutcome)MemberwiseClone();
    }
    [Serializable] public sealed class KinderMaterialEntry
    {public string kind;public int weight,amount;}
    [Serializable] public sealed class FormalKinderBanner
    {
        public const int StoneCost=300,ExchangeCost=100;
        public const string ProductionVersion="kinder-production-2026-10-05";
        public string id,contentVersion,status;
        public string[] heroineIds;
        public KinderMaterialEntry[] materials;
        public bool ticketPoints;
        public FormalKinderBanner Copy()=>new FormalKinderBanner {id=id,contentVersion=contentVersion,status=status,ticketPoints=ticketPoints,heroineIds=(string[])heroineIds.Clone(),materials=materials.Select(m=>new KinderMaterialEntry {kind=m.kind,weight=m.weight,amount=m.amount}).ToArray()};
        public void Validate(string[] known)
        {
            bool trial=contentVersion=="kinder-trial-2026-10-02" && status=="trial";
            bool production=contentVersion==ProductionVersion && status=="production-candidate";
            if(!(trial && id=="kinder.initial-five" || production && id=="kinder.all-implemented") || known==null || heroineIds==null || heroineIds.Length==0 || heroineIds.Any(id=>!known.Contains(id)) || heroineIds.Distinct().Count()!=heroineIds.Length || trial && heroineIds.Length!=5 || production && !known.All(id=>heroineIds.Contains(id)) || ticketPoints)throw new ArgumentException("Incomplete kinder rules.");
            if(materials==null || materials.Length==0 || materials.Any(m=>m==null || m.kind!="nectar" && m.kind!="crystal" || m.weight<=0 || m.amount<=0) || materials.Sum(m=>(long)m.weight)>int.MaxValue)throw new ArgumentException("Invalid material rewards.");
        }
        public KinderOutcome Draw(Func<int,int> nextBelow)
        {
            int category=Roll(nextBelow,10000);
            if(category<300)return new KinderOutcome {kind="heroine",heroineId=heroineIds[Roll(nextBelow,heroineIds.Length)],amount=1};
            int roll=Roll(nextBelow,materials.Sum(m=>m.weight));foreach(var material in materials){if(roll<material.weight)return new KinderOutcome {kind=material.kind,amount=material.amount};roll-=material.weight;}
            throw new InvalidOperationException("Invalid reward weights.");
        }
        private static int Roll(Func<int,int> next,int max){int value=next(max);if(value<0||value>=max)throw new ArgumentException("Random value out of range.");return value;}
    }
    public static class KinderRandom
    {
        public static int NextBelow(int maximum)
        {
            if(maximum<=0)throw new ArgumentOutOfRangeException(nameof(maximum));
            ulong range=1UL<<32,ceiling=range-range%(uint)maximum;var bytes=new byte[4];
            using(var source=RandomNumberGenerator.Create()){uint value;do{source.GetBytes(bytes);value=BitConverter.ToUInt32(bytes,0);}while(value>=ceiling);return (int)(value%(uint)maximum);}
        }
    }
    public enum KinderOperation { StoneDraw, Exchange, TicketDraw, IntroGrant }
    public sealed class KinderRequest
    {
        public string Id {get;}
        public long Revision {get;}
        public KinderOperation Operation {get;}
        public int Count {get;}
        public string HeroineId {get;}
        public string BannerVersion {get;}
        public KinderRequest(string id,long revision,KinderOperation operation,int count=1,string heroineId=null,string bannerVersion="kinder-trial-2026-10-02")
        {
            if(string.IsNullOrWhiteSpace(id)||id.Contains("|")||heroineId!=null&&heroineId.Contains("|"))throw new ArgumentException("Invalid request ID.");
            Id=id;Revision=revision;Operation=operation;Count=count;HeroineId=heroineId;BannerVersion=bannerVersion;
        }
        public string Signature=>"kinder|"+BannerVersion+"|"+Revision+"|"+(int)Operation+"|"+Count+"|"+HeroineId;
    }
    public sealed partial class FormalProgression
    {
        public GrowthReceipt KinderReceipt(string id)=>current.receipts.SingleOrDefault(r=>r.transactionId==id)?.Copy();
        public void PreviewKinder(KinderRequest request,FormalKinderBanner banner)
        {
            if(saving || HasPending)throw new InvalidOperationException("Finish pending save first.");
            ValidateKinder(request,banner);
        }
        private void ValidateKinder(KinderRequest r,FormalKinderBanner banner)
        {
            if(r==null||banner==null)throw new ArgumentNullException();banner.Validate(knownIds);
            if(r.Revision!=current.revision || r.BannerVersion!=banner.contentVersion || !Enum.IsDefined(typeof(KinderOperation),r.Operation))throw new ArgumentException("Stale kinder request.");
            if(r.Operation==KinderOperation.IntroGrant){if(r.Id!="grant.plan4-kinder-introduction" || r.Count!=1 || r.HeroineId!=null)throw new ArgumentException("Invalid one-time intro grant.");}
            else if(r.Operation==KinderOperation.StoneDraw){if(r.Count!=1 && r.Count!=10 || r.HeroineId!=null || current.stones<300*r.Count)throw new ArgumentException("Invalid draw or insufficient stones.");}
            else if(r.Count!=1 || !banner.heroineIds.Contains(r.HeroineId) || r.Operation==KinderOperation.Exchange && current.kinderPoints<100 || r.Operation==KinderOperation.TicketDraw && (current.tickets.SingleOrDefault(t=>t.heroineId==r.HeroineId)?.count??0)<1)throw new ArgumentException("Invalid ticket operation.");
        }
        public GrowthCommitResult CommitKinder(KinderRequest r,FormalKinderBanner banner,Func<int,int> random,Func<FormalGrowthSave,bool> save)
        {
            if(r==null||save==null)throw new ArgumentNullException();if(saving)throw new InvalidOperationException("Concurrent save.");
            var existing=current.receipts.SingleOrDefault(x=>x.transactionId==r.Id);
            if(existing!=null){if(existing.signature!=r.Signature)throw new ArgumentException("ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(pending!=null){if(pendingRequest!=null || pendingEconomyId!=r.Id || pendingEconomySignature!=r.Signature)throw new InvalidOperationException("Retry pending operation first.");}
            else {
                ValidateKinder(r,banner);banner=banner.Copy();var next=current.Copy();KinderOutcome[] outcomes=Array.Empty<KinderOutcome>();
                if(r.Operation==KinderOperation.IntroGrant)next.stones=checked(next.stones+3000);
                else if(r.Operation==KinderOperation.StoneDraw){
                    if(random==null)throw new ArgumentNullException(nameof(random));
                    next.stones-=300*r.Count;next.kinderPoints=checked(next.kinderPoints+r.Count);next.totalKinderDraws=checked(next.totalKinderDraws+r.Count);
                    outcomes=Enumerable.Range(0,r.Count).Select(_=>banner.Draw(random)).ToArray();foreach(var outcome in outcomes)ApplyKinder(next,outcome);
                }else if(r.Operation==KinderOperation.Exchange){
                    next.kinderPoints-=100;var ticket=next.tickets.SingleOrDefault(t=>t.heroineId==r.HeroineId);
                    if(ticket==null)next.tickets=next.tickets.Concat(new[]{new HeroineTicket {heroineId=r.HeroineId,count=1}}).ToArray();else ticket.count=checked(ticket.count+1);
                }else {
                    next.tickets.Single(t=>t.heroineId==r.HeroineId).count--;outcomes=new[]{new KinderOutcome {kind="heroine",heroineId=r.HeroineId,amount=1}};ApplyKinder(next,outcomes[0]);
                }
                next.revision=checked(next.revision+1);next.receipts=next.receipts.Concat(new[]{new GrowthReceipt {transactionId=r.Id,signature=r.Signature,kinderOutcomes=outcomes.Select(x=>x.Copy()).ToArray()}}).ToArray();next.Validate();
                pending=next;pendingEconomyId=r.Id;pendingEconomySignature=r.Signature;
            }
            bool success;saving=true;try{success=save(pending.Copy());}finally{saving=false;}
            if(!success)return GrowthCommitResult.SaveFailed;
            current=pending;pending=null;pendingEconomyId=null;pendingEconomySignature=null;return GrowthCommitResult.Committed;
        }
        private static void ApplyKinder(FormalGrowthSave save,KinderOutcome reward)
        {
            reward.Validate();
            if(reward.kind=="nectar")save.nectar=checked(save.nectar+reward.amount);
            else if(reward.kind=="crystal")save.awakeningCrystals=checked(save.awakeningCrystals+reward.amount);
            else {
                var hero=save.heroines.SingleOrDefault(h=>h.heroineId==reward.heroineId);
                if(hero==null){save.heroines=save.heroines.Concat(new[]{new FormalHeroineGrowth {heroineId=reward.heroineId}}).ToArray();reward.grantKind="owned";}
                else if(hero.duplicateRank==5){save.overflow=checked(save.overflow+100);reward.grantKind="overflow";}
                else{hero.fragments=checked(hero.fragments+100);reward.grantKind="fragments";}
            }
        }
    }
}
