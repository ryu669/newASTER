using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class CollectionMaterial
    {
        public string id,sourceColossusId;
        public int amount;
    }
    [Serializable] public sealed class CollectionRelic
    {
        public string id,contentVersion=CollectionCatalog.FixtureVersion;
        public int level=1,attackRoll,hpRoll;
    }
    [Serializable] public sealed class CollectionEquipment {public string heroineId,relicId;}
    public static class FormalRelicRules
    {
        // Versioned independent inventory tuning; historical receipt costs stay unchanged.
        public const int AttackMaximum=100,HpMaximum=1000;
        public static int UpgradeCost(CollectionRelic r,RelicOperation operation,string contentVersion) => contentVersion==CollectionCatalog.ProductionVersion ? (operation==RelicOperation.LevelUp?r.level:30) : (operation==RelicOperation.LevelUp?checked(10*r.level):1000);
        public static int Attack(CollectionRelic r)=>checked(10+2*(r.level-1)+r.attackRoll);
        public static int Hp(CollectionRelic r)=>checked(50+5*(r.level-1)+r.hpRoll);
        public static bool DirectEligible(int value,int maximum)=>(long)value*5 >= (long)maximum*4;
        public static int MaterialBalance(FormalCollectionLedger ledger,CollectionRelicDef definition)=>definition.materialIds.Min(id=>ledger?.materials.SingleOrDefault(m=>m.id==id)?.amount??0);
        public static CollectionRelic Equipped(FormalCollectionLedger ledger,string heroineId)
        {
            var e=ledger?.equipment.SingleOrDefault(x=>x.heroineId==heroineId);
            return e==null?null:ledger.relics.Single(x=>x.id==e.relicId);
        }
    }
    public enum RelicOperation { LevelUp, AttackUp, HpUp, Equip, Unequip }
    public sealed class FormalRelicRequest
    {
        public string Id {get;}
        public string RelicId {get;}
        public string HeroineId {get;}
        public long Revision {get;}
        public RelicOperation Operation {get;}
        public string ContentVersion {get;}
        public string Signature=>"relic|"+ContentVersion+"|"+Operation+"|"+RelicId+"|"+HeroineId;
        public FormalRelicRequest(string id,string relicId,string heroineId,long revision,RelicOperation operation,string contentVersion=CollectionCatalog.FixtureVersion)
        {
            if(!CollectionCatalog.ValidId(id) || !CollectionCatalog.ValidId(relicId) || heroineId!=null && !CollectionCatalog.ValidId(heroineId) || revision<0 || !Enum.IsDefined(typeof(RelicOperation),operation))throw new ArgumentException("Invalid relic request.");
            if(!CollectionCatalog.SupportedVersion(contentVersion))throw new ArgumentException("Unknown relic request content.");
            Id=id;RelicId=relicId;HeroineId=heroineId;Revision=revision;Operation=operation;ContentVersion=contentVersion;
        }
    }
    public sealed partial class FormalCampaignJournal
    {
        private FormalRelicRequest pendingRelic;
        public GrowthCommitResult CommitRelic(FormalRelicRequest request,CollectionCatalog catalog,Func<FormalCampaignSave,bool> save)
        {
            if(request==null || save==null)throw new ArgumentNullException();if(writing)throw new InvalidOperationException("Concurrent save.");
            var receipt=current.growth.receipts.SingleOrDefault(x=>x.transactionId==request.Id);
            if(receipt!=null){if(receipt.signature!=request.Signature)throw new ArgumentException("Relic request ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){
                if(pendingRelic==null || pendingRelic.Id!=request.Id || pendingRelic.Signature!=request.Signature || pendingRelic.Revision!=request.Revision)throw new InvalidOperationException("Retry same relic operation.");
            }else{
                catalog.Validate();
                if(catalog.contentVersion!=request.ContentVersion)throw new ArgumentException("Relic request content mismatch.");
                current.collection?.ValidateContent(catalog);
                if(request.Revision!=current.revision || current.collection==null)throw new ArgumentException("Stale or missing relic inventory.");
                var next=Snapshot;var inventory=next.collection;
                var def=catalog.relics.SingleOrDefault(x=>x.id==request.RelicId)??throw new ArgumentException("Unknown relic definition.");
                var relic=inventory.relics.SingleOrDefault(x=>x.id==def.id)??throw new ArgumentException("Relic not owned.");
                if(relic.contentVersion!=catalog.contentVersion)throw new ArgumentException("Relic content version mismatch.");
                if(request.Operation==RelicOperation.Equip || request.Operation==RelicOperation.Unequip){
                    if(!catalog.owners.Any(x=>x.id==request.HeroineId && x.kind=="heroine") || !next.growth.heroines.Any(x=>x.heroineId==request.HeroineId))throw new ArgumentException("Heroine not owned.");
                    if(request.Operation==RelicOperation.Equip){
                        if(inventory.equipment.Any(x=>x.relicId==relic.id))throw new ArgumentException("One relic can be equipped by one heroine.");
                        inventory.equipment=inventory.equipment.Where(x=>x.heroineId!=request.HeroineId).Concat(new[]{new CollectionEquipment {heroineId=request.HeroineId,relicId=relic.id}}).ToArray();
                    }else{
                        if(!inventory.equipment.Any(x=>x.heroineId==request.HeroineId && x.relicId==relic.id))throw new ArgumentException("Relic is not equipped.");
                        inventory.equipment=inventory.equipment.Where(x=>x.heroineId!=request.HeroineId).ToArray();
                    }
                }else{
                    int cost=FormalRelicRules.UpgradeCost(relic,request.Operation,catalog.contentVersion);
                    if(request.Operation==RelicOperation.LevelUp && relic.level>=120 ||
                       request.Operation==RelicOperation.AttackUp && (!FormalRelicRules.DirectEligible(relic.attackRoll,100) || relic.attackRoll>=100) ||
                       request.Operation==RelicOperation.HpUp && (!FormalRelicRules.DirectEligible(relic.hpRoll,1000) || relic.hpRoll>=1000))throw new ArgumentException("Relic upgrade unavailable.");
                    var mats=def.materialIds.Select(id=>inventory.materials.SingleOrDefault(x=>x.id==id)).ToArray();
                    if(mats.Any(x=>x==null || x.amount<cost))throw new ArgumentException("Insufficient colossus materials.");
                    foreach(var m in mats)m.amount-=cost;
                    if(request.Operation==RelicOperation.LevelUp)relic.level++;
                    else if(request.Operation==RelicOperation.AttackUp)relic.attackRoll=Math.Min(100,relic.attackRoll+1);
                    else relic.hpRoll=Math.Min(1000,relic.hpRoll+10);
                }
                next.growth.receipts=next.growth.receipts.Concat(new[]{new GrowthReceipt {transactionId=request.Id,signature=request.Signature}}).ToArray();
                next.growth.revision=checked(next.growth.revision+1);next.revision=checked(next.revision+1);next.Validate();pending=next;pendingRelic=request;
            }
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;
            pending=null;pendingRelic=null;return GrowthCommitResult.Committed;
        }
    }
}
