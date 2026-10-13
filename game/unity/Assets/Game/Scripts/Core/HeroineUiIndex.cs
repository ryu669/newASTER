using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace NewAster.Core
{
    // Read-only view of one catalog/growth revision. No portrait or battle creation during filtering.
    public sealed class HeroineUiIndex
    {
        private readonly Dictionary<string,HeroineCombatDef> definitions;
        private readonly Dictionary<string,FormalHeroineGrowth> owned;
        private readonly Dictionary<string,string> names;
        public HeroineUiIndex(CombatDefinitionCatalog catalog,FormalGrowthSave growth)
        {
            definitions=catalog.heroines.ToDictionary(h=>h.id,StringComparer.Ordinal);
            owned=growth.heroines.ToDictionary(h=>h.heroineId,StringComparer.Ordinal);
            names=definitions.ToDictionary(p=>p.Key,p=>Normalize(p.Value.name),StringComparer.Ordinal);
            PersonCount=owned.Keys.Select(id=>string.IsNullOrEmpty(definitions[id].personId)?id:definitions[id].personId).Distinct().Count();
        }
        public int PersonCount { get; }
        public bool Owns(string id)=>owned.ContainsKey(id);
        public FormalHeroineGrowth Growth(string id)=>owned[id];
        public HeroineCombatDef Definition(string id)=>definitions[id];
        public static string Normalize(string value)=>(value??"").Normalize(NormalizationForm.FormKC).Trim().ToUpperInvariant();
        public string[] Search(string query,string job=null,bool ownedOnly=false,int order=0,IEnumerable<string> source=null)
        {
            string text=Normalize(query);
            var found=(source??definitions.Keys).Where(id=>definitions.ContainsKey(id) && (!ownedOnly || Owns(id)) && (job==null || definitions[id].jobId==job) && names[id].Contains(text));
            return (order==1?found.OrderByDescending(id=>Owns(id)?owned[id].level:0).ThenBy(id=>names[id],StringComparer.Ordinal):found.OrderBy(id=>names[id],StringComparer.Ordinal)).ThenBy(id=>id,StringComparer.Ordinal).ToArray();
        }
        public static int FirstVisible(float scroll,int rowHeight,int count)=>Math.Min(count,Math.Max(0,(int)(scroll/rowHeight)));
        public static int VisibleEnd(int first,int rowHeight,int height,int count)=>Math.Min(count,first+(height+rowHeight-1)/rowHeight+1);
    }
}
