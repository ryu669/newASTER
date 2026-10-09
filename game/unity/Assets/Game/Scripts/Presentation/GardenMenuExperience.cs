using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private enum GardenPanel { None,Furniture,Residents,Events,Navigation }
        private GardenPanel gardenPanel;
        private bool gardenMenuExpanded;
        private Vector2 gardenMenuScroll;
        private Vector2 gardenCraftScroll;
        private static readonly Rect gardenViewport=new Rect(0,88,1600,730);
        private static readonly Rect gardenDrawer=new Rect(1040,120,530,625);
        private void ResetGardenMenu(){gardenPanel=GardenPanel.None;gardenMenuExpanded=false;gardenMenuScroll=Vector2.zero;selectedFurniture=null;selectedResident=null;placing=false;}
        private string HomeOperationSummary()
        {
            string kind=homeOperation.Kind;
            if(kind=="commander")return homeOperation.Target=="formation.auto"?"指揮官を編成順の自動選択へ戻します。":combatDefinitions.Hero(homeOperation.Target).name+"を指揮官へ指定します。";
            if(kind=="sniper-support")return combatDefinitions.Hero(homeOperation.Target).name+"の支援対象を"+combatDefinitions.Hero(homeOperation.Owner).name+"に変更します。";
            if(kind=="formation")return "編成枠"+(int.Parse(homeOperation.Owner)+1)+"へ "+combatDefinitions.Hero(homeOperation.Target).name+"を配置します。";
            if(kind=="craft")return GardenFurnitureName(homeOperation.Target)+"を作ります。";
            if(kind=="place" || kind=="remove"){
                var item=HomeState.furnitureInstances.SingleOrDefault(f=>f.instanceId==homeOperation.Target);
                return (item==null?"家具":GardenFurnitureName(item.defId))+(kind=="place"?"の配置を変更します。":"を庭から片付けます。");
            }
            if(kind=="occupant" || kind=="talk" || kind=="use")return combatDefinitions.Hero(homeOperation.Target).name+(kind=="occupant"?"をこの庭へ移動します。":kind=="talk"?"と交流します。":"が選んだ家具を使います。");
            return kind=="weapon"?"選択した神器を解放します。":kind=="weapon-level"?"神器をLv."+homeOperation.Owner+"へ強化します。":kind=="equip"?"装備を変更します。":"変更内容を保存します。";
        }
        private void OpenGardenPanel(GardenPanel panel)
        {
            if(!BookInputAllowed || book.IsTransitioning)return;
            gardenPanel=gardenPanel==panel?GardenPanel.None:panel;gardenMenuScroll=Vector2.zero;gardenMenuExpanded=true;
        }
        private bool CloseGardenMenuLayer()
        {
            if(title || encounter!=null || help || storyText!=null || kinderGarden || collectionOpen || engagementOpen || recoveryActive || book==null || book.Bookmark!=BookBookmark.Gardens)return false;
            if(gardenPanel!=GardenPanel.None){gardenPanel=GardenPanel.None;gardenMenuScroll=Vector2.zero;return true;}
            return false;
        }
        private void BeginGardenPlacement()
        {
            if(!BookInputAllowed || selectedFurniture==null)return;
            var placement=HomeState.furniturePlacements.SingleOrDefault(p=>p.instanceId==selectedFurniture);
            previewX=placement?.x??.5f;previewY=placement?.y??.65f;placing=true;
        }
        private void PrepareGardenMenuCapture(string scenario)
        {
            if(!formalDiagnostic || capturePath==null || book.Bookmark!=BookBookmark.Gardens)throw new InvalidOperationException("Garden menu capture requires isolated garden.");
            string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);
            gardenPanel=GardenPanel.None;gardenMenuExpanded=false;
            OpenGardenPanel(GardenPanel.Furniture);AcceptanceCheck(gardenPanel==GardenPanel.Furniture && gardenMenuExpanded,"garden panel opens on demand");
            OpenGardenPanel(GardenPanel.Residents);AcceptanceCheck(gardenPanel==GardenPanel.Residents,"garden drawer switches exclusively");
            AcceptanceCheck(CloseGardenMenuLayer() && gardenPanel==GardenPanel.None && gardenMenuExpanded,"Escape closes drawer first");
            AcceptanceCheck(!CloseGardenMenuLayer(),"Persistent toolbar adds no hidden Back layer");
            OpenGardenPanel(GardenPanel.Furniture);help=true;AcceptanceCheck(!CloseGardenMenuLayer() && gardenPanel==GardenPanel.Furniture,"help gets Escape before garden menu");help=false;CloseGardenMenuLayer();CloseGardenMenuLayer();
            AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"menu visibility does not mutate saved progress");
            if(scenario=="expanded")gardenMenuExpanded=true;
            else if(scenario=="furniture")OpenGardenPanel(GardenPanel.Furniture);
            else if(scenario=="residents")OpenGardenPanel(GardenPanel.Residents);
            else if(scenario=="residents-last" || scenario=="events-last"){
                bool events=scenario=="events-last";OpenGardenPanel(events?GardenPanel.Events:GardenPanel.Residents);
                var heroes=formalCampaign.Snapshot.growth.heroines;selectedResident=heroes[heroes.Length-1].heroineId;gardenMenuScroll=new Vector2(0,heroes.Length*(events?40:48));
            }
            else if(scenario=="events")OpenGardenPanel(GardenPanel.Events);
            else if(scenario=="navigation")OpenGardenPanel(GardenPanel.Navigation);
            else if(scenario=="placement"){
                OpenGardenPanel(GardenPanel.Furniture);selectedFurniture=HomeState.furnitureInstances[0].instanceId;BeginGardenPlacement();
                AcceptanceCheck(placing && !BookInputAllowed,"placement blocks navigation until cancelled");
            }else if(scenario=="confirmation"){
                OpenGardenPanel(GardenPanel.Furniture);ProposeHome(new HomeOperation("craft",HomeData().furniture[0].id,"menu.capture.furniture"));
                AcceptanceCheck(homeRequest!=null && !BookInputAllowed && before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"confirmation blocks menus without spending");
            }else if(scenario!="closed")throw new ArgumentException("Unknown garden menu scenario");
            Debug.Log("GARDEN_MENU_CAPTURE_PASS "+scenario+" / isolated");
        }
        private static string GardenFurnitureName(string id)=>NewAster.Data.ProductionGardenCatalog.FurnitureName(id);
        private void DrawGardenHome()
        {
            if(ProductionStoryActive && (!plan10UiCapture || gardenLifeCapture)){DrawLifeGarden();return;}
            var catalog=HomeData();var snapshot=formalCampaign.Snapshot;var state=HomeState;string garden=book.SubjectId;
            var layout=catalog.gardens.Single(g=>g.id==garden);bool available=snapshot.world.unlockedGardenIds.Contains(garden) && !layout.unmade;
            Panel(0,0,1600,900,dark);
            if(available)DrawGardenScene(gardenViewport,state,garden,layout,false);
            else Label(100,270,930,160,layout.unmade?"この庭の景色は制作中です。":"世界を取り戻すと、この庭が開きます。",heading,Color.white);
            Panel(0,0,1600,88,dark);Label(28,20,730,52,"万物の書 ／ "+NewAster.Data.ProductionGardenCatalog.GardenName(garden),heading,Color.white);
            Label(770,25,575,42,$"素材 {AvailableCollectionMaterials}　TP {campaign.Terraform.totalTp}　詩 {campaign.Progress.CollectedPoemIds.Count}",small,Color.white);
            bool interactive=homeRequest==null && !formalCampaign.HasPending && !formalProgression.HasPending;
            if(placing && selectedFurniture!=null){
                GrowthFill(gardenViewport.x+previewX*gardenViewport.width-55,gardenViewport.y+previewY*gardenViewport.height-40,110,40,new Color(.4f,.8f,.9f,.55f));
                if(interactive && Event.current.type==EventType.MouseDown && gardenViewport.Contains(Event.current.mousePosition) && !gardenDrawer.Contains(Event.current.mousePosition) && Event.current.mousePosition.y<750){
                    previewX=Mathf.Clamp01((Event.current.mousePosition.x-gardenViewport.x)/gardenViewport.width);previewY=Mathf.Clamp01((Event.current.mousePosition.y-gardenViewport.y)/gardenViewport.height);Event.current.Use();
                }
            }
            if(gardenPanel!=GardenPanel.None)DrawGardenDrawer(state,garden,available,interactive);
            bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled && BookInputAllowed && !book.IsTransitioning;
            string[] labels={"家具","人物","交流・物語","庭を切替"};
            var panels=new[]{GardenPanel.Furniture,GardenPanel.Residents,GardenPanel.Events,GardenPanel.Navigation};
            for(int i=0;i<labels.Length;i++)if(Btn(28+i*260,755,245,50,(gardenPanel==panels[i]?"✓ ":"")+labels[i]))OpenGardenPanel(panels[i]);
            GUI.enabled=oldEnabled;
            if(homeRequest!=null){GrowthFill(0,0,1600,900,new Color(0,0,0,.5f));drawingModal=true;DrawHomeConfirmation();}
            if(help){drawingModal=true;DrawHelp();}
            DrawBookTransition();
        }
        private void DrawGardenDrawer(FormalHomeProgress state,string garden,bool available,bool interactive)
        {
            Panel(gardenDrawer.x,gardenDrawer.y,gardenDrawer.width,gardenDrawer.height,dark);
            string titleText=gardenPanel==GardenPanel.Furniture?"家具・模様替え":gardenPanel==GardenPanel.Residents?"庭の人物":gardenPanel==GardenPanel.Events?"交流・物語":"庭を切替";
            Label(1064,138,400,45,titleText,heading,Color.white);
            if(Btn(1500,138,45,45,"×",interactive && !placing)){gardenPanel=GardenPanel.None;return;}
            bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled && interactive && !help;
            if(placing){
                Label(1064,210,470,75,"X "+previewX.ToString("0.000")+" ／ Y "+previewY.ToString("0.000"),small,Color.white);
                if(Btn(1064,330,470,55,"この位置を確認"))ProposeHome(new HomeOperation("place",selectedFurniture,garden:garden,zone:HomeData().gardens.Single(g=>g.id==garden).zones[0].id,x:previewX,y:previewY));
                if(Btn(1064,410,470,55,"配置を撤去"))ProposeHome(new HomeOperation("remove",selectedFurniture));
                if(Btn(1064,490,470,55,"プレビューを取消"))placing=false;
            }else if(gardenPanel==GardenPanel.Navigation){
                Label(1064,210,470,95,"万物の書のページで、見たい庭を切り替えます。",small,Color.white);
                if(Btn(1064,340,225,55,"‹ 前の庭",book.CanTurnPrevious)){gardenPanel=GardenPanel.None;RequestBookTurn(-1);}
                if(Btn(1305,340,225,55,"次の庭 ›",book.CanTurnNext)){gardenPanel=GardenPanel.None;RequestBookTurn(1);}
            }else if(!available){Label(1064,215,470,110,"この庭では、まだ操作できません。",small,Color.white);}
            else if(gardenPanel==GardenPanel.Furniture){
                var data=HomeData();
                gardenCraftScroll=GUI.BeginScrollView(new Rect(1064,204,470,150),gardenCraftScroll,new Rect(0,0,445,Math.Max(150,data.furniture.Length*52)));
                for(int i=0;i<data.furniture.Length;i++){var item=data.furniture[i];int balance=HomeRules.Balance(formalCampaign.Snapshot,item.costs[0].resourceId);string source=NewAster.Data.WorldCatalog.Colossi.Single(c=>c.Id==data.materials.Single(m=>m.id==item.costs[0].resourceId).colossusId).DisplayName;if(Btn(0,i*52,440,46,GardenFurnitureName(item.id)+(TerraformRules.FurnitureUnlocked(formalCampaign.Snapshot.world,item.id,state)?"":"（領域発展で解放）")+" ／ "+source+" "+balance+"/"+item.costs[0].amount,HomeOperationsAllowed && balance>=item.costs[0].amount && TerraformRules.FurnitureUnlocked(formalCampaign.Snapshot.world,item.id,state)))ProposeHome(new HomeOperation("craft",item.id,"furniture."+Guid.NewGuid().ToString("N")));}
                GUI.EndScrollView();
                Label(1064,365,470,32,"持っている家具",small,Color.white);
                gardenMenuScroll=GUI.BeginScrollView(new Rect(1064,407,470,170),gardenMenuScroll,new Rect(0,0,445,Math.Max(170,state.furnitureInstances.Length*50)));
                for(int i=0;i<state.furnitureInstances.Length;i++){var item=state.furnitureInstances[i];if(Btn(0,i*50,440,44,(selectedFurniture==item.instanceId?"◆ ":"")+GardenFurnitureName(item.defId)+" "+(i+1)))selectedFurniture=item.instanceId;}
                GUI.EndScrollView();
                if(Btn(1064,600,470,55,"選んだ家具を配置・移動",HomeOperationsAllowed && selectedFurniture!=null))BeginGardenPlacement();
                Label(1064,665,470,35,"選んだ家具は「人物」から利用できます。",small,Color.white);
            }else{
                var heroes=formalCampaign.Snapshot.growth.heroines;if(selectedResident==null && heroes.Length>0)selectedResident=heroes[0].heroineId;
                int spacing=gardenPanel==GardenPanel.Events?40:48,height=gardenPanel==GardenPanel.Events?35:42,viewportHeight=gardenPanel==GardenPanel.Events?200:240;
                gardenMenuScroll=GUI.BeginScrollView(new Rect(1064,204,470,viewportHeight),gardenMenuScroll,new Rect(0,0,445,Math.Max(viewportHeight,heroes.Length*spacing)));
                for(int i=0;i<heroes.Length;i++){string hero=heroes[i].heroineId;if(Btn(0,i*spacing,440,height,(hero==selectedResident?"◆ ":"")+combatDefinitions.Hero(hero).name))selectedResident=hero;}
                GUI.EndScrollView();
                if(gardenPanel==GardenPanel.Residents){
                    if(Btn(1064,470,470,52,"この庭へ移動（家具利用を解除）",HomeOperationsAllowed && selectedResident!=null))ProposeHome(new HomeOperation("occupant",selectedResident,garden:garden,x:.15f+(Array.FindIndex(heroes,h=>h.heroineId==selectedResident)%5)*.16f,y:.72f));
                    if(Btn(1064,535,470,52,"選んだ家具を使う",HomeOperationsAllowed && selectedFurniture!=null && state.furniturePlacements.Any(p=>p.instanceId==selectedFurniture && p.gardenId==garden) && state.occupants.Any(o=>o.heroineId==selectedResident && o.gardenId==garden)))ProposeHome(new HomeOperation("use",selectedResident,selectedFurniture));
                    Label(1064,610,470,85,selectedFurniture==null?"家具パネルで、利用する家具を選んでください。":"選択中："+GardenFurnitureName(state.furnitureInstances.Single(f=>f.instanceId==selectedFurniture).defId),small,Color.white);
                }else if(selectedResident!=null){
                    Label(1064,415,470,32,"好感度 "+(state.affections.SingleOrDefault(a=>a.heroineId==selectedResident)?.value??0),small,Color.white);
                    if(Btn(1064,465,470,44,"交流 ／ 素材1・好感度＋1",HomeOperationsAllowed))ProposeHome(new HomeOperation("talk",selectedResident));
                    var events=HomeData().events.Where(e=>e.heroineId==selectedResident && (!plan8StoryTrial || HasTrialText(e.id))).ToArray();
                    for(int i=0;i<events.Length;i++){var item=events[i];bool read=state.readEventIds.Contains(item.id),open=state.unlockedEventIds.Contains(item.id);if(Btn(1064,525+i*40,470,35,(read?"回想":open?"物語を読む":"未解放")+" "+(ProductionStoryActive?ProductionStoryTitle(item.id):plan8StoryTrial?OriginalStoryTitle(item.id):(i+1).ToString()),HomeOperationsAllowed && open))BeginAdv(item.id,read);}
                    if(ProductionStoryActive)Label(1064,725,470,22,"好感度1・5・10・15・20と、前の物語の読了で解放",new GUIStyle(small){fontSize=14},Color.white);
                    if(plan8StoryTrial && events.Length==0)Label(1064,605,470,70,"この人物の交流本文は未制作です。",small,Color.white);
                    else if(plan8StoryTrial && events.Any(e=>!state.unlockedEventIds.Contains(e.id)))Label(1064,605,470,70,"解放条件：素材1を使って交流し、好感度1にする。",small,Color.white);
                }
            }
            GUI.enabled=oldEnabled;
            if(!HomeOperationsAllowed && gardenPanel!=GardenPanel.Navigation)Label(1064,712,470,27,"操作は下の「庭の検証セーブ」で試せます。",new GUIStyle(small){fontSize=14},Color.white);
            // Consume the remaining pointer event before it can reach controls behind this drawer.
            if(interactive && !help && gardenDrawer.Contains(Event.current.mousePosition) && (Event.current.type==EventType.MouseDown || Event.current.type==EventType.MouseUp || Event.current.type==EventType.ScrollWheel))Event.current.Use();
        }
    }
}
