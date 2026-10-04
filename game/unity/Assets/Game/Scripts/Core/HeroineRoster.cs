using System;
using System.Collections.Generic;
using System.Linq;
namespace NewAster.Core
{
    // Authoring roster is separate from the five actors selected for a battle.
    [Serializable] public sealed class HeroineRosterEntry
    {
        public string id, name, jobId, stage;
        public string[] sourceIds = Array.Empty<string>();
        public string[] unresolved = Array.Empty<string>();
        public bool originalStats, originalSkills;
    }
    public sealed class HeroineRoster
    {
        public const string Version = "heroine-roster-2026-10-05";
        private readonly Dictionary<string, HeroineRosterEntry> entries;
        private readonly HashSet<string> implementedJobs;
        public int Count => entries.Count;
        public HeroineRoster(IEnumerable<HeroineRosterEntry> source, IEnumerable<string> knownJobs, IEnumerable<string> readyJobs)
        {
            var jobs = new HashSet<string>(knownJobs ?? throw new ArgumentNullException(nameof(knownJobs)));
            implementedJobs = new HashSet<string>(readyJobs ?? throw new ArgumentNullException(nameof(readyJobs)));
            if(jobs.Count==0 || jobs.Any(j=>!CollectionCatalog.ValidId(j)) || !implementedJobs.IsSubsetOf(jobs)) throw new ArgumentException("Invalid roster jobs.");
            entries = new Dictionary<string, HeroineRosterEntry>(StringComparer.Ordinal);
            foreach(var e in source ?? throw new ArgumentNullException(nameof(source))) {
                if(e==null || !CollectionCatalog.ValidId(e.id) || string.IsNullOrWhiteSpace(e.name) || !jobs.Contains(e.jobId) || !new[]{"planned","authoring","validated","available"}.Contains(e.stage) || e.sourceIds==null || e.sourceIds.Any(s=>!CollectionCatalog.ValidId(s)) || e.sourceIds.Distinct().Count()!=e.sourceIds.Length || e.unresolved==null || e.unresolved.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Invalid heroine authoring entry.");
                if((e.stage=="validated" || e.stage=="available") && (!implementedJobs.Contains(e.jobId) || e.unresolved.Length>0 || e.sourceIds.Length==0 && (!e.originalStats || !e.originalSkills))) throw new ArgumentException("A completed heroine requires a ready job and resolved original or sourced rules.");
                if(entries.ContainsKey(e.id)) throw new ArgumentException("Duplicate stable heroine ID.");
                entries.Add(e.id, Copy(e));
            }
        }
        private static HeroineRosterEntry Copy(HeroineRosterEntry e) => new HeroineRosterEntry{id=e.id,name=e.name,jobId=e.jobId,stage=e.stage,sourceIds=(string[])e.sourceIds.Clone(),unresolved=(string[])e.unresolved.Clone(),originalStats=e.originalStats,originalSkills=e.originalSkills};
        public HeroineRosterEntry Get(string id) => Copy(entries.TryGetValue(id,out var e)?e:throw new ArgumentException("Unknown heroine ID."));
        public HeroineRosterEntry[] Page(int offset,int limit,string jobId=null,bool availableOnly=true,IEnumerable<string> ownedIds=null)
        {
            if(offset<0 || limit<1 || limit>100) throw new ArgumentOutOfRangeException();
            var owned=ownedIds==null?null:new HashSet<string>(ownedIds);
            if(owned!=null && owned.Any(id=>!entries.ContainsKey(id))) throw new ArgumentException("Unknown owned heroine ID.");
            return entries.Values.Where(e=>(jobId==null || e.jobId==jobId) && (!availableOnly || e.stage=="available") && (owned==null || owned.Contains(e.id))).OrderBy(e=>e.id,StringComparer.Ordinal).Skip(offset).Take(limit).Select(Copy).ToArray();
        }
        public string[] Formation(IEnumerable<string> selected,IEnumerable<string> ownedIds)
        {
            var ids=selected?.ToArray();var owned=new HashSet<string>(ownedIds??throw new ArgumentNullException(nameof(ownedIds)));
            if(ids==null || ids.Length!=5 || ids.Distinct().Count()!=5 || ids.Any(id=>id==null || !owned.Contains(id) || !entries.TryGetValue(id,out var e) || e.stage!="available" || !implementedJobs.Contains(e.jobId))) throw new ArgumentException("Formation requires five distinct owned available heroines.");
            return (string[])ids.Clone();
        }
    }
}
