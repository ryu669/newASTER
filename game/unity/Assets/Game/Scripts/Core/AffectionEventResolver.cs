using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class HeroineEventDef
    {
        public string id,personId,formId,category,scriptId,visibility="visible";
        public int requiredAffectionLevel,displayOrder;
    }
    public static class AffectionEventResolver
    {
        public static int Required(HomeCondition c)=>c.kind=="atLeast" && c.domain=="affection"?c.value:(c.items??Array.Empty<HomeCondition>()).Select(Required).DefaultIfEmpty(0).Max();
        public static HeroineEventDef[] Definitions(HomeExperienceCatalog c)=>c.affectionEvents!=null && c.affectionEvents.Length>0?c.affectionEvents:c.events.Select((e,i)=>new HeroineEventDef{id=e.id,personId=c.PersonId(e.heroineId),formId=e.heroineId,requiredAffectionLevel=Required(e.unlockCondition),category=e.kind,scriptId=e.sceneId,displayOrder=i}).ToArray();
        public static void Validate(HomeExperienceCatalog c)
        {
            if(c.events==null)return;
            var defs=Definitions(c);if(defs.Select(d=>d.id).Distinct().Count()!=defs.Length || defs.Any(d=>!HomeExperienceCatalog.Id(d.id) || !HomeExperienceCatalog.Id(d.personId) || d.requiredAffectionLevel<0 || d.requiredAffectionLevel>99 || d.requiredAffectionLevel>20 && c.events.Any(e=>e.id==d.id && e.rewards.Length>0) || !new[]{"visible","hint","hidden"}.Contains(d.visibility) || !c.events.Any(e=>e.id==d.id && e.sceneId==d.scriptId && c.PersonId(e.heroineId)==d.personId) || !string.IsNullOrEmpty(d.formId) && c.PersonId(d.formId)!=d.personId))throw new ArgumentException("Invalid affection event definition.");
        }
        public static HeroineEventDef[] ForPerson(FormalCampaignSave s,HomeExperienceCatalog c,string form)
        {var a=AffectionService.State(s,c,form);return Definitions(c).Where(e=>e.personId==a.personId && (string.IsNullOrEmpty(e.formId) || e.formId==form) && (e.visibility!="hidden" || a.level>=e.requiredAffectionLevel)).OrderBy(e=>e.displayOrder).ThenBy(e=>e.id,StringComparer.Ordinal).ToArray();}
        public static void Refresh(FormalCampaignSave s,HomeExperienceCatalog c)
        {
            AffectionSaveAdapter.Migrate(s,c);if(s.home==null)return;
            s.home.unlockedEventIds=s.home.unlockedEventIds.Union(Definitions(c).Where(e=>s.affection.states.Any(a=>a.personId==e.personId && a.level>=e.requiredAffectionLevel) && (string.IsNullOrEmpty(e.formId) || s.growth.heroines.Any(h=>h.heroineId==e.formId))).Select(e=>e.id)).ToArray();
        }
    }
}
