using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private FormalEngagementRules engagementRules;
        private bool engagementOpen,engagementComplete;
        private FormalEngagementRequest engagementRequest;
        private int engagementAmount;
        private double unsavedActiveSeconds;
        private float nextClockAttempt;
        private double lastRewardClock;
        private string engagementError;
        private void InitializeEngagement()
        {
            if(ProductionStoryActive){engagementRules=NewAster.Data.ProductionEconomyCatalog.Engagement();engagementRules.Validate();return;}
            var source=Resources.Load<TextAsset>("Economy/engagement-trial");if(source==null)throw new ArgumentException("Engagement rules missing.");
            engagementRules=JsonUtility.FromJson<FormalEngagementRules>(source.text);engagementRules.Validate();
        }
        private void UpdateEngagement()
        {
            if(formalDiagnostic && !plan15Manual || recoveryActive || combatDefinitionError!=null || formalCampaign==null)return;
            bool visible=WatchWindowAdapter.IsVisible && (watchModeActive || Application.isFocused);
            double now=WatchWindowAdapter.RewardClock;double elapsed=lastRewardClock==0?0:now-lastRewardClock;lastRewardClock=now;if(visible && elapsed>0 && elapsed<=3600)unsavedActiveSeconds+=elapsed;
            if(Time.unscaledTime>=nextClockAttempt){FlushActiveTime();nextClockAttempt=Time.unscaledTime+10;}

        }
        private bool FlushActiveTime()
        {
            if(formalDiagnostic && !plan8StoryTrial && !plan15Manual)return true;
            if(formalCampaign.HasPending || formalProgression.HasPending)return false;
            if(unsavedActiveSeconds==0 && formalCampaign.Snapshot.playRewards?.lastDailyRewardDate==DailyRewardService.Day(DateTime.UtcNow))return true;
            double seconds=Math.Min(3600,unsavedActiveSeconds);
            try{if(!(plan8StoryTrial?formalCampaign.CommitActiveSeconds((int)seconds,SaveDiagnosticCampaign):formalCampaign.CommitPlayRewards(seconds,DateTime.UtcNow,formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign)))return false;unsavedActiveSeconds-=seconds;formalProgression=new FormalProgression(formalCampaign.Snapshot.growth,combatDefinitions.HeroineIds);return true;}
            catch(Exception e){Debug.LogException(e);engagementError="プレイ時間を保存できませんでした。再試行まで今回の時間を保持します。";return false;}
        }
        private void OpenEngagement()
        {engagementOpen=true;engagementComplete=false;engagementRequest=null;engagementError=FlushActiveTime()?null:"プレイ時間を保存できません。空き容量・権限を確認してください。";}
        private void EngagementBack()
        {
            if(formalCampaign.HasPending)return;
            if(engagementRequest!=null){engagementRequest=null;engagementComplete=false;engagementError=null;return;}
            engagementOpen=false;
        }
        private void ConfirmEngagement(bool login)
        {
            if(!FlushActiveTime())return;
            var state=formalCampaign.Snapshot;var ledger=state.engagement??new FormalEngagementState();
            var request=new FormalEngagementRequest(login,FormalEngagementRules.Day(DateTime.UtcNow),ledger.activeSeconds/engagementRules.periodSeconds,state.revision);
            engagementAmount=formalCampaign.PreviewEngagement(request,engagementRules,DateTime.UtcNow);engagementRequest=request;engagementError=null;
        }
        private void ReceiveEngagement()
        {
            if(engagementRequest==null || engagementComplete)return;
            try {
                Func<FormalCampaignSave,bool> writer=plan8StoryTrial?SaveDiagnosticCampaign:formalDiagnostic?(s=>true):SaveTrialObservedCampaign;
                if(formalCampaign.CommitEngagement(engagementRequest,engagementRules,DateTime.UtcNow,writer)==GrowthCommitResult.SaveFailed){engagementError="保存できませんでした。同じ報酬で再試行してください。";return;}
                formalProgression=new FormalProgression(formalCampaign.Snapshot.growth,combatDefinitions.HeroineIds);engagementComplete=true;engagementError=null;
            }catch(Exception e){engagementError=formalCampaign.HasPending?"保存できませんでした。同じ報酬を保持しています。":"日付や保存状態が変わりました。戻って受取内容を再確認してください。";Debug.LogException(e);}
        }
        private void DrawEngagement()
        {
            if(ProductionStoryActive){GrowthStyles();PalaceBackdrop("star");GrowthFrame(120,90,1360,720);var reward=formalCampaign.Snapshot;Label(230,220,1140,70,"星の恵み",growthTitleStyle);Label(230,340,1140,220,"所持石  "+reward.growth.stones+"\n毎日5時更新：300石 ／ 30分ごと：100石\n恵みは自動で受け取ります。\n次の時間報酬まで "+Math.Ceiling((1800-(reward.playRewards?.rewardRemainderSeconds??0))/60)+"分",growthTextStyle);if(GrowthButton(230,707,1140,62,"戻る"))EngagementBack();return;}
            GrowthStyles();PalaceBackdrop("star");GrowthFrame(120,90,1360,720);GrowthDiamond(800,159,24);
            Label(230,220,1140,70,engagementComplete?"星の恵みを受け取りました":engagementRequest!=null?"受け取る恵みの確認":"星の恵み",growthTitleStyle);
            GrowthLine(230,303,1370,303,gold);DrawSanctuaryIcon(new Rect(1287,210,70,70),"star",gold);
            var state=formalCampaign.Snapshot;var ledger=state.engagement??new FormalEngagementState();
            if(engagementRequest!=null){
                Label(230,355,1140,180,$"{(engagementRequest.Login?"今日のログイン":"プレイ時間の積み重ね")}\n石 ＋{engagementAmount}\n{(engagementComplete?"所持石  "+state.growth.stones:"費用なし ／ 受取履歴と石を一括保存します。")}",growthTextStyle);
                Label(230,570,1140,94,engagementError??"確認・取消では付与しません。保存済みの報酬は再付与しません。",growthSmallStyle);
                if(engagementComplete){if(GrowthButton(230,707,1140,62,"恵みの入口へ",true,true))EngagementBack();}
                else {
                    if(GrowthButton(230,707,750,62,formalCampaign.HasPending?"同じ報酬で保存を再試行":"この報酬を受け取る",true,true))ReceiveEngagement();
                    if(GrowthButton(1010,707,360,62,"取消",!formalCampaign.HasPending))EngagementBack();
                }
                return;
            }
            bool login=FormalEngagementRules.Day(DateTime.UtcNow)>ledger.lastLoginDay;
            long available=ledger.activeSeconds/engagementRules.periodSeconds-ledger.claimedPeriods;
            Label(230,340,1140,55,$"所持石  {state.growth.stones}    ／    保存済みプレイ時間  {ledger.activeSeconds/60}分",growthTextStyle);
            if(GrowthButton(230,423,1140,70,login?$"今日のログイン ／ {engagementRules.loginStones}石を確認":"今日のログインは受取済み",login,true))ConfirmEngagement(true);
            if(GrowthButton(230,525,1140,70,available>0?$"プレイ時間 ／ {available*engagementRules.periodStones}石を確認":$"次の時間報酬まで {Math.Ceiling((engagementRules.periodSeconds-ledger.activeSeconds%engagementRules.periodSeconds)/60d)}分",available>0))ConfirmEngagement(false);
            Label(230,615,1140,75,engagementError??"日本時間0時に日替わり300石、アクティブ30分ごとに100石。\n裏で起動中・停止中・60秒超の未操作は計測しません。端末時計を使います。",growthSmallStyle);
            if(GrowthButton(230,727,1140,52,"戻る"))EngagementBack();
        }
        private void PrepareEngagementCapture(string[] args)
        {
            encounter=null;engagementOpen=true;
            formalCampaign.CommitActiveSeconds(1800,s=>true);
            if(args.Contains("-captureEngagementConfirm") || args.Contains("-captureEngagementComplete"))ConfirmEngagement(false);
            if(args.Contains("-captureEngagementComplete"))ReceiveEngagement();
            if(args.Contains("-captureEngagementNavigation")){
                int checks=0;Action<bool> check=ok=>{checks++;if(!ok)throw new Exception("Engagement navigation failed.");};
                string before=JsonUtility.ToJson(formalCampaign.Snapshot);
                ConfirmEngagement(false);check(JsonUtility.ToJson(formalCampaign.Snapshot)==before);
                EngagementBack();check(engagementRequest==null && engagementOpen && JsonUtility.ToJson(formalCampaign.Snapshot)==before);
                ReceiveEngagement();check(JsonUtility.ToJson(formalCampaign.Snapshot)==before);
                ConfirmEngagement(false);ReceiveEngagement();check(engagementComplete && formalProgression.Snapshot.stones==100);
                ReceiveEngagement();check(formalProgression.Snapshot.stones==100);
                Debug.Log("FORMAL_ENGAGEMENT_NAVIGATION_PASS "+checks+" assertions");
            }
        }
    }
}
