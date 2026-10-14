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
            if(value==null)throw new ArgumentNullException(nameof(value));string text=JsonUtility.ToJson(value,false);
            var absent=new System.Collections.Generic.List<string>();
            if(value.home==null)absent.Add("home");
            if(value.affection==null)absent.Add("affection");
            if(value.gardenLife==null)absent.Add("gardenLife");
            if(value.collection==null)absent.Add("collection");
            if(value.engagement==null)absent.Add("engagement");
            if(value.previousNarrative==null)absent.Add("previousNarrative");
            if(absent.Count>0)text=FormalCampaignJsonShape.WithNullRootMembers(text,absent.ToArray());
            if(value.collection!=null && value.collection.ooparts==null){var collection=FormalCampaignJsonShape.RootMemberJson(text,"collection");text=FormalCampaignJsonShape.WithRootMemberJson(text,"collection",FormalCampaignJsonShape.WithNullRootMember(collection,"ooparts"));}
            return text;
        }
        public static FormalCampaignSave Decode(string text)
        {
            var value=new FormalCampaignSave{version=0,saveId=null};JsonUtility.FromJsonOverwrite(text,value);var shape=FormalCampaignJsonShape.Inspect(text);
            if(!shape.Contains("collection") || shape.IsNull("collection"))value.collection=null;
            if(!shape.Contains("engagement") || shape.IsNull("engagement"))value.engagement=null;
            if(!shape.Contains("previousNarrative") || shape.IsNull("previousNarrative"))value.previousNarrative=null;
            if(!shape.Contains("home") || shape.IsNull("home"))value.home=null;
            if(!shape.Contains("gardenLife") || shape.IsNull("gardenLife"))value.gardenLife=null;
            if(!shape.Contains("affection") || shape.IsNull("affection"))value.affection=null;
            GardenLifeCatalog.NormalizeOptionalFields(value.gardenLife);
            if(!shape.Contains("world") || shape.IsNull("world"))value.world=null;
            if(!shape.Contains("growth") || shape.IsNull("growth"))value.growth=null;
            NormalizeKinderOptionalFields(value.growth);
            string collectionJson=shape.Json("collection");
            if(value.collection!=null && (collectionJson==null || collectionJson=="null" || !FormalCampaignJsonShape.HasRootMember(collectionJson,"ooparts") || FormalCampaignJsonShape.RootMemberIsNull(collectionJson,"ooparts")))value.collection.ooparts=null;
            if(value.collection?.ooparts!=null){
                var inventory=value.collection.ooparts;
                foreach(var slots in new[]{inventory.slots}.Concat((inventory.presets??Array.Empty<OopartFormationPreset>()).Where(p=>p!=null).Select(p=>p.slots))){if(slots==null)continue;foreach(var slot in slots){if(slot==null)continue;if(slot.heroineFormId=="")slot.heroineFormId=null;if(slot.equippedOopartId=="")slot.equippedOopartId=null;}}
            }
            if(value.home?.allowEmptyFormationSlots==true && value.home.formationIds!=null)for(int i=0;i<value.home.formationIds.Length;i++)if(value.home.formationIds[i]=="")value.home.formationIds[i]=null;
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
            var value=new FormalCampaignHeader();JsonUtility.FromJsonOverwrite(text,value);var shape=FormalCampaignJsonShape.Inspect(text);
            if(!shape.Contains("world") || shape.IsNull("world"))value.world=null;
            if(!shape.Contains("growth") || shape.IsNull("growth"))value.growth=null;
            if(!shape.Contains("engagement") || shape.IsNull("engagement"))value.engagement=null;
            if(!shape.Contains("collection") || shape.IsNull("collection"))value.collection=null;
            if(!shape.Contains("home") || shape.IsNull("home"))value.home=null;return value;
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
            if(!formalDiagnostic && !plan8StoryTrial){
                try{InitializeSlotCampaign();}
                catch(Exception e)when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException){
                    saveSlotBlocked=true;BindFormalCampaign(NewSlotProgress());saveManagementOpen=true;saveManagementMessage="保存場所の初期化に失敗しました。元の記録を保持しています。空き容量・アクセス権を確認して再起動してください。";status=saveManagementMessage;Debug.LogWarning("SAVE_SLOT_INITIALIZATION_FAILED "+e.Message);
                }
                return;
            }
            FormalCampaignSave unified=null;
            var unifiedLoad=formalDiagnostic?FormalLoadResult.Missing:formalCampaignStore.Load(out unified);
            if(unifiedLoad==FormalLoadResult.Blocked || unifiedLoad==FormalLoadResult.RecoveredBackup){BeginSaveRecovery();return;}
            if(unifiedLoad==FormalLoadResult.Missing){
                FormalGrowthSave growth=null;CampaignSaveV2 world=null;
                var growthLoad=formalDiagnostic?FormalLoadResult.Missing:splitGrowthStore.Load(out growth);
                var worldLoad=formalDiagnostic?FormalLoadResult.Missing:CampaignSaveStore.LoadFormalSplit(out world);
                if(growthLoad==FormalLoadResult.Blocked || growthLoad==FormalLoadResult.RecoveredBackup || worldLoad==FormalLoadResult.Blocked){BeginSplitSaveRecovery();return;}
                if(growth==null)growth=new FormalGrowthSave {saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()};
                unified=new FormalCampaignSave {growth=growth,world=world??new CampaignState(WorldCatalog.ColossusIds).CreateSave()};
                if(ProductionStoryActive){unified=ProductionStoryMigration.Prepare(unified,combatDefinitions,ProductionStoryData(),s=>UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(s)));unified.revision=0;unified.collection=new FormalCollectionLedger{contentVersion=CollectionCatalog.ProductionVersion};}
                unified.Validate();
                if(!formalDiagnostic)SaveTrialObservedCampaign(unified);
            }
            BindFormalCampaign(unified);
        }
        private void BindFormalCampaign(FormalCampaignSave unified)
        {
            lifeSnapshotCached=null;gardenLifeRuntime?.Stop();gardenLifeRuntime=null;StopLifeAudio();
            if(ProductionStoryActive)unified=PrepareProductionCampaign(unified);
            if(ProductionStoryActive){WeaponGrowthRules.EnsureRoots(unified,HomeData());OopartSaveAdapter.Migrate(unified.collection,CollectionData(),unified.home?.formationIds.Length==5?unified.home.formationIds:combatDefinitions.FormationIds);AffectionSaveAdapter.Migrate(unified,HomeData());AffectionEventResolver.Refresh(unified,HomeData());AffectionSaveAdapter.ValidateContent(unified,HomeData());}
            unified.collection?.ValidateContent(CollectionData());
            unified.home?.ValidateContent(HomeData(),unified);
            unified.gardenLife?.ValidateContent(unified,HomeData());
            formalCampaign=new FormalCampaignJournal(unified,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode);
            campaign=new CampaignState(WorldCatalog.ColossusIds,unified.world);
            formalProgression=new FormalProgression(unified.growth,combatDefinitions.HeroineIds);
            book=CreateFormalBook();
            Debug.Log("FORMAL_CAMPAIGN_READY revision="+unified.revision+" growth="+unified.growth.revision);
            RuntimeTimingMark("campaign-ready");
            TrialObserve("save","loaded","revision="+unified.revision);
            TrialObserve("economy","opening-balance",$"nectar={unified.growth.nectar};crystals={unified.growth.awakeningCrystals};stones={unified.growth.stones}");
        }
        private bool SaveFormalGrowth(FormalGrowthSave next)
        {
            if(formalDiagnostic)return acceptanceStore!=null && formalCampaign.CommitGrowth(next,SaveDiagnosticCampaign,ProductionStoryActive?HomeData():null);
            return formalCampaign.CommitGrowth(next,SaveTrialObservedCampaign,ProductionStoryActive?HomeData():null);
        }
        private void DrawFormalGrowth() => DrawGrowthExperience();
    }
}
