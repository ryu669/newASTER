using System.Linq;
using NewAster.Core;
namespace NewAster.Data
{
    public static class Plan12ExpressionAlignment
    {
        public static void Apply(HomeExperienceCatalog home)
        {
            // All expression sources use the standing canvas and head position.
            Map(home,"arcane",490,330,80,66,1024,1536);
            Map(home,"annihilator",480,250,91,65,1024,1536);
            Map(home,"annihilator-holy",452,219,104,80,1024,1536);
            Map(home,"r",568,402,100,78,1145,1374);
            Map(home,"arcane-academy",498,245,76,66,1024,1536);
            Map(home,"nighthawk",489,180,86,65,1024,1536);
            Map(home,"oriflamme",449,186,87,65,1024,1536);
            Map(home,"shangrila",449,337,81,62,1024,1536);
            Map(home,"shell",470,280,85,67,1024,1536);
            Map(home,"slayer-swim",444,196,100,71,1024,1536);
        }
        private static void Map(HomeExperienceCatalog home,string key,float x,float y,float w,float h,float canvasW,float canvasH)
        {
            var display=home.displays.SingleOrDefault(d=>d.heroineId=="heroine."+key);if(display==null)return;
            foreach(var variant in display.expressions.Where(v=>v.id!="expression.normal")){
                var asset=home.assets.Single(a=>a.id==variant.assetId);
                asset.fullFrame=false;asset.regionalOverlay=true;asset.mappedOverlay=true;
                asset.overlayRegion01=new HomeRect{x=x/canvasW,y=y/canvasH,width=w/canvasW,height=h/canvasH};
                asset.overlaySourceRegion01=new HomeRect{x=x/canvasW,y=y/canvasH,width=w/canvasW,height=h/canvasH};
            }
        }
    }
}
