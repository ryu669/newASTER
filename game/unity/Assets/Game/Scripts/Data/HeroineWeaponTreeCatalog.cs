using System;
using System.Linq;
using NewAster.Core;
namespace NewAster.Data
{
    [Serializable] public sealed class HeroineWeaponTreeCatalog
    {
        public int schemaVersion;
        public HeroineWeaponTreeDef[] entries;
        public void Validate()
        {
            if(schemaVersion!=1 || entries==null || entries.Length==0 || entries.Any(e=>e==null) || entries.Select(e=>e.heroineId).Distinct().Count()!=entries.Length)throw new ArgumentException("Invalid weapon tree catalog.");
            foreach(var e in entries){
                if(string.IsNullOrEmpty(e.heroineId) || string.IsNullOrEmpty(e.resourcePath) || !e.resourcePath.StartsWith("Illustrations/heroine-weapon-tree-",StringComparison.Ordinal) || e.resourcePath.Contains("..") || e.nodes==null || e.nodes.Length!=13 || e.nodes.Any(n=>n==null || string.IsNullOrEmpty(n.nodeId) || n.position==null || float.IsNaN(n.position.x) || float.IsNaN(n.position.y) || n.position.x<.04f || n.position.x>.96f || n.position.y<.04f || n.position.y>.94f) || e.nodes.Select(n=>n.nodeId).Distinct().Count()!=13)throw new ArgumentException("Invalid weapon tree image or anchors: "+e.heroineId);
            }
        }
        public HeroineWeaponTreeDef Entry(string hero)=>entries.SingleOrDefault(e=>e.heroineId==hero)??throw new ArgumentException("Missing heroine weapon tree: "+hero);
    }
    [Serializable] public sealed class HeroineWeaponTreeDef
    {
        public string heroineId,resourcePath;
        public HeroineWeaponTreeAnchor[] nodes;
        public HomePoint Position(string id)=>nodes.SingleOrDefault(n=>n.nodeId==id)?.position??throw new ArgumentException("Missing tree branch anchor: "+id);
    }
    [Serializable] public sealed class HeroineWeaponTreeAnchor {public string nodeId;public HomePoint position;}
}
