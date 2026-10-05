using System;
using System.Linq;
namespace NewAster.Data
{
    [Serializable] public sealed class HeroIllustrationBinding
    { public string heroineId; public string resourcePath,attackResourcePath,hitResourcePath,cutinResourcePath; public bool placeholder; public bool fullCanvas; }
    [Serializable] public sealed class PartIllustrationBinding
    { public string partId; public float x,y,width,height; public string resourcePath, destroyedResourcePath; public bool hideWhenDestroyed; public int drawOrder,inputPriority; public IllustrationLayerPlacement placement; }
    [Serializable] public sealed class IllustrationLayerPlacement
    { public bool enabled; public float x,y,scale; }
    [Serializable] public sealed class BattleIllustrationManifest
    {
        public int schemaVersion;
        public bool placeholder;
        public string backgroundResourcePath, bodyResourcePath;
        public string middleResourcePath,foregroundResourcePath,enemyMajorResourcePath;
        public IllustrationLayerPlacement bodyPlacement;
        public HeroIllustrationBinding[] heroes;
        public PartIllustrationBinding[] parts;
        public int HeroIndex(string heroineId)
        {
            return Array.FindIndex(heroes,h=>h.heroineId==heroineId);
        }
        public void Validate()
        {
            if(schemaVersion!=1 || heroes==null || heroes.Length!=5 || parts==null || parts.Length<4 || parts.Length>6)
                throw new ArgumentException("Illustration manifest requires version 1, five heroes and four to six parts.");
            if(heroes.Any(h=>h==null || string.IsNullOrWhiteSpace(h.heroineId) || (!h.placeholder && string.IsNullOrWhiteSpace(h.resourcePath))) ||
                heroes.Select(h=>h.heroineId).Distinct().Count()!=5) throw new ArgumentException("Invalid heroine bindings.");
            if(parts.Any(p=>p==null || string.IsNullOrWhiteSpace(p.partId) || !Valid(p.x,p.y,p.width,p.height)) ||
                parts.Select(p=>p.partId).Distinct().Count()!=parts.Length) throw new ArgumentException("Invalid part rectangles.");
            if(bodyPlacement!=null && bodyPlacement.enabled && !Valid(bodyPlacement.x,bodyPlacement.y,bodyPlacement.scale,bodyPlacement.scale))throw new ArgumentException("Invalid enemy body placement.");
            if(parts.Any(p=>p.placement!=null && p.placement.enabled && !Valid(p.placement.x,p.placement.y,p.placement.scale,p.placement.scale)))throw new ArgumentException("Invalid enemy layer placement.");
            if(!placeholder && heroes.Any(h=>h.placeholder)) throw new ArgumentException("Final manifest cannot contain placeholder heroes.");
            if(!placeholder && (string.IsNullOrWhiteSpace(backgroundResourcePath) || string.IsNullOrWhiteSpace(bodyResourcePath) ||
                parts.Any(p=>string.IsNullOrWhiteSpace(p.resourcePath) || (!p.hideWhenDestroyed && string.IsNullOrWhiteSpace(p.destroyedResourcePath)))))
                throw new ArgumentException("Final manifest requires background, independent body/parts and explicit destruction visuals.");
        }
        // Native enemy layers share a canvas. Optional uniform placement is independent of hit rectangles.
        public static string PartResource(PartIllustrationBinding part,int hitPoints)
            => hitPoints>0?part.resourcePath:part.hideWhenDestroyed?null:
                string.IsNullOrWhiteSpace(part.destroyedResourcePath)?part.resourcePath:part.destroyedResourcePath;
        public string HitPart(float x,float y,Func<string,bool> alive)
            => parts.Where(p=>alive(p.partId) && x>=p.x && x<=p.x+p.width && y>=p.y && y<=p.y+p.height)
                .OrderByDescending(p=>p.inputPriority).ThenBy(p=>p.partId,StringComparer.Ordinal).Select(p=>p.partId).FirstOrDefault();
        private static bool Valid(float x,float y,float w,float h)
            => !float.IsNaN(x+y+w+h) && !float.IsInfinity(x+y+w+h) && x>=0 && y>=0 && w>0 && h>0 && x+w<=1 && y+h<=1;
    }
}
