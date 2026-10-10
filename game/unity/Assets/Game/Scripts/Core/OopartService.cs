using System;
using System.Linq;
namespace NewAster.Core
{
    public static class OopartSaveAdapter
    {
        public static void Migrate(FormalCollectionLedger ledger,CollectionCatalog catalog,string[] formation)
        {
            if(ledger==null || catalog.oopartDefs.Length==0)return;
            if(ledger.ooparts==null){ledger.ooparts=new OopartInventorySave();ledger.ooparts.SyncFormation(formation);if(formation!=null && formation.Length==5)for(int i=0;i<5;i++)ledger.ooparts.slots[i].equippedOopartId=ledger.equipment.SingleOrDefault(e=>e.heroineId==formation[i])?.relicId;}
            foreach(var r in ledger.relics)if(!ledger.ooparts.progress.Any(p=>p.oopartId==r.id))ledger.ooparts.progress=ledger.ooparts.progress.Concat(new[]{new OopartProgress{oopartId=r.id,level=r.level,legacyHpMaximum=r.hpRoll>catalog.Oopart(r.id).randomStats.Single(s=>s.stat=="hp").maximum?1000:0,accumulatedRandomStats=new StatValues{hp=r.hpRoll,attack=r.attackRoll}}}).ToArray();
            ledger.ooparts.ValidateContent(catalog);
        }
    }
    public static class OopartService
    {
        // Read-only plan: use the same per-level cost as the committed operation.
        public static int ReachableLevel(FormalCampaignSave save,CollectionCatalog catalog,string id,int count)
        {
            if(count<1 || count>120)throw new ArgumentOutOfRangeException(nameof(count));
            int level=save.collection.ooparts.progress.Single(p=>p.oopartId==id).level;
            int balance=FormalRelicRules.MaterialBalance(save.collection,catalog.relics.Single(r=>r.id==id));
            for(int changed=0;level<120 && changed<count;changed++){
                int cost=FormalRelicRules.UpgradeCost(new CollectionRelic{level=level},RelicOperation.LevelUp,catalog.contentVersion);
                if(balance<cost)break;balance-=cost;level++;
            }
            return level;
        }
        public static StatValues Roll(OopartDef d,Random rng){var v=new StatValues();foreach(var r in d.randomStats){int min=(r.maximum*r.minimumPercent+99)/100,max=r.maximum*r.maximumPercent/100;v.Set(r.stat,rng.Next(min,max+1));}return v;}
        public static StatValues Merge(OopartInventorySave inventory,OopartDef d,StatValues incoming)
        {
            foreach(var r in d.randomStats)if(incoming.Get(r.stat)<0 || incoming.Get(r.stat)>r.maximum)throw new ArgumentException("Invalid oopart roll.");
            var p=inventory.progress.SingleOrDefault(i=>i.oopartId==d.id);if(p==null){p=new OopartProgress{oopartId=d.id};inventory.progress=inventory.progress.Concat(new[]{p}).ToArray();}
            var before=p.accumulatedRandomStats.Copy();foreach(var r in d.randomStats)p.accumulatedRandomStats.Set(r.stat,Math.Max(p.accumulatedRandomStats.Get(r.stat),incoming.Get(r.stat)));return before;
        }
        public static StatValues Fixed(OopartProgress p,OopartDef d)=>StatValues.Add(d.levelStats.At(p.level),p.accumulatedRandomStats);
        public static void Equip(OopartInventorySave inventory,int slot,string id){if(slot<0 || slot>=5 || id!=null && !inventory.progress.Any(p=>p.oopartId==id))throw new ArgumentException("Unowned oopart or invalid slot.");if(id!=null && inventory.slots.Where((s,i)=>i!=slot).Any(s=>s.equippedOopartId==id))throw new ArgumentException("同じ編成で重複装備できません。");inventory.slots[slot].equippedOopartId=id;}
        public static void SavePreset(OopartInventorySave inventory,string id,string name){var p=new OopartFormationPreset{id=id,name=name,slots=inventory.slots.Select(s=>s.Copy()).ToArray()};inventory.presets=inventory.presets.Where(x=>x.id!=id).Concat(new[]{p}).ToArray();inventory.Validate();}
        public static void LoadPreset(OopartInventorySave inventory,string id){inventory.slots=inventory.presets.Single(p=>p.id==id).slots.Select(s=>s.Copy()).ToArray();inventory.Validate();}
        public static int Maximum(OopartProgress p,RandomStatDef d)=>d.stat=="hp"?Math.Max(d.maximum,p.legacyHpMaximum):d.maximum;
        public static bool DirectEligible(OopartProgress p,RandomStatDef r)=>5L*p.accumulatedRandomStats.Get(r.stat)>=4L*Maximum(p,r) && p.accumulatedRandomStats.Get(r.stat)<Maximum(p,r);
        public static void Direct(FormalCampaignSave save,CollectionCatalog c,string id,string stat){var d=c.Oopart(id);var p=save.collection.ooparts.progress.Single(i=>i.oopartId==id);var r=d.randomStats.Single(i=>i.stat==stat);if(!DirectEligible(p,r) || save.growth.nectar<d.directNectarCost)throw new ArgumentException("80%条件またはネクタル不足です。");var m=save.collection.materials.SingleOrDefault(i=>i.id==d.directMaterialId);if(m==null || m.amount<d.directMaterialCost)throw new ArgumentException("高Lv巨神獣素材が不足しています。");save.growth.nectar-=d.directNectarCost;m.amount-=d.directMaterialCost;int increase=stat=="speed"?1:Math.Max(1,(Maximum(p,r)+19)/20);p.accumulatedRandomStats.Set(stat,Math.Min(Maximum(p,r),checked(p.accumulatedRandomStats.Get(stat)+increase)));}
        public static int LevelUp(FormalCampaignSave save,CollectionCatalog catalog,string id,int count)
        {
            var p=save.collection.ooparts.progress.Single(i=>i.oopartId==id);var d=catalog.relics.Single(i=>i.id==id);int changed=0;
            while(p.level<120 && changed<count){int cost=FormalRelicRules.UpgradeCost(new CollectionRelic{level=p.level},RelicOperation.LevelUp,catalog.contentVersion);var mats=d.materialIds.Select(m=>save.collection.materials.SingleOrDefault(i=>i.id==m)).ToArray();if(mats.Any(m=>m==null || m.amount<cost))break;foreach(var m in mats)m.amount-=cost;p.level++;changed++;}
            if(changed==0)throw new ArgumentException("Lv上限または素材不足です。");return changed;
        }
    }
    public sealed class OopartRequest
    {
        public string Id{get;}public long Revision{get;}public string Kind{get;}public string Target{get;}public int Slot{get;}public int Count{get;}public string Stat{get;}
        public string Signature=>"oopart.v1|"+Kind+"|"+Target+"|"+Slot+"|"+Count+"|"+Stat;
        public OopartRequest(string id,long revision,string kind,string target,int slot=0,int count=1,string stat=null){if(!CollectionCatalog.ValidId(id) || revision<0 || !new[]{"equip","level","direct","preset-save","preset-load","clear-slot"}.Contains(kind) || target!=null && !CollectionCatalog.ValidId(target) || slot<0 || slot>=5 || count<1 || count>120 || stat!=null && !StatValues.Names.Contains(stat))throw new ArgumentException("Invalid oopart operation.");Id=id;Revision=revision;Kind=kind;Target=target;Slot=slot;Count=count;Stat=stat;}
        public void Apply(FormalCampaignSave s,CollectionCatalog c,string[] defaults)
        {
            var formation=s.home?.formationIds.Length==5?s.home.formationIds:defaults;OopartSaveAdapter.Migrate(s.collection,c,formation);var o=s.collection.ooparts;
            if(Kind=="equip")OopartService.Equip(o,Slot,Target);else if(Kind=="level")OopartService.LevelUp(s,c,Target,Count);else if(Kind=="direct")OopartService.Direct(s,c,Target,Stat);else if(Kind=="preset-save")OopartService.SavePreset(o,Target,"編成 "+Target);else if(Kind=="preset-load"){OopartService.LoadPreset(o,Target);s.home.formationIds=o.slots.Select(i=>i.heroineFormId).ToArray();}else{o.slots[Slot].heroineFormId=null;s.home.formationIds=o.slots.Select(i=>i.heroineFormId).ToArray();}
            s.home.allowEmptyFormationSlots=o.slots.Any(i=>i.heroineFormId==null);
            if(o.slots.Any(i=>i.heroineFormId!=null && !s.growth.heroines.Any(h=>h.heroineId==i.heroineFormId)))throw new ArgumentException("Preset contains unowned heroines.");
            // Keep the old inventory readable by older tools; legacy owner equipment is an archive.
            foreach(var p in o.progress){var r=s.collection.relics.Single(i=>i.id==p.oopartId);r.level=p.level;r.attackRoll=Math.Min(100,p.accumulatedRandomStats.attack);r.hpRoll=Math.Min(1000,p.accumulatedRandomStats.hp);}
            o.ValidateContent(c);
        }
    }
    public sealed partial class FormalCampaignJournal
    {
        private OopartRequest pendingOopart;
        public bool OopartBattleActive{get;private set;}
        public void LockOopartsForBattle()=>OopartBattleActive=true;
        public void UnlockOopartsAfterBattle()=>OopartBattleActive=false;
        public GrowthCommitResult CommitOopart(OopartRequest request,CollectionCatalog c,string[] defaults,Func<FormalCampaignSave,bool> save,HomeExperienceCatalog homeCatalog=null)
        {
            if(request==null || c==null || save==null)throw new ArgumentNullException();if(writing || OopartBattleActive)throw new InvalidOperationException("戦闘中はオーパーツを変更できません。");var receipt=current.growth.receipts.SingleOrDefault(r=>r.transactionId==request.Id);if(receipt!=null){if(receipt.signature!=request.Signature)throw new ArgumentException("Oopart operation ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){if(pendingOopart==null || pendingOopart.Id!=request.Id || pendingOopart.Signature!=request.Signature || pendingOopart.Revision!=request.Revision)throw new InvalidOperationException("Retry the same oopart operation.");}
            else{if(request.Revision!=current.revision || (current.home?.receipts.Any(r=>r.transactionId==request.Id)??false) || (current.affection?.receipts.Any(r=>r.id==request.Id)??false) || (current.gardenLife?.receipts.Any(r=>r.transactionId==request.Id)??false))throw new ArgumentException("Stale or reused oopart operation.");c.Validate();var next=Snapshot;request.Apply(next,c,defaults);next.growth.receipts=next.growth.receipts.Concat(new[]{new GrowthReceipt{transactionId=request.Id,signature=request.Signature}}).ToArray();next.growth.revision=checked(next.growth.revision+1);next.revision=checked(next.revision+1);next.collection.ValidateContent(c);if(homeCatalog!=null){next.home.ValidateContent(homeCatalog,next);foreach(var preset in next.collection.ooparts.presets){var people=preset.slots.Where(i=>i.heroineFormId!=null).Select(i=>homeCatalog.PersonId(i.heroineFormId)).ToArray();if(people.Distinct().Count()!=people.Length)throw new ArgumentException("同じ人物の衣装違いは同時に編成できません。");}}next.Validate();pending=Copy(next);pendingOopart=request;}
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;pending=null;pendingOopart=null;return GrowthCommitResult.Committed;
        }
    }
}
