using System;
using System.Linq;
using NewAster.Core;
namespace NewAster.Data
{
    public static class ProductionGardenLifeCatalog
    {
        public const string FurnitureAtlas="UI/garden-memorial-furniture-v1";
        public static void Apply(HomeExperienceCatalog home)
        {
            const string assetId="art.production.garden.memorial";
            home.assets=home.assets.Concat(new[]{new HomeAssetDef{id=assetId,kind="furniture",resourcePath=FurnitureAtlas,placeholder=true}}).ToArray();
            int[] costs={2,3,5,4,3,4,3,4,3,8};
            float[] heights={88,120,200,130,150,150,140,140,180,185},aspects={1.03623f,.85754f,.76238f,.96238f,.60128f,.58720f,.56954f,.56054f,.59121f,.55824f};
            home.furniture=home.furniture.Concat(GardenLifeCatalog.RecipeIds.Select((id,i)=>new HomeFurnitureLayout{id="furniture.memorial."+id,assetId=assetId,size01=new HomePoint{x=heights[i]*aspects[i]/1600,y=heights[i]/730},drawAnchor=new HomePoint{x=.5f,y=1},footprint=new HomeRect{x=.18f,y=.79f,width=.64f,height=.20f},orientationIds=GardenLifeCatalog.Orientations,supportedHeroineIds=home.heroineIds,costs=new[]{new HomeCost{resourceId=home.materials[i%home.materials.Length].id,amount=costs[i]}},slots=new[]{new HomeFurnitureSlot{id="slot.use",offset=new HomePoint{x=.95f,y=1},actionIds=new[]{"action.look"}}}})).ToArray();
            foreach(var f in home.furniture){
                f.orientationIds=(string[])GardenLifeCatalog.Orientations.Clone();
                f.lifeCategory=f.id=="furniture.fixture.2" || f.id.EndsWith(".obelisk") || f.id.EndsWith(".lantern")?"Decoration":f.id=="furniture.fixture.1" || f.id.EndsWith(".tools") || f.id.EndsWith(".planter") || f.id.EndsWith(".telescope")?"Activity":"Life";
                f.lifeInteractionTags=f.lifeCategory=="Decoration"?Array.Empty<string>():GardenLifeRuntime.FurnitureTags(f.id);f.lifeCovered=false;
            }
        }
        public sealed class GardenEventEntry
        { public string eventId,status; }
        // Read-only bridge for 11-3; old authored event IDs and rewards stay in Home/ADV.
        public static GardenEventEntry[] GetGardenEvents(FormalCampaignSave save,HomeExperienceCatalog home,string heroineId,string readingEventId=null)
        {return home.events.Where(e=>e.heroineId==heroineId).Select(e=>new GardenEventEntry{eventId=e.id,status=e.id==readingEventId?"reading":save.home.readEventIds.Contains(e.id)?"read":save.home.unlockedEventIds.Contains(e.id)?"available":"locked"}).ToArray();}
        public static string[] GetAvailableGardenEvents(FormalCampaignSave save,HomeExperienceCatalog home,string heroineId)
        {return home.events.Where(e=>e.heroineId==heroineId && save.home.unlockedEventIds.Contains(e.id)).Select(e=>e.id).ToArray();}
    }
}
