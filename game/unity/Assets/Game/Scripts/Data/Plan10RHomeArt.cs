using System.Linq;
using NewAster.Core;
namespace NewAster.Data
{
    public static class Plan10RHomeArt
    {
        public static void Apply(HomeExperienceCatalog home)
        {
            var display=home.displays.SingleOrDefault(d=>d.heroineId=="heroine.r");if(display==null)return;
            var assets=home.assets.ToList();
            string standing="art.plan10.r.standing";
            assets.Add(new HomeAssetDef{id=standing,kind="standing",resourcePath="Illustrations/r-standing-candidate-v1",fullFrame=true,placeholder=true});
            display.standingAssetId=standing;display.usePortraitCrop=true;display.portraitCrop01=new HomeRect{x=0,y=0,width=1,height=.57f};
            display.expressions=new[]{"normal","joy","puzzled","determined"}.Select(e=>{
                string id="art.plan10.r.expression."+e;
                assets.Add(new HomeAssetDef{id=id,kind="expression",resourcePath="Illustrations/r-"+(e=="normal"?"standing":"expression-"+e)+"-candidate-v1",fullFrame=true,placeholder=true});
                return new HomeDisplayVariant{id="expression."+e,assetId=id};
            }).ToArray();
            string pose="art.plan10.r.pose.idle";
            assets.Add(new HomeAssetDef{id=pose,kind="pose",resourcePath="Illustrations/r-standing-candidate-v1",fullFrame=true,placeholder=true});
            display.poses=new[]{new HomeDisplayVariant{id="pose.idle",assetId=pose}};
            home.assets=assets.ToArray();
        }
    }
}
