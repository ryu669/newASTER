using System;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private GardenLifeRuntime gardenLifeRuntime;
        private bool gardenLifeCapture;
        private GardenLifeEditor gardenLifeEditor;
        private GardenLifeDiscoveries gardenDiscoveries;
        private readonly Dictionary<string,GardenLifeDiscoveries> gardenDiscoveryVisits=new Dictionary<string,GardenLifeDiscoveries>();
        private string lastLifeGarden;
        private GardenLifeRequest gardenLifeRequest;
        private Action<FormalCampaignSave> gardenLifeBuild;
        private Action gardenLifeAfter;
        private string gardenLifeError,gardenLifeMessage,gardenLifePanel,gardenLifeQuery="",gardenLifeResidentFilter="全員";
        private string gardenLifeCategory="全て";
        private bool gardenViewing,gardenViewingFrame=true,gardenDiscardConfirm,gardenLifeRecords;
        private Vector2 gardenLifeScroll;
        private int gardenCraftQuantity=1,gardenPresetIndex;
        private float gardenGridStep;
        private bool gardenLifeEditPlace,gardenLifeResidentPlace;
        private readonly HashSet<string> gardenActivityParticipants=new HashSet<string>();
        private GardenLayoutPreset gardenPresetShortage;
        private readonly Queue<Tuple<string,GardenLifeContext>> gardenLifeTriggers=new Queue<Tuple<string,GardenLifeContext>>();
        private readonly Queue<Tuple<GardenLifeDiscoveryDef,GardenLifeContext>> gardenLifeFinds=new Queue<Tuple<GardenLifeDiscoveryDef,GardenLifeContext>>();
        private HomeOccupant LifeOccupant(GardenLifeAgent a,string garden,FormalHomeProgress state)
        {
            var slot=a.slotId==null?null:gardenLifeRuntime.Slots.Single(s=>s.id==a.slotId);
            var placement=slot==null?null:state.furniturePlacements.SingleOrDefault(p=>p.instanceId==slot.ownerId);
            var use=placement==null?null:GardenUse(placement.defId,a.heroineId);
            string action=a.tag=="sit" || a.tag=="sit_together" || a.tag=="rest" || a.tag=="eat" || a.tag=="drink" || a.tag=="read"?"action.sit":a.tag=="care_plant"?"action.work":a.kind=="Scenery" || a.kind=="Social"?"action.look":null;
            bool compose=a.kind=="Furniture" && use!=null && !placement.orientationId.Contains("rotate");
            return new HomeOccupant{heroineId=a.heroineId,gardenId=garden,slotId="slot.idle",x=a.x,y=a.y,actionId=compose?"action."+use.action:action,furnitureInstanceId=compose?placement.instanceId:null};
        }
        private int lifeSnapshotFrame=-1;
        private long lifeSnapshotRevision=-1;
        private FormalCampaignSave lifeSnapshotCached;
        private FormalCampaignSave LifeSnapshot()
        {
            if(lifeSnapshotCached==null || lifeSnapshotFrame!=Time.frameCount || lifeSnapshotRevision!=formalCampaign.Revision){lifeSnapshotCached=formalCampaign.Snapshot;GardenLifeCatalog.Migrate(lifeSnapshotCached);lifeSnapshotFrame=Time.frameCount;lifeSnapshotRevision=formalCampaign.Revision;}
            return lifeSnapshotCached;
        }
        private void CommitLife(string kind,string payload,Action<FormalCampaignSave> build,Action after=null,string transactionId=null)
        {
            if(gardenLifeRequest!=null || formalCampaign.HasPending || formalProgression.HasPending)return;
            gardenLifeRequest=new GardenLifeRequest(transactionId??Guid.NewGuid().ToString("N"),kind,formalCampaign.Snapshot.revision,payload);gardenLifeBuild=build;gardenLifeAfter=after;RetryLife();
        }
        private void RetryLife()
        {
            try{
                var result=formalCampaign.CommitGardenLife(gardenLifeRequest,HomeData(),gardenLifeBuild,formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign);
                if(result==GrowthCommitResult.SaveFailed){gardenLifeError="保存できませんでした。同じ内容を保持しています。";return;}
                var after=gardenLifeAfter;gardenLifeRequest=null;gardenLifeBuild=null;gardenLifeAfter=null;gardenLifeError=null;after?.Invoke();
            }catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is System.IO.IOException){gardenLifeError=e.Message;if(!formalCampaign.HasPending){gardenLifeRequest=null;gardenLifeBuild=null;gardenLifeAfter=null;}}
        }
        private void StartLifeScene()
        {
            gardenLifeRuntime?.Stop();gardenLifeTriggers.Clear();gardenLifeFinds.Clear();var save=LifeSnapshot();string id=book.SubjectId;
            gardenLifeRuntime=new GardenLifeRuntime(save,HomeData(),id,113711);
            lastLifeGarden=id;string visit=id+"/"+save.gardenLife.Setting(id).visitCounter;
            if(!gardenDiscoveryVisits.TryGetValue(visit,out gardenDiscoveries)){gardenDiscoveries=new GardenLifeDiscoveries(GardenLifeRuntime.StableSeed(113711,id,save.gardenLife.Setting(id).visitCounter));gardenDiscoveryVisits[visit]=gardenDiscoveries;}
            gardenLifeRuntime.Trigger+=(trigger,context)=>{gardenLifeTriggers.Enqueue(Tuple.Create(trigger,context));PlayLifeSound(trigger,context);};
            gardenLifeTriggers.Enqueue(Tuple.Create("environment",gardenLifeRuntime.Context()));gardenLifeTriggers.Enqueue(Tuple.Create("terraform",gardenLifeRuntime.Context()));
        }
        private void UpdateGardenLife()
        {
            bool visible=formalCampaign!=null && book!=null && !title && encounter==null && adv==null && !modelViewer && !collectionOpen && !engagementOpen && book.Bookmark==BookBookmark.Gardens && !book.IsTransitioning;
            if(capturePath!=null && !gardenLifeCapture)return;
            if(!visible){if(gardenLifeRuntime!=null){gardenLifeRuntime.Stop();gardenLifeRuntime=null;StopLifeAudio();}gardenViewing=false;return;}
            if(!LifeSnapshot().world.unlockedGardenIds.Contains(book.SubjectId))return;
            if(affectionInteraction!=null || affectionRequest!=null || affectionPanel || affectionShop || gardenLifeEditor!=null || gardenLifeRequest!=null || formalCampaign.HasPending || formalProgression.HasPending)return;
            if(gardenLifeRuntime==null || gardenLifeRuntime.GardenId!=book.SubjectId){
                string id=book.SubjectId;CommitLife("visit",id,s=>{s.gardenLife.Setting(id).visitCounter=checked(s.gardenLife.Setting(id).visitCounter+1);s.gardenLife.lastGardenId=id;},StartLifeScene);return;
            }
            if(!gardenLifeRuntime.Paused && !help && !bookSystemOpen && Application.isFocused)gardenLifeRuntime.Tick(Time.unscaledDeltaTime);
            SyncLifeAudio();MeasureLifeAcceptance();
            if(gardenLifeFinds.Count==0 && gardenLifeTriggers.Count>0){var t=gardenLifeTriggers.Dequeue();foreach(var d in gardenDiscoveries.Evaluate(t.Item1,t.Item2,LifeSnapshot().gardenLife))gardenLifeFinds.Enqueue(Tuple.Create(d,t.Item2));}
            if(gardenLifeFinds.Count>0){var found=gardenLifeFinds.Dequeue();var transaction=new GardenDiscoveryTransaction(Guid.NewGuid().ToString("N"),found.Item1.id,found.Item2);CommitLife("discovery",found.Item1.id+"/"+JsonUtility.ToJson(found.Item2),transaction.Apply,()=>{gardenLifeMessage="生活発見："+found.Item1.name+(found.Item1.recipeId==null?"":" ／ 記念家具のレシピを記録しました。");},transaction.Id);}
        }
        private bool CloseGardenLifeLayer()
        {
            if(gardenLifeRecords){gardenLifeRecords=false;return true;}
            if(book==null || book.Bookmark!=BookBookmark.Gardens || title || encounter!=null)return false;
            if(gardenViewing){gardenViewing=false;return true;}
            if(gardenDiscardConfirm){gardenDiscardConfirm=false;return true;}
            if(gardenPresetShortage!=null){gardenPresetShortage=null;return true;}
            if(gardenLifeResidentPlace){gardenLifeResidentPlace=false;return true;}
            if(gardenLifeEditPlace){gardenLifeEditPlace=false;return true;}
            if(gardenLifeEditor!=null){gardenDiscardConfirm=true;return true;}
            if(gardenLifePanel!=null){gardenLifePanel=null;return true;}return false;
        }
        private void DrawLifeGarden()
        {
            bool backgroundEnabled=GUI.enabled;GUI.enabled=backgroundEnabled && !gardenDiscardConfirm && gardenPresetShortage==null && gardenLifeError==null && gardenLifeRequest==null;
            var save=LifeSnapshot();string garden=book.SubjectId;var layout=HomeData().gardens.Single(g=>g.id==garden);bool unlocked=save.world.unlockedGardenIds.Contains(garden);
            Panel(0,0,1600,900,dark);var view=new Rect(0,88,1600,730);var state=save.home;
            if(gardenLifeEditor!=null)state.furniturePlacements=gardenLifeEditor.Placements;
            if(gardenLifeRuntime!=null && gardenLifeRuntime.GardenId==garden)state.occupants=gardenLifeRuntime.Agents.Select(a=>LifeOccupant(a,garden,state)).ToArray();
            if(unlocked){DrawGardenScene(view,state,garden,layout,false);DrawLifeEnvironment(view,save.gardenLife.Setting(garden));}
            else Label(150,300,1000,90,"世界の発展で、この庭が開きます。",heading,Color.white);
            if(gardenViewing){
                if(gardenViewingFrame)DrawGardenViewFrame(view,save.gardenLife.Setting(garden).frameId);
                if(Btn(1350,835,220,45,"鑑賞を終える"))gardenViewing=false;
                if(Btn(28,835,220,45,gardenLifeRuntime?.Paused==true?"動きを再開":"動きを止める"))if(gardenLifeRuntime!=null)gardenLifeRuntime.Paused=!gardenLifeRuntime.Paused;
                if(Btn(265,835,220,45,gardenViewingFrame?"フレームを隠す":"フレームを飾る"))gardenViewingFrame=!gardenViewingFrame;GUI.enabled=backgroundEnabled;DrawLifeSaveError();return;
            }
            Panel(85,116,560,62,dark);Label(105,124,520,43,ProductionGardenCatalog.GardenName(garden)+" ／ "+LifeTimeName(save.gardenLife.Setting(garden).timePhase),text,Color.white);
            if(gardenLifeRuntime?.WarningOwnerIds.Length>0)Label(105,255,800,70,"既存配置を維持しています。通路・景観を塞ぐ家具、または到達できない家具があります。",new GUIStyle(small){fontSize=18},Color.white);
            if(gardenLifeRuntime!=null && gardenLifeEditor==null)DrawLifeSceneHitTargets(view);
            if(gardenLifeEditor!=null)DrawLifeEditor(view,state,garden);
            else if(gardenLifeResidentPlace){
                if(Btn(970,755,560,45,"人物の位置指定を取消"))gardenLifeResidentPlace=false;
                if(GUI.enabled && Event.current.type==EventType.MouseDown && view.Contains(Event.current.mousePosition) && Event.current.mousePosition.y<740){
                    var point=Event.current.mousePosition;var value=new GardenLifeAssignment{heroineId=selectedResident,gardenId=garden,mode="Fixed",x=Mathf.Clamp01((point.x-view.x)/view.width),y=Mathf.Clamp((point.y-view.y)/view.height,.45f,.9f),allowSocial=true};
                    CommitLife("assignment",JsonUtility.ToJson(value),s=>s.gardenLife.assignments=s.gardenLife.assignments.Where(a=>HomeData().PersonId(a.heroineId)!=HomeData().PersonId(value.heroineId)).Concat(new[]{value}).ToArray(),()=>{gardenLifeResidentPlace=false;StartLifeScene();});Event.current.Use();
                }
            }
            else if(unlocked){
                string[] tabs={"家具","人物","催事","環境","庭を切替","鑑賞"};
                for(int i=0;i<tabs.Length;i++)if(Btn(28+i*205,835,195,48,tabs[i],gardenLifeRequest==null && BookInputAllowed)){
                    if(i==5){gardenViewing=true;gardenLifePanel=null;}else{gardenLifePanel=gardenLifePanel==tabs[i]?null:tabs[i];gardenLifeScroll=Vector2.zero;gardenLifeQuery="";}
                }
            }
            if(Btn(1325,835,245,48,"システム",CanOpenBookSystem && gardenLifeEditor==null))bookSystemOpen=true;
            if(gardenLifePanel!=null)DrawLifeDrawer(save);
            if(gardenLifeMessage!=null){Panel(85,190,800,55,dark);Label(105,196,760,43,gardenLifeMessage,new GUIStyle(small){fontSize=18},Color.white);if(Btn(895,195,45,42,"×"))gardenLifeMessage=null;}
            GUI.enabled=backgroundEnabled;DrawLifeSaveError();
            if(gardenPresetShortage!=null){
                GrowthFill(0,0,1600,900,new Color(0,0,0,.6f));Panel(330,285,940,300,dark);var counts=gardenLifeEditor.Shortages(gardenPresetShortage,state);Label(370,315,850,100,"不足："+string.Join("・",counts.Select(x=>GardenFurnitureName(x.Key)+" "+x.Value))+"\n所持している家具だけで適用できます。",text,Color.white);
                if(Btn(370,470,390,55,"所持分だけ適用")){var preset=gardenPresetShortage;LifeEditorTry(()=>gardenLifeEditor.ApplyPreset(preset,state,true));gardenPresetShortage=null;}if(Btn(795,470,390,55,"中止"))gardenPresetShortage=null;
            }
            if(gardenDiscardConfirm){GrowthFill(0,0,1600,900,new Color(0,0,0,.6f));Panel(380,310,840,230,dark);Label(420,338,755,55,"未確定の模様替えを取り消しますか？",text,Color.white);if(Btn(420,440,350,55,"取消して元に戻す")){gardenLifeEditor.Cancel();gardenLifeEditor=null;gardenDiscardConfirm=false;StartLifeScene();}if(Btn(805,440,350,55,"編集を続ける"))gardenDiscardConfirm=false;}
            if(help){drawingModal=true;DrawHelp();}DrawBookTransition();
        }
        private static string LifeTimeName(string value)=>new[]{"朝","昼","夕方","夜"}[Array.IndexOf(GardenLifeCatalog.Times,value)];
        private static string LifeWeatherName(string value)=>new[]{"晴れ","曇り","雨","雪"}[Array.IndexOf(GardenLifeCatalog.Weathers,value)];
        private static string LifeModeName(string value){int i=Array.IndexOf(GardenLifeCatalog.Modes,value);return i<0?value:new[]{"固定","優先","自動","非表示"}[i];}
        private void DrawLifeSaveError()
        {
            if(gardenLifeError==null)return;Panel(200,605,1200,170,dark);Label(230,625,1110,65,gardenLifeError,small,Color.white);
            if(gardenLifeRequest!=null){if(Btn(250,710,1040,45,"同じ内容で保存を再試行"))RetryLife();}else if(Btn(250,710,1040,45,"閉じる"))gardenLifeError=null;
        }
        private void DrawLifeSceneHitTargets(Rect view)
        {
            if(!GUI.enabled || gardenLifePanel!=null || Event.current.type!=EventType.MouseDown || !view.Contains(Event.current.mousePosition))return;
            var point=Event.current.mousePosition;var heroes=gardenLifeRuntime.Agents.Where(a=>new Rect(view.x+a.x*view.width-70,view.y+a.y*view.height-155,140,165).Contains(point)).Select(a=>a.heroineId);
            var furniture=LifeSnapshot().home.furniturePlacements.Where(p=>p.gardenId==book.SubjectId && GardenFurnitureImageRect(view,p).Contains(point)).OrderByDescending(p=>p.y).Select(p=>p.instanceId);var hits=heroes.Concat(furniture).ToArray();
            if(hits.Length>0){bool hero=combatDefinitions.HeroineIds.Contains(hits[0]);if(hero)selectedResident=hits[0];else selectedFurniture=hits[0];gardenLifePanel=hits.Length>1?"重なり選択":hero?"人物詳細":"家具詳細";gardenLifeQuery=string.Join("|",hits);Event.current.Use();}
        }
        private void DrawLifeDrawer(FormalCampaignSave save)
        {
            Panel(970,190,600,615,dark);Label(998,206,520,42,gardenLifePanel,heading,Color.white);if(Btn(1508,204,42,42,"×",gardenLifeRequest==null)){gardenLifePanel=null;return;}
            bool enabled=GUI.enabled;GUI.enabled=enabled && gardenLifeRequest==null && !formalCampaign.HasPending;
            if(gardenLifePanel=="家具")DrawLifeFurniture(save);
            else if(gardenLifePanel=="人物")DrawLifeResidents(save);
            else if(gardenLifePanel=="人物詳細")DrawLifeResidentMenu(save);
            else if(gardenLifePanel=="家具詳細"){
                var item=save.home.furnitureInstances.Single(i=>i.instanceId==selectedFurniture);var def=HomeData().furniture.Single(f=>f.id==item.defId);Label(998,265,550,55,GardenFurnitureName(item.defId),text,Color.white);Label(998,328,550,80,string.Join("・",GardenLifeRuntime.FurnitureTags(def).Select(GardenLifeCatalog.InteractionName)),small,Color.white);
                if(Btn(998,430,550,48,"配置・移動を編集する")){gardenLifeRuntime.Stop();gardenLifeEditor=new GardenLifeEditor(save.home,book.SubjectId,HomeData());gardenLifePanel=null;gardenLifeEditPlace=true;}
                if(Btn(998,490,550,48,"収納を編集する")){gardenLifeRuntime.Stop();gardenLifeEditor=new GardenLifeEditor(save.home,book.SubjectId,HomeData());gardenLifeEditor.Store(selectedFurniture);gardenLifePanel=null;}
            }
            else if(gardenLifePanel=="交流物語"){
                var events=ProductionGardenLifeCatalog.GetAvailableGardenEvents(save,HomeData(),selectedResident);
                for(int i=0;i<events.Length;i++){string id=events[i];bool read=save.home.readEventIds.Contains(id);if(Btn(998,265+i*65,550,58,(read?"回想：":"読む：")+ProductionStoryTitle(id))){gardenLifeRuntime?.Stop();BeginAdv(id,read);}}
                if(events.Length==0)Label(998,265,550,70,"今読める物語はありません。",small,Color.white);
            }
            else if(gardenLifePanel=="催事")DrawLifeActivities(save);
            else if(gardenLifePanel=="催事参加者"){
                var people=gardenLifeRuntime?.Agents.ToArray()??Array.Empty<GardenLifeAgent>();
                for(int i=0;i<people.Length;i++){string id=people[i].heroineId;if(Btn(998,265+i*48,550,42,(gardenActivityParticipants.Contains(id)?"◆ ":"○ ")+combatDefinitions.Hero(id).name)){if(!gardenActivityParticipants.Remove(id))gardenActivityParticipants.Add(id);}}
                if(Btn(998,675,550,44,"自動で選ぶ"))gardenActivityParticipants.Clear();if(Btn(998,733,550,44,"催事へ戻る"))gardenLifePanel="催事";
            }
            else if(gardenLifePanel=="環境")DrawLifeSettings(save);
            else if(gardenLifePanel=="庭を切替"){
                var ids=save.world.unlockedGardenIds.Where(id=>HomeData().gardens.Any(g=>g.id==id)).ToArray();for(int i=0;i<ids.Length;i++){string id=ids[i];if(Btn(998,263+i*52,550,45,ProductionGardenCatalog.GardenName(id))){gardenLifeRuntime?.Stop();gardenLifeRuntime=null;gardenLifePanel=null;if(book.RequestSubject(BookBookmark.Gardens,id))bookTransitionElapsed=0;}}
            }else if(gardenLifePanel=="重なり選択"){var ids=gardenLifeQuery.Split('|');for(int i=0;i<ids.Length;i++){string id=ids[i];bool hero=combatDefinitions.HeroineIds.Contains(id);string label=hero?combatDefinitions.Hero(id).name:GardenFurnitureName(save.home.furnitureInstances.Single(f=>f.instanceId==id).defId);if(Btn(998,264+i*55,550,48,label)){if(hero){selectedResident=id;gardenLifePanel="人物詳細";}else{selectedFurniture=id;gardenLifePanel="家具詳細";}}}}
            GUI.enabled=enabled;
        }
        private void DrawLifeFurniture(FormalCampaignSave save)
        {
            gardenLifeQuery=GUI.TextField(new Rect(998,260,550,36),gardenLifeQuery);if(Btn(998,303,550,35,"分類："+gardenLifeCategory+" ／ 名前・用途で検索")){string[] names={"全て","装飾","生活","催事"};gardenLifeCategory=names[(Array.IndexOf(names,gardenLifeCategory)+1)%4];}
            var defs=HomeData().furniture.Where(f=>gardenLifeCategory=="全て" || f.lifeCategory==(gardenLifeCategory=="装飾"?"Decoration":gardenLifeCategory=="生活"?"Life":"Activity")).Where(f=>gardenLifeQuery.Length==0 || GardenFurnitureName(f.id).Contains(gardenLifeQuery) || string.Join(" ",GardenLifeRuntime.FurnitureTags(f).Select(GardenLifeCatalog.InteractionName)).Contains(gardenLifeQuery)).ToArray();
            gardenLifeScroll=GUI.BeginScrollView(new Rect(998,345,550,340),gardenLifeScroll,new Rect(0,0,525,Math.Max(340,defs.Length*90)));
            for(int i=0;i<defs.Length;i++){
                var f=defs[i];int count=save.home.furnitureInstances.Count(item=>item.defId==f.id),max=GardenLifeCrafting.Maximum(save,f);Label(8,i*90+2,510,32,GardenFurnitureName(f.id)+" ／ 所持 "+count,new GUIStyle(small){fontSize=17},Color.white);
                int quantity=gardenCraftQuantity==0?Math.Min(10000,max):gardenCraftQuantity;
                string costs=string.Join("・",f.costs.Select(c=>"素材 "+HomeRules.Balance(save,c.resourceId)+"/"+c.amount));
                if(Btn(8,i*90+40,510,40,GardenLifeCrafting.Unlocked(save,f.id)?quantity+"個作成 ／ "+costs:"生活発見・領域発展でレシピ解放",quantity>0 && quantity<=max)){string id=Guid.NewGuid().ToString("N");CommitLife("craft",f.id+"/"+quantity+"/"+id,s=>GardenLifeCrafting.Craft(s,HomeData(),f.id,quantity,id),()=>gardenLifeMessage=GardenFurnitureName(f.id)+"を"+quantity+"個作成しました。");}
            }GUI.EndScrollView();
            int[] nums={1,5,10,0};for(int i=0;i<4;i++)if(Btn(998+i*138,694,130,40,nums[i]==0?"MAX":nums[i]+"個"))gardenCraftQuantity=nums[i];
            if(Btn(998,746,550,45,"模様替えを始める")){gardenLifeRuntime?.Stop();gardenLifeEditor=new GardenLifeEditor(save.home,book.SubjectId,HomeData());gardenLifePanel=null;selectedFurniture=save.home.furnitureInstances.FirstOrDefault()?.instanceId;}
        }
        private void DrawLifeResidents(FormalCampaignSave save)
        {
            gardenLifeQuery=GUI.TextField(new Rect(998,260,550,36),gardenLifeQuery);
            string[] filters={"全員","現在の庭","Fixed","Preferred","Auto","Hidden"};if(Btn(998,309,550,40,"絞り込み："+LifeModeName(gardenLifeResidentFilter)))gardenLifeResidentFilter=filters[(Array.IndexOf(filters,gardenLifeResidentFilter)+1)%filters.Length];
            var ids=save.growth.heroines.Select(h=>h.heroineId).Where(id=>combatDefinitions.Hero(id).name.Contains(gardenLifeQuery)).Where(id=>gardenLifeResidentFilter=="全員" || gardenLifeResidentFilter=="現在の庭" && (gardenLifeRuntime?.Agents.Any(a=>a.heroineId==id)??false) || (save.gardenLife.assignments.SingleOrDefault(a=>a.heroineId==id)?.mode??"Auto")==gardenLifeResidentFilter).ToArray();
            gardenLifeScroll=GUI.BeginScrollView(new Rect(998,365,550,390),gardenLifeScroll,new Rect(0,0,525,Math.Max(390,ids.Length*55)));
            for(int i=0;i<ids.Length;i++){string id=ids[i];string mode=save.gardenLife.assignments.SingleOrDefault(a=>a.heroineId==id)?.mode??"Auto";if(Btn(0,i*55,520,48,combatDefinitions.Hero(id).name+" ／ "+LifeModeName(mode))){selectedResident=id;gardenLifePanel="人物詳細";}}
            GUI.EndScrollView();
        }
        private void DrawLifeResidentMenu(FormalCampaignSave save)
        {
            string id=selectedResident;if(id==null)return;var agent=gardenLifeRuntime?.Agents.SingleOrDefault(a=>a.heroineId==id);Label(998,265,550,38,combatDefinitions.Hero(id).name,text,Color.white);
            if(Btn(998,315,268,44,"話す",agent!=null)){BeginPlayerAffection(id,false);}
            if(Btn(1280,315,268,44,"観察",agent!=null))gardenLifeMessage=gardenLifeRuntime.Observe(id);
            var assignment=save.gardenLife.assignments.SingleOrDefault(a=>a.heroineId==id)??new GardenLifeAssignment{heroineId=id};
            string[] names={"この庭に固定","この庭を優先","自動で訪れる","生活から隠す"};
            for(int i=0;i<4;i++){int index=i;if(Btn(998,373+i*48,550,42,names[i]+(assignment.mode==GardenLifeCatalog.Modes[i]?" ◆":""))){var value=new GardenLifeAssignment{heroineId=id,gardenId=i<2?book.SubjectId:null,mode=GardenLifeCatalog.Modes[i],x=agent?.x??.3f,y=agent?.y??.7f,allowSocial=true};CommitLife("assignment",JsonUtility.ToJson(value),s=>s.gardenLife.assignments=s.gardenLife.assignments.Where(a=>HomeData().PersonId(a.heroineId)!=HomeData().PersonId(id)).Concat(new[]{value}).ToArray(),StartLifeScene);}}
            if(assignment.mode=="Fixed"){
                if(Btn(998,568,268,42,assignment.fixedPose?"ポーズ固定：ON":"ポーズ固定：OFF")){assignment.fixedPose=!assignment.fixedPose;CommitLife("assignment",JsonUtility.ToJson(assignment),s=>s.gardenLife.assignments=s.gardenLife.assignments.Where(a=>a.heroineId!=id).Concat(new[]{assignment}).ToArray(),StartLifeScene);}
                if(Btn(1280,568,268,42,assignment.allowSocial?"交流を受ける":"交流を受けない")){assignment.allowSocial=!assignment.allowSocial;CommitLife("assignment",JsonUtility.ToJson(assignment),s=>s.gardenLife.assignments=s.gardenLife.assignments.Where(a=>a.heroineId!=id).Concat(new[]{assignment}).ToArray(),StartLifeScene);}
            }
            if(Btn(998,621,268,44,"位置を指定")){gardenLifeResidentPlace=true;gardenLifePanel=null;}
            if(Btn(1280,621,268,44,"誓女の詳細")){gardenLifePanel=null;gardenLifeRuntime?.Stop();if(book.RequestSubject(BookBookmark.Heroines,id))bookTransitionElapsed=0;heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;}
            var events=ProductionGardenLifeCatalog.GetAvailableGardenEvents(save,HomeData(),id);if(Btn(998,680,268,42,"一緒に過ごす",agent!=null))BeginPlayerAffection(id,true);if(Btn(1280,680,268,42,"好感度・物語"))OpenAffection(id);
            if(assignment.fixedPose && Btn(998,733,550,42,"固定行動："+GardenLifeCatalog.InteractionName(assignment.interactionTag??"stand"))){string[] tags={"stand","sit","read","look","rest"};assignment.interactionTag=tags[(Array.IndexOf(tags,assignment.interactionTag??"stand")+1)%tags.Length];CommitLife("assignment",JsonUtility.ToJson(assignment),s=>s.gardenLife.assignments=s.gardenLife.assignments.Where(a=>a.heroineId!=id).Concat(new[]{assignment}).ToArray(),StartLifeScene);}
        }
        private void DrawLifeActivities(FormalCampaignSave save)
        {
            if(gardenLifeRuntime?.ActivityId!=null && Btn(998,260,268,43,"催事を終える"))FinishPlayerActivity();
            if(Btn(1280,260,268,43,"参加者："+(gardenActivityParticipants.Count==0?"自動":gardenActivityParticipants.Count+"人")))gardenLifePanel="催事参加者";
            for(int i=0;i<GardenLifeCatalog.Activities.Length;i++){var a=GardenLifeCatalog.Activities[i];bool unlocked=save.gardenLife.unlockedActivityIds.Contains(a.id);var selected=gardenActivityParticipants.Count==0?null:gardenActivityParticipants.ToArray();string reason=gardenLifeRuntime?.ActivityUnavailable(a.id,selected);if(Btn(998,318+i*44,550,39,a.name+(unlocked?reason==null?"を開く":" ／ "+reason:" ／ 領域発展で解放"),unlocked && reason==null && gardenLifeRuntime!=null))gardenLifeRuntime.StartActivity(a.id,selected);}
        }
        private void DrawLifeSettings(FormalCampaignSave save)
        {
            var setting=save.gardenLife.Setting(book.SubjectId);
            Action persist=()=>CommitLife("settings",JsonUtility.ToJson(setting),s=>s.gardenLife.settings=s.gardenLife.settings.Where(x=>x.gardenId!=setting.gardenId).Concat(new[]{setting}).ToArray(),StartLifeScene);
            if(Btn(998,264,550,48,"時刻："+LifeTimeName(setting.timePhase))){setting.timePhase=GardenLifeCatalog.Times[(Array.IndexOf(GardenLifeCatalog.Times,setting.timePhase)+1)%4];persist();}
            if(Btn(998,324,550,48,setting.autoLife?"自律生活：ON":"自律生活：OFF")){setting.autoLife=!setting.autoLife;persist();}
            if(Btn(998,384,550,48,setting.autoWeather?"天候：自動":"天候：固定")){setting.autoWeather=!setting.autoWeather;persist();}
            if(Btn(998,444,550,48,"固定天候："+LifeWeatherName(setting.weather),!setting.autoWeather)){var weathers=GardenLifeCatalog.AvailableWeathers(save,book.SubjectId);setting.weather=weathers[(Array.IndexOf(weathers,setting.weather)+1)%weathers.Length];persist();}
            if(Btn(998,504,550,48,"フレーム："+GardenLifeCatalog.FrameName(setting.frameId))){var ids=save.gardenLife.unlockedFrameIds;setting.frameId=ids[(Array.IndexOf(ids,setting.frameId)+1)%ids.Length];persist();}
            if(Btn(998,565,550,48,save.gardenLife.favoriteGardenId==book.SubjectId?"お気に入りの庭 ◆":"この庭をお気に入りにする")){string id=book.SubjectId;CommitLife("settings","favorite/"+id,s=>s.gardenLife.favoriteGardenId=id);}
            string reason=GardenLifeCatalog.PhenomenonUnavailable(save.world.terraform.activeWorldPhenomenonId,setting.timePhase,book.SubjectId);
            Label(998,631,550,75,reason??"Lv6・7でも、アステリアが暮らしの場を安定させています。",small,Color.white);
            if(Btn(998,720,550,43,"世界現象を選ぶ")){gardenLifePanel=null;RequestBookBookmark(BookBookmark.PossibleWorlds);}
        }
        private void DrawLifeEditor(Rect view,FormalHomeProgress home,string garden)
        {
            if(gardenLifeEditPlace){
                if(Btn(970,755,560,45,"編集パネルへ戻る"))gardenLifeEditPlace=false;
                if(GUI.enabled && Event.current.type==EventType.MouseDown && view.Contains(Event.current.mousePosition) && Event.current.mousePosition.y<745 && selectedFurniture!=null){var item=home.furnitureInstances.Single(i=>i.instanceId==selectedFurniture);var old=home.furniturePlacements.SingleOrDefault(p=>p.instanceId==selectedFurniture);var point=Event.current.mousePosition;LifeEditorTry(()=>gardenLifeEditor.Place(new HomePlacement{instanceId=item.instanceId,defId=item.defId,gardenId=garden,zoneId="zone.ground",orientationId=old?.orientationId??"orientation.default",x=(point.x-view.x)/view.width,y=(point.y-view.y)/view.height},gardenGridStep));Event.current.Use();}return;
            }
            Panel(970,190,600,615,dark);Label(998,205,550,40,"模様替え ／ 確定まで保存しません",text,Color.white);
            var items=home.furnitureInstances;gardenLifeScroll=GUI.BeginScrollView(new Rect(998,260,550,250),gardenLifeScroll,new Rect(0,0,525,Math.Max(250,items.Length*48)));
            for(int i=0;i<items.Length;i++){var item=items[i];if(Btn(0,i*48,520,42,(selectedFurniture==item.instanceId?"◆ ":"")+GardenFurnitureName(item.defId)+" "+(i+1))){selectedFurniture=item.instanceId;gardenLifeEditPlace=true;}}GUI.EndScrollView();
            if(Btn(998,524,170,42,"戻す "+gardenLifeEditor.UndoCount))gardenLifeEditor.Undo();if(Btn(1180,524,170,42,"やり直す"))gardenLifeEditor.Redo();if(Btn(1362,524,170,42,"全て収納"))gardenLifeEditor.StoreAll();
            var placement=home.furniturePlacements.SingleOrDefault(p=>p.instanceId==selectedFurniture);
            if(Btn(998,578,170,42,"収納",placement!=null))gardenLifeEditor.Store(selectedFurniture);
            if(Btn(1180,578,170,42,"回転",placement!=null))LifeEditorTry(()=>gardenLifeEditor.Rotate(selectedFurniture,false));
            if(Btn(1362,578,170,42,"左右反転",placement!=null))LifeEditorTry(()=>gardenLifeEditor.Rotate(selectedFurniture,true));
            if(Btn(998,632,170,42,"グリッド "+(gardenGridStep==0?"OFF":gardenGridStep==.025f?"小":"大")))gardenGridStep=gardenGridStep==0?.025f:gardenGridStep==.025f?.05f:0;
            if(Btn(1180,632,170,42,"配置枠 "+(gardenPresetIndex+1)))gardenPresetIndex=(gardenPresetIndex+1)%5;
            if(Btn(1362,632,170,42,"枠へ記録")){var preset=gardenLifeEditor.SavePreset(gardenPresetIndex);CommitLife("layout",JsonUtility.ToJson(preset),s=>s.gardenLife.layoutPresets=s.gardenLife.layoutPresets.Where(p=>p.gardenId!=garden || p.index!=preset.index).Concat(new[]{preset}).ToArray());}
            var saved=LifeSnapshot().gardenLife.layoutPresets.SingleOrDefault(p=>p.gardenId==garden && p.index==gardenPresetIndex);
            if(Btn(998,684,260,42,"配置枠を適用",saved!=null)){var shortages=gardenLifeEditor.Shortages(saved,home);if(shortages.Count>0)gardenPresetShortage=saved;else LifeEditorTry(()=>gardenLifeEditor.ApplyPreset(saved,home));}
            if(Btn(1274,684,260,42,"編集を取消"))gardenDiscardConfirm=true;
            if(Btn(998,742,550,45,"配置を確定して保存",gardenLifeRequest==null)){var editor=gardenLifeEditor;CommitLife("edit",JsonUtility.ToJson(new LifePlacementPayload{placements=editor.Placements}),s=>editor.Apply(s,HomeData()),()=>{gardenLifeEditor=null;StartLifeScene();});}
            if(GUI.enabled && Event.current.type==EventType.MouseDown && view.Contains(Event.current.mousePosition) && Event.current.mousePosition.x<960 && selectedFurniture!=null){var item=items.Single(i=>i.instanceId==selectedFurniture);var point=Event.current.mousePosition;LifeEditorTry(()=>gardenLifeEditor.Place(new HomePlacement{instanceId=item.instanceId,defId=item.defId,gardenId=garden,zoneId="zone.ground",orientationId=placement?.orientationId??"orientation.default",x=(point.x-view.x)/view.width,y=(point.y-view.y)/view.height},gardenGridStep));Event.current.Use();}
        }
        [Serializable] private sealed class LifePlacementPayload{public HomePlacement[] placements;}
        private void LifeEditorTry(Action edit){try{edit();gardenLifeError=null;}catch(ArgumentException e){gardenLifeError=e.Message;}}
    }
}
