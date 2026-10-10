using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool formationOpen;
        private int formationSlot,formationPage,materialPage;
        private string formationQuery="";
        private string[] CurrentFormation()=>HomeState.formationIds!=null && HomeState.formationIds.Length==5?(string[])HomeState.formationIds.Clone():combatDefinitions.FormationIds;
        private void DrawFormation()
        {DrawHierarchicalFormation();}
        private void DrawLegacyFormation()
        {
            bool previous=GUI.enabled;if(panzerSetupOpen)GUI.enabled=false;
            SanctuaryHeader("誓いの編成","5人の絆を、出撃の順番へ");
            if(GrowthButton(58,108,240,48,"‹ 誓女一覧",homeRequest==null && BookInputAllowed)){formationOpen=false;return;}

            if(idsOwnShell() && GrowthButton(1180,108,355,48,"シェルの装甲・ツール設定",homeRequest==null && BookInputAllowed)){
                var l=SavedPanzerLoadout();panzerResistance=Array.IndexOf(new[]{"physical","magic","fire"},l.Resistance);panzerTool0=Array.IndexOf(PlayableBattle.PanzerTools,l.FirstTool);panzerTool1=Array.IndexOf(PlayableBattle.PanzerTools,l.SecondTool);panzerSetupOpen=true;
            }
            var ids=CurrentFormation();
            for(int i=0;i<5;i++){
                float x=65+i*298;GrowthFrame(x,180,278,265);DrawHeroPortrait(new Rect(x+8,188,262,160),ids[i]);
                Label(x+14,352,250,40,combatDefinitions.Hero(ids[i]).name,growthTextStyle);
                if(GrowthButton(x+12,398,254,38,"編成枠 "+(i+1)+(formationSlot==i?"  選択中":""),homeRequest==null && BookInputAllowed,formationSlot==i))formationSlot=i;
            }
            Label(65,465,260,38,"入れ替える誓女を選択",growthSmallStyle,gold);
            string query=ImageUiSkin.TextField(new Rect(345,462,680,45),formationQuery,64,new GUIStyle(GUI.skin.textField){font=font,fontSize=22});
            if(query!=formationQuery){formationQuery=query;formationPage=0;}
            var heroes=formalProgression.Snapshot.heroines.Where(h=>combatDefinitions.HeroineIds.Contains(h.heroineId) && combatDefinitions.Hero(h.heroineId).name.Contains(formationQuery)).ToArray();
            int pages=Math.Max(1,(heroes.Length+9)/10);formationPage=Mathf.Clamp(formationPage,0,pages-1);
            var entries=heroes.Skip(formationPage*10).Take(10).ToArray();
            for(int i=0;i<entries.Length;i++){
                string id=entries[i].heroineId;float x=65+i%5*298,y=530+i/5*116;GrowthFrame(x,y,278,104);
                DrawHeroPortrait(new Rect(x+5,y+5,76,94),id);
                if(GrowthButton(x+88,y+10,180,82,combatDefinitions.Hero(id).name+"\nLv."+entries[i].level,homeRequest==null && BookInputAllowed && ids[formationSlot]!=id))ProposeHome(new HomeOperation("formation",id,formationSlot.ToString()));
            }
            var guardStyle=new GUIStyle(button){fontSize=16,wordWrap=false,padding=new RectOffset(2,2,0,0),alignment=TextAnchor.MiddleCenter};
            for(int i=0;i<5;i++)if(Btn(65+i*298,754,278,30,(protectedFormationSlot==i?"◆ ":"")+"護衛対象："+combatDefinitions.Hero(ids[i]).name,homeRequest==null && BookInputAllowed,guardStyle))protectedFormationSlot=i;
            if(GrowthButton(65,790,220,52,"‹ 前の10人",formationPage>0 && homeRequest==null))formationPage--;
            Label(450,800,640,42,"変更は保存後、次の出撃から反映 ／ 素材の消費なし",growthSmallStyle,gold);
            if(GrowthButton(1260,790,275,52,"次の10人 ›",formationPage+1<pages && homeRequest==null))formationPage++;
            if(homeRequest!=null)DrawSanctuaryHomeConfirmation("編成変更は無消費です。編成中の場合は2人の位置を交換します。");
            GUI.enabled=previous;DrawPanzerSetup();
        }
        private bool idsOwnShell()=>formalProgression.Snapshot.heroines.Any(h=>h.heroineId=="heroine.shell");
        private Color MaterialColor(int rarity)=>rarity>=4?gold:rarity==3?new Color(.78f,.57f,1):rarity==2?new Color(.4f,.78f,1):ivory;
        private void DrawMaterialInventory()
        {
            if(exchangeMaterial!=null){DrawExchange();return;}
            var owners=CollectionData().owners.Where(o=>o.kind=="colossus").ToArray();materialPage=Mathf.Clamp(materialPage,0,owners.Length-1);
            var owner=owners[materialPage];
            if(GrowthButton(150,280,180,50,"‹ 巨神獣",materialPage>0))materialPage--;
            Label(360,282,870,50,WorldCatalog.Colossi.Single(c=>c.Id==owner.id).DisplayName+"  ／  素材図鑑",growthTitleStyle);
            if(GrowthButton(1250,280,180,50,"巨神獣 ›",materialPage+1<owners.Length))materialPage++;
            for(int i=0;i<owner.materialIds.Length;i++){
                var r=CollectionData().resources.Single(m=>m.id==owner.materialIds[i]);float y=350+i*108;GrowthFrame(150,y,1280,98);
                DrawSanctuaryIcon(new Rect(169,y+22,52,52),"resource",MaterialColor(r.rarity));
                Label(245,y+14,615,42,r.RarityName+"  "+r.name,growthTextStyle,MaterialColor(r.rarity));
                var state=formalCampaign.Snapshot;Label(885,y+14,505,40,"所持 "+HomeRules.Balance(state,r.id)+" ／ Lv."+r.minDropLevel+"以上の勝利",growthSmallStyle);
                int unit=MaterialExchangeService.UnitCost(r);if(unit>0){Label(245,y+58,620,30,"交換：1個 "+unit+"ネクタル",growthSmallStyle);int[] quantities={1,5,10,MaterialExchangeService.Maximum(state,r)};string[] labels={"1","5","10","MAX"};for(int n=0;n<4;n++)if(GrowthButton(885+n*122,y+55,112,34,labels[n],quantities[n]>0 && quantities[n]<=MaterialExchangeService.Maximum(state,r) && BookInputAllowed))ProposeExchange(r,quantities[n]);}
            }
            Label(150,794,1280,60,"N → R → SR → SSR。上位神器は複数の巨神獣の希少素材を使用します。\n敗北・撤退では素材を獲得しません。",growthSmallStyle);
        }
    }
}
