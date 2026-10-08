using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed class GardenLifeRequest
    {
        public string Id{get;} public string Kind{get;} public string Signature{get;} public long Revision{get;}
        public GardenLifeRequest(string id,string kind,long revision,string payload)
        {if(!HomeExperienceCatalog.Id(id) || revision<0 || !new[]{"settings","assignment","visit","discovery","craft","edit","layout"}.Contains(kind) || string.IsNullOrEmpty(payload))throw new ArgumentException("Invalid life transaction.");Id=id;Kind=kind;Revision=revision;Signature="garden-life.v1|"+kind+"|"+payload;}
    }
    public sealed class GardenDiscoveryTransaction
    {
        public string Id{get;} public string DiscoveryId{get;} public GardenLifeContext Context{get;}
        public GardenDiscoveryTransaction(string id,string discoveryId,GardenLifeContext context)
        {
            if(!HomeExperienceCatalog.Id(id) || context==null || !GardenLifeCatalog.Discoveries.Any(d=>d.id==discoveryId && GardenLifeDiscoveries.Matches(d.condition,context)))throw new ArgumentException("Invalid discovery transaction.");
            Id=id;DiscoveryId=discoveryId;Context=context;
        }
        public void Apply(FormalCampaignSave save)=>GardenLifeDiscoveries.Grant(save,DiscoveryId,Id,Context);
    }
    public sealed partial class FormalCampaignJournal
    {
        private GardenLifeRequest pendingGardenLife;
        public GrowthCommitResult CommitGardenLife(GardenLifeRequest request,HomeExperienceCatalog catalog,Action<FormalCampaignSave> build,Func<FormalCampaignSave,bool> save)
        {
            if(request==null || catalog==null || save==null)throw new ArgumentNullException();if(writing)throw new InvalidOperationException("Reentrant life save.");
            var receipt=current.gardenLife?.receipts.SingleOrDefault(r=>r.transactionId==request.Id);
            if(receipt!=null){if(receipt.signature!=request.Signature)throw new ArgumentException("Life transaction ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){if(pendingGardenLife==null || pendingGardenLife.Id!=request.Id || pendingGardenLife.Revision!=request.Revision || pendingGardenLife.Signature!=request.Signature)throw new InvalidOperationException("Retry the same life operation first.");}
            else{
                if(request.Revision!=current.revision || build==null)throw new ArgumentException("Stale life operation.");
                var before=Snapshot;var next=Snapshot;GardenLifeCatalog.Migrate(next);writing=true;try{build(next);}finally{writing=false;}next=Copy(next);
                // Life actions may change only life flags, furniture editing/crafting, and material costs.
                // Formation, affection, narrative, world evolution and combat growth cannot be touched.
                var probe=Copy(next);probe.gardenLife=before.gardenLife;
                if(request.Kind=="edit" || request.Kind=="craft"){
                    if(next.home==null || before.home==null)throw new ArgumentException("Missing furniture inventory.");
                    probe.home.furnitureInstances=before.home.furnitureInstances;probe.home.furniturePlacements=before.home.furniturePlacements;probe.home.occupants=before.home.occupants;
                    if(before.home.furnitureInstances.Any(i=>!next.home.furnitureInstances.Any(n=>n.instanceId==i.instanceId && n.defId==i.defId)))throw new ArgumentException("Life edits cannot remove owned furniture.");
                    if(request.Kind=="edit" && !before.home.furnitureInstances.Select(i=>i.instanceId).SequenceEqual(next.home.furnitureInstances.Select(i=>i.instanceId)))throw new ArgumentException("Editing cannot create furniture.");
                    if(request.Kind=="craft"){
                        if(next.collection==null || before.collection==null || next.collection.materials.Any(m=>m.amount>HomeRules.Balance(before,m.id)))throw new ArgumentException("Crafting cannot grant material rewards.");
                        probe.collection.materials=before.collection.materials;
                    }
                }
                if(encode(probe)!=encode(before))throw new ArgumentException("Life operation changed protected campaign state.");
                if(request.Kind=="discovery" && next.affection!=null){foreach(var record in next.gardenLife.records.Where(r=>!(before.gardenLife?.records??Array.Empty<GardenLifeRecord>()).Any(old=>old.discoveryId==r.discoveryId)))AffectionRewardService.Discovery(next,catalog,record.context.participantIds!=null && record.context.participantIds.Length>0?record.context.participantIds:new[]{record.context.heroineId,record.context.partnerId});AffectionEventResolver.Refresh(next,catalog);}
                GardenLifeCatalog.RefreshUnlocks(next);next.gardenLife.ValidateContent(next,catalog);
                if(before.gardenLife!=null && (before.gardenLife.records.Any(r=>!next.gardenLife.records.Any(n=>n.discoveryId==r.discoveryId && n.transactionId==r.transactionId)) || before.gardenLife.unlockedRecipeIds.Except(next.gardenLife.unlockedRecipeIds).Any() || before.gardenLife.unlockedFrameIds.Except(next.gardenLife.unlockedFrameIds).Any() || before.gardenLife.receipts.Any(r=>!next.gardenLife.receipts.Any(n=>n.transactionId==r.transactionId && n.signature==r.signature))))throw new ArgumentException("Life progress cannot be removed.");
                if(request.Kind=="edit" || request.Kind=="craft")next.home.ValidateContent(catalog,next);
                next.gardenLife.receipts=next.gardenLife.receipts.Concat(new[]{new GardenLifeReceipt{transactionId=request.Id,signature=request.Signature}}).ToArray();next.revision=checked(next.revision+1);next.Validate();pending=next;pendingGardenLife=request;
            }
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;pending=null;pendingGardenLife=null;return GrowthCommitResult.Committed;
        }
    }
    public sealed class GardenLifeDiscoveries
    {
        private readonly Random random;private readonly System.Collections.Generic.Dictionary<string,int> failures=new System.Collections.Generic.Dictionary<string,int>();
        public GardenLifeDiscoveries(int seed){random=new Random(seed);}
        public GardenLifeDiscoveryDef[] Evaluate(string trigger,GardenLifeContext context,GardenLifeProgress progress)
        {
            var results=new System.Collections.Generic.List<GardenLifeDiscoveryDef>();
            foreach(var d in GardenLifeCatalog.Discoveries.Where(d=>d.trigger==trigger && !progress.records.Any(r=>r.discoveryId==d.id) && Matches(d.condition,context))){
                int missed=failures.TryGetValue(d.id,out int f)?f:0;if(d.immediate || missed>=5 || random.Next(100)<20){results.Add(d);failures[d.id]=0;}else failures[d.id]=missed+1;
            }return results.ToArray();
        }
        public static bool Matches(string condition,GardenLifeContext c)
        {
            if(GardenLifeCatalog.Interactions.Contains(condition) && !GardenLifeCatalog.ActivityIds.Contains(condition))return c.interactionTag==condition && (!condition.EndsWith("_together",StringComparison.Ordinal) || c.partnerId!=null);
            switch(condition){
                case "social-first":return c.partnerId!=null;
                case "social-long":return c.partnerId!=null && c.socialSeconds>=20;
                case "social-furniture":return c.partnerId!=null && c.furnitureId!=null && !c.furnitureId.StartsWith("scenery.");
                case "rain-covered":return c.weather=="rain" && c.covered && c.partnerId!=null;
                case "scenery-quiet":return c.sharedScenery && c.quiet && c.partnerId!=null;
                case "morning":case "evening":return c.timePhase==condition;
                case "rain-scenery":return c.weather=="rain" && c.sharedScenery;
                case "snow":return c.weather=="snow";
                case "meteor-night":return c.timePhase=="night" && (c.phenomenonId??"").Contains("meteor");
                case "aurora":return (c.phenomenonId??"").Contains("aurora");
                case "level5":return c.domains.Any(d=>d.currentLevel==5);
                case "level6":return c.domains.Any(d=>d.currentLevel==6);
                case "level7":return c.domains.Any(d=>d.currentLevel==7);
                case "cross-world":return c.domains.Any(d=>!string.IsNullOrEmpty(d.activeExtremeId) && c.domains.Any(other=>other.domainId!=d.domainId && other.currentLevel>=5));
                case "asteria":return c.asteriaCleared && c.gardenId=="garden.integrated-world";
                default:return c.activityId==condition;
            }
        }
        public static void Grant(FormalCampaignSave s,string discoveryId,string transactionId,GardenLifeContext context)
        {
            var d=GardenLifeCatalog.Discoveries.Single(x=>x.id==discoveryId);if(s.gardenLife.records.Any(r=>r.discoveryId==discoveryId))return;
            if(!Matches(d.condition,context))throw new ArgumentException("Discovery condition no longer matches context.");
            s.gardenLife.records=s.gardenLife.records.Concat(new[]{new GardenLifeRecord{discoveryId=discoveryId,transactionId=transactionId,context=context,grantedRecipeIds=d.recipeId==null?Array.Empty<string>():new[]{d.recipeId}}}).ToArray();if(d.recipeId!=null)s.gardenLife.unlockedRecipeIds=s.gardenLife.unlockedRecipeIds.Union(new[]{d.recipeId}).ToArray();
        }
        public static string Hint(GardenLifeDiscoveryDef d)=>d.trigger=="furniture"?"家具や景観で「"+GardenLifeCatalog.InteractionName(d.condition)+"」時間に。":d.trigger=="social"?"二人が同じ場所で過ごす時間に。":d.trigger=="activity"?"庭で「"+GardenLifeCatalog.Activities.Single(a=>a.id==d.condition).name+"」を開いてみよう。":d.trigger=="terraform"?"新天地の発展が、暮らしの景色を変える。":"時間・天候・世界現象によって見える景色に。";
    }
}
