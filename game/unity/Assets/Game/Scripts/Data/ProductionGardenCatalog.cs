using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;

namespace NewAster.Data
{
    public static class ProductionGardenCatalog
    {
        public static readonly string[] WorldStyles={"forest","crystal","water","stars","oasis","spring","snow"};
        public static readonly string[] FurnitureStyles={"planter","obelisk","birdbath","telescope","well","lantern","brazier"};
        public static readonly string[] FurnitureNames={"森の苗床","結晶の観測柱","花と水の小鳥鉢","星見の望遠鏡","オアシスの井戸","湯けむりの灯籠","雪原の火鉢"};
        public static readonly string[] GardenNames={"草原と森林の庭","結晶高原の庭","花と水の庭","竹林と滝の庭","桜と星の庭","オアシスの庭","温泉の庭","雪原の庭","世界を結ぶ庭"};
        public static string FurnitureName(string id)
        {
            if(id=="furniture.fixture.0")return "花のベンチ";
            if(id=="furniture.fixture.1")return "薬草の作業台";
            if(id=="furniture.fixture.2")return "庭の噴水";
            int index=Array.FindIndex(FurnitureStyles,s=>id=="furniture.production."+s);
            return index<0?id:FurnitureNames[index];
        }
        public static string GardenName(string id)
        {int index=GardenCatalog.Requirements.ToList().FindIndex(g=>g.GardenId==id);return index<0?id:GardenNames[index];}
        public static void Apply(HomeExperienceCatalog home,CombatDefinitionCatalog combat,CollectionCatalog collection)
        {
            var assets=home.assets.ToList();
            Func<string,string,string> art=(kind,path)=>{
                string id="art.production.garden."+path.Replace('/','.');
                if(!assets.Any(a=>a.id==id))assets.Add(new HomeAssetDef{id=id,kind=kind,resourcePath=path,placeholder=true});return id;
            };
            string[] styles={"forest","crystal","water","water","stars","oasis","spring","snow","stars"};
            for(int i=0;i<home.gardens.Length;i++){
                var garden=home.gardens[i];string style=styles[i];
                string far=i==0?"forest-far-candidate-v1":i==3?"heaven-forest-far-candidate-v1":i==8?"world-confluence-far-candidate-v1":"world-"+style+"-far-candidate-v1";
                string front=style=="forest"?"forest-front-candidate-v1":"world-"+style+"-front-candidate-v1";
                garden.unmade=false;garden.schemaVersion=1;
                garden.backgroundAssetId=art("background","Illustrations/"+far);
                garden.middleAssetIds=new[]{art("foreground","Illustrations/world-"+style+"-mid-candidate-v1")};
                garden.foregroundAssetIds=new[]{art("foreground","Illustrations/"+front)};
                garden.zones=new[]{new HomeGardenZone{id="zone.ground",order=0,bounds=new HomeRect{x=0,y=0,width=1,height=1}}};
            }
            var furniture=home.furniture.ToList();
            for(int i=0;i<WorldStyles.Length;i++){
                var owner=WorldCatalog.Colossi.First(c=>c.WorldLineId=="W0"+(i+1));
                string material=collection.owners.Single(o=>o.id==owner.Id).materialIds[0];
                furniture.Add(new HomeFurnitureLayout{id="furniture.production."+FurnitureStyles[i],assetId=art("furniture","Illustrations/garden-"+FurnitureStyles[i]+"-candidate-v1"),
                    size01=new HomePoint{x=.19f,y=.24f},drawAnchor=new HomePoint{x=.5f,y=1},footprint=new HomeRect{x=.15f,y=.78f,width=.7f,height=.22f},
                    orientationIds=new[]{"orientation.default"},supportedHeroineIds=combat.FormationIds.ToArray(),costs=new[]{new HomeCost{resourceId=material,amount=4+i}},
                    slots=new[]{new HomeFurnitureSlot{id="slot.use",offset=new HomePoint{x=1,y=.95f},actionIds=new[]{"action.look"}}}});
            }
            home.furniture=furniture.ToArray();home.assets=assets.ToArray();
        }
    }
}
