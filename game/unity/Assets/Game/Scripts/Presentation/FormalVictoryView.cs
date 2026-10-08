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
        private FormalBattleEndRequest formalBattleEndRequest;
        private CollectionCatalog collectionCatalog;
        private BattleCollectionSession collectionSession;
        private System.Random singingRandom;
        private string formalVictorySummary,lastSinging;
        private bool formalVictoryDiagnosticFailure;
        private void StartCollection()
        {
            collectionCatalog=SelectCollectionCatalog();
            collectionSession=new BattleCollectionSession(collectionCatalog,battleId,activeColossus,selectedLevel,formalCampaign.Snapshot.revision,encounter.State.Heroes.Select(h=>h.Id).ToArray(),encounter.Seed,ActiveColossusDefinition(activeColossus).contentVersion,activeRelicHunt);
            singingRandom=new System.Random(unchecked(encounter.Seed ^ 0x534F4E47));lastSinging=null;
            // Trial prioritizes missing source poems; ordinary fixture keeps uniform sampling.
            // No presentation callback or battle RNG participates in collection.
            encounter.CompletedEnemyAction=()=>{
                var songs=collectionCatalog.owners.Single(o=>o.id==activeColossus).poemIds;
                var id=TrialSingingSelector.Select(collectionCatalog,activeColossus,collectionSession.Snapshot.formationIds,formalCampaign.Snapshot.world.poemIds,collectionSession.Snapshot.heardPoemIds,singingRandom,plan8StoryTrial);
                collectionSession.RecordCompletedSinging(id);
                TrialObserve("collection","singing-completed",id);
                var poem=plan8StoryTrial?StoryData().chapters.SelectMany(c=>c.poems).SingleOrDefault(p=>p.id==id):null;
                var productionPoem=ProductionStoryActive?ProductionStoryData().chapters.SelectMany(c=>c.poems).SingleOrDefault(p=>p.id==id):null;
                lastSinging=productionPoem!=null?"歌唱："+productionPoem.text:poem!=null?"歌唱："+poem.text:"歌唱を記録：詩 "+(Array.IndexOf(songs,id)+1)+"（収集テスト用・本文未制作）";
            };
        }
        private void PrepareFormalVictory()=>PrepareFormalBattleEnd(BattleEndReason.Victory);
        private void PrepareFormalBattleEnd(BattleEndReason reason)
        {
            var current=formalCampaign.Snapshot;
            var receipt=collectionSession.Finish(reason,current.world.poemIds,current.world.unlockedStoryIds);
            TrialObserve("battle","end",reason.ToString(),battleId+"/end");
            formalBattleEndRequest=new FormalBattleEndRequest(receipt,current.revision);
            formalVictoryRequest=reason==BattleEndReason.Victory?new FormalVictoryRequest(battleId,receipt.battle.colossusId,receipt.battle.level,current.revision):null;
            formalVictorySummary=null;PersistFormalVictory();
        }
        private CampaignSaveV2 BuildVictoryWorld(CampaignSaveV2 snapshot)
        {
            var next=new CampaignState(WorldCatalog.ColossusIds,snapshot);
            int priorTp=next.Terraform.totalTp;
            var c=WorldCatalog.Colossi.First(x=>x.Id==formalVictoryRequest.ColossusId);
            var band=collectionCatalog.rewardBands.Single(x=>x.ownerId==c.Id && formalVictoryRequest.Level>=x.minLevel && formalVictoryRequest.Level<=x.maxLevel);
            var reward=next.ClaimColossusVictory(c.Id,c.EnvironmentTags,new VictoryReward(formalVictoryRequest.BattleId,formalVictoryRequest.Level,10,band.terraforming-(formalVictoryRequest.Level-1)/10,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
            if(!reward.Reward.Claimed)throw new ArgumentException("Victory was already claimed.");
            next.Playable.RecordVictory(formalVictoryRequest.Level);
            formalVictorySummary=$"討伐成功！\n素材 +{reward.Reward.Materials} ／ TP +{next.Terraform.totalTp-priorTp}\nネクタル +{formalVictoryRequest.Nectar} ／ 覚醒結晶 +{formalVictoryRequest.Crystals} ／ 石 +{formalVictoryRequest.Stones}";
            if(reward.FirstClear)formalVictorySummary+="\n初回討伐：次のページと環境が開放されました。";
            if(reward.NewGardenIds.Count>0)formalVictorySummary+="\n庭が開放！庭のしおりから訪ねましょう。";
            return next.CreateSave();
        }
        private void PersistFormalVictory()
        {
            try{
                Func<FormalCampaignSave,bool> writer=formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign;
                var outcome=formalCampaign.CommitBattleEnd(formalBattleEndRequest,collectionCatalog,BuildVictoryWorld,writer,HomeData());
                if(outcome==GrowthCommitResult.SaveFailed){TrialObserve("save","battle-end-failed");result="戦闘終了 ／ 保存待ち";return;}
                var r=formalBattleEndRequest.Receipt;var saved=formalCampaign.Snapshot;
                lastCollectionResult=saved.collection.receipts.Single(x=>x.battle.battleId==r.battle.battleId);resultTab=0;
                TrialObserve("collection","committed",$"heard={r.battle.heardPoemIds.Length};new={r.acquiredPoemIds.Length};chapters={r.unlockedChapterIds.Length}",battleId+"/collection-committed");
                TrialObserve("economy","balance",$"nectar={saved.growth.nectar};crystals={saved.growth.awakeningCrystals}",battleId+"/balance");
                campaign=new CampaignState(WorldCatalog.ColossusIds,saved.world);
                formalProgression=new FormalProgression(saved.growth,combatDefinitions.HeroineIds);
                result=(formalVictorySummary??(r.reason==BattleEndReason.Defeat?"敗北":"撤退"))+$"\n聞いた詩 {r.battle.heardPoemIds.Length} ／ 新しい詩 {r.acquiredPoemIds.Length} ／ 開いた章 {r.unlockedChapterIds.Length}";
                if(r.reason!=BattleEndReason.Victory)result+="\n詩と章を保存しました。素材・石・世界復元の報酬はありません。";
                result+=ProductionStoryActive?"\n物語のしおりから、開いた章を読めます。":plan8StoryTrial?"\nオリジナル試遊本文：物語から、開いた章を読めます。歌唱率・戦闘値は調整中です。":"\n詩対応と歌唱率は検証用。本文・正式対応は未制作です。";
                formalBattleEndRequest=null;formalVictoryRequest=null;status="戦闘の取得結果を一括保存しました。";
            }catch(Exception e){result="戦闘終了 ／ 保存待ち";Debug.LogException(e);}
        }
        private void DrawVictorySavePending()
        {
            GrowthStyles();GrowthFill(0,0,1600,900,ink);GrowthFrame(120,100,1360,690);GrowthDiamond(800,170,26);
            Label(240,240,1120,70,"戦闘終了 ／ 記憶と報酬の保存待ち",growthTitleStyle);
            Label(240,350,1120,170,"聞いた詩・章と、勝利時の世界進行・育成報酬は、まだ保存されていません。\n保存先の空き容量・権限を確認して再試行してください。\n同じ取得結果を保持し、再抽選・二重受取は発生しません。",growthTextStyle);
            Label(240,555,1120,85,"このまま終了すると未保存の結果を失う場合があります。\n保存が完了するまで、出撃・育成・ガチャへの移動は停止します。",growthSmallStyle);
            if(GrowthButton(240,680,1120,65,"同じ戦闘結果を保存する",true,true))PersistFormalVictory();
        }
        private void DrawFormalVictoryComplete()
        {
            GrowthStyles();GrowthFill(0,0,1600,900,ink);GrowthFrame(120,100,1360,690);GrowthDiamond(800,170,26);
            Label(240,240,1120,65,"記憶が、新しい世界を育てる",growthTitleStyle);
            GrowthLine(240,322,1360,322,gold);
            var tabs=new[]{"概要","詩・章","世界再生","遺物"};for(int i=0;i<4;i++)if(GrowthButton(240+i*280,340,260,45,tabs[i],true,resultTab==i)){resultTab=i;resultScroll=Vector2.zero;}
            var detail=ResultDetail();float contentHeight=Math.Max(190,growthSmallStyle.CalcHeight(new GUIContent(detail),1080)+12);
            resultScroll=GUI.BeginScrollView(new Rect(240,405,1120,190),resultScroll,new Rect(0,0,1080,contentHeight));
            GUI.Label(new Rect(0,0,1080,contentHeight),detail,growthSmallStyle);GUI.EndScrollView();
            Label(240,608,1120,65,ProductionStoryActive?"取得結果を保存しました。開いた章は物語のしおりから読めます。\n世界の記憶・人物の詩・好感度は、それぞれの進捗で確かめられます。":(plan8StoryTrial?"取得結果を専用保存に確定しました。開いた章は物語から読めます。":"取得結果は一つの正式保存に確定しました。詩の本文は未制作です。")+"\n世界・育成・歌唱の数値は調整中です。",growthSmallStyle);
            if(GrowthButton(240,680,260,65,"万物の書へ")){result=null;encounter=null;}
            if(GrowthButton(520,680,260,65,"人物を育てる",true,true)){result=null;encounter=null;heroineRosterOpen=true;growthScreen=GrowthScreen.Overview;book.ChangeBookmark(BookBookmark.Heroines);}
            if(GrowthButton(800,680,260,65,"記憶・遺物")){result=null;encounter=null;collectionOpen=true;}
            if(GrowthButton(1080,680,280,65,"同じLvで再戦"))StartBattle(activeColossus);
        }
        private void PrepareVictoryCapture(string[] args)
        {
            var before=JsonUtility.ToJson(formalCampaign.Snapshot);
            formalVictoryDiagnosticFailure=args.Contains("-captureVictoryPending") || args.Contains("-captureVictoryRetry");
            encounter.State.ApplyBossDamage(int.MaxValue);FinishCheck();
            if(args.Contains("-captureVictoryRetry")){
                if(JsonUtility.ToJson(formalCampaign.Snapshot)!=before || !formalCampaign.HasPending || formalBattleEndRequest==null)throw new Exception("End failure was not atomic.");
                var request=formalBattleEndRequest;formalVictoryDiagnosticFailure=false;PersistFormalVictory();
                if(formalCampaign.HasPending || formalBattleEndRequest!=null || !campaign.CreateSave().claimedBattleIds.Contains(request.Id))throw new Exception("End retry did not publish both states.");
                long revision=formalCampaign.Snapshot.revision;
                if(formalCampaign.CommitBattleEnd(request,null,null,s=>throw new Exception("Duplicate write"))!=GrowthCommitResult.AlreadyCommitted || formalCampaign.Snapshot.revision!=revision)throw new Exception("End replay consumed twice.");
                Debug.Log("FORMAL_VICTORY_NAVIGATION_PASS 4 assertions");
            }
        }
    }
}
