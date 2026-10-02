using System.Collections.Generic;
using NewAster.Core;

namespace NewAster.Data
{
    /// <summary>箱庭は環境記述で解放する。素材量と家具レシピの数値は個別コンテンツ制作時に確定する。</summary>
    public static class GardenCatalog
    {
        public static readonly IReadOnlyList<GardenRequirement> Requirements = new[]
        {
            new GardenRequirement("garden.grassland-forest", new[] { "草原", "森林" }),
            new GardenRequirement("garden.crystal-highland", new[] { "鉱床", "結晶地帯", "高原" }),
            new GardenRequirement("garden.flower-water", new[] { "花園", "海", "湖" }),
            new GardenRequirement("garden.bamboo-waterfall", new[] { "竹林", "滝", "紅葉林" }),
            new GardenRequirement("garden.sakura-stargazing", new[] { "桜", "星空" }),
            new GardenRequirement("garden.oasis", new[] { "砂漠", "オアシス" }),
            new GardenRequirement("garden.hot-spring", new[] { "火山", "地熱", "温泉" }),
            new GardenRequirement("garden.snowfield", new[] { "雪原", "氷河", "雪山" }),
            new GardenRequirement("garden.integrated-world", new[] { "世界統合" }),
        };
    }
}
