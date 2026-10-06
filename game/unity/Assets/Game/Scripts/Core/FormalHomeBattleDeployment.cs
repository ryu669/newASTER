using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class HomeSniperSupport {public string heroineId,targetId;}
    [Serializable] public sealed class HomeBattleRole {public string heroineId,jobId;}
    public sealed partial class HomeExperienceCatalog
    {
        public HomeBattleRole[] battleRoles=Array.Empty<HomeBattleRole>();
        public string BattleJob(string id)=>battleRoles?.SingleOrDefault(r=>r.heroineId==id)?.jobId;
    }
    public sealed partial class FormalHomeProgress
    {
        public string commanderHeroineId;
        public HomeSniperSupport[] sniperSupports=Array.Empty<HomeSniperSupport>();
        public BattleDeployment Deployment(string[] formation)
        {
            string commander=formation.Contains(commanderHeroineId)?commanderHeroineId:null;
            return new BattleDeployment(commander,(sniperSupports??Array.Empty<HomeSniperSupport>()).Where(s=>formation.Contains(s.heroineId) && formation.Contains(s.targetId)).ToArray());
        }
        private void ValidateBattleDeploymentStructure()
        {
            if(!string.IsNullOrEmpty(commanderHeroineId) && !HomeExperienceCatalog.Id(commanderHeroineId))throw new ArgumentException("Invalid commander identity.");
            var entries=sniperSupports??Array.Empty<HomeSniperSupport>();
            if(entries.Any(s=>s==null || !HomeExperienceCatalog.Id(s.heroineId) || !HomeExperienceCatalog.Id(s.targetId) || s.heroineId==s.targetId) || entries.Select(s=>s.heroineId).Distinct().Count()!=entries.Length)throw new ArgumentException("Invalid sniper support identity.");
        }
        private void ValidateBattleDeploymentContent(HomeExperienceCatalog catalog,string[] owned)
        {
            if(!string.IsNullOrEmpty(commanderHeroineId) && (!owned.Contains(commanderHeroineId) || catalog.BattleJob(commanderHeroineId)!="job.general"))throw new ArgumentException("Unowned or invalid commander.");
            foreach(var s in sniperSupports??Array.Empty<HomeSniperSupport>())if(!owned.Contains(s.heroineId) || !owned.Contains(s.targetId) || catalog.BattleJob(s.heroineId)!="job.sniper")throw new ArgumentException("Unowned or invalid sniper support.");
        }
    }
}
