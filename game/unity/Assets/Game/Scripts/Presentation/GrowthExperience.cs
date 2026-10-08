using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private enum GrowthScreen { Overview, Level, Awakening, Duplicate, Information, Confirmation, Complete, Weapons, Skill }
        private GrowthScreen growthScreen,growthOrigin;
        private GrowthPreview growthPreview;
        private int growthTargetLevel;
        private string growthOutcome,growthDelta;
        private Texture2D growthPortrait;
        private Texture2D iconoclastPortrait;
        private Texture2D underminePortrait;
        private Texture2D echidnaPortrait;
        private Texture2D excalipanPortrait;
        private readonly Color ink=new Color(.035f,.065f,.10f),navy=new Color(.06f,.11f,.16f),gold=new Color(.72f,.57f,.32f),ivory=new Color(.94f,.89f,.77f),muted=new Color(.67f,.71f,.73f);
        private GUIStyle growthTitleStyle,growthTextStyle,growthSmallStyle,growthButtonStyle;
        private void GrowthStyles()
        {
            if(growthTitleStyle!=null)return;
            growthTitleStyle=new GUIStyle(heading){fontSize=34};growthTitleStyle.normal.textColor=ivory;
            growthTextStyle=new GUIStyle(text){fontSize=ArtSampleSettings.LargeText?26:23};growthTextStyle.normal.textColor=ivory;
            growthSmallStyle=new GUIStyle(small){fontSize=ArtSampleSettings.LargeText?20:18};growthSmallStyle.normal.textColor=muted;
            growthButtonStyle=new GUIStyle(growthTextStyle){alignment=TextAnchor.MiddleCenter,fontSize=22};
            growthPortrait=Resources.Load<Texture2D>("Illustrations/slayer-portrait-candidate-v1");
            iconoclastPortrait=Resources.Load<Texture2D>("Illustrations/iconoclast-portrait-candidate-v1");
            underminePortrait=Resources.Load<Texture2D>("Illustrations/undermine-portrait-candidate-v1");
            echidnaPortrait=Resources.Load<Texture2D>("Illustrations/echidna-portrait-candidate-v1");
            excalipanPortrait=Resources.Load<Texture2D>("Illustrations/excalipan-portrait-candidate-v1");
        }
        private void GrowthFill(float x,float y,float w,float h,Color color)
        {ImageUiSkin.Surface(new Rect(x,y,w,h),color);}
        private void GrowthLine(float x1,float y1,float x2,float y2,Color color,float width=1)
        {
            float dx=x2-x1,dy=y2-y1;
            // Avoid rotating a scaled GUI matrix: its pivot is not resolution invariant.
            if(Mathf.Abs(dy)<.01f){GrowthFill(Mathf.Min(x1,x2),y1,Mathf.Abs(dx),width,color);return;}
            if(Mathf.Abs(dx)<.01f){GrowthFill(x1,Mathf.Min(y1,y2),width,Mathf.Abs(dy),color);return;}
            int steps=Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx),Mathf.Abs(dy)));
            for(int i=0;i<=steps;i++){float t=steps==0?0:i/(float)steps;GrowthFill(x1+dx*t,y1+dy*t,width+1,width+1,color);}
        }
        private void GrowthDiamond(float x,float y,float size)
        {GrowthLine(x,y-size,x+size,y,gold);GrowthLine(x+size,y,x,y+size,gold);GrowthLine(x,y+size,x-size,y,gold);GrowthLine(x-size,y,x,y-size,gold);}
        private void GrowthFrame(float x,float y,float w,float h)
        {
            ImageUiSkin.Frame(new Rect(x,y,w,h));
        }
        private void PalaceBackdrop(string emblem)
        {
            GrowthFill(0,0,1600,900,ink);
            for(int i=0;i<9;i++){float x=95+i*180;GrowthLine(x,0,x+170,900,new Color(.3f,.45f,.48f,.06f));GrowthDiamond(x,50,16);}
            GrowthLine(52,54,1548,54,gold);GrowthLine(52,847,1548,847,gold);
            DrawSanctuaryIcon(new Rect(747,13,106,82),emblem,new Color(.93f,.78f,.46f,.85f));
        }
        private bool GrowthButton(float x,float y,float w,float h,string caption,bool enabled=true,bool primary=false)
        {
            var rect=new Rect(x,y,w,h);
            var color=growthButtonStyle.normal.textColor;growthButtonStyle.normal.textColor=enabled?ivory:muted;
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;bool clicked=ImageUiSkin.Button(rect,caption,growthButtonStyle,primary);GUI.enabled=previous;growthButtonStyle.normal.textColor=color;if(clicked){TrialObserve("navigation","button",caption);PlayProductionUiSound(caption);}return clicked;
        }
        private void GrowthBack()
        {
            if(homeRequest!=null){if(!formalCampaign.HasPending){homeRequest=null;homeOperation=null;}return;}
            if(formalProgression.HasPending || formalCampaign.HasPending)return;
            if(formationOpen){BackFormationLayer();return;}
            if(returnToFormationFromWeapon && growthScreen==GrowthScreen.Weapons){ReturnFromFormationWeapon();return;}
            if(heroineRosterOpen){if(book.GoBack()){bookTransitionElapsed=0;heroineRosterOpen=false;}return;}
            if(growthScreen==GrowthScreen.Overview){heroineRosterOpen=true;selectedTrait=-1;return;}
            if(growthScreen==GrowthScreen.Confirmation){growthRequest=null;growthPreview=null;growthScreen=growthOrigin;return;}
            if(growthScreen==GrowthScreen.Information){if(book.Face==BookFace.Details)book.FlipPage();growthScreen=GrowthScreen.Overview;}else growthScreen=book.Face==BookFace.Details?GrowthScreen.Information:GrowthScreen.Overview;growthRequest=null;
        }
        private void GrowthSelect(GrowthScreen screen,FormalHeroineGrowth heroine)
        {growthScreen=screen;growthTargetLevel=Math.Min(heroine.LevelCap,heroine.level+1);if(screen==GrowthScreen.Information&&book.Face==BookFace.Overview)book.FlipPage();}
        private void GrowthConfirm(GrowthOperation operation,string id,FormalGrowthSave snapshot,int target=0)
        {
            growthRequest=new GrowthRequest(Guid.NewGuid().ToString("N"),id,snapshot.revision,operation,target);growthPreview=formalProgression.Preview(growthRequest);
            var after=snapshot.Copy();int index=Array.FindIndex(after.heroines,h=>h.heroineId==id);after.heroines[index]=growthPreview.HeroineAfter.Copy();
            int actorIndex=Array.IndexOf(PreviewFormation(id),id);var before=HeroinePreview(snapshot,heroId:id).State.Heroes[actorIndex];
            var result=HeroinePreview(after,heroId:id).State.Heroes[actorIndex];
            growthDelta=$"HP  {before.MaxHitPoints} → {result.MaxHitPoints}     攻撃  {before.Attack} → {result.Attack}\n防御  {before.PhysicalDefense} → {result.PhysicalDefense}     魔法防御  {before.MagicDefense} → {result.MagicDefense}";
            if(operation==GrowthOperation.Awaken) growthDelta=$"育成上限  Lv.{snapshot.heroines[index].LevelCap} → Lv.{growthPreview.HeroineAfter.LevelCap}\n現在のLvと能力はそのまま、新しい成長の余地がひらきます。";
            growthOrigin=growthScreen;growthScreen=GrowthScreen.Confirmation;growthOutcome=null;
        }
        private void DrawLegacyGrowthExperience()
        {
            GrowthStyles();if(!book.HasSubject)return;string id=book.SubjectId;var definition=combatDefinitions.Hero(id);
            var snapshot=formalProgression.Snapshot;var heroine=snapshot.heroines.Single(h=>h.heroineId==id);
            SanctuaryStyles();SanctuaryHeader("","");GrowthFill(38,95,1524,732,new Color(.055f,.10f,.14f));
            GrowthFrame(62,176,473,632);GrowthFrame(571,176,965,632);GrowthLine(555,105,555,800,gold,2);
            GrowthLine(90,56,640,56,gold);GrowthLine(960,56,1510,56,gold);GrowthDiamond(800,56,17);
            if(GrowthButton(80,118,180,48,growthScreen==GrowthScreen.Overview?"本へ戻る":"‹ 戻る",!formalProgression.HasPending))GrowthBack();
            if(growthScreen==GrowthScreen.Overview){if(GrowthButton(1130,118,180,48,"‹ 前の人物",BookInputAllowed && book.CanTurnPrevious))RequestBookTurn(-1);if(GrowthButton(1325,118,180,48,"次の人物 ›",BookInputAllowed && book.CanTurnNext))RequestBookTurn(1);}
            DrawBookTransition(true);
            bool previousGrowthEnabled=GUI.enabled;GUI.enabled=previousGrowthEnabled && !book.IsTransitioning && (!formalCampaign.HasPending || formalProgression.HasPending || homeRequest!=null);
            Label(92,194,425,56,definition.name,growthTitleStyle);Label(92,258,420,30,"★ ★ ★ ★ ★ ★   ／   Lv."+heroine.level,growthSmallStyle,gold);
            DrawHeroPortrait(new Rect(78,310,459,355),id);
            string[] names={"誓女の記憶","ネクタル育成","覚醒の儀","誓いの強化","人物の記録","選択の確認","誓いの結実","装備の樹"};Label(605,204,860,55,names[(int)growthScreen],growthTitleStyle);GrowthLine(605,270,1498,270,gold);
            if(growthScreen==GrowthScreen.Overview){
                if(!ProductionStoryActive && GrowthButton(605,785,885,42,homeTrial?"検証用の別セーブ ／ 通常へ戻る":"計画6の機能検証用セーブを開く",BookInputAllowed)){if(homeTrial)ExitHomeTrial();else EnterHomeTrial();}
                var entries=new[]{GrowthScreen.Level,GrowthScreen.Awakening,GrowthScreen.Duplicate};string[] captions={"01    ネクタルで育てる","02    覚醒して可能性をひらく","03    重複した誓いを力にする"};
                for(int i=0;i<3;i++)if(GrowthButton(605,363+i*104,885,78,captions[i]))GrowthSelect(entries[i],heroine);
                if(GrowthButton(605,690,430,58,"能力・スキルを見る"))GrowthSelect(GrowthScreen.Information,heroine);
                if(GrowthButton(1055,690,435,58,"装備の樹へ"))GrowthSelect(GrowthScreen.Weapons,heroine);
            }else if(growthScreen==GrowthScreen.Weapons){DrawFormalWeaponTree(id);
            }else if(growthScreen==GrowthScreen.Information){
                var actor=new PlayableBattle(1,campaign.Playable,combatDefinitions:combatDefinitions,formalGrowth:snapshot).State.Heroes[book.SubjectIndex];
                Label(605,310,880,130,$"HP  {actor.MaxHitPoints}    攻撃  {actor.Attack}\n防御  {actor.PhysicalDefense}    魔法防御  {actor.MagicDefense}\n速度  {actor.Speed}    重複強化  {heroine.duplicateRank} / 5",growthTextStyle);
                for(int i=0;i<3;i++)Label(605,480+i*68,880,52,$"0{i+1}  {combatDefinitions.Skill(id,i).name}",growthTextStyle);
                Label(605,716,880,50,"Lv・重複で速度とチェイン率は増えません。",growthSmallStyle);
            }else if(growthScreen==GrowthScreen.Level){
                if(growthTargetLevel<=heroine.level)growthTargetLevel=Math.Min(heroine.LevelCap,heroine.level+1);
                Label(605,307,880,48,$"現在 Lv.{heroine.level}    →    目標 Lv.{growthTargetLevel} / {heroine.LevelCap}",growthTitleStyle);
                int[] steps={1,5,10};for(int i=0;i<3;i++)if(GrowthButton(605+i*205,398,190,62,"＋"+steps[i]+" Lv",heroine.level<heroine.LevelCap))growthTargetLevel=Math.Min(heroine.LevelCap,growthTargetLevel+steps[i]);
                if(GrowthButton(1220,398,125,62,"−1",growthTargetLevel>heroine.level+1))growthTargetLevel--;
                if(GrowthButton(1360,398,130,62,"MAX",heroine.level<heroine.LevelCap))growthTargetLevel=heroine.LevelCap;
                int cost=growthTargetLevel>heroine.level?FormalProgression.LevelCost(heroine.level,growthTargetLevel):0;bool usable=cost>0&&snapshot.nectar>=cost;
                Label(605,505,880,100,$"ネクタル    必要 {cost} ／ 所持 {snapshot.nectar}\n育成上限    Lv.{heroine.LevelCap}",growthTextStyle);
                Label(605,640,880,42,cost==0?"覚醒して上限をひらくと、さらに育成できます。":!usable?"ネクタルが不足しています。":"ネクタル残量 "+(snapshot.nectar-cost),growthSmallStyle);
                if(GrowthButton(605,709,885,62,"変化を確認する",usable,true))GrowthConfirm(GrowthOperation.Level,id,snapshot,growthTargetLevel);
            }else if(growthScreen==GrowthScreen.Awakening){
                int cost=heroine.awakeningStage==0?20:60;bool complete=heroine.awakeningStage==2;
                Label(605,307,880,100,complete?"二度の覚醒を果たした誓女":$"覚醒 {heroine.awakeningStage} → {heroine.awakeningStage+1}\nLv上限 {heroine.LevelCap} → {(heroine.awakeningStage==0?80:120)}",growthTitleStyle);
                Label(605,465,880,100,complete?"最終Lv上限：120":$"覚醒結晶    必要 {cost} ／ 所持 {snapshot.awakeningCrystals}\n条件    Lv.{heroine.LevelCap} 到達（現在 Lv.{heroine.level}）",growthTextStyle);
                Label(605,630,880,50,"覚醒ではLvは増えず、育成できる上限がひらきます。",growthSmallStyle);
                if(GrowthButton(605,709,885,62,"覚醒内容を確認する",!complete&&heroine.level==heroine.LevelCap&&snapshot.awakeningCrystals>=cost,true))GrowthConfirm(GrowthOperation.Awaken,id,snapshot);
            }else if(growthScreen==GrowthScreen.Duplicate){
                int dedicated=Math.Min(100,heroine.fragments),common=100-dedicated;
                Label(605,307,880,60,$"誓いの強化    {heroine.duplicateRank} / 5",growthTitleStyle);Label(605,427,880,155,$"専用欠片    所持 {heroine.fragments}\n汎用超過素材    所持 {snapshot.overflow}\n今回の消費    専用 {dedicated} ＋ 汎用 {common}",growthTextStyle);
                Label(605,630,880,50,"専用欠片を優先。最大後の余りは汎用素材へ変換します。",growthSmallStyle);
                if(GrowthButton(605,709,885,62,"強化内容を確認する",heroine.duplicateRank<5&&snapshot.overflow>=common,true))GrowthConfirm(GrowthOperation.Strengthen,id,snapshot);
            }else if(growthScreen==GrowthScreen.Confirmation){
                string change=growthRequest.Operation==GrowthOperation.Level?$"Lv.{heroine.level} → Lv.{growthPreview.HeroineAfter.level}":growthRequest.Operation==GrowthOperation.Awaken?$"覚醒 {heroine.awakeningStage} → {growthPreview.HeroineAfter.awakeningStage}":$"重複強化 {heroine.duplicateRank} → {growthPreview.HeroineAfter.duplicateRank}";
                Label(605,310,880,90,change,growthTextStyle);Label(605,425,880,100,growthDelta,growthTextStyle);
                string cost=growthRequest.Operation==GrowthOperation.Level?$"ネクタル  {growthPreview.NectarCost}":growthRequest.Operation==GrowthOperation.Awaken?$"覚醒結晶  {growthPreview.CrystalCost}":$"専用欠片 {growthPreview.FragmentCost} ＋ 汎用 {growthPreview.OverflowCost}（汎用化 {growthPreview.OverflowGrant}）";
                Label(605,567,880,52,"消費："+cost,growthTextStyle,gold);Label(605,637,880,52,growthOutcome??"",growthSmallStyle);
                if(GrowthButton(605,709,570,62,formalProgression.HasPending?"同じ内容で保存を再試行":"この内容で確定する",true,true)){
                    try{if(formalProgression.Commit(growthRequest,SaveFormalGrowth)!=GrowthCommitResult.SaveFailed){growthRequest=null;growthScreen=GrowthScreen.Complete;growthOutcome="新しい力を、次の出撃へ。";}else growthOutcome="保存できませんでした。所持量は変更していません。";}
                    catch(Exception e){growthOutcome="保存できませんでした。所持量は変更していません。";Debug.LogException(e);}
                }
                if(GrowthButton(1190,709,300,62,"取消",!formalProgression.HasPending))GrowthBack();
            }else{
                GrowthDiamond(1050,385,48);Label(605,482,885,58,"育成を保存しました",growthTitleStyle,gold);Label(605,573,885,94,growthDelta,growthTextStyle);
                if(GrowthButton(605,709,885,62,"誓女の記憶へ戻る",true,true))GrowthBack();
            }
            GUI.enabled=previousGrowthEnabled;
        }
        private void ValidateGrowthScreenNavigation()
        {
            var original=formalProgression;var save=original.Snapshot;int checks=0;
            Action<bool> check=ok=>{checks++;if(!ok)throw new InvalidOperationException("Growth screen navigation assertion "+checks);};
            try {
                formalProgression=new FormalProgression(save,combatDefinitions.HeroineIds);
                string id=book.SubjectId;var hero=save.heroines.Single(h=>h.heroineId==id);
                GrowthSelect(GrowthScreen.Level,hero);check(growthScreen==GrowthScreen.Level);
                GrowthConfirm(GrowthOperation.Level,id,save,hero.level+1);check(growthScreen==GrowthScreen.Confirmation && growthPreview.NectarCost==12);
                GrowthBack();check(growthScreen==GrowthScreen.Level && growthRequest==null);
                check(JsonUtility.ToJson(formalProgression.Snapshot)==JsonUtility.ToJson(save));
                GrowthSelect(GrowthScreen.Information,hero);check(book.Face==BookFace.Details);
                GrowthBack();check(book.Face==BookFace.Overview && growthScreen==GrowthScreen.Overview);
                GrowthSelect(GrowthScreen.Level,hero);GrowthConfirm(GrowthOperation.Level,id,save,hero.level+1);
                formalProgression.Commit(growthRequest,s=>false);GrowthBack();
                check(growthScreen==GrowthScreen.Confirmation && formalProgression.HasPending);
                check(JsonUtility.ToJson(formalProgression.Snapshot)==JsonUtility.ToJson(save));
                formalProgression.Commit(growthRequest,s=>true);growthRequest=null;growthScreen=GrowthScreen.Complete;GrowthBack();
                check(growthScreen==GrowthScreen.Overview && formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id).level==hero.level+1);
                book.FlipPage();GrowthSelect(GrowthScreen.Level,formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id));growthScreen=GrowthScreen.Complete;GrowthBack();
                check(book.SubjectId==id && book.Face==BookFace.Details && growthScreen==GrowthScreen.Information);
                GrowthBack();check(book.Face==BookFace.Overview);
                Debug.Log("GROWTH_SCREEN_NAVIGATION_PASS "+checks+" assertions");
            } finally {formalProgression=original;growthRequest=null;growthPreview=null;growthScreen=GrowthScreen.Overview;growthOutcome=null;growthDelta=null;}
        }
    }
}
