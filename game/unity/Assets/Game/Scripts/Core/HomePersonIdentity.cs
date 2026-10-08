using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class HomePersonLink { public string heroineId,personId; }
    public sealed partial class HomeExperienceCatalog
    {
        public HomePersonLink[] personLinks=Array.Empty<HomePersonLink>();
        public string PersonId(string id)=>personLinks?.SingleOrDefault(p=>p.heroineId==id)?.personId??id;
        public void SharePersonProgress(FormalCampaignSave save)
        {
            if(save.home==null || save.affection!=null)return;
            var owned=save.growth.heroines.Select(g=>g.heroineId).Where(heroineIds.Contains).ToArray();
            foreach(var group in owned.GroupBy(PersonId).Where(g=>g.Count()>1)){
                int value=save.home.affections.Where(a=>group.Contains(a.heroineId)).Select(a=>a.value).DefaultIfEmpty(0).Max();
                if(value>0)foreach(var id in group){var entry=save.home.affections.SingleOrDefault(a=>a.heroineId==id);if(entry==null){entry=new HomeAffection{heroineId=id};save.home.affections=save.home.affections.Concat(new[]{entry}).ToArray();}entry.value=value;}
                if(group.Any(save.home.loverHeroineIds.Contains))save.home.loverHeroineIds=save.home.loverHeroineIds.Union(group).ToArray();
            }
        }
    }
}
