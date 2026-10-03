using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed class FormalBattleEndRequest
    {
        private readonly CollectionReceipt receipt;
        public long Revision {get;}
        public CollectionReceipt Receipt=>Copy(receipt);
        public string Id=>receipt.battle.battleId;
        public string Signature=>"battle-end|"+receipt.battle.contentVersion+"|"+receipt.battle.combatVersion+"|"+receipt.battle.colossusVersion+"|"+receipt.reason+"|"+receipt.battle.colossusId+"|"+receipt.battle.level+"|"+receipt.battle.revision+"|"+receipt.battle.seed+"|"+string.Join(",",receipt.battle.formationIds)+"|"+string.Join(",",receipt.battle.heardPoemIds)+"|"+string.Join(",",receipt.acquiredPoemIds)+"|"+string.Join(",",receipt.unlockedChapterIds);
        public FormalBattleEndRequest(CollectionReceipt receipt,long revision)
        {
            new FormalCollectionLedger {contentVersion=receipt?.battle?.contentVersion,receipts=new[]{receipt}}.Validate();
            if(revision<0)throw new ArgumentException("Invalid end revision.");
            this.receipt=Copy(receipt);Revision=revision;
        }
        private static CollectionReceipt Copy(CollectionReceipt r)=>new CollectionReceipt {battle=r.battle.Copy(),reason=r.reason,acquiredPoemIds=(string[])r.acquiredPoemIds.Clone(),unlockedChapterIds=(string[])r.unlockedChapterIds.Clone()};
    }
    public sealed partial class FormalCampaignJournal
    {
        private FormalBattleEndRequest pendingBattleEnd;
        private static void ApplyHunt(FormalCollectionLedger ledger,CollectionCatalog catalog,CollectionReceipt receipt)
        {
            var b=receipt.battle;
            var band=catalog.rewardBands.Single(x=>x.ownerId==b.colossusId && b.level>=x.minLevel && b.level<=x.maxLevel);
            var source=catalog.owners.Single(x=>x.id==b.colossusId);
            foreach(var id in source.materialIds){
                var material=ledger.materials.SingleOrDefault(x=>x.id==id);
                if(material==null){material=new CollectionMaterial {id=id,sourceColossusId=b.colossusId};ledger.materials=ledger.materials.Concat(new[]{material}).ToArray();}
                material.amount=checked(material.amount+10+b.level);
            }
            var rng=new Random(unchecked(b.seed ^ 0x48554E54));var drops=new System.Collections.Generic.List<CollectionRelic>();
            for(int i=0;i<band.draws;i++){
                // Fixture: 75% per attempt. A pool that forbids empty results always grants.
                if(band.allowEmpty && rng.Next(10000)>=7500)continue;
                var id=band.relicIds[rng.Next(band.relicIds.Length)];
                var drop=new CollectionRelic {id=id,contentVersion=catalog.contentVersion,attackRoll=rng.Next(101),hpRoll=rng.Next(1001)};
                drops.Add(drop);
                var item=ledger.relics.SingleOrDefault(x=>x.id==id);
                if(item==null)ledger.relics=ledger.relics.Concat(new[]{new CollectionRelic {id=id,contentVersion=drop.contentVersion,attackRoll=drop.attackRoll,hpRoll=drop.hpRoll}}).ToArray();
                else{
                    if(item.contentVersion!=drop.contentVersion)throw new ArgumentException("Same relic has different content version.");
                    item.attackRoll=Math.Max(item.attackRoll,drop.attackRoll);item.hpRoll=Math.Max(item.hpRoll,drop.hpRoll);
                }
            }
            receipt.relicDrawCount=band.draws;receipt.relicDrops=drops.ToArray();
        }
        public GrowthCommitResult CommitBattleEnd(FormalBattleEndRequest request,CollectionCatalog catalog,Func<CampaignSaveV2,CampaignSaveV2> victoryWorld,Func<FormalCampaignSave,bool> save,HomeExperienceCatalog homeCatalog=null)
        {
            if(request==null || save==null)throw new ArgumentNullException();
            if(writing)throw new InvalidOperationException("Concurrent campaign write.");
            var old=current.growth.receipts.SingleOrDefault(x=>x.transactionId==request.Id);
            if(old!=null){if(old.signature!=request.Signature)throw new ArgumentException("Battle ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){
                if(pendingBattleEnd==null || pendingBattleEnd.Id!=request.Id || pendingBattleEnd.Signature!=request.Signature || pendingBattleEnd.Revision!=request.Revision)
                    throw new InvalidOperationException("Retry same battle end first.");
            }else{
                if(request.Revision!=current.revision || current.world.claimedBattleIds.Contains(request.Id))throw new ArgumentException("Stale battle end.");
                catalog.Validate();current.collection?.ValidateContent(catalog);var r=request.Receipt;var b=r.battle;
                if(b.revision>request.Revision || b.contentVersion!=catalog.contentVersion)throw new ArgumentException("Battle content revision mismatch.");
                var session=new BattleCollectionSession(catalog,b.battleId,b.colossusId,b.level,b.revision,b.formationIds);
                foreach(var poem in b.heardPoemIds)session.RecordCompletedSinging(poem);
                var expected=session.Finish(r.reason,current.world.poemIds,current.world.unlockedStoryIds);
                if(!expected.acquiredPoemIds.SequenceEqual(r.acquiredPoemIds) || !expected.unlockedChapterIds.SequenceEqual(r.unlockedChapterIds))throw new ArgumentException("Collection result mismatch.");
                var next=Snapshot;
                if(r.reason==BattleEndReason.Victory){
                    if(victoryWorld==null)throw new ArgumentNullException(nameof(victoryWorld));
                    writing=true;try{next.world=victoryWorld(next.world);}finally{writing=false;}
                    if(next.world==null || !next.world.claimedBattleIds.Contains(request.Id))throw new ArgumentException("Victory callback must claim current battle.");
                    var rewards=new FormalVictoryRequest(request.Id,b.colossusId,b.level,request.Revision);
                    next.growth.nectar=checked(next.growth.nectar+rewards.Nectar);
                    next.growth.awakeningCrystals=checked(next.growth.awakeningCrystals+rewards.Crystals);
                    next.growth.stones=checked(next.growth.stones+rewards.Stones);
                }else next.world.claimedBattleIds=next.world.claimedBattleIds.Concat(new[]{request.Id}).ToArray();
                // Never permit a callback to erase previously acquired formal world history.
                if(current.world.poemIds.Except(next.world.poemIds).Any() || current.world.claimedBattleIds.Except(next.world.claimedBattleIds).Any())throw new ArgumentException("World history lost.");
                next.world.poemIds=next.world.poemIds.Union(r.acquiredPoemIds).ToArray();
                next.world.unlockedStoryIds=next.world.unlockedStoryIds.Union(r.unlockedChapterIds).ToArray();
                if(next.collection==null)next.collection=new FormalCollectionLedger{contentVersion=catalog.contentVersion};
                if(r.reason==BattleEndReason.Victory)ApplyHunt(next.collection,catalog,r);
                next.collection.receipts=next.collection.receipts.Concat(new[]{r}).ToArray();
                next.growth.receipts=next.growth.receipts.Concat(new[]{new GrowthReceipt {transactionId=request.Id,signature=request.Signature}}).ToArray();
                next.growth.revision=checked(next.growth.revision+1);next.revision=checked(next.revision+1);
                if(homeCatalog!=null){homeCatalog.Validate();HomeConditions.Refresh(next,homeCatalog);next.home?.ValidateContent(homeCatalog,next);}else if(next.home!=null)throw new ArgumentException("Home conditions must join the battle transaction.");
                next=Copy(next);next.Validate();pending=next;pendingBattleEnd=request;
            }
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;
            pending=null;pendingBattleEnd=null;return GrowthCommitResult.Committed;
        }
    }
}
