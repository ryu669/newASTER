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
        private FormalProgression formalProgression;
        private FormalCampaignStore formalCampaignStore;
        private FormalCampaignJournal formalCampaign;
        private bool formalDiagnostic;
        private GrowthRequest growthRequest;
        private void InitializeFormalGrowth()
        {
            var splitGrowthStore=new FormalGrowthStore(Path.Combine(Application.persistentDataPath,"formal-growth-v1.json"),"newaster.formal-growth",
                s=>JsonUtility.ToJson(s,true),text=>JsonUtility.FromJson<FormalGrowthSave>(text));
            formalDiagnostic=Environment.GetCommandLineArgs().Contains("-presentationCapture");
            Func<FormalCampaignSave,string> encode=s=>JsonUtility.ToJson(s,true);
            Func<string,FormalCampaignSave> decode=t=>JsonUtility.FromJson<FormalCampaignSave>(t);
            formalCampaignStore=new FormalCampaignStore(Path.Combine(Application.persistentDataPath,"formal-campaign-v1.json"),encode,decode,t=>JsonUtility.FromJson<FormalCampaignHeader>(t));
            FormalCampaignSave unified=null;
            var unifiedLoad=formalDiagnostic?FormalLoadResult.Missing:formalCampaignStore.Load(out unified);
            if(unifiedLoad==FormalLoadResult.Blocked || unifiedLoad==FormalLoadResult.RecoveredBackup){BeginSaveRecovery();return;}
            if(unifiedLoad==FormalLoadResult.Missing){
                FormalGrowthSave growth=null;CampaignSaveV2 world=null;
                var growthLoad=formalDiagnostic?FormalLoadResult.Missing:splitGrowthStore.Load(out growth);
                var worldLoad=formalDiagnostic?FormalLoadResult.Missing:CampaignSaveStore.LoadFormalSplit(out world);
                if(growthLoad==FormalLoadResult.Blocked || growthLoad==FormalLoadResult.RecoveredBackup || worldLoad==FormalLoadResult.Blocked){BeginSaveRecovery("統合元の正式保存を確認できません。元ファイルを保持し、統合を停止しました。\n統合前の個別ファイルの復旧には別途対応が必要です。");return;}
                if(growth==null)growth=new FormalGrowthSave {saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()};
                unified=new FormalCampaignSave {growth=growth,world=world??new CampaignState(WorldCatalog.ColossusIds).CreateSave()};unified.Validate();
                if(!formalDiagnostic)formalCampaignStore.Save(unified);
            }
            BindFormalCampaign(unified);
        }
        private void BindFormalCampaign(FormalCampaignSave unified)
        {
            formalCampaign=new FormalCampaignJournal(unified,s=>JsonUtility.ToJson(s,true),t=>JsonUtility.FromJson<FormalCampaignSave>(t));
            campaign=new CampaignState(WorldCatalog.ColossusIds,unified.world);
            formalProgression=new FormalProgression(unified.growth,combatDefinitions.FormationIds);
            book=new BookNavigationState(new System.Collections.Generic.Dictionary<BookBookmark,System.Collections.Generic.IReadOnlyList<string>> {
                [BookBookmark.Colossi]=NewAster.Data.WorldCatalog.ColossusIds,
                [BookBookmark.Heroines]=combatDefinitions.FormationIds,
                [BookBookmark.Gardens]=new[]{"garden.grassland-forest"},
                [BookBookmark.Stories]=new[]{"story.green-return-dragon"}
            });
            Debug.Log("FORMAL_CAMPAIGN_READY revision="+unified.revision+" growth="+unified.growth.revision);
        }
        private bool SaveFormalGrowth(FormalGrowthSave next)
        {
            if(formalDiagnostic)return false;
            return formalCampaign.CommitGrowth(next,formalCampaignStore.Save);
        }
        private void DrawFormalGrowth() => DrawGrowthExperience();
    }
}
