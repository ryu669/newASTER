using System;
using System.Linq;
namespace NewAster.Core
{
    public static class HomeConditions
    {
        public static bool Evaluate(HomeCondition e,FormalCampaignSave s)
        {
            if(e==null)throw new ArgumentException("Undefined condition.");
            if(e.kind=="always")return true;
            if(e.kind=="all" || e.kind=="any"){if(e.items==null || e.items.Length==0)throw new ArgumentException("Empty condition.");return e.kind=="all"?e.items.All(x=>Evaluate(x,s)):e.items.Any(x=>Evaluate(x,s));}
            if(e.kind=="flag")switch(e.domain){case "poemOwned":return s.world.poemIds.Contains(e.id);case "storyUnlocked":return s.world.unlockedStoryIds.Contains(e.id);case "storyRead":return s.world.readStoryIds.Contains(e.id);case "eventRead":return s.home?.readEventIds.Contains(e.id)??false;case "heroineOwned":return s.growth.heroines.Any(h=>h.heroineId==e.id);case "lover":return s.home?.loverHeroineIds.Contains(e.id)??false;}
            if(e.kind=="atLeast" && e.value>=0)switch(e.domain){case "affection":if(!HomeExperienceCatalog.Id(e.ownerId))throw new ArgumentException("Missing owner.");return (s.home?.affections.SingleOrDefault(a=>a.heroineId==e.ownerId)?.value??0)>=e.value;case "terraformingXp":return s.world.terraformingExperience>=e.value;case "highestClearedLevel":if(!HomeExperienceCatalog.Id(e.ownerId))throw new ArgumentException("Missing owner.");return (s.collection?.receipts.Where(r=>r.reason==BattleEndReason.Victory && r.battle.colossusId==e.ownerId).Select(r=>r.battle.level).DefaultIfEmpty(0).Max()??0)>=e.value;}
            throw new ArgumentException("Unknown condition.");
        }
        public static void Refresh(FormalCampaignSave s,HomeExperienceCatalog c)
        {
            s.world.unlockedStoryIds=s.world.unlockedStoryIds.Union(c.chapters.Where(ch=>ch.requiredPoemIds.All(s.world.poemIds.Contains)).Select(ch=>ch.id)).ToArray();
            if(s.home!=null)s.home.unlockedEventIds=s.home.unlockedEventIds.Union(c.events.Where(e=>s.growth.heroines.Any(h=>h.heroineId==e.heroineId) && Evaluate(e.unlockCondition,s)).Select(e=>e.id)).ToArray();
        }
    }
}
