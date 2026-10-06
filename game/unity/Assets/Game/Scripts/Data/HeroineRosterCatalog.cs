using System.Linq;
using NewAster.Core;
namespace NewAster.Data
{
    public static class HeroineRosterCatalog
    {
        public static readonly string[] PlannedJobs={"job.chaser","job.sniper","job.healer","job.artist","job.gambler","job.general","job.alchemist","job.panzer"};
        public static HeroineRoster InitialFive(CombatDefinitionCatalog combat)
        {
            combat.Validate();
            return new HeroineRoster(combat.heroines.Select(h=>new HeroineRosterEntry{id=h.id,name=h.name,jobId=h.jobId,stage="available",sourceIds=new[]{"reference.angelica."+h.id},originalStats=true,originalSkills=true}),combat.jobs.Select(j=>j.id).Concat(PlannedJobs).Distinct(),combat.jobs.Select(j=>j.id));
        }
    }
}
