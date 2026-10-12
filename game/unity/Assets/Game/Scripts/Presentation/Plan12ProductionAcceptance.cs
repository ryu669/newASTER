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
        private void PreparePlan12ProductionCapture(string[] args)
        {
            if(!args.Contains("-captureProductionRoster") || capturePath==null)return;
            bool weaponMaster=combatDefinitions.HeroineIds.Contains("heroine.weapon-master");
            bool vindicator=combatDefinitions.HeroineIds.Contains("heroine.vindicator");
            bool viper=combatDefinitions.HeroineIds.Contains("heroine.viper");
            bool invisible=combatDefinitions.HeroineIds.Contains("heroine.invisible");
            var additions=weaponMaster?new[]{"heroine.weapon-master"}:vindicator?new[]{"heroine.vindicator"}:viper?new[]{"heroine.viper"}:invisible?new[]{"heroine.invisible"}:new[]{"heroine.aeon","heroine.iron-maiden","heroine.avalanche","heroine.anathema","heroine.shiva"};
            AcceptanceCheck(combatDefinitions.HeroineIds.Length==(weaponMaster?24:vindicator?23:viper?22:invisible?21:20) && additions.All(combatDefinitions.HeroineIds.Contains),"Default production catalog contains all twenty forms");
            AcceptanceCheck(ProductionStoryActive && ProductionStoryData().chapters.Count(c=>additions.Contains(c.ownerId))==additions.Length*3,"Default production story contains all five new people");
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"production-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var oldCombat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>(weaponMaster?"Combat/battle-plan12-vindicator":vindicator?"Combat/battle-plan12-viper":viper?"Combat/battle-plan12-invisible":invisible?"Combat/battle-plan12-first-batch":"Combat/battle-plan11-7").text);
            var oldStory=JsonUtility.FromJson<ProductionStoryContent>(Resources.Load<TextAsset>(weaponMaster?"Story/plan12-vindicator-story-content":vindicator?"Story/plan12-viper-story-content":viper?"Story/plan12-invisible-story-content":invisible?"Story/plan12-first-batch-story-content":"Story/plan10-shangrila-story-content").text);
            var oldHome=ProductionStoryCatalog.Home(oldCombat,oldStory);
            var oldSave=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
            oldSave.growth.heroines=oldCombat.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=23,duplicateRank=2,skillLevels=new[]{4,5,6}}).ToArray();
            oldSave.growth.nectar=5432;oldSave.growth.stones=3210;oldSave.home=FormalHomeProgress.Empty(oldHome.contentVersion);oldSave.home.formationIds=oldCombat.FormationIds;
            HomeConditions.Refresh(oldSave,oldHome);AffectionSaveAdapter.Migrate(oldSave,oldHome);oldSave.Validate();
            AcceptanceCheck(acceptanceStore.Save(oldSave),"Fifteen-form save writes");
            AcceptanceCheck(acceptanceStore.Load(out var loaded)==FormalLoadResult.Loaded,"Fifteen-form save physically reloads");
            var before=UnityFormalCampaignJson.Encode(loaded);
            var migrated=ProductionStoryMigration.Prepare(loaded,combatDefinitions,ProductionStoryData(),s=>UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(s)));
            AcceptanceCheck(before==UnityFormalCampaignJson.Encode(migrated),"Fifteen-to-twenty expansion preserves every existing save field");
            BindFormalCampaign(migrated);
            foreach(var id in additions){RecruitmentCapture(id);AcceptanceCheck(RecruitExpansionForm(id)==GrowthCommitResult.AlreadyCommitted,"Production join cannot grant duplicate mastery");}
            AcceptanceCheck(acceptanceStore.Load(out var joined)==FormalLoadResult.Loaded,"All five joins survive save reload");BindFormalCampaign(joined);
            AcceptanceCheck(joined.growth.heroines.Length==(weaponMaster?24:vindicator?23:viper?22:invisible?21:20) && joined.growth.nectar==5432 && joined.growth.stones==3210,"New joins preserve the old wallet and roster");
            foreach(var id in additions)AcceptanceCheck(joined.growth.heroines.Single(h=>h.heroineId==id).level==1 && joined.home.weaponNodeIds.Contains(id+".weapon.root"),"Each new person starts with her own root");
            var banner=ProductionEconomyCatalog.Kinder(combatDefinitions.HeroineIds);banner.Validate(combatDefinitions.HeroineIds);
            foreach(var id in additions){int roll=0;AcceptanceCheck(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,id)).heroineId==id,"Each addition is summonable");}
            title=false;encounter=null;book.ChangeBookmark(BookBookmark.Heroines);book.CompleteTransition();heroineRosterOpen=true;
            Debug.Log("PLAN12_PRODUCTION_PLAYER_PASS defaultCatalog="+combatDefinitions.HeroineIds.Length+" people="+combatDefinitions.HeroineIds.Select(combatDefinitions.PersonId).Distinct().Count()+" oldSave="+oldCombat.HeroineIds.Length+" joins="+additions.Length+" summonable="+additions.Length+" wallet=preserved resolution=default");
        }
    }
}
