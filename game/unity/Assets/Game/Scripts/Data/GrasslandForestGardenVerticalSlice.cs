using System.Collections.Generic;
using NewAster.Core;

namespace NewAster.Data
{
    /// <summary>緑還竜の初回討伐後に使える草原・森林箱庭の縦切りレシピ。</summary>
    public static class GrasslandForestGardenVerticalSlice
    {
        public const string GardenId = "garden.grassland-forest";
        public static readonly IReadOnlyList<FurnitureRecipe> Recipes = new[]
        {
            new FurnitureRecipe("furniture.root-bench", GardenId, new Dictionary<string, int> { ["forest-fiber"] = 2 }),
            new FurnitureRecipe("furniture.moss-lantern", GardenId, new Dictionary<string, int> { ["forest-fiber"] = 1, ["green-crystal"] = 1 }),
        };
    }
}
