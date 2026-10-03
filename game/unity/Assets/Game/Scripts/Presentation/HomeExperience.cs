using System;
using System.Linq;
using System.IO;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private HomeExperienceCatalog homeData;
        private bool homeTrial;private FormalCampaignSave homeOriginal;
        private void EnterHomeTrial()
        {
            if(!BookInputAllowed || homeTrial)return;homeOriginal=formalCampaign.Snapshot;
            acceptanceStore=new FormalCampaignStore(Path.Combine(Application.persistentDataPath,"plan6-home-trial-v1.json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var load=acceptanceStore.Load(out var trial);if(load!=FormalLoadResult.Loaded && load!=FormalLoadResult.Missing){status="検証セーブを読めません。元ファイルを保持しています。";acceptanceStore=null;return;}
            if(load==FormalLoadResult.Missing){trial=CreateHomeTrial();if(!acceptanceStore.Save(trial)){status="検証セーブを保存できません。";acceptanceStore=null;return;}}
            ResetGardenMenu();formalDiagnostic=true;homeTrial=true;BindFormalCampaign(trial);title=false;encounter=null;book.ChangeBookmark(BookBookmark.Gardens);
        }
        private FormalCampaignSave CreateHomeTrial()
        {
            var c=HomeData();var s=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(c.contentVersion),collection=new FormalCollectionLedger{materials=c.materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=100}).ToArray()}};
            s.world.unlockedGardenIds=c.gardens.Take(2).Select(g=>g.id).ToArray();HomeConditions.Refresh(s,c);return s;
        }
        private void ExitHomeTrial(){if(!BookInputAllowed || homeOriginal==null)return;ResetGardenMenu();formalDiagnostic=false;homeTrial=false;plan8StoryTrial=false;acceptanceStore=null;homeData=null;collectionCatalog=null;BindFormalCampaign(homeOriginal);homeOriginal=null;title=true;encounter=null;}
        private HomeExperienceCatalog HomeData()=>homeData??(homeData=HomeExperienceFixture.Create(combatDefinitions));
        private HomeOperation homeOperation;private FormalHomeRequest homeRequest;
        private string homeError,selectedFurniture,selectedResident,selectedNode;private float previewX=.5f,previewY=.65f;private bool placing;
        private FormalHomeProgress HomeState=>formalCampaign.Snapshot.home??FormalHomeProgress.Empty(HomeData().contentVersion);
        private void ProposeHome(HomeOperation op){if(!formalDiagnostic){status="検証用の別セーブを開くと操作できます。";return;}if(homeRequest!=null || formalCampaign.HasPending || formalProgression.HasPending)return;homeOperation=op;homeRequest=new FormalHomeRequest(Guid.NewGuid().ToString("N"),op.Kind=="equip"?"weapon":op.Kind=="remove"?"place":op.Kind=="use"?"occupant":op.Kind,formalCampaign.Snapshot.revision,HomeData().contentVersion,op.Key);homeError=null;}
        private void ConfirmHome()
        {
            try{var result=formalCampaign.CommitHomeOperation(homeRequest,HomeData(),homeOperation,formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign);if(result==GrowthCommitResult.SaveFailed){homeError="保存できません。同じ内容で再試行してください。";return;}homeRequest=null;homeOperation=null;placing=false;homeError=null;}
            catch(ArgumentException e){homeError=e.Message;}catch(InvalidOperationException e){homeError=e.Message;}
        }
        private void DrawHomeConfirmation(bool growth=false)
        {
            if(homeRequest==null)return;
            Panel(590,580,920,220,dark);Label(610,590,870,85,(homeError??"確定すると保存します。取消は無消費です。")+"\n"+HomeOperationSummary(),small,Color.white);
            if(Btn(610,704,530,58,formalCampaign.HasPending?"同じ内容で保存を再試行":"確定する"))ConfirmHome();
            if(Btn(1160,704,330,58,"取消",!formalCampaign.HasPending)){homeRequest=null;homeOperation=null;homeError=null;}
        }
        private void DrawFormalWeaponTree(string hero)
        {
            var h=HomeState;var nodes=HomeData().weaponNodes.Where(n=>n.heroineId==hero).ToArray();
            Label(605,295,890,42,"人物別の装備の樹 ／ 検証用4ノード",growthSmallStyle);
            foreach(var n in nodes)foreach(var parent in n.parentIds){var p=nodes.Single(x=>x.id==parent);GrowthLine(680+p.treePosition.x*640,340+p.treePosition.y*250,680+n.treePosition.x*640,340+n.treePosition.y*250,gold,3);}
            foreach(var n in nodes){float x=680+n.treePosition.x*640,y=340+n.treePosition.y*250;bool acquired=h.weaponNodeIds.Contains(n.id);if(GrowthButton(x-65,y-25,130,50,(acquired?"✿ ":"○ ")+(n.terminal??"初期"),homeRequest==null))selectedNode=n.id;}
            var selected=nodes.SingleOrDefault(n=>n.id==selectedNode)??nodes[0];selectedNode=selected.id;bool owned=h.weaponNodeIds.Contains(selected.id);
            Label(605,630,890,60,$"攻撃＋{selected.attackBonus} ／ 通常攻撃枠の倍率 {selected.skillPower:0.00}\n"+string.Join(" / ",selected.costs.Select(c=>$"素材 必要{c.amount}・所持{HomeRules.Balance(formalCampaign.Snapshot,c.resourceId)}")),growthSmallStyle);
            if(GrowthButton(605,710,420,58,owned?"装備する":"素材で取得する",homeRequest==null))ProposeHome(new HomeOperation(owned?"equip":"weapon",selected.id,owned?hero:null));
            if(GrowthButton(1045,710,445,58,"装備を外す",homeRequest==null))ProposeHome(new HomeOperation("equip","unequip",hero));
            DrawHomeConfirmation(true);
        }
        private void DrawFormalGarden()
        {
            string garden=book.SubjectId;var c=HomeData();var state=HomeState;var snapshot=formalCampaign.Snapshot;var layout=c.gardens.Single(g=>g.id==garden);
            bool unlocked=snapshot.world.unlockedGardenIds.Contains(garden);Label(32,272,920,55,"2D箱庭 ／ "+garden,heading);
            if(!unlocked || layout.unmade){Label(32,370,920,130,!unlocked?"世界に環境を取り戻すと、この庭が解放されます。":"区画条件は達成済みです。正式レイアウトは未制作です。",text);return;}
            if(book.Face==BookFace.Details){
                var heroesForEvents=snapshot.growth.heroines;for(int i=0;i<heroesForEvents.Length;i++){string hero=heroesForEvents[i].heroineId;if(Btn(32+i*187,355,178,45,combatDefinitions.Hero(hero).name))selectedResident=hero;}
                string owner=selectedResident??heroesForEvents[0].heroineId;Label(32,415,920,50,$"好感度 {state.affections.SingleOrDefault(a=>a.heroineId==owner)?.value??0} ／ 恋人 {(state.loverHeroineIds.Contains(owner)?"成立":"未成立")} ／ 正式本文は未制作",text);
                var events=HomeData().events.Where(e=>e.heroineId==owner && (!plan8StoryTrial || HasTrialText(e.id))).ToArray();for(int i=0;i<events.Length;i++){var ev=events[i];bool open=state.unlockedEventIds.Contains(ev.id),read=state.readEventIds.Contains(ev.id);Label(32,485+i*55,520,45,$"{(ev.kind=="affinity"?"好感度":"恋人")} {i+1} ／ {(read?"読了":open?"解放・未読":"前提イベントの読了待ち")}",small);if(Btn(565,480+i*55,210,45,plan8StoryTrial?"物語を読む":"検証ADV",formalDiagnostic && open))BeginAdv(ev.id,false);if(Btn(790,480+i*55,170,45,"回想",formalDiagnostic && read))BeginAdv(ev.id,true);}return;
            }
            var area=new Rect(32,355,930,235);GrowthFill(area.x,area.y,area.width,area.height,new Color(.17f,.28f,.24f));Label(48,364,890,30,"検証用静的2D素材 ／ 家具選択と人物名簿を分けて操作",small,Color.white);
            DrawGardenScene(area,state,garden,layout,true);
            // A translucent ground preview never mutates inventory or the journal.
            if(placing && selectedFurniture!=null){GrowthFill(area.x+previewX*area.width-55,area.y+previewY*area.height-40,110,40,new Color(.4f,.8f,.9f,.55f));if(Event.current.type==EventType.MouseDown && area.Contains(Event.current.mousePosition) && homeRequest==null){previewX=Mathf.Clamp01((Event.current.mousePosition.x-area.x)/area.width);previewY=Mathf.Clamp01((Event.current.mousePosition.y-area.y)/area.height);Event.current.Use();}}
            if(!placing){for(int i=0;i<c.furniture.Length;i++){var f=c.furniture[i];var cost=f.costs[0];if(Btn(32+i*313,610,302,45,$"家具{i+1}を作る 素材{cost.amount}",homeRequest==null))ProposeHome(new HomeOperation("craft",f.id,"furniture."+Guid.NewGuid().ToString("N")));}
                var inventory=state.furnitureInstances;for(int i=0;i<Math.Min(inventory.Length,3);i++){var item=inventory[i];if(Btn(32+i*313,670,302,45,"家具個体 "+(i+1)+" を選ぶ",homeRequest==null)){selectedFurniture=item.instanceId;placing=true;var p=state.furniturePlacements.SingleOrDefault(p0=>p0.instanceId==item.instanceId);previewX=p?.x??.5f;previewY=p?.y??.65f;}}
                if(inventory.Length>3){if(Btn(32,727,302,40,"次の家具候補",homeRequest==null)){int index=Array.FindIndex(inventory,i=>i.instanceId==selectedFurniture);selectedFurniture=inventory[(index+1)%inventory.Length].instanceId;placing=true;}}}
            else{Label(32,606,920,40,$"接地位置 X {previewX:0.000} / Y {previewY:0.000} ／ 庭をクリックして移動",small);
                if(Btn(32,660,295,52,"この位置を確認",homeRequest==null))ProposeHome(new HomeOperation("place",selectedFurniture,garden:garden,zone:layout.zones[0].id,x:previewX,y:previewY));
                if(Btn(345,660,295,52,"配置を撤去",homeRequest==null))ProposeHome(new HomeOperation("remove",selectedFurniture));if(Btn(658,660,295,52,"プレビュー取消",homeRequest==null)){placing=false;selectedFurniture=null;}}
            GrowthFill(1024,215,576,580,new Color(.035f,.065f,.08f));Label(1050,235,510,45,"人物名簿 ／ 編成とは独立",heading,Color.white);
            var heroes=snapshot.growth.heroines;for(int i=0;i<heroes.Length;i++){string hero=heroes[i].heroineId;var occupant=state.occupants.SingleOrDefault(o=>o.heroineId==hero);if(Btn(1050,300+i*54,505,45,combatDefinitions.Hero(hero).name+" / "+(occupant==null?"未配置":occupant.gardenId==garden?"この庭":"別庭"),homeRequest==null && !placing))selectedResident=hero;}
            if(selectedResident!=null){if(Btn(1050,600,505,45,"この庭へ移動（使用解除）",homeRequest==null && !placing))ProposeHome(new HomeOperation("occupant",selectedResident,garden:garden,x:.15f+Array.FindIndex(heroes,h=>h.heroineId==selectedResident)*.16f,y:.72f));
                if(Btn(1050,655,505,45,"交流 ／ 検証素材1・好感度＋1",homeRequest==null && !placing))ProposeHome(new HomeOperation("talk",selectedResident));
                if(Btn(1050,710,505,45,"家具を使う ／ 未対応ならidle",homeRequest==null && !placing && selectedFurniture!=null && state.occupants.Any(o=>o.heroineId==selectedResident && o.gardenId==garden)))ProposeHome(new HomeOperation("use",selectedResident,selectedFurniture));}
            Label(32,775,950,30,"人物は名簿から選択。美術は候補／スレイヤー以外のSD・会話本文は未制作。",small);
        }
        private void DrawGardenScene(Rect area,FormalHomeProgress state,string garden,HomeGardenLayout layout,bool names)
        {
            GUI.BeginGroup(area);
            try{
                var localArea=new Rect(0,0,area.width,area.height);
                var backgroundTexture=AdvTexture(layout.backgroundAssetId);if(backgroundTexture!=null)GUI.DrawTexture(localArea,backgroundTexture,ScaleMode.ScaleAndCrop);
                var entries=state.furniturePlacements.Where(p=>p.gardenId==garden).Select(p=>new{key=p.instanceId,y=p.y,zone=layout.zones.Single(z=>z.id==p.zoneId).order,p=p,o=(HomeOccupant)null}).Concat(state.occupants.Where(o=>o.gardenId==garden && GardenUsePlacement(o,state)==null).Select(o=>new{key=o.heroineId,y=o.y,zone=layout.zones[0].order,p=(HomePlacement)null,o=o})).OrderBy(e=>e.zone).ThenBy(e=>e.y).ThenBy(e=>e.key,StringComparer.Ordinal);
                foreach(var e in entries){if(e.p!=null)DrawGardenFurniture(localArea,e.p,state);else DrawGardenResident(localArea,e.o);}
                foreach(var asset in layout.foregroundAssetIds){var mask=AdvTexture(asset);if(mask!=null)GUI.DrawTexture(localArea,mask,ScaleMode.StretchToFill,true);else GrowthFill(0,0,area.width,8,new Color(.1f,.2f,.14f));}
                if(names)DrawGardenResidentLabels(localArea,state,garden);
            }finally{GUI.EndGroup();}
        }
        private void DrawGardenFurniture(Rect area,HomePlacement placement,FormalHomeProgress state)
        {
            var f=HomeData().furniture.Single(item=>item.id==placement.defId);var image=GardenFurnitureImageRect(area,placement);
            var texture=AdvTexture(f.assetId);if(texture!=null)DrawGardenArtUse(image,texture,GardenUse(placement.defId),state.occupants.Any(o=>GardenUsePlacement(o,state)?.instanceId==placement.instanceId));else{GrowthFill(image.x,image.y,image.width,image.height,new Color(.6f,.44f,.28f));Label(image.x,image.y,150,30,placement.defId.Substring(placement.defId.Length-1),small,Color.white);}
        }
        private void DrawGardenResident(Rect area,HomeOccupant occupant)
        {
            float x=area.x+occupant.x*area.width,y=area.y+occupant.y*area.height;
            if(occupant.heroineId=="heroine.slayer"){
                string action=occupant.actionId=="action.sit"?"sit":occupant.actionId=="action.work"?"work":occupant.actionId=="action.look"?"look":"idle";
                var texture=SampleImage("slayer-sd-"+action);if(texture==null)texture=SampleImage("slayer-sd-idle");
                float size=area.width*.128f*.95f;
                if(texture!=null)GUI.DrawTexture(new Rect(x-size*.5f,y-size*.98f,size,size),texture,ScaleMode.ScaleToFit,true);else GrowthDiamond(x,y,14);
            }else GrowthDiamond(x,y,14);
        }
        private void DrawGardenResidentLabels(Rect area,FormalHomeProgress state,string garden)
        {
            var residents=state.occupants.Where(o=>o.gardenId==garden).OrderBy(o=>o.x).ThenBy(o=>o.heroineId,StringComparer.Ordinal).ToArray();
            if(residents.Length==0)return;
            float cell=area.width/residents.Length,width=Math.Min(130,cell-4);
            for(int i=0;i<residents.Length;i++){
                float x=area.x+cell*(i+.5f)-width*.5f,y=area.y+2;
                GrowthFill(x,y,width,28,new Color(.035f,.065f,.08f,.9f));
                Label(x,y,width,28,combatDefinitions.Hero(residents[i].heroineId).name,new GUIStyle(small){fontSize=15,alignment=TextAnchor.MiddleCenter},Color.white);
            }
        }
    }
}
