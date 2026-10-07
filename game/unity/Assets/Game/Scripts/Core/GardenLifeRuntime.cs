using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    public sealed class GardenLifeAgent
    {
        public string heroineId,personId,kind="Idle",tag="stand",slotId,partnerId,socialState,activityId;
        public float x,y,facing=1,remaining,elapsed;
        public bool fixedPose,allowSocial=true;
        public readonly Queue<string> recentActions=new Queue<string>(),recentPartners=new Queue<string>();
        internal Queue<HomePoint> path=new Queue<HomePoint>();
        internal readonly Dictionary<string,float> cooldowns=new Dictionary<string,float>();
    }
    public sealed class GardenLifeRuntime
    {
        private readonly Random random;
        private readonly HomeExperienceCatalog home;
        private readonly GardenLifeSetting setting;
        private readonly List<GardenInteractionSlot> slots=new List<GardenInteractionSlot>();
        private readonly Dictionary<string,List<string>> reservations=new Dictionary<string,List<string>>();
        private readonly Dictionary<string,GardenBehaviorProfile> profiles;
        private readonly bool[,] blocked=new bool[32,24];
        private readonly TerraformSave terraform;
        private readonly string[] availableWeathers;
        private readonly Dictionary<string,string> originalTags=new Dictionary<string,string>();
        private float clock;
        private readonly Dictionary<string,bool> originalFixed=new Dictionary<string,bool>();
        public string GardenId{get;}
        public string Weather{get;private set;}
        public string TimePhase=>setting.timePhase;
        public string SceneTemplate{get;}
        public string ActivityId{get;private set;}
        public bool Paused{get;set;}
        public IReadOnlyList<GardenLifeAgent> Agents{get;}
        public IReadOnlyList<GardenInteractionSlot> Slots=>slots;
        public string[] WarningOwnerIds{get;}
        public event Action<string,GardenLifeContext> Trigger;
        public GardenLifeRuntime(FormalCampaignSave save,HomeExperienceCatalog catalog,string gardenId,int seed=1,Dictionary<string,GardenBehaviorProfile> optionalProfiles=null)
        {
            GardenLifeCatalog.Migrate(save);home=catalog;GardenId=gardenId;setting=save.gardenLife.Setting(gardenId);terraform=TerraformRules.Copy(save.world.terraform);profiles=optionalProfiles??new Dictionary<string,GardenBehaviorProfile>();
            if(string.IsNullOrEmpty(terraform.activeWorldPhenomenonId))terraform.activeWorldPhenomenonId=null;foreach(var d in terraform.domains){if(string.IsNullOrEmpty(d.activeDeepRecordId))d.activeDeepRecordId=null;if(string.IsNullOrEmpty(d.activeExtremeId))d.activeExtremeId=null;}
            availableWeathers=GardenLifeCatalog.AvailableWeathers(save,gardenId);
            random=new Random(StableSeed(seed,gardenId,setting.visitCounter));Weather=setting.autoWeather?ChooseWeather():setting.weather;
            BuildSlots(save.home);BuildObstacles(save.home);
            var owned=save.growth.heroines.Select(h=>h.heroineId).ToArray();var assignments=save.gardenLife.assignments;
            var chosen=owned.Where(id=>!assignments.Any(a=>home.PersonId(a.heroineId)==home.PersonId(id) && (a.mode=="Hidden" || a.mode=="Fixed" && a.gardenId!=gardenId || a.heroineId!=id)))
                .OrderBy(id=>{var a=assignments.SingleOrDefault(x=>x.heroineId==id);return a?.mode=="Fixed"?0:a?.mode=="Preferred" && a.gardenId==gardenId?1:2;})
                .ThenBy(id=>{int i=Array.IndexOf(owned,id);return (i+setting.visitCounter)%Math.Max(1,owned.Length);}).GroupBy(home.PersonId).Select(g=>g.First()).Take(8).ToArray();
            Agents=chosen.Select((id,i)=>{
                var a=assignments.SingleOrDefault(x=>x.heroineId==id);var old=save.home?.occupants.SingleOrDefault(x=>x.heroineId==id && x.gardenId==gardenId);
                return new GardenLifeAgent{heroineId=id,personId=home.PersonId(id),x=a?.mode=="Fixed"?a.x:old?.x??.15f+(i%4)*.21f,y=a?.mode=="Fixed"?a.y:old?.y??.7f+(i/4)*.12f,fixedPose=a?.fixedPose??false,allowSocial=a?.allowSocial??true,tag=a?.interactionTag??"stand"};
            }).ToArray();
            SeparateStoppedAgents();
            WarningOwnerIds=(save.home?.furniturePlacements??Array.Empty<HomePlacement>()).Where(p=>p.gardenId==gardenId && (GardenLifeEditor.Reserved(p.x,p.y) || slots.Any(s=>s.ownerId==p.instanceId) && !slots.Where(s=>s.ownerId==p.instanceId).Any(s=>Agents.Any(a=>Path(a.x,a.y,s.x,s.y).Length>0)))).Select(p=>p.instanceId).ToArray();
            SceneTemplate=new[]{"calm","lively","gathering","solitary"}[random.Next(4)];
            for(int i=0;i<Agents.Count;i++){
                var a=Agents[i];originalTags[a.heroineId]=a.tag;originalFixed[a.heroineId]=a.fixedPose;if(a.fixedPose || !setting.autoLife || i<Math.Max(1,(int)Math.Ceiling(Agents.Count*.25)))Idle(a);
                else Decide(a,SceneTemplate=="lively"?"Move":SceneTemplate=="gathering"?"Furniture":SceneTemplate=="solitary"?"Scenery":i%2==0?"Furniture":"Scenery");
            }
        }
        public static int StableSeed(int seed,string garden,int visit)
        {unchecked{uint hash=2166136261;foreach(char c in garden)hash=(hash^c)*16777619;return (int)(hash^(uint)seed^(uint)visit*397);}}
        private string ChooseWeather()
        {
            int water=Level("water"),sky=Level("sky");int[] weights={45,25,15+water*2,Level("earth")+water+sky};for(int i=0;i<weights.Length;i++)if(!availableWeathers.Contains(GardenLifeCatalog.Weathers[i]))weights[i]=0;int total=weights.Sum();int n=random.Next(total);for(int i=0;i<weights.Length;i++){n-=weights[i];if(n<0)return GardenLifeCatalog.Weathers[i];}return "clear";
        }
        private int Level(string domain)=>terraform?.domains.SingleOrDefault(d=>d.domainId==domain)?.currentLevel??0;
        private void BuildSlots(FormalHomeProgress progress)
        {
            foreach(var p in progress?.furniturePlacements.Where(p=>p.gardenId==GardenId)??Enumerable.Empty<HomePlacement>()){
                var def=home.furniture.Single(f=>f.id==p.defId);string[] tags=FurnitureTags(def);if(tags.Length==0)continue;
                int count=tags.Any(t=>t.EndsWith("_together",StringComparison.Ordinal))?2:Math.Max(1,def.slots.Length);
                for(int i=0;i<count;i++){
                    var source=def.slots.ElementAtOrDefault(i)??def.slots.FirstOrDefault();float dx=(source?.offset.x??.9f)-def.drawAnchor.x;float dy=(source?.offset.y??1)-def.drawAnchor.y;
                    dx*=def.size01.x;dy*=def.size01.y;
                    if(p.orientationId.Contains("flip"))dx=-dx;if(p.orientationId.Contains("rotate")){float t=dx;dx=-dy*730f/1600f;dy=t*1600f/730f;}
                    slots.Add(new GardenInteractionSlot{id=p.instanceId+".life."+i,ownerId=p.instanceId,tags=tags,x=p.x+dx+(i==1?.075f:0),y=p.y+dy,facing=(i==1?-1:1)*(p.orientationId.Contains("flip")?-1:1),covered=def.lifeCovered});
                }
            }
            string type=GardenLifeCatalog.Types[Array.IndexOf(GardenLifeCatalog.GardenIds,GardenId)];
            for(int i=0;i<4;i++)for(int j=0;j<2;j++){
                var tags=new List<string>{"stand","look","look_together","talk","rest"};
                if(i==0)tags.AddRange(new[]{"sit","sit_together","picnic","eat","eat_together","meal","camp"});
                if(i==1)tags.AddRange(new[]{"read","drink","drink_together","tea_party","care_plant","flower_viewing"});
                if(i==2 && (TimePhase=="night" || type=="stargazing"))tags.AddRange(new[]{"stargaze","stargaze_together","stargazing","fireworks"});
                if(type=="hotspring" && i==3)tags.AddRange(new[]{"bathe","bathe_together","hot_spring"});
                if(type=="snow" && i==3 || Weather=="snow" && i==3)tags.Add("snow_play");
                if((type=="lakeside" || type=="flower") && i==3)tags.Add("water_play");
                slots.Add(new GardenInteractionSlot{id="scenery."+i+"."+j,ownerId="scenery."+i,tags=tags.ToArray(),x=.16f+i*.21f+j*.075f,y=.58f+(i%2)*.15f,covered=i==1,facing=j==0?1:-1});
            }
            slots.RemoveAll(s=>!GardenLifeProgress.Coordinate(s.x) || !GardenLifeProgress.Coordinate(s.y));
        }
        public static string[] FurnitureTags(string id)
        {
            if(id=="furniture.fixture.0" || id.EndsWith(".bench"))return new[]{"sit","sit_together","rest","talk"};
            if(id=="furniture.fixture.1")return new[]{"read","care_plant","eat","drink","drink_together","tea_party","meal"};
            if(id.EndsWith(".planter") || id.EndsWith(".tools"))return new[]{"care_plant","look","flower_viewing"};
            if(id.EndsWith(".telescope") || id.EndsWith(".star-chart"))return new[]{"stargaze","stargaze_together","look","stargazing"};
            if(id.EndsWith(".books"))return new[]{"read","rest"};
            if(id.EndsWith(".tea"))return new[]{"drink","drink_together","tea_party"};
            if(id.EndsWith(".brazier"))return new[]{"rest","look","camp"};
            return new[]{"look","look_together","stand"};
        }
        public static string[] FurnitureTags(HomeFurnitureLayout def)=>def.lifeCategory=="Decoration"?Array.Empty<string>():def.lifeInteractionTags!=null && def.lifeInteractionTags.Length>0?def.lifeInteractionTags:FurnitureTags(def.id);
        private void BuildObstacles(FormalHomeProgress progress)
        {
            foreach(var p in progress?.furniturePlacements.Where(p=>p.gardenId==GardenId)??Enumerable.Empty<HomePlacement>()){
                var f=home.furniture.Single(x=>x.id==p.defId);var footprint=HomeGeometry.Footprint(p,f);float left=(float)footprint[0],top=(float)footprint[1];
                for(int x=0;x<32;x++)for(int y=0;y<24;y++)if((x+.5f)/32>=left && (x+.5f)/32<=left+(float)footprint[2] && (y+.5f)/24>=top && (y+.5f)/24<=top+(float)footprint[3])blocked[x,y]=true;
            }
        }
        public HomePoint[] Path(float ax,float ay,float bx,float by)
        {
            int start=Cell(ax,ay),end=Cell(bx,by);var queue=new Queue<int>();var previous=Enumerable.Repeat(-1,768).ToArray();queue.Enqueue(start);previous[start]=start;
            while(queue.Count>0){int c=queue.Dequeue();if(c==end)break;int x=c%32,y=c/32;foreach(int n in new[]{x>0?c-1:-1,x<31?c+1:-1,y>9?c-32:-1,y<22?c+32:-1})if(n>=0 && previous[n]<0 && (!blocked[n%32,n/32] || n==end)){previous[n]=c;queue.Enqueue(n);}}
            if(previous[end]<0)return Array.Empty<HomePoint>();var result=new List<HomePoint>();for(int c=end;c!=start;c=previous[c])result.Add(new HomePoint{x=(c%32+.5f)/32,y=(c/32+.5f)/24});result.Reverse();result.Add(new HomePoint{x=bx,y=by});return result.ToArray();
        }
        private void SeparateStoppedAgents()
        {
            var stopped=new List<GardenLifeAgent>();
            foreach(var a in Agents){if(a.path.Count>0)continue;if(!a.fixedPose && a.slotId==null){
                for(int radius=0;radius<12;radius++){bool found=false;for(int sign=0;sign<2;sign++){
                    float x=Math.Max(.065f,Math.Min(.935f,a.x+radius*.035f*(sign==0?1:-1)));
                    int cell=Cell(x,a.y);if(!blocked[cell%32,cell/32] && stopped.All(b=>Math.Abs(b.x-x)>.06f || Math.Abs(b.y-a.y)>.025f)){a.x=x;found=true;break;}
                }if(found)break;}
            }stopped.Add(a);}
        }
        private static int Cell(float x,float y)=>Math.Max(0,Math.Min(31,(int)(x*32)))+Math.Max(0,Math.Min(23,(int)(y*24)))*32;
        private bool Free(GardenInteractionSlot s)=>!reservations.TryGetValue(s.id,out var ids) || ids.Count<s.capacity;
        private void Release(GardenLifeAgent a)
        {if(a.slotId!=null){var slot=slots.Single(s=>s.id==a.slotId);a.cooldowns[slot.ownerId]=clock+(slot.ownerId.StartsWith("scenery.")?45:30);}foreach(var r in reservations.Values)r.Remove(a.heroineId);a.slotId=null;a.path.Clear();}
        private void Idle(GardenLifeAgent a){Release(a);a.kind="Idle";a.tag=a.fixedPose && originalTags.TryGetValue(a.heroineId,out string fixedTag)?fixedTag:"stand";a.remaining=Range(4,10);a.elapsed=0;}
        private float Range(float min,float max)=>min+(float)random.NextDouble()*(max-min);
        public void Tick(float seconds)
        {
            if(Paused || seconds<=0)return;seconds=Math.Min(seconds,.25f);clock+=seconds;
            foreach(var a in Agents){
                if(a.fixedPose || !setting.autoLife)continue;a.remaining-=seconds;a.elapsed+=seconds;
                if(a.path.Count>0){var p=a.path.Peek();float dx=p.x-a.x,dy=p.y-a.y,len=(float)Math.Sqrt(dx*dx+dy*dy),step=seconds*.05f;a.facing=dx<0?-1:1;if(len<=step){a.x=p.x;a.y=p.y;a.path.Dequeue();if(a.path.Count==0)Arrive(a);}else{a.x+=dx/len*step;a.y+=dy/len*step;}continue;}
                if(a.kind=="Social"){if(a.socialState=="Face" && a.elapsed>=2)a.socialState="Interaction";if(a.socialState=="Interaction" && a.remaining<4)a.socialState="Reaction";}
                if(a.remaining<=0){if(a.kind=="Social")EndSocial(a);else Decide(a);}
            }
            SeparateStoppedAgents();
        }
        private void Decide(GardenLifeAgent a,string forced=null)
        {
            Release(a);a.partnerId=null;a.socialState=null;
            string[] kinds={"Idle","Move","Scenery","Furniture","Social"};float[] weights={30,20,15,20,15};
            if(a.activityId!=null){weights=new[]{10f,10f,30f,30f,20f};}
            for(int i=0;i<kinds.Length;i++){
                var recent=a.recentActions.Reverse().ToArray();if(recent.ElementAtOrDefault(0)==kinds[i])weights[i]*=.2f;else if(recent.Skip(1).Contains(kinds[i]))weights[i]*=.5f;
                if(profiles.TryGetValue(a.heroineId,out var p)){int k=Array.IndexOf(p.actionKinds,kinds[i]);if(k>=0 && k<p.weightModifiers.Length)weights[i]*=Math.Max(0,p.weightModifiers[k]);}
            }
            string kind=forced??Weighted(kinds,weights);a.recentActions.Enqueue(kind);while(a.recentActions.Count>3)a.recentActions.Dequeue();
            if(kind=="Social" && TrySocial(a))return;
            if(kind=="Scenery" || kind=="Furniture"){
                var options=slots.Where(s=>Free(s) && (s.ownerId.StartsWith("scenery.")== (kind=="Scenery")) && (!a.cooldowns.TryGetValue(s.ownerId,out float until) || until<=clock));
                if(a.activityId!=null){var activity=GardenLifeCatalog.Activities.Single(d=>d.id==a.activityId);options=options.Where(s=>s.tags.Intersect(activity.tags).Any());}
                var preference=profiles.TryGetValue(a.heroineId,out var profile)?profile.preferredInteractionTags:Array.Empty<string>();
                var list=options.OrderBy(s=>(Weather=="rain" || Weather=="snow") && s.covered?0:1).ThenBy(s=>s.tags.Intersect(preference).Any()?0:1).ThenBy(s=>random.Next()).ToArray();
                foreach(var s in list){var path=Path(a.x,a.y,s.x,s.y);if(path.Length==0)continue;Reserve(a,s);a.kind="Approach";a.tag=s.tags[random.Next(s.tags.Length)];if(a.activityId!=null){var tags=s.tags.Intersect(GardenLifeCatalog.Activities.Single(d=>d.id==a.activityId).tags).ToArray();a.tag=tags[random.Next(tags.Length)];}a.path=new Queue<HomePoint>(path);a.remaining=120;a.elapsed=0;return;}
            }
            if(kind=="Move"){
                for(int i=0;i<5;i++){float x=Range(.08f,.92f),y=Range(.48f,.87f);var path=Path(a.x,a.y,x,y);if(path.Length==0)continue;a.path=new Queue<HomePoint>(path);a.kind="Move";a.tag="stand";a.remaining=120;Trigger?.Invoke("movement",Context(a));return;}
            }
            Idle(a);
        }
        private string Weighted(string[] values,float[] weights){float n=Range(0,weights.Sum());for(int i=0;i<values.Length;i++){n-=weights[i];if(n<=0)return values[i];}return values[0];}
        private void Reserve(GardenLifeAgent a,GardenInteractionSlot s){if(!reservations.ContainsKey(s.id))reservations[s.id]=new List<string>();reservations[s.id].Add(a.heroineId);a.slotId=s.id;}
        private void Arrive(GardenLifeAgent a)
        {
            if(a.kind=="Move"){Idle(a);return;}
            if(a.partnerId!=null){a.kind="Social";a.socialState="Face";a.remaining=Range(15,40);a.elapsed=0;var other=Agents.Single(x=>x.heroineId==a.partnerId);other.remaining=a.remaining;other.elapsed=0;other.socialState="Face";a.facing=a.x<=other.x?1:-1;other.facing=-a.facing;Trigger?.Invoke("social",Context(other));if(other.slotId!=null && !slots.Single(s=>s.id==other.slotId).ownerId.StartsWith("scenery."))Trigger?.Invoke("furniture",Context(other));return;}
            var slot=slots.Single(s=>s.id==a.slotId);a.kind=slot.ownerId.StartsWith("scenery.")?"Scenery":"Furniture";a.facing=slot.facing;a.remaining=a.kind=="Scenery"?Range(8,20):Range(10,30);a.elapsed=0;a.cooldowns[slot.ownerId]=clock+(a.kind=="Scenery"?45:30);Trigger?.Invoke(a.kind=="Furniture"?"furniture":"environment",Context(a));
            if(a.allowSocial && a.tag.EndsWith("_together",StringComparison.Ordinal))TrySocial(a);
        }
        private bool TrySocial(GardenLifeAgent a)
        {
            if(!a.allowSocial || a.fixedPose || Agents.Count(v=>v.partnerId!=null)>=4)return false;
            var b=Agents.Where(b=>b!=a && b.allowSocial && b.activityId==a.activityId && b.partnerId==null && b.path.Count==0 && (!a.cooldowns.TryGetValue(b.heroineId,out float until) || until<=clock)).OrderBy(b=>(b.x-a.x)*(b.x-a.x)+(b.y-a.y)*(b.y-a.y)).FirstOrDefault();
            if(b==null)return false;if(b.fixedPose){var moving=a;a=b;b=moving;}var own=a.slotId==null?null:slots.Single(s=>s.id==a.slotId);var sibling=own==null?null:slots.FirstOrDefault(s=>s.ownerId==own.ownerId && s.id!=own.id && Free(s));if(own!=null && sibling==null)return false;
            var path=Path(b.x,b.y,sibling?.x??Math.Min(.94f,a.x+.075f),sibling?.y??a.y);if(path.Length==0)return false;
            a.partnerId=b.heroineId;b.partnerId=a.heroineId;a.kind="Social";a.socialState="Approach";a.remaining=120;a.elapsed=0;Release(b);if(sibling!=null)Reserve(b,sibling);b.kind="Approach";b.socialState="Approach";b.path=new Queue<HomePoint>(path);b.remaining=120;
            string shared=a.tag+"_together";b.tag=a.tag.EndsWith("_together",StringComparison.Ordinal)?a.tag:GardenLifeCatalog.Interactions.Contains(shared)?shared:"talk";a.tag=b.tag;
            a.recentPartners.Enqueue(b.heroineId);b.recentPartners.Enqueue(a.heroineId);while(a.recentPartners.Count>3)a.recentPartners.Dequeue();while(b.recentPartners.Count>3)b.recentPartners.Dequeue();return true;
        }
        private void EndSocial(GardenLifeAgent a)
        {
            Trigger?.Invoke("social",Context(a));var partner=a.partnerId==null?null:Agents.SingleOrDefault(b=>b.heroineId==a.partnerId);if(partner!=null){partner.cooldowns[a.heroineId]=clock+60;partner.partnerId=null;partner.socialState="End";Idle(partner);}if(a.partnerId!=null)a.cooldowns[a.partnerId]=clock+60;a.partnerId=null;a.socialState="End";Idle(a);
        }
        public GardenLifeContext Context(GardenLifeAgent a=null)
        {
            var s=a?.slotId==null?null:slots.Single(x=>x.id==a.slotId);
            return new GardenLifeContext{gardenId=GardenId,timePhase=TimePhase,weather=Weather,phenomenonId=GardenLifeCatalog.PhenomenonUnavailable(terraform?.activeWorldPhenomenonId,TimePhase,GardenId)==null?terraform?.activeWorldPhenomenonId:null,interactionTag=a?.tag,heroineId=a?.heroineId,partnerId=a?.partnerId,furnitureId=s?.ownerId,covered=s?.covered??false,sharedScenery=s?.ownerId.StartsWith("scenery.")??false,quiet=a?.tag=="look_together",socialSeconds=(int)(a?.elapsed??0),activityId=a==null?ActivityId:a.activityId,asteriaCleared=terraform?.worldIntegrated??false,domains=terraform?.domains.Select(d=>new TerraformDomainState{domainId=d.domainId,currentLevel=d.currentLevel,maxReachedLevel=d.maxReachedLevel,activeDeepRecordId=d.activeDeepRecordId,activeExtremeId=d.activeExtremeId}).ToArray()??Array.Empty<TerraformDomainState>()};
        }
        public string ActivityUnavailable(string id,string[] selectedIds=null)
        {
            var def=GardenLifeCatalog.Activities.Single(a=>a.id==id);if(!slots.Any(s=>s.tags.Intersect(def.tags).Any()))return "利用できる家具や景観ポイントがありません。";
            if(selectedIds!=null && (selectedIds.Length<1 || selectedIds.Length>8 || selectedIds.Distinct().Count()!=selectedIds.Length || selectedIds.Any(h=>!Agents.Any(a=>a.heroineId==h))))return "この庭の1～8人を選んでください。";return null;
        }
        public void StartActivity(string id,string[] selectedIds=null)
        {
            var unavailable=ActivityUnavailable(id,selectedIds);if(unavailable!=null)throw new ArgumentException(unavailable);EndActivity();ActivityId=id;
            foreach(var a in Agents.Where(a=>selectedIds==null || selectedIds.Contains(a.heroineId))){a.fixedPose=false;a.activityId=id;Decide(a);}
            Trigger?.Invoke("activity",Context());
        }
        public void EndActivity(){ActivityId=null;foreach(var a in Agents){a.activityId=null;if(a.partnerId!=null)EndSocial(a);else Idle(a);if(originalFixed.TryGetValue(a.heroineId,out bool fixedPose))a.fixedPose=fixedPose;}}
        public void Stop(){EndActivity();Paused=true;reservations.Clear();}
        public string Observe(string heroineId)
        {var a=Agents.Single(x=>x.heroineId==heroineId);return a.kind=="Social"?"二人で同じ時間を過ごしています。":a.kind=="Furniture"?"家具を使って過ごしています。":a.kind=="Scenery"?"庭の景色を眺めています。":a.path.Count>0?"庭を歩いています。":"静かにひと休みしています。";}
        public void Greet(string heroineId){var a=Agents.Single(x=>x.heroineId==heroineId);a.facing=1;a.remaining=Math.Max(a.remaining,4);}
    }
}
