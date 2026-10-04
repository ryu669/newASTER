using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public static class UnityFormalCampaignJson
    {
        public static string Encode(FormalCampaignSave value)
        {
            if(value==null)throw new ArgumentNullException(nameof(value));string text=JsonUtility.ToJson(value,true);
            if(value.home==null)text=FormalCampaignJsonShape.WithNullRootMember(text,"home");
            if(value.collection==null)text=FormalCampaignJsonShape.WithNullRootMember(text,"collection");
            if(value.engagement==null)text=FormalCampaignJsonShape.WithNullRootMember(text,"engagement");
            if(value.previousNarrative==null)text=FormalCampaignJsonShape.WithNullRootMember(text,"previousNarrative");
            return text;
        }
        public static FormalCampaignSave Decode(string text)
        {
            var value=new FormalCampaignSave{version=0,saveId=null};JsonUtility.FromJsonOverwrite(text,value);
            if(!FormalCampaignJsonShape.HasRootMember(text,"collection") || FormalCampaignJsonShape.RootMemberIsNull(text,"collection"))value.collection=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"engagement") || FormalCampaignJsonShape.RootMemberIsNull(text,"engagement"))value.engagement=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"previousNarrative") || FormalCampaignJsonShape.RootMemberIsNull(text,"previousNarrative"))value.previousNarrative=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"home") || FormalCampaignJsonShape.RootMemberIsNull(text,"home"))value.home=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"world") || FormalCampaignJsonShape.RootMemberIsNull(text,"world"))value.world=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"growth") || FormalCampaignJsonShape.RootMemberIsNull(text,"growth"))value.growth=null;
            NormalizeKinderOptionalFields(value.growth);
            return value;
        }
        private static void NormalizeKinderOptionalFields(FormalGrowthSave growth)
        {
            // JsonUtility serializes nullable string fields as empty strings. Restore only optional absent fields.
            if(growth?.receipts==null)return;
            foreach(var receipt in growth.receipts){if(receipt?.kinderOutcomes==null)continue;
                foreach(var outcome in receipt.kinderOutcomes){if(outcome==null)continue;
                    if(outcome.kind!="heroine" && outcome.heroineId=="")outcome.heroineId=null;
                    if(outcome.grantKind=="")outcome.grantKind=null;
                }
            }
        }
        public static FormalCampaignHeader DecodeHeader(string text)
        {
            var value=new FormalCampaignHeader();JsonUtility.FromJsonOverwrite(text,value);
            if(!FormalCampaignJsonShape.HasRootMember(text,"world") || FormalCampaignJsonShape.RootMemberIsNull(text,"world"))value.world=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"growth") || FormalCampaignJsonShape.RootMemberIsNull(text,"growth"))value.growth=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"engagement") || FormalCampaignJsonShape.RootMemberIsNull(text,"engagement"))value.engagement=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"collection") || FormalCampaignJsonShape.RootMemberIsNull(text,"collection"))value.collection=null;
            if(!FormalCampaignJsonShape.HasRootMember(text,"home") || FormalCampaignJsonShape.RootMemberIsNull(text,"home"))value.home=null;return value;
        }
    }
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
            Func<FormalCampaignSave,string> encode=NewAster.Presentation.UnityFormalCampaignJson.Encode;
            Func<string,FormalCampaignSave> decode=UnityFormalCampaignJson.Decode;
            formalCampaignStore=new FormalCampaignStore(Path.Combine(Application.persistentDataPath,"formal-campaign-v1.json"),encode,decode,UnityFormalCampaignJson.DecodeHeader);
            FormalCampaignSave unified=null;
            var unifiedLoad=formalDiagnostic?FormalLoadResult.Missing:formalCampaignStore.Load(out unified);
            if(unifiedLoad==FormalLoadResult.Blocked || unifiedLoad==FormalLoadResult.RecoveredBackup){BeginSaveRecovery();return;}
            if(unifiedLoad==FormalLoadResult.Missing){
                FormalGrowthSave growth=null;CampaignSaveV2 world=null;
                var growthLoad=formalDiagnostic?FormalLoadResult.Missing:splitGrowthStore.Load(out growth);
                var worldLoad=formalDiagnostic?FormalLoadResult.Missing:CampaignSaveStore.LoadFormalSplit(out world);
                if(growthLoad==FormalLoadResult.Blocked || growthLoad==FormalLoadResult.RecoveredBackup || worldLoad==FormalLoadResult.Blocked){BeginSplitSaveRecovery();return;}
                if(growth==null)growth=new FormalGrowthSave {saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()};
                unified=new FormalCampaignSave {growth=growth,world=world??new CampaignState(WorldCatalog.ColossusIds).CreateSave()};unified.Validate();
                if(!formalDiagnostic)SaveTrialObservedCampaign(unified);
            }
            BindFormalCampaign(unified);
        }
        private void BindFormalCampaign(FormalCampaignSave unified)
        {
            unified.collection?.ValidateContent(plan8StoryTrial?TrialStoryCatalog.Collection(combatDefinitions,StoryData()):CollectionContractFixture.Create(combatDefinitions));
            unified.home?.ValidateContent(plan8StoryTrial?HomeData():HomeExperienceFixture.Create(combatDefinitions),unified);
            formalCampaign=new FormalCampaignJournal(unified,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode);
            campaign=new CampaignState(WorldCatalog.ColossusIds,unified.world);
            formalProgression=new FormalProgression(unified.growth,combatDefinitions.FormationIds);
            book=CreateFormalBook();
            Debug.Log("FORMAL_CAMPAIGN_READY revision="+unified.revision+" growth="+unified.growth.revision);
            TrialObserve("save","loaded","revision="+unified.revision);
            TrialObserve("economy","opening-balance",$"nectar={unified.growth.nectar};crystals={unified.growth.awakeningCrystals};stones={unified.growth.stones}");
        }
        private bool SaveFormalGrowth(FormalGrowthSave next)
        {
            if(formalDiagnostic)return acceptanceStore!=null && formalCampaign.CommitGrowth(next,SaveDiagnosticCampaign);
            return formalCampaign.CommitGrowth(next,SaveTrialObservedCampaign);
        }
        private void DrawFormalGrowth() => DrawGrowthExperience();
    }
}
