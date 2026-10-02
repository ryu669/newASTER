using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private FormalVictoryRequest formalVictoryRequest;
        private string formalVictorySummary;
        private bool formalVictoryDiagnosticFailure;
        private void PrepareFormalVictory()
        {
            formalVictoryRequest=new FormalVictoryRequest(battleId,activeColossus,selectedLevel,formalCampaign.Snapshot.revision);
            PersistFormalVictory();
        }
        private CampaignSaveV2 BuildVictoryWorld(CampaignSaveV2 snapshot)
        {
            var next=new CampaignState(WorldCatalog.ColossusIds,snapshot);
            var c=WorldCatalog.Colossi.First(x=>x.Id==formalVictoryRequest.ColossusId);
            var poems=c.Id==GreenReturnDragonVerticalSlice.ColossusId?GreenReturnDragonVerticalSlice.PoemIds.Where(id=>!next.Progress.CollectedPoemIds.Contains(id)).Take(4).ToArray():Array.Empty<string>();
            var reward=next.ClaimColossusVictory(c.Id,c.EnvironmentTags,new VictoryReward(formalVictoryRequest.BattleId,formalVictoryRequest.Level,10,4,poems),GreenReturnDragonVerticalSlice.StoryChapters,Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
            if(!reward.Reward.Claimed)throw new ArgumentException("Victory was already claimed.");
            next.Playable.RecordVictory(formalVictoryRequest.Level);
            formalVictorySummary=$"討伐成功！\n素材 +{reward.Reward.Materials} ／ 世界復元 +{reward.Reward.Terraforming}\nネクタル +{formalVictoryRequest.Nectar} ／ 覚醒結晶 +{formalVictoryRequest.Crystals} ／ 石 +{formalVictoryRequest.Stones}\n新しい詩 {reward.Reward.NewPoemIds.Count} ／ 開いた章 {reward.Reward.NewStoryIds.Count}";
            if(reward.FirstClear)formalVictorySummary+="\n初回討伐：次のページと環境が開放されました。";
            if(reward.NewGardenIds.Count>0)formalVictorySummary+="\n庭が開放！庭のしおりから訪ねましょう。";
            if(c.IsIntegrationBoss)formalVictorySummary+="\n世界統合達成。新しい物語が始まります。";
            return next.CreateSave();
        }
        private void PersistFormalVictory()
        {
            try{
                Func<FormalCampaignSave,bool> writer=formalDiagnostic?(s=>!formalVictoryDiagnosticFailure):formalCampaignStore.Save;
                var outcome=formalCampaign.CommitVictory(formalVictoryRequest,BuildVictoryWorld,writer);
                if(outcome==GrowthCommitResult.SaveFailed){result="討伐成功 ／ 保存待ち";return;}
                var saved=formalCampaign.Snapshot;
                campaign=new CampaignState(WorldCatalog.ColossusIds,saved.world);
                formalProgression=new FormalProgression(saved.growth,combatDefinitions.FormationIds);
                result=formalVictorySummary??"この戦闘の報酬は確定済みです。";
                formalVictoryRequest=null;status="世界の解放・育成素材・石を一括保存しました。";
            }catch(Exception e){result="討伐成功 ／ 保存待ち";Debug.LogException(e);}
        }
        private void DrawVictorySavePending()
        {
            GrowthStyles();GrowthFill(0,0,1600,900,ink);GrowthFrame(120,100,1360,690);GrowthDiamond(800,170,26);
            Label(240,240,1120,70,"討伐成功 ／ 報酬の保存待ち",growthTitleStyle);
            Label(240,350,1120,170,"ページ・庭の解放と育成素材・石は、まだ一括保存が完了していません。\n保存先の空き容量・権限を確認して再試行してください。\n再試行で同じ報酬を保持し、二重受取は発生しません。",growthTextStyle);
            Label(240,555,1120,85,"このまま終了すると、未保存の討伐結果は失われる場合があります。\n保存が完了するまで、出撃・育成・ガチャへの移動は停止します。",growthSmallStyle);
            if(GrowthButton(240,680,1120,65,"同じ討伐結果を保存する",true,true))PersistFormalVictory();
        }
        private void DrawFormalVictoryComplete()
        {
            GrowthStyles();GrowthFill(0,0,1600,900,ink);GrowthFrame(120,100,1360,690);GrowthDiamond(800,170,26);
            Label(240,240,1120,65,"記憶が、新しい世界を育てる",growthTitleStyle);
            GrowthLine(240,322,1360,322,gold);Label(240,355,1120,250,result,growthTextStyle);
            Label(240,608,1120,65,"世界の解放・育成素材・石を、一つの正式保存に確定しました。\n育成・石の報酬量は検証用の調整値です。",growthSmallStyle);
            if(GrowthButton(240,680,350,65,"万物の書へ")){result=null;encounter=null;}
            if(GrowthButton(625,680,350,65,"人物を育てる",true,true)){result=null;encounter=null;book.ChangeBookmark(BookBookmark.Heroines);}
            if(GrowthButton(1010,680,350,65,"キンダーガーデン")){result=null;encounter=null;kinderGarden=true;}
        }
        private void PrepareVictoryCapture(string[] args)
        {
            var before=JsonUtility.ToJson(formalCampaign.Snapshot);
            formalVictoryDiagnosticFailure=args.Contains("-captureVictoryPending") || args.Contains("-captureVictoryRetry");
            encounter.State.ApplyBossDamage(int.MaxValue);FinishCheck();
            if(args.Contains("-captureVictoryRetry")){
                if(JsonUtility.ToJson(formalCampaign.Snapshot)!=before || !formalCampaign.HasPending || formalVictoryRequest==null)throw new Exception("Victory failure was not atomic.");
                var request=formalVictoryRequest;formalVictoryDiagnosticFailure=false;PersistFormalVictory();
                if(formalCampaign.HasPending || formalVictoryRequest!=null || !campaign.CreateSave().claimedBattleIds.Contains(request.BattleId) || formalProgression.Snapshot.stones!=request.Stones)throw new Exception("Victory retry did not publish both states.");
                long revision=formalCampaign.Snapshot.revision;
                if(formalCampaign.CommitVictory(request,null,s=>throw new Exception("Duplicate write"))!=GrowthCommitResult.AlreadyCommitted || formalCampaign.Snapshot.revision!=revision)throw new Exception("Victory replay consumed twice.");
                Debug.Log("FORMAL_VICTORY_NAVIGATION_PASS 4 assertions");
            }
        }
    }
}
