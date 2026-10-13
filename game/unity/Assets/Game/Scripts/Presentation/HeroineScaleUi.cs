using System;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private HeroineUiIndex heroineUiIndex;
        private CombatDefinitionCatalog heroineUiCatalog;
        private FormalGrowthSave heroineUiGrowth;
        private HeroineUiIndex HeroineIndex()
        {
            var growth=GrowthSnapshot();
            if(heroineUiIndex==null || !ReferenceEquals(heroineUiCatalog,combatDefinitions) || !ReferenceEquals(heroineUiGrowth,growth)){
                heroineUiIndex=new HeroineUiIndex(combatDefinitions,growth);heroineUiCatalog=combatDefinitions;heroineUiGrowth=growth;
            }
            return heroineUiIndex;
        }
        private void DrawPageEnds(float x,float y,float width,ref int page,int pages,bool interactive)
        {
            if(GrowthButton(x,y,70,40,"先頭",interactive && page>0))page=0;
            if(GrowthButton(x+width-70,y,70,40,"末尾",interactive && page<pages-1))page=pages-1;
        }
    }
}
