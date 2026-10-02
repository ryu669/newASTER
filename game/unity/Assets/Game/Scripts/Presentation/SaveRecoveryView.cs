using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool recoveryActive,recoveryConfirm,recoveryComplete,recoveryShowLocation,recoveryWasRestored,recoveryShowHeroes;
        private FormalRecoveryOffer recoveryOffer;
        private bool recoverySplit;
        private string SplitGrowthPath=>System.IO.Path.Combine(Application.persistentDataPath,"formal-growth-v1.json");
        private void BeginSplitSaveRecovery()
        {
            BeginSaveRecovery();recoverySplit=true;
            var initial=new FormalGrowthSave {saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()};
            recoveryOffer=formalCampaignStore.InspectSplitRecovery(SplitGrowthPath,CampaignSaveStore.SavePath,
                t=>JsonUtility.FromJson<FormalGrowthHeader>(t),t=>JsonUtility.FromJson<FormalGrowthSave>(t),
                t=>JsonUtility.FromJson<FormalWorldHeader>(t),t=>JsonUtility.FromJson<CampaignSaveV2>(t),initial,new CampaignState(NewAster.Data.WorldCatalog.ColossusIds).CreateSave());
            recoveryPreview=recoveryOffer.CanRestore?formalCampaignStore.RecoveryPreview(recoveryOffer):null;
        }
        private FormalCampaignSave recoveryPreview;
        private string recoveryBlockedReason,recoveryError,recoveryPreservedPath;
        private void BeginSaveRecovery(string blockedReason=null)
        {
            recoveryActive=true;recoverySplit=false;recoveryBlockedReason=blockedReason;recoveryConfirm=false;recoveryComplete=false;recoveryError=null;
            recoveryOffer=blockedReason==null?formalCampaignStore.InspectRecovery():null;
            recoveryPreview=recoveryOffer!=null && recoveryOffer.CanRestore?formalCampaignStore.RecoveryPreview(recoveryOffer):null;
        }
        private string RecoveryReason()
        {
            if(recoveryBlockedReason!=null)return recoveryBlockedReason;
            if(recoveryOffer?.Status==FormalRecoveryStatus.Unsupported)return "新しい版、別の保存ID、または未対応の内容版です。\n古いバックアップで上書きせず、この保存に対応するゲーム版で開いてください。";
            if(recoveryOffer?.Status==FormalRecoveryStatus.Unavailable)return "保存場所を読み取れません。\n空き容量・権限・他のゲーム起動を確認し、もう一度確認してください。";
            if(recoveryPreview==null)return "利用できる正常なバックアップがありません。\n元ファイルを残したまま停止しています。新規状態で上書きしません。";
            return recoverySplit?"統合前の正式保存を読み込めませんでした。\n正常な側と確認済みバックアップを使い、新しい統合保存を作成できます。":"現在の保存を読み込めませんでした。\n確認済みのバックアップから、世界・人物・所持品をまとめて戻せます。";
        }
        private void RestoreSaveFromConfirmation()
        {
            if(!recoveryConfirm || recoveryPreview==null)return;
            try{
                var saved=formalDiagnostic?recoveryPreview:recoverySplit?formalCampaignStore.RestoreSplitConfirmed(recoveryOffer,SplitGrowthPath,CampaignSaveStore.SavePath):formalCampaignStore.RestoreConfirmed(recoveryOffer,out recoveryPreservedPath);
                BindFormalCampaign(saved);recoveryComplete=true;recoveryWasRestored=true;recoveryConfirm=false;recoveryError=null;
            }catch(Exception e){recoveryError="復旧は完了していません。保存内容の変化や空き容量・権限を確認し、再確認してください。";Debug.LogException(e);}
        }
        private void RefreshSaveRecovery()
        {
            if(formalDiagnostic)return;
            recoveryActive=false;
            try{InitializeFormalGrowth();if(!recoveryActive){recoveryActive=true;recoveryComplete=true;recoveryWasRestored=false;recoveryError=null;}}
            catch(Exception e){BeginSaveRecovery("保存を読み込めません。元ファイルを保持しています。\n空き容量・権限・対応するゲーム版を確認してください。");Debug.LogException(e);}
        }
        private void DrawSaveRecovery()
        {
            GrowthStyles();GrowthFill(0,0,1600,900,ink);GrowthFrame(95,70,1410,760);GrowthDiamond(800,130,22);
            Label(200,193,1200,66,recoveryComplete?(recoveryWasRestored?"記憶を取り戻しました":"保存を読み込めました"):recoveryConfirm?"バックアップへ戻す前の確認":"記憶の保全と復旧",growthTitleStyle);
            GrowthLine(200,274,1400,274,gold);
            if(recoveryComplete){
                Label(200,330,1200,150,recoveryWasRestored?(recoverySplit?"世界・人物・所持品を新しい統合保存へ復旧しました。\n再配布や再抽選は行っていません。\n統合前の元ファイルとバックアップは、すべてそのまま保持しています。":"世界・人物・素材・石・ポイント・チケットを同じ保存へ復旧しました。\n再配布や再抽選は行っていません。\n正常なバックアップは保持し、壊れた元ファイルがあれば別名で保全しています。"):"正常な正式保存を読み込めました。\nバックアップへの巻戻し・再配布・再抽選は行っていません。",growthTextStyle);
                if(recoveryPreservedPath!=null)Label(200,515,1200,90,"保全先："+recoveryPreservedPath,growthSmallStyle);
                if(GrowthButton(200,725,1200,65,"タイトルへ進む",true,true)){recoveryActive=false;title=true;}
                return;
            }
            Label(200,307,1200,100,recoveryConfirm?"このバックアップの時点まで戻します。\nそれ以降に進めた討伐・育成・抽選結果は、この復旧では引き継げません。":RecoveryReason(),growthTextStyle);
            if(recoveryPreview!=null){
                var s=recoveryPreview;
                if(recoveryShowHeroes && !recoveryConfirm){
                    string detail=string.Join("\n",s.growth.heroines.Take(5).Select(h=>$"{(combatDefinitions.FormationIds.Contains(h.heroineId)?combatDefinitions.Hero(h.heroineId).name:h.heroineId)}  Lv.{h.level}/{h.LevelCap} ／ 覚醒{h.awakeningStage} ／ 重複強化{h.duplicateRank}"));
                    Label(200,423,1200,160,detail,growthTextStyle);
                }else Label(200,423,1200,140,$"バックアップの保存番号  {s.revision}\n人物 {s.growth.heroines.Length}人 ／ 討伐記録 {s.world.claimedBattleIds.Length}回 ／ 詩 {s.world.poemIds.Length}\nネクタル {s.growth.nectar} ／ 覚醒結晶 {s.growth.awakeningCrystals}\n石 {s.growth.stones} ／ ポイント {s.growth.kinderPoints} ／ 専用チケット {s.growth.tickets.Sum(t=>(long)t.count)}枚",growthTextStyle);
            }
            Label(200,592,1200,100,recoveryError??(recoveryShowLocation?"保存場所："+formalCampaignStore.SavePath:recoverySplit?"閲覧・取消だけでは書き換えません。\n復旧時も統合前の元ファイルとバックアップをすべて保持します。":"閲覧・取消だけでは書き換えません。\n復旧時は元ファイルを別名で残し、正常なバックアップを維持します。"),growthSmallStyle);
            if(recoveryConfirm){
                if(GrowthButton(200,725,810,65,"このバックアップから復旧する",true,true))RestoreSaveFromConfirmation();
                if(GrowthButton(1040,725,360,65,"取消（書換えなし）")){recoveryConfirm=false;recoveryError=null;}
            }else{
                if(recoveryPreview!=null){if(GrowthButton(200,725,480,65,"復旧内容を確認する",true,true))recoveryConfirm=true;}
                else if(GrowthButton(200,725,480,65,"保存せずゲームを終了する"))Application.Quit();
                if(GrowthButton(705,725,215,65,recoveryShowHeroes?"所持品の概要":"人物の状態",recoveryPreview!=null))recoveryShowHeroes=!recoveryShowHeroes;
                if(GrowthButton(945,725,215,65,"もう一度確認"))RefreshSaveRecovery();
                if(GrowthButton(1185,725,215,65,"保存場所"))recoveryShowLocation=!recoveryShowLocation;
            }
        }
        private void PrepareRecoveryCapture(string[] args)
        {
            encounter=null;recoveryActive=true;recoveryPreview=formalCampaign.Snapshot;
            if(args.Contains("-captureRecoveryBlocked")){recoveryPreview=null;recoveryBlockedReason="新しい版の保存です。古いバックアップへの復旧は停止しています。\nこの保存に対応したゲーム版で開いてください。";}
            recoveryConfirm=args.Contains("-captureRecoveryConfirm");
            if(args.Contains("-captureRecoveryNavigation")){
                var before=JsonUtility.ToJson(formalCampaign.Snapshot);int checks=0;
                Action<bool> check=ok=>{checks++;if(!ok)throw new Exception("Recovery navigation failed.");};
                recoveryConfirm=true;recoveryConfirm=false;check(JsonUtility.ToJson(formalCampaign.Snapshot)==before);
                RestoreSaveFromConfirmation();check(!recoveryComplete);
                recoveryConfirm=true;RestoreSaveFromConfirmation();check(recoveryComplete && JsonUtility.ToJson(formalCampaign.Snapshot)==before);
                check(!recoveryConfirm && recoveryError==null);Debug.Log("FORMAL_RECOVERY_NAVIGATION_PASS "+checks+" assertions");
            }
        }
    }
}
