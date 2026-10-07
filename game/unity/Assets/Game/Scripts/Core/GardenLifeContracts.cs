using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class GardenLifeSetting
    { public string gardenId,timePhase="day",weather="clear",frameId="book"; public bool autoLife=true,autoWeather=true; public int visitCounter; }
    [Serializable] public sealed class GardenLifeAssignment
    { public string heroineId,gardenId,mode="Auto",interactionTag; public bool fixedPose,allowSocial=true; public float x=.5f,y=.7f; }
    [Serializable] public sealed class GardenLifeContext
    { public string gardenId,timePhase,weather,phenomenonId,interactionTag,activityId,heroineId,partnerId,furnitureId; public bool covered,sharedScenery,quiet,asteriaCleared; public int socialSeconds; public TerraformDomainState[] domains=Array.Empty<TerraformDomainState>(); }
    [Serializable] public sealed class GardenLifeRecord
    { public string discoveryId,transactionId; public GardenLifeContext context; public string[] grantedRecipeIds=Array.Empty<string>(); }
    [Serializable] public sealed class GardenLifeReceipt
    { public string transactionId,signature; }
    [Serializable] public sealed class GardenLayoutItem
    { public string furnitureDefId,orientationId="orientation.default"; public float x,y; }
    [Serializable] public sealed class GardenLayoutPreset
    { public string gardenId; public int index; public GardenLayoutItem[] items=Array.Empty<GardenLayoutItem>(); }
    [Serializable] public sealed class GardenLifeProgress
    {
        public int version=1;
        public string favoriteGardenId,lastGardenId;
        public GardenLifeSetting[] settings=Array.Empty<GardenLifeSetting>();
        public GardenLifeAssignment[] assignments=Array.Empty<GardenLifeAssignment>();
        public GardenLifeRecord[] records=Array.Empty<GardenLifeRecord>();
        public string[] unlockedRecipeIds=Array.Empty<string>(),unlockedFrameIds=new[]{"book"},unlockedActivityIds=Array.Empty<string>();
        public GardenLayoutPreset[] layoutPresets=Array.Empty<GardenLayoutPreset>();
        public GardenLifeReceipt[] receipts=Array.Empty<GardenLifeReceipt>();
        public GardenLifeSetting Setting(string id)=>settings.Single(s=>s.gardenId==id);
        public void Validate()
        {
            if(version!=1)throw new ArgumentException("Unsupported garden life save.");
            Unique(settings,s=>s.gardenId);Unique(assignments,a=>a.heroineId);Unique(records,r=>r.discoveryId);Unique(receipts,r=>r.transactionId);
            foreach(var s in settings)if(!GardenLifeCatalog.GardenIds.Contains(s.gardenId) || !GardenLifeCatalog.Times.Contains(s.timePhase) || !GardenLifeCatalog.Weathers.Contains(s.weather) || !GardenLifeCatalog.Frames.Contains(s.frameId) || s.visitCounter<0)throw new ArgumentException("Invalid life settings.");
            foreach(var a in assignments)if(!GardenLifeCatalog.Modes.Contains(a.mode) || a.gardenId!=null && !GardenLifeCatalog.GardenIds.Contains(a.gardenId) || !Coordinate(a.x) || !Coordinate(a.y) || a.interactionTag!=null && !GardenLifeCatalog.Interactions.Contains(a.interactionTag))throw new ArgumentException("Invalid resident assignment.");
            Set(unlockedRecipeIds);Set(unlockedFrameIds);Set(unlockedActivityIds);
            if(unlockedRecipeIds.Any(id=>!GardenLifeCatalog.RecipeIds.Contains(id)) || unlockedFrameIds.Any(id=>!GardenLifeCatalog.Frames.Contains(id)) || unlockedActivityIds.Any(id=>!GardenLifeCatalog.ActivityIds.Contains(id)))throw new ArgumentException("Unknown life unlock.");
            foreach(var r in records)if(!GardenLifeCatalog.Discoveries.Any(d=>d.id==r.discoveryId) || !HomeExperienceCatalog.Id(r.transactionId) || r.context==null || !GardenLifeCatalog.GardenIds.Contains(r.context.gardenId) || !GardenLifeCatalog.Times.Contains(r.context.timePhase) || !GardenLifeCatalog.Weathers.Contains(r.context.weather) || r.context.socialSeconds<0 || r.context.domains==null || r.context.domains.Select(d=>d?.domainId).Distinct().Count()!=r.context.domains.Length || r.context.domains.Any(d=>d==null || !TerraformRules.DomainIds.Contains(d.domainId) || d.currentLevel<0 || d.currentLevel>7))throw new ArgumentException("Invalid discovery context.");
            if(layoutPresets==null || layoutPresets.Any(p=>p==null || !GardenLifeCatalog.GardenIds.Contains(p.gardenId) || p.index<0 || p.index>=5 || p.items==null || p.items.Any(i=>i==null || !HomeExperienceCatalog.Id(i.furnitureDefId) || !Coordinate(i.x) || !Coordinate(i.y) || !GardenLifeCatalog.Orientations.Contains(i.orientationId))) || layoutPresets.GroupBy(p=>p.gardenId+"/"+p.index).Any(g=>g.Count()>1))throw new ArgumentException("Invalid layout preset.");
            if(receipts.Any(r=>string.IsNullOrWhiteSpace(r.signature)))throw new ArgumentException("Invalid life receipt.");
        }
        public void ValidateContent(FormalCampaignSave save,HomeExperienceCatalog home)
        {
            Validate();var owned=save.growth.heroines.Select(h=>h.heroineId).ToArray();
            if(assignments.Any(a=>!owned.Contains(a.heroineId) || !home.heroineIds.Contains(a.heroineId) || a.gardenId!=null && !save.world.unlockedGardenIds.Contains(a.gardenId)) || assignments.Select(a=>home.PersonId(a.heroineId)).Distinct().Count()!=assignments.Length)throw new ArgumentException("Unowned or duplicate resident person.");
            if(layoutPresets.Any(p=>p.items.Any(i=>!home.furniture.Any(f=>f.id==i.furnitureDefId))))throw new ArgumentException("Unknown preset furniture.");
            if(lastGardenId!=null && !save.world.unlockedGardenIds.Contains(lastGardenId))throw new ArgumentException("Locked last garden.");
            if(favoriteGardenId!=null && !save.world.unlockedGardenIds.Contains(favoriteGardenId))throw new ArgumentException("Locked favorite garden.");
            if(settings.Any(s=>!unlockedFrameIds.Contains(s.frameId) || !s.autoWeather && !GardenLifeCatalog.AvailableWeathers(save,s.gardenId).Contains(s.weather)))throw new ArgumentException("Locked frame or fixed weather.");
            foreach(var r in records){var d=GardenLifeCatalog.Discoveries.Single(x=>x.id==r.discoveryId);if(!GardenLifeDiscoveries.Matches(d.condition,r.context) || r.grantedRecipeIds==null || !r.grantedRecipeIds.SequenceEqual(d.recipeId==null?Array.Empty<string>():new[]{d.recipeId}))throw new ArgumentException("Invalid discovery condition or recipe grant.");if(d.recipeId!=null && !unlockedRecipeIds.Contains(d.recipeId))throw new ArgumentException("Discovery recipe missing.");}
        }
        public static bool Coordinate(float v)=>!float.IsNaN(v) && !float.IsInfinity(v) && v>=0 && v<=1;
        private static void Unique<T>(T[] values,Func<T,string> key) where T:class
        { if(values==null || values.Any(v=>v==null || !HomeExperienceCatalog.Id(key(v))) || values.Select(key).Distinct().Count()!=values.Length)throw new ArgumentException("Duplicate or missing life entity."); }
        private static void Set(string[] values){if(values==null || values.Any(string.IsNullOrWhiteSpace) || values.Distinct().Count()!=values.Length)throw new ArgumentException("Invalid life ID set.");}
    }
    public sealed class GardenLifeDiscoveryDef
    { public string id,name,trigger,condition,recipeId; public bool immediate; }
    public sealed class GardenLifeActivityDef
    { public string id,name; public string[] tags,domains; public int[] levels; }
    public sealed class GardenInteractionSlot
    { public string id,ownerId; public string[] tags; public float x,y,facing; public int capacity=1; public bool covered,playerSlot; }
    public sealed class GardenSceneryPoint
    { public string id; public GardenInteractionSlot[] slots; }
    public sealed class GardenBehaviorProfile
    { public string[] preferredInteractionTags=Array.Empty<string>(),preferredEnvironmentTags=Array.Empty<string>(),specialBehaviorIds=Array.Empty<string>(); public string[] actionKinds=Array.Empty<string>();public float[] weightModifiers=Array.Empty<float>(); }
    public static class GardenLifeCatalog
    {
        public static readonly string[] GardenIds={"garden.grassland-forest","garden.crystal-highland","garden.flower-water","garden.bamboo-waterfall","garden.sakura-stargazing","garden.oasis","garden.hot-spring","garden.snowfield","garden.integrated-world"};
        public static readonly string[] Types={"forest","highland","flower","lakeside","stargazing","city","hotspring","snow","central"};
        public static readonly string[][] Domains={new[]{"life","night"},new[]{"sky","light"},new[]{"life","light"},new[]{"water","sky"},new[]{"night","sky"},new[]{"civilization","earth","water"},new[]{"earth","water"},new[]{"water","sky","earth"},TerraformRules.DomainIds};
        public static readonly string[] Times={"morning","day","evening","night"},Weathers={"clear","cloudy","rain","snow"},Modes={"Fixed","Preferred","Auto","Hidden"},Orientations={"orientation.default","orientation.flip","orientation.rotate","orientation.rotate-flip"};
        public static readonly string[] Frames={"book","flower","forest","water","star","snow","gold","steam","cyber","genesis"};
        public static readonly string[] Interactions={"stand","sit","rest","sleep","read","eat","drink","look","stargaze","care_plant","bathe","talk","sit_together","eat_together","drink_together","look_together","stargaze_together","bathe_together","picnic","tea_party","flower_viewing","stargazing","hot_spring","camp","fireworks","meal","snow_play","water_play"};
        public static readonly string[] RecipeIds={"books","tools","bench","tea","rain-lantern","star-chart","snow-lantern","meteor-lamp","flower-lantern","asteria"};
        public static readonly string[] RecipeNames={"小さな本積み","花道具セット","記念ベンチ","ティーセット","雨音ランタン","星図","雪灯籠","流星ランプ","花見提灯","アステリア記念飾り"};
        public static readonly string[] RecipeTags={"read","care_plant","sit_together","drink_together","look","stargaze_together","look","stargaze","look","look"};
        public static readonly string[] ActivityIds={"picnic","tea_party","flower_viewing","stargazing","hot_spring","camp","fireworks","meal","snow_play","water_play"};
        public static readonly GardenLifeActivityDef[] Activities=MakeActivities();
        public static readonly GardenLifeDiscoveryDef[] Discoveries=MakeDiscoveries();
        private static GardenLifeActivityDef[] MakeActivities()
        {
            string[] names={"ピクニック","お茶会","花見","星見","温泉","キャンプ","花火","食事会","雪遊び","水遊び"};
            string[][] domains={new[]{"life"},new[]{"civilization"},new[]{"life"},new[]{"night"},new[]{"earth","water"},new[]{"life","civilization"},new[]{"night","civilization"},new[]{"civilization"},new[]{"water","sky"},new[]{"water"}};
            int[][] levels={new[]{2},new[]{2},new[]{3},new[]{2},new[]{3,2},new[]{2,1},new[]{3,3},new[]{2},new[]{4,3},new[]{3}};
            string[][] tags={new[]{"eat","picnic"},new[]{"drink","tea_party"},new[]{"look","flower_viewing"},new[]{"stargaze","stargazing"},new[]{"bathe","hot_spring"},new[]{"rest","camp"},new[]{"look","fireworks"},new[]{"eat","meal"},new[]{"snow_play"},new[]{"water_play"}};
            return ActivityIds.Select((id,i)=>new GardenLifeActivityDef{id=id,name=names[i],domains=domains[i],levels=levels[i],tags=tags[i]}).ToArray();
        }
        private static GardenLifeDiscoveryDef[] MakeDiscoveries()
        {
            string[] conditions={"sit","read","care_plant","eat","sit_together","drink_together","look_together","bathe","social-first","social-long","social-furniture","stargaze_together","rain-covered","scenery-quiet","morning","evening","rain-scenery","snow","meteor-night","aurora","picnic","tea_party","flower_viewing","stargazing","meal","level5","level6","level7","cross-world","asteria"};
            string[] names={"ひと休み","読書の時間","花の世話","ひとりの食卓","二人掛け","お茶の時間","同じ景色","湯けむり","偶然の挨拶","話が弾んで","隣り合わせ","一緒に星を","雨宿り","静かな時間","朝の庭","夕暮れ","雨音を聞く","雪の庭","流星の夜","オーロラの下で","初めてのピクニック","庭のお茶会","花の宴","星を見る夜","みんなの食卓","調和した世界","過ぎた再生","極相の生活","異なる世界の交差","第八世界の日常"};
            int[] rewardIndices={1,2,4,5,12,11,17,18,22,29};
            return conditions.Select((c,i)=>new GardenLifeDiscoveryDef{id="garden.discovery."+c,name=names[i],condition=c,trigger=i<8?"furniture":i<14?"social":i<20?"environment":i<25?"activity":"terraform",immediate=i>=14 || i==8,recipeId=Array.IndexOf(rewardIndices,i)<0?null:RecipeIds[Array.IndexOf(rewardIndices,i)]}).ToArray();
        }
        public static void Migrate(FormalCampaignSave save)
        {
            if(save.gardenLife==null){
                save.gardenLife=new GardenLifeProgress{settings=GardenIds.Select(id=>new GardenLifeSetting{gardenId=id}).ToArray()};
                save.gardenLife.assignments=(save.home?.occupants??Array.Empty<HomeOccupant>()).Select(o=>new GardenLifeAssignment{heroineId=o.heroineId,gardenId=o.gardenId,mode="Fixed",x=o.x,y=o.y}).ToArray();
            }
            RefreshUnlocks(save);
        }
        public static void NormalizeOptionalFields(GardenLifeProgress life)
        {
            if(life==null)return;
            if(life.favoriteGardenId=="")life.favoriteGardenId=null;if(life.lastGardenId=="")life.lastGardenId=null;
            foreach(var a in life.assignments??Array.Empty<GardenLifeAssignment>()){if(a==null)continue;if(a.gardenId=="")a.gardenId=null;if(a.interactionTag=="")a.interactionTag=null;}
            foreach(var r in life.records??Array.Empty<GardenLifeRecord>()){
                var c=r?.context;if(c==null)continue;if(c.phenomenonId=="")c.phenomenonId=null;if(c.interactionTag=="")c.interactionTag=null;if(c.activityId=="")c.activityId=null;if(c.heroineId=="")c.heroineId=null;if(c.partnerId=="")c.partnerId=null;if(c.furnitureId=="")c.furnitureId=null;
                foreach(var d in c.domains??Array.Empty<TerraformDomainState>()){if(d==null)continue;if(d.activeDeepRecordId=="")d.activeDeepRecordId=null;if(d.activeExtremeId=="")d.activeExtremeId=null;}
            }
        }
        public static void RefreshUnlocks(FormalCampaignSave save)
        {
            var life=save.gardenLife;if(life==null)return;var t=save.world.terraform;
            if(t!=null){life.unlockedActivityIds=life.unlockedActivityIds.Union(Activities.Where(a=>a.domains.Select((d,i)=>t.domains.Single(s=>s.domainId==d).maxReachedLevel>=a.levels[i]).All(x=>x)).Select(a=>a.id)).ToArray();
                string[] frameDomains={null,"life","life","water","night","water","light","earth","civilization",null};
                life.unlockedFrameIds=life.unlockedFrameIds.Union(Frames.Where((f,i)=>i==0 || i==9 && t.worldIntegrated || i>0 && i<9 && t.domains.Single(d=>d.domainId==frameDomains[i]).maxReachedLevel>=2)).ToArray();}
            if(life.settings.Length!=GardenIds.Length)life.settings=life.settings.Concat(GardenIds.Where(id=>!life.settings.Any(s=>s.gardenId==id)).Select(id=>new GardenLifeSetting{gardenId=id})).ToArray();
        }
        public static string[] AvailableWeathers(FormalCampaignSave save,string garden)
        {
            var t=save.world.terraform;int water=t?.domains.SingleOrDefault(d=>d.domainId=="water")?.maxReachedLevel??0,sky=t?.domains.SingleOrDefault(d=>d.domainId=="sky")?.maxReachedLevel??0;
            return Weathers.Where(w=>w=="clear" || w=="cloudy" || w=="rain" && water>=1 || w=="snow" && (water>=4 && sky>=3 || garden=="garden.snowfield")).ToArray();
        }
        public static string PhenomenonUnavailable(string phenomenon,string time,string garden)
        {
            if(phenomenon==null)return null;
            if((phenomenon.Contains("meteor") || phenomenon.Contains("aurora")) && time!="night")return "この世界現象は夜の空で見られます。";
            if(phenomenon.Contains("fireworks") && garden=="garden.hot-spring")return "この庭では花火の観覧範囲がありません。";
            return null;
        }
        public static string FrameName(string id){string[] names={"古書","花","森","水","星","雪","金","湯けむり","電脳","創世"};int i=Array.IndexOf(Frames,id);return i<0?id:names[i];}
        public static string[] MainDomains(string garden){int i=Array.IndexOf(GardenIds,garden);return Domains[i];}
        public static string InteractionName(string tag)
        {
            string[] names={"立つ","腰掛ける","休む","眠る","読書","食事","飲み物","景色を眺める","星を見る","植物の世話","入浴","挨拶","二人で腰掛ける","二人で食事","二人でお茶","同じ景色","一緒に星を見る","二人で入浴"};
            int i=Array.IndexOf(Interactions,tag);return i<0?tag:i<names.Length?names[i]:Activities.Single(a=>a.id==tag).name;
        }
    }
}
