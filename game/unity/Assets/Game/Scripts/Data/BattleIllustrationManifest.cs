using System;
using System.Linq;
namespace NewAster.Data
{
    [Serializable] public sealed class HeroIllustrationBinding
    { public string heroineId; public string resourcePath; public bool placeholder; public bool fullCanvas; }
    [Serializable] public sealed class PartIllustrationBinding
    { public string partId; public float x,y,width,height; public string resourcePath, destroyedResourcePath; public bool hideWhenDestroyed; public int drawOrder; }
    [Serializable] public sealed class BattleIllustrationManifest
    {
        public int schemaVersion;
        public bool placeholder;
        public string backgroundResourcePath, bodyResourcePath;
        public HeroIllustrationBinding[] heroes;
        public PartIllustrationBinding[] parts;
        public int HeroIndex(string heroineId)
        {
            return Array.FindIndex(heroes,h=>h.heroineId==heroineId);
        }
        public void Validate()
        {
            if(schemaVersion!=1 || heroes==null || heroes.Length!=5 || parts==null || parts.Length!=4)
                throw new ArgumentException("Illustration manifest requires version 1, five heroes and four parts.");
            if(heroes.Any(h=>h==null || string.IsNullOrWhiteSpace(h.heroineId) || (!h.placeholder && string.IsNullOrWhiteSpace(h.resourcePath))) ||
                heroes.Select(h=>h.heroineId).Distinct().Count()!=5) throw new ArgumentException("Invalid heroine bindings.");
            if(parts.Any(p=>p==null || string.IsNullOrWhiteSpace(p.partId) || !Valid(p.x,p.y,p.width,p.height)) ||
                parts.Select(p=>p.partId).Distinct().Count()!=4) throw new ArgumentException("Invalid part rectangles.");
            if(!placeholder && heroes.Any(h=>h.placeholder)) throw new ArgumentException("Final manifest cannot contain placeholder heroes.");
            if(!placeholder && (string.IsNullOrWhiteSpace(backgroundResourcePath) || string.IsNullOrWhiteSpace(bodyResourcePath) ||
                parts.Any(p=>string.IsNullOrWhiteSpace(p.resourcePath) || (!p.hideWhenDestroyed && string.IsNullOrWhiteSpace(p.destroyedResourcePath)))))
                throw new ArgumentException("Final manifest requires background, independent body/parts and explicit destruction visuals.");
        }
        // Every enemy layer occupies the same canvas; hit rectangles never resize the artwork.
        public static string PartResource(PartIllustrationBinding part,int hitPoints)
            => hitPoints>0?part.resourcePath:part.hideWhenDestroyed?null:
                string.IsNullOrWhiteSpace(part.destroyedResourcePath)?part.resourcePath:part.destroyedResourcePath;
        private static bool Valid(float x,float y,float w,float h)
            => !float.IsNaN(x+y+w+h) && !float.IsInfinity(x+y+w+h) && x>=0 && y>=0 && w>0 && h>0 && x+w<=1 && y+h<=1;
    }
}
