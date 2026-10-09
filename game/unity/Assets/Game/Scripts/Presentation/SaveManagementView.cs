using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private SaveSlotManager saveSlots;
        private DelayedSaveCoordinator delayedSaves;
        private bool saveManagementOpen,saveSlotBlocked,saveSlotDeleted;
        private int saveSelectedSlot=1;
        private SaveSlotSummary[] saveSummaries;
        private SaveSlotSummary restoreChoice;
        private SaveDeleteConfirmation deleteChoice;
        private string saveManagementMessage;
        private float settingsSaveDue;
        private bool settingsSaveDirty;
        private double nextSaveRetry;
        private void BackSaveManagement()
        {
            if(deleteChoice!=null){if(deleteChoice.Stage==2)saveSlots.BackDeleteConfirmation(deleteChoice);else deleteChoice=null;}
            else if(restoreChoice!=null)restoreChoice=null;
            else saveManagementOpen=false;
        }
        private void InitializeSaveSlots(string basePath,int generation)
        {
            var validation=new SaveValidationService(generation,Application.version,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,s=>JsonUtility.ToJson(s,true),s=>JsonUtility.FromJson<GameSave>(s),ValidateSlotContent);
            saveSlots=new SaveSlotManager(new SaveTransactionService(SaveSlotManager.GenerationDirectory(basePath,generation),validation));
            delayedSaves=new DelayedSaveCoordinator(saveSlots);saveSelectedSlot=saveSlots.ActiveSlot;
        }
        private void ValidateSlotContent(FormalCampaignSave value)
        {
            value.Validate();
            if(value.growth.heroines.Any(h=>!combatDefinitions.HeroineIds.Contains(h.heroineId)))throw new ArgumentException("未対応の人物が含まれています。対応するゲーム版で開いてください。");
            value.collection?.ValidateContent(SelectCollectionCatalog());
            if(value.home!=null && !ProductionStoryMigration.Required(value))value.home.ValidateContent(HomeData(),value);
            value.gardenLife?.ValidateContent(value,HomeData());
            if(value.affection!=null)AffectionSaveAdapter.ValidateContent(value,HomeData());
        }
        private FormalCampaignSave NewSlotProgress()
        {
            var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()}};
            if(ProductionStoryActive){save=ProductionStoryMigration.Prepare(save,combatDefinitions,ProductionStoryData(),s=>UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(s)));save.revision=0;save.collection=new FormalCollectionLedger{contentVersion=CollectionCatalog.ProductionVersion};}
            return save;
        }
        private void InitializeSlotCampaign()
        {
            int.TryParse(Application.version.Split('.')[0],out int major);int generation=major>=1?1:0;
            InitializeSaveSlots(Application.persistentDataPath,generation);
            var summary=saveSlots.Inspect(saveSlots.ActiveSlot);
            if(summary.Status==SaveSlotStatus.Ready){BindFormalCampaign(saveSlots.Load(saveSlots.ActiveSlot));return;}
            if(summary.Status!=SaveSlotStatus.Empty){
                saveSlotBlocked=true;BindFormalCampaign(NewSlotProgress());OpenSaveManagement();saveManagementMessage="このスロットを読み込めません。バックアップ・他スロット・空きスロットの新規開始を選択してください。";return;
            }
            if(File.Exists(Path.Combine(saveSlots.Transactions.Root,"active-slot"))){saveSlotDeleted=true;BindFormalCampaign(NewSlotProgress());title=true;OpenSaveManagement();saveManagementMessage="使用中のスロットは空です。他の記録をロードするか、空き枠で新規開始してください。";return;}
            // Development-only import. Source files are read and left intact; release never reads them.
            FormalCampaignSave progress=null;
            if(generation==0 && saveSlots.ActiveSlot==1 && (File.Exists(formalCampaignStore.SavePath) || File.Exists(formalCampaignStore.SavePath+".bak"))){
                var result=formalCampaignStore.Load(out progress);
                if(result!=FormalLoadResult.Loaded){saveSlotBlocked=true;BindFormalCampaign(NewSlotProgress());OpenSaveManagement();saveManagementMessage="開発版の旧セーブを自動移行できませんでした。元ファイルは保持しています。空きスロットで新規開始できます。";return;}
                if(ProductionStoryMigration.Required(progress))progress=ProductionStoryMigration.Prepare(progress,combatDefinitions,ProductionStoryData(),s=>UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(s)));
            }
            else if(generation==0 && saveSlots.ActiveSlot==1){
                var legacy=new FormalGrowthStore(Path.Combine(Application.persistentDataPath,"formal-growth-v1.json"),"newaster.formal-growth",s=>JsonUtility.ToJson(s,true),s=>JsonUtility.FromJson<FormalGrowthSave>(s));
                var growthResult=legacy.Load(out var growth);var worldResult=CampaignSaveStore.LoadFormalSplit(out var world);
                if(growthResult==FormalLoadResult.Blocked || growthResult==FormalLoadResult.RecoveredBackup || worldResult==FormalLoadResult.Blocked || worldResult==FormalLoadResult.RecoveredBackup){saveSlotBlocked=true;BindFormalCampaign(NewSlotProgress());OpenSaveManagement();saveManagementMessage="旧形式の記録を安全に読み込めません。元ファイルを保持しています。他スロットで新規開始できます。";return;}
                if(growth!=null || world!=null){var defaults=NewSlotProgress();progress=new FormalCampaignSave{growth=growth??defaults.growth,world=world??defaults.world};
                    if(ProductionStoryActive){progress=ProductionStoryMigration.Prepare(progress,combatDefinitions,ProductionStoryData(),s=>UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(s)));progress.revision=0;}}
            }
            progress=progress??NewSlotProgress();
            saveSlots.Save(saveSlots.ActiveSlot,progress,DateTime.UtcNow);BindFormalCampaign(progress);
        }
        private bool SaveDelayedCampaign(FormalCampaignSave next)
        {
            if(saveSlots==null)return SaveTrialObservedCampaign(next);
            if(saveSlotBlocked || saveSlotDeleted)return false;
            return delayedSaves.Queue(next,Time.realtimeSinceStartupAsDouble);
        }
        private void UpdateSaveScheduling()
        {
            if(settingsSaveDirty && Time.realtimeSinceStartup>=settingsSaveDue)FlushSettingsSave();
            if(delayedSaves==null || saveSlotBlocked || saveSlotDeleted || Time.realtimeSinceStartupAsDouble<nextSaveRetry)return;
            try{delayedSaves.Flush(Time.realtimeSinceStartupAsDouble,DateTime.UtcNow);saveSlots.FlushActiveSlotSetting();if(saveSlots.ActiveSlotSettingPending){nextSaveRetry=Time.realtimeSinceStartupAsDouble+5;status=saveManagementMessage="進行は保存済みですが、使用中スロットの設定を保存できません。再起動時は記録を選び直してください。";}}
            catch(Exception e){nextSaveRetry=Time.realtimeSinceStartupAsDouble+5;saveManagementMessage="自動保存できませんでした。変更を保持して再試行します。空き容量・権限を確認してください。";status=saveManagementMessage;Debug.LogWarning("SAVE_SLOT_DEFERRED_FAILED "+e.GetType().Name);}
        }
        private void RequestSettingsSave(){settingsSaveDirty=true;settingsSaveDue=Time.realtimeSinceStartup+5;}
        private bool FlushSettingsSave()
        {try{if(settingsSaveDirty)PlayerPrefs.Save();settingsSaveDirty=false;return true;}catch(Exception e){status="設定を保存できませんでした。";Debug.LogWarning(e.Message);return false;}}
        private bool FlushSaveChanges()
        {
            if(saveSlots==null)return true;
            if(saveSlotBlocked || saveSlotDeleted)return false;
            SaveBookNavigation();
            try{return delayedSaves.Flush(Time.realtimeSinceStartupAsDouble,DateTime.UtcNow,true) && FlushSettingsSave();}
            catch(Exception e){saveManagementMessage="未保存の変更を確定できません。空き容量・権限を確認して再試行してください。";status=saveManagementMessage;Debug.LogWarning(e.Message);return false;}
        }
        private void OpenSaveManagement()
        {
            if(!saveSlotBlocked && !saveSlotDeleted && (!FlushSaveChanges() || !FlushActiveTime()))return;
            saveManagementOpen=true;bookSystemOpen=false;restoreChoice=null;deleteChoice=null;RefreshSaveSummaries();
        }
        private void RefreshSaveSummaries()
        {try{saveSummaries=new[]{saveSlots.Inspect(1),saveSlots.Inspect(2)};}catch(Exception e){saveManagementMessage="保存場所を確認できません。他の保存操作の終了とアクセス権を確認してください。";Debug.LogWarning(e.Message);}}
        private string SaveSummaryText(SaveSlotSummary s)
        {
            if(s==null)return "読込できません";
            if(s.Status==SaveSlotStatus.Empty)return "空きスロット";
            if(s.Status==SaveSlotStatus.Unsupported)return "このゲーム版では読込できません";
            if(s.Status!=SaveSlotStatus.Ready)return "破損または読込不可。バックアップを確認してください";
            DateTime.TryParse(s.Header.savedAtUtc,out var at);var duration=TimeSpan.FromSeconds(s.PlaySeconds);
            return at.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")+"\nプレイ時間 "+(int)duration.TotalHours+"時間 "+duration.Minutes+"分 ／ 人物 "+s.Heroes+"\n討伐 "+s.Victories+"回 ／ 最高Lv "+s.HighestLevel;
        }
        private void SaveSelectedSlot()
        {
            if(!FlushSaveChanges() || !FlushActiveTime())return;
            try{saveSlots.Save(saveSelectedSlot,formalCampaign.Snapshot,DateTime.UtcNow);saveManagementMessage="スロット"+saveSelectedSlot+"に保存しました。以後の自動保存もこのスロットを使用します。";RefreshSaveSummaries();}
            catch(Exception e){saveManagementMessage="保存できませんでした。直前のセーブとバックアップを保持しています。";Debug.LogWarning(e.Message);}
        }
        private void ResetForSlotLoad(FormalCampaignSave progress)
        {
            affectionInteraction?.Cancel();affectionInteraction=null;dailyDateForm=null;affectionRequest=null;affectionError=null;affectionPanel=affectionShop=affectionConfirm=false;
            StopLifeAudio();gardenLifeRuntime?.Stop();gardenLifeRuntime=null;if(watchModeActive)ExitWatchMode();
            encounter=null;adv=null;result=null;homeRequest=null;homeOperation=null;growthRequest=null;kinderRequest=null;relicRequest=null;terraformRequest=null;gardenLifeRequest=null;placing=false;gardenLifeEditor=null;gardenDiscardConfirm=false;
            gardenLifePanel=null;gardenViewing=gardenLifeRecords=gardenLifeEditPlace=gardenLifeResidentPlace=false;gardenPanel=GardenPanel.None;gardenMenuExpanded=false;
            oopartPanel=false;oopartRequest=null;oopartError=null;exchangeMaterial=exchangeError=null;collectionOpen=formationOpen=kinderGarden=engagementOpen=false;
            formalVictoryRequest=null;formalBattleEndRequest=null;collectionSession=null;advBacklog=advHelp=false;terraformWarning=false;trialPoemChapter=null;
            bookSystemOpen=false;titlePanel=null;help=false;recoveryActive=false;saveSlotBlocked=saveSlotDeleted=false;unsavedActiveSeconds=0;lastRewardClock=0;bookNavigationDirty=false;
            BindFormalCampaign(progress);delayedSaves=new DelayedSaveCoordinator(saveSlots);title=false;restoreChoice=null;deleteChoice=null;
        }
        private void LoadSelectedSlot()
        {
            if(!saveSlotBlocked && !saveSlotDeleted && (!FlushSaveChanges() || !FlushActiveTime()))return;
            try{ResetForSlotLoad(saveSlots.Load(saveSelectedSlot));saveManagementMessage="スロット"+saveSelectedSlot+"を読み込みました。";RefreshSaveSummaries();}
            catch(Exception e){saveManagementMessage="読み込めませんでした。バックアップまたは他のスロットを選択してください。";Debug.LogWarning(e.Message);}
        }
        private void RestoreChosenBackup()
        {
            if(restoreChoice==null)return;
            try{ResetForSlotLoad(saveSlots.RestoreConfirmed(restoreChoice,DateTime.UtcNow));saveManagementMessage="バックアップを復元しました。復元前の正常データはバックアップ1へ保持しました。";RefreshSaveSummaries();}
            catch(Exception e){saveManagementMessage="復元できませんでした。元のセーブを保持しています。選び直して再試行してください。";Debug.LogWarning(e.Message);restoreChoice=null;}
        }
        private void DeleteChosenSlot()
        {
            if(deleteChoice==null || deleteChoice.Stage!=2)return;
            try{
                bool active=saveSlots.ActiveSlot==deleteChoice.Slot;saveSlots.DeleteConfirmed(deleteChoice);
                if(active){ResetForSlotLoad(NewSlotProgress());saveSlotDeleted=true;title=true;saveManagementOpen=false;}
                deleteChoice=null;saveManagementMessage=saveSlots.DeleteCleanupPending?"スロットを空にしました。退避ファイルの除去が完了していません。保存場所のアクセス権を確認してください。":"選択したスロットのセーブとバックアップ2世代を削除しました。";RefreshSaveSummaries();
            }catch(Exception e){saveManagementMessage="削除を完了できませんでした。アクセス権と他の起動を確認してください。";Debug.LogWarning(e.Message);deleteChoice=null;RefreshSaveSummaries();}
        }
        private void NewGameInSelectedSlot()
        {
            try{if(saveSlots.Inspect(saveSelectedSlot).Status!=SaveSlotStatus.Empty)throw new InvalidOperationException("空きスロットを選択してください。");var save=NewSlotProgress();saveSlots.Save(saveSelectedSlot,save,DateTime.UtcNow);ResetForSlotLoad(save);saveManagementMessage="新しい記録を開始しました。";RefreshSaveSummaries();}catch(Exception e){saveManagementMessage="新規開始できませんでした。空きスロットと保存場所を確認してください。";Debug.LogWarning(e.Message);}
        }
        private void DrawSaveManagement()
        {
            GrowthStyles();drawingModal=true;GrowthFill(0,0,1600,900,ink);GrowthFrame(70,50,1460,800);Label(115,80,1370,55,"セーブ管理",growthTitleStyle,gold);
            if(saveSlots==null){Label(115,160,1370,150,saveManagementMessage??"この撮影モードではセーブ管理は利用できません。",growthTextStyle);if(GrowthButton(115,730,1370,60,"戻る"))saveManagementOpen=false;return;}
            if(saveSummaries==null)RefreshSaveSummaries();
            if(deleteChoice!=null){
                Label(140,180,1320,65,"スロット"+deleteChoice.Slot+"の削除（確認 "+deleteChoice.Stage+" / 2）",growthTitleStyle);
                Label(140,280,1320,180,deleteChoice.Stage==1?"このスロットの最新セーブとバックアップ2世代を削除します。\n他のスロットには影響しません。":"削除した記録は戻せません。本当に削除しますか？\n使用中のスロットを削除すると表紙へ戻ります。",growthTextStyle);
                if(GrowthButton(140,590,850,65,deleteChoice.Stage==1?"削除内容を確認しました":"このスロットを削除する",true,true)){if(deleteChoice.Stage==1)saveSlots.ConfirmDelete(deleteChoice);else DeleteChosenSlot();}
                if(GrowthButton(1020,590,430,65,"取消"))deleteChoice=null;
            }else if(restoreChoice!=null){
                Label(140,180,1320,55,"バックアップ"+restoreChoice.Backup+"へ戻す",growthTitleStyle);
                Label(140,260,1320,145,SaveSummaryText(restoreChoice),growthTextStyle);
                Label(140,430,1320,100,"この時点まで進行が巻き戻ります。それ以降の抽選・育成・報酬は引き継ぎません。\n現在の正常なセーブはバックアップとして保持します。",growthTextStyle);
                if(GrowthButton(140,590,850,65,"このバックアップを復元する",true,true))RestoreChosenBackup();
                if(GrowthButton(1020,590,430,65,"取消"))restoreChoice=null;
            }else{
                for(int i=1;i<=2;i++){
                    float x=i==1?115:815;var s=saveSummaries?[i-1];GrowthFrame(x,160,670,240);
                    if(GrowthButton(x+15,175,640,52,"スロット"+i+(saveSlots.ActiveSlot==i?" ／ 使用中":"")+(saveSelectedSlot==i?" ✓":""),true,saveSelectedSlot==i))saveSelectedSlot=i;
                    Label(x+25,245,620,135,SaveSummaryText(s),growthTextStyle);
                }
                bool ready=saveSummaries?[saveSelectedSlot-1]?.Status==SaveSlotStatus.Ready;
                if(GrowthButton(115,430,420,58,"選択した枠に保存",!saveSlotBlocked && !saveSlotDeleted,true))SaveSelectedSlot();
                if(GrowthButton(565,430,420,58,"ロード",ready))LoadSelectedSlot();
                if(GrowthButton(1015,430,420,58,"空き枠で新規開始",saveSummaries?[saveSelectedSlot-1]?.Status==SaveSlotStatus.Empty))NewGameInSelectedSlot();
                for(int b=1;b<=2;b++){int backup=b;if(GrowthButton(115+(b-1)*450,515,420,58,"バックアップ"+b+"を確認")){try{var offer=saveSlots.Inspect(saveSelectedSlot,backup);if(offer.Status==SaveSlotStatus.Ready)restoreChoice=offer;else saveManagementMessage="このバックアップは利用できません。他の候補・スロット・新規開始を選択してください。";}catch(Exception e){saveManagementMessage="バックアップを確認できません。";Debug.LogWarning(e.Message);}}}
                if(GrowthButton(1015,515,420,58,"スロット削除")){try{deleteChoice=saveSlots.RequestDelete(saveSelectedSlot);}catch(Exception e){saveManagementMessage="削除の準備ができません。";Debug.LogWarning(e.Message);}}
                if(GrowthButton(115,730,1320,58,"戻る"))saveManagementOpen=false;
            }
            Label(115,665,1320,60,saveManagementMessage??"自動保存は使用中のスロットへ行います。バックアップは自動管理されます。",growthSmallStyle);
        }
    }
}
