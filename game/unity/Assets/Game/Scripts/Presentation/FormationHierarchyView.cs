using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation {
 public sealed partial class PrototypeBootstrap {
  private int formationLayer;
  private const float forty=40;
  private bool returnToFormationFromWeapon;
  private void BackFormationLayer(){if(formationLayer>0)formationLayer--;else formationOpen=false;}
  private void ReturnFromFormationWeapon(){growthScreen=GrowthScreen.Overview;if(returnToFormationFromWeapon){returnToFormationFromWeapon=false;formationOpen=true;formationLayer=1;}}
  private void DrawHierarchicalFormation(){
   var ids=CurrentFormation();formationSlot=Mathf.Clamp(formationSlot,0,4);bool interactive=homeRequest==null && BookInputAllowed && !panzerSetupOpen;
   SanctuaryHeader("誓いの編成","配置 › 隊員の装備・支援 › 入れ替え候補");
   if(GrowthButton(58,108,260,48,formationLayer==0?"‹ 誓女一覧":formationLayer==2?"‹ 隊員の設定":"‹ 5人の配置",interactive)){BackFormationLayer();return;}
   Label(344,115,1150, forty,"枠を選ぶと、その隊員の神器・装備枠・出撃時の役割を設定できます。",growthSmallStyle);
   for(int i=0;i<5;i++){
    float x=65+i*298;GrowthFrame(x,176,278,254);var portrait=HeroPortrait(ids[i]);
    if(portrait!=null)GUI.DrawTexture(new Rect(x+8,183,262,145),portrait,ScaleMode.ScaleToFit,true);
    var def=combatDefinitions.Hero(ids[i]);DrawSanctuaryIcon(new Rect(x+12,335,32,32),"resource."+def.jobId.Replace("job.",""),Color.white);
    Label(x+53,333,220,40,def.name,new GUIStyle(growthTextStyle){fontSize=20});
    if(GrowthButton(x+12,380,254,38,"第"+(i+1)+"枠・設定"+(formationSlot==i && formationLayer>0?" ◆":""),interactive,formationSlot==i && formationLayer>0)){formationSlot=i;formationLayer=1;}
   }
   if(formationLayer==0){
    GrowthFrame(65,456,1470,329);Label(95,480,1400, fifty,"5人の配置",growthTitleStyle,gold);
    Label(95,547,1380,160,"各隊員のカードから神器と支援設定へ進みます。\n神器：1人1つ。オーパーツ：1人1枠分を準備（装備機能は後続実装）。\n指揮官とスナイパーの支援対象は出撃前に選び、保存後の次の戦闘へ反映します。\n同じ人物の衣装違いは、同時に編成できません。",growthTextStyle);
   }else if(formationLayer==1){
    string id=ids[formationSlot];var def=combatDefinitions.Hero(id);GrowthFrame(65,456,1470,346);
    Label(90,476,760, fifty,"第"+(formationSlot+1)+"枠 ／ "+def.name,growthTitleStyle,gold);
    if(GrowthButton(1170,475,330,48,"隊員を入れ替える ›",interactive))formationLayer=2;
    var equip=HomeState.weaponEquipment.SingleOrDefault(e=>e.heroineId==id);string weapon=equip==null?"未装備":HomeData().weaponNodes.Single(n=>n.id==equip.nodeId).terminal;
    Label(90,543,660, forty,"神器  1枠："+weapon,growthTextStyle);
    if(GrowthButton(90,590,640, fifty,"神器の樹・装備を開く ›",interactive)){returnToFormationFromWeapon=true;formationOpen=false;heroineRosterOpen=false;book.RequestSubject(BookBookmark.Heroines,id);growthScreen=GrowthScreen.Weapons;selectedNode=null;}
    Label(90,652,650, forty,"オーパーツ  1枠",growthTextStyle,muted);
    Label(90,696,650,32,"装備機能は後続実装です。",growthSmallStyle,muted);
    if(GrowthButton(90,736,640, forty,(protectedFormationSlot==formationSlot?"◆ 護衛対象に指定済み":"この隊員をディフェンダーの護衛対象にする"),interactive))protectedFormationSlot=formationSlot;
    Label(800,541,690, forty,"出撃時の役割 ／ "+HeroineIdentityCatalog.JobName(def.jobId),growthTextStyle,gold);
    if(def.jobId=="job.general"){
     bool selected=HomeState.commanderHeroineId==id;
     if(GrowthButton(800,590,690, fifty,selected?"◆ 指揮官に指定済み":"このジェネラルを指揮官にする",interactive && !selected))ProposeHome(new HomeOperation("commander",id));
     Label(800,653,670,80,"本人固有の5枠効果だけが有効です。\n指揮官を指定しない場合、編成順で自動選択します。",growthSmallStyle);
     if(GrowthButton(800,742,690, forty,"指揮官の選択を自動へ戻す",interactive && HomeState.commanderHeroineId!=null))ProposeHome(new HomeOperation("commander","formation.auto"));
    }else if(def.jobId=="job.sniper"){
     string chosen=HomeState.sniperSupports?.SingleOrDefault(s=>s.heroineId==id)?.targetId;
     if(chosen==null || !ids.Contains(chosen))chosen=ids[(formationSlot+1)%5];
     Label(800,587,690, forty,"通常時の支援対象を1人選択",growthSmallStyle);
     int item=0;for(int j=0;j<5;j++){if(j==formationSlot)continue;string target=ids[j];if(GrowthButton(800+(item%2)*350,640+(item/2)*43,335,38,(chosen==target?"◆ ":"")+combatDefinitions.Hero(target).name,interactive && chosen!=target))ProposeHome(new HomeOperation("sniper-support",id,target));item++;}
    }else if(def.jobId=="job.panzer"){
     if(GrowthButton(800,590,690, fifty,"装甲の耐性・ツール2枠を設定 ›",interactive)){var l=SavedPanzerLoadout();panzerResistance=Array.IndexOf(new[]{"physical","magic","fire"},l.Resistance);panzerTool0=Array.IndexOf(PlayableBattle.PanzerTools,l.FirstTool);panzerTool1=Array.IndexOf(PlayableBattle.PanzerTools,l.SecondTool);panzerSetupOpen=true;}
    }else Label(800,590,660,100,"このジョブに出撃前の個別支援設定はありません。\n固有操作は戦闘のREADY時に選べます。",growthSmallStyle);
   }else{
    Label(65,450,260, forty,"第"+(formationSlot+1)+"枠の候補",growthSmallStyle,gold);
    string query=ImageUiSkin.TextField(new Rect(345,448,830,42),formationQuery,64,new GUIStyle(GUI.skin.textField){font=font,fontSize=22});if(query!=formationQuery){formationQuery=query;formationPage=0;}
    var heroes=formalProgression.Snapshot.heroines.Where(h=>combatDefinitions.HeroineIds.Contains(h.heroineId) && combatDefinitions.Hero(h.heroineId).name.Contains(formationQuery)).ToArray();int pages=Math.Max(1,(heroes.Length+9)/10);formationPage=Mathf.Clamp(formationPage,0,pages-1);
    var entries=heroes.Skip(formationPage*10).Take(10).ToArray();
    for(int i=0;i<entries.Length;i++){string id=entries[i].heroineId;float x=65+i%5*298,y=510+i/5*116;GrowthFrame(x,y,278,104);var portrait=HeroPortrait(id);if(portrait!=null)GUI.DrawTexture(new Rect(x+5,y+5,76,94),portrait,ScaleMode.ScaleToFit,true);if(GrowthButton(x+88,y+10,180,82,combatDefinitions.Hero(id).name+"\nLv."+entries[i].level,interactive && ids[formationSlot]!=id))ProposeHome(new HomeOperation("formation",id,formationSlot.ToString()));}
    if(GrowthButton(65,762,250, fifty,"‹ 前の10人",interactive && formationPage>0))formationPage--;Label(430,773,660, forty,"保存後、次の出撃から反映 ／ 無消費",growthSmallStyle,gold);if(GrowthButton(1260,762,275, fifty,"次の10人 ›",interactive && formationPage+1<pages))formationPage++;
   }
   if(homeRequest!=null)DrawSanctuaryHomeConfirmation("編成・役割の変更は無消費です。");DrawPanzerSetup();
  }
 }
}
