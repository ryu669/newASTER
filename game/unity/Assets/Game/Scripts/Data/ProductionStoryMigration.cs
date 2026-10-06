using System;
using System.Linq;
using NewAster.Core;

namespace NewAster.Data
{
    public static class ProductionStoryMigration
    {
        public static bool Required(FormalCampaignSave save)=>save.home==null || (save.home.contentVersion==HomeExperienceCatalog.FixtureVersion || save.home.contentVersion==HomeExperienceCatalog.CandidateVersion || save.home.contentVersion==HomeExperienceCatalog.PreviousProductionVersion) || save.collection!=null && (save.collection.contentVersion==CollectionCatalog.FixtureVersion || save.collection.contentVersion==CollectionCatalog.CandidateVersion);
        public static FormalCampaignSave Prepare(FormalCampaignSave original,CombatDefinitionCatalog combat,ProductionStoryContent story,Func<FormalCampaignSave,FormalCampaignSave> clone)
        {
            original.Validate();var collection=ProductionStoryCatalog.Collection(combat,story);var home=ProductionStoryCatalog.Home(combat,story);
            if(original.home!=null && original.home.contentVersion!=HomeExperienceCatalog.FixtureVersion && original.home.contentVersion!=HomeExperienceCatalog.ProductionVersion && original.home.contentVersion!=HomeExperienceCatalog.PreviousProductionVersion && original.home.contentVersion!=HomeExperienceCatalog.CandidateVersion || original.collection!=null && original.collection.contentVersion!=CollectionCatalog.FixtureVersion && original.collection.contentVersion!=CollectionCatalog.ProductionVersion && original.collection.contentVersion!=CollectionCatalog.CandidateVersion)
                throw new ArgumentException("Trial or future saves cannot migrate into the normal production profile.");
            var next=clone(original);if(next==null || ReferenceEquals(next,original) || ReferenceEquals(next.world,original.world) || original.home!=null && ReferenceEquals(next.home,original.home))throw new ArgumentException("Migration requires an independent campaign clone.");
            next.Validate();if(!Required(original)){next.collection?.ValidateContent(collection);next.home.ValidateContent(home,next);return next;}
            if(original.collection?.contentVersion==CollectionCatalog.FixtureVersion)original.collection.ValidateContent(CollectionContractFixture.Create(combat));
            if(original.home?.contentVersion==HomeExperienceCatalog.FixtureVersion)original.home.ValidateContent(HomeExperienceFixture.Create(combat),original);
            bool narrative=next.home==null || next.home.contentVersion==HomeExperienceCatalog.FixtureVersion;
            if(narrative){
                if(next.previousNarrative!=null)throw new ArgumentException("Prior narrative archive cannot be overwritten.");
                var prior=next.home;
                next.previousNarrative=new ProductionNarrativeArchive{
                    homeVersion=prior?.contentVersion??"none",readStoryIds=next.world.readStoryIds.ToArray(),
                    unlockedEventIds=prior?.unlockedEventIds.ToArray()??Array.Empty<string>(),readEventIds=prior?.readEventIds.ToArray()??Array.Empty<string>(),
                    loverHeroineIds=prior?.loverHeroineIds.ToArray()??Array.Empty<string>(),claimedRewardIds=prior?.claimedRewardIds.ToArray()??Array.Empty<string>(),
                    readLineKeys=prior?.readLineKeys.Select(l=>new HomeReadLine{sceneId=l.sceneId,lineId=l.lineId,scriptVersion=l.scriptVersion}).ToArray()??Array.Empty<HomeReadLine>()};
                next.world.readStoryIds=next.world.readStoryIds.Except(story.chapters.Select(c=>c.id)).ToArray();
                next.home=prior??FormalHomeProgress.Empty(home.contentVersion);next.home.contentVersion=home.contentVersion;
                next.home.readLineKeys=Array.Empty<HomeReadLine>();next.home.readEventIds=Array.Empty<string>();next.home.unlockedEventIds=Array.Empty<string>();next.home.loverHeroineIds=Array.Empty<string>();next.home.claimedRewardIds=Array.Empty<string>();
            }
            next.home.contentVersion=home.contentVersion;
            if(next.collection!=null)next.collection.contentVersion=collection.contentVersion;
            if(next.collection!=null)foreach(var relic in next.collection.relics)relic.contentVersion=collection.contentVersion;
            HomeConditions.Refresh(next,home);next.revision=checked(next.revision+1);
            next.Validate();next.collection?.ValidateContent(collection);next.home.ValidateContent(home,next);return next;
        }
    }
}
