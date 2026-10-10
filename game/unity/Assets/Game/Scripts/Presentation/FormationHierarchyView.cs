using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private int formationLayer;
        private const float forty=40;
        private bool returnToFormationFromWeapon;
        private void OpenFormationPage(){RequestBookBookmark(BookBookmark.Formation);}
        private void BackFormationLayer(){if(formationLayer>0)formationLayer=0;else BackBookPage();}
        private void ReturnFromFormationWeapon()
        {
            if(returnToFormationFromWeapon){
                BackBookPage();
                if(book.Bookmark!=BookBookmark.Formation)return;
                book.CompleteTransition();formationOpen=true;formationLayer=1;
            }
            growthScreen=GrowthScreen.Overview;
        }
        private void OpenFormationHeroReplacement(int slot){formationSlot=slot;formationLayer=2;formationSelectionPagePending=true;formationDetailScroll=Vector2.zero;formationPage=0;formationQuery="";formationCandidateHero=CurrentFormation()[slot];}
        private void OpenFormationOopartReplacement(int slot){formationSlot=slot;oopartSlot=slot;formationLayer=3;formationSelectionPagePending=true;formationDetailScroll=Vector2.zero;oopartPickerPage=0;oopartPickerQuery="";formationCandidateOopart=OopartSnapshot().collection.ooparts.slots[slot].equippedOopartId;}
        private void DrawHierarchicalFormation()
        {
            SanctuaryStyles();PalaceBackdrop("crown");formationSlot=Mathf.Clamp(formationSlot,0,4);bool interactive=BookInputAllowed && !help && !panzerSetupOpen;
            if(formationLayer==0)DrawFormationCards(interactive);
            else{
                if(GrowthButton(65,108,245,48,"‹ 編成",interactive))formationLayer=0;
                if(formationLayer==1)DrawFormationMemberSettings(interactive);
                else if(formationLayer==2)DrawFormationHeroReplacement(interactive);
                else DrawFormationOopartReplacement(interactive);
            }
            if(homeRequest!=null)DrawSanctuaryHomeConfirmation("");DrawPanzerSetup();
        }
        private void DrawFormationCards(bool interactive)
        {
            var ids=CurrentFormation();var o=OopartSnapshot().collection.ooparts;
            for(int i=0;i<3;i++){
                int index=i;string preset="preset."+(i+1);
                bool selected=i==oopartPresetIndex;
                if(selected){GrowthFill(61+i*320,108,308,54,new Color(.62f,.43f,.12f));GrowthLine(77+i*320,165,353+i*320,165,gold,4);}
                if(GrowthButton(65+i*320,112,300,46,(selected?"◆ 選択中　":"")+"編成 "+(i+1),interactive,selected)){
                    oopartPresetIndex=index;if(o.presets.Any(p=>p.id==preset)){ProposeOopart("preset-load",preset);CommitOopart();}
                }
            }
            if(GrowthButton(1115,112,185,46,"編成保存",interactive)){ProposeOopart("preset-save","preset."+(oopartPresetIndex+1));CommitOopart();}
            for(int i=0;i<5;i++){
                int slot=i;float x=65+i*298;GrowthFrame(x,210,278,520);
                var portrait=new Rect(x+9,247,260,302);string id=ids[i];
                if(id!=null)DrawHeroPortrait(portrait,id);else Label(x+35,335,208,100,"＋",new GUIStyle(growthTitleStyle){fontSize=76,alignment=TextAnchor.MiddleCenter},muted);
                if(ImageUiSkin.Button(portrait,"",GUIStyle.none) && interactive)OpenFormationHeroReplacement(slot);
                Label(x+17,214,120,31,(i+1).ToString(),growthSmallStyle,gold);
                if(GrowthButton(x+220,216,42,30,"⋯",interactive)){formationSlot=slot;formationLayer=1;}
                var h=id==null?null:formalProgression.Snapshot.heroines.Single(g=>g.heroineId==id);
                string name=id==null?"—":combatDefinitions.Hero(id).name;var nameStyle=new GUIStyle(growthTextStyle){fontSize=21,alignment=TextAnchor.MiddleCenter};while(nameStyle.fontSize>14 && nameStyle.CalcSize(new GUIContent(name)).x>246)nameStyle.fontSize--;
                Label(x+14,550,250,39,name,nameStyle);if(h!=null)Label(x+14,589,250,30,"Lv."+h.level,new GUIStyle(growthSmallStyle){alignment=TextAnchor.MiddleCenter});
                string gear=o.slots[i].equippedOopartId;var itemRect=new Rect(x+12,632,254,81);GrowthFrame(itemRect.x,itemRect.y,itemRect.width,itemRect.height);
                if(gear!=null){DrawOopartImage(new Rect(x+24,643,63,60),gear);Label(x+96,645,165,50,CollectionData().Oopart(gear).name,new GUIStyle(growthSmallStyle){fontSize=16,alignment=TextAnchor.MiddleLeft});}
                else Label(itemRect.x,itemRect.y,itemRect.width,itemRect.height,"＋",new GUIStyle(growthTitleStyle){fontSize=42,alignment=TextAnchor.MiddleCenter},muted);
                if(ImageUiSkin.Button(itemRect,"",GUIStyle.none) && interactive)OpenFormationOopartReplacement(slot);
            }
        }
        private void DrawFormationHeroReplacement(bool interactive)=>DrawFormationHeroPicker(interactive);
        private void DrawFormationMemberSettings(bool interactive)
        {
            var ids=CurrentFormation();string id=ids[formationSlot];var def=id==null?null:combatDefinitions.Hero(id);
            Label(350,113,1050,50,"第"+(formationSlot+1)+"枠 ／ "+(def?.name??"—"),growthTitleStyle);if(id!=null)DrawHeroPortrait(new Rect(95,215,490,480),id);
            var equip=HomeState.weaponEquipment.SingleOrDefault(e=>e.heroineId==id);string weapon=equip==null?"未装備":HomeData().weaponNodes.Single(n=>n.id==equip.nodeId).terminal;
            Label(665,230,820,48,"神器 ／ "+weapon,growthTextStyle);
            if(GrowthButton(665,300,820,60,"神器へ",interactive && id!=null)){returnToFormationFromWeapon=true;formationOpen=false;heroineRosterOpen=false;book.RequestSubject(BookBookmark.Heroines,id);growthScreen=GrowthScreen.Weapons;selectedNode=null;}
            if(def?.jobId=="job.defender"){
                Label(665,380,820,40,"護衛する誓女",growthTextStyle);
                for(int j=0;j<5;j++){
                    if(ids[j]==null)continue;int slot=j;
                    if(GrowthButton(665+j%2*420,435+j/2*66,400,54,(protectedFormationSlot==j?"◆ ":"")+combatDefinitions.Hero(ids[j]).name,interactive,protectedFormationSlot==j))protectedFormationSlot=slot;
                }
            }
            if(def?.jobId=="job.general"){
                bool selected=HomeState.commanderHeroineId==id;if(GrowthButton(665,490,820,55,selected?"✓ 指揮官に指定中":"指揮官に指定",interactive && !selected))ProposeHome(new HomeOperation("commander",id));if(GrowthButton(665,570,820,55,"指揮官を自動選択",interactive && HomeState.commanderHeroineId!=null))ProposeHome(new HomeOperation("commander","formation.auto"));
            }else if(def?.jobId=="job.sniper"){
                string chosen=HomeState.sniperSupports?.SingleOrDefault(s=>s.heroineId==id)?.targetId;Label(665,490,820,40,"支援対象",growthTextStyle);int n=0;
                for(int j=0;j<5;j++){if(j==formationSlot || ids[j]==null)continue;string target=ids[j];if(GrowthButton(665+n%2*420,555+n/2*70,400,55,(chosen==target?"◆ ":"")+combatDefinitions.Hero(target).name,interactive && chosen!=target))ProposeHome(new HomeOperation("sniper-support",id,target));n++;}
            }else if(def?.jobId=="job.panzer"){
                if(GrowthButton(665,490,820,60,"装甲・ツール",interactive)){var l=SavedPanzerLoadout();panzerResistance=Array.IndexOf(new[]{"physical","magic","fire"},l.Resistance);panzerTool0=Array.IndexOf(PlayableBattle.PanzerTools,l.FirstTool);panzerTool1=Array.IndexOf(PlayableBattle.PanzerTools,l.SecondTool);panzerSetupOpen=true;}
            }
        }
    }
}
