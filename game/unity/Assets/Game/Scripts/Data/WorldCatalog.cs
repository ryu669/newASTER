using System.Collections.Generic;
using NewAster.Core;

namespace NewAster.Data
{
    public static class WorldCatalog
    {
        public static readonly IReadOnlyList<ColossusDefinition> Colossi = new[]
        {
            new ColossusDefinition("colossus.green-return-dragon", "緑還竜", "W01", false, "草原", "森林"),
            new ColossusDefinition("colossus.red-crystal-tyrant", "赤晶暴竜", "W01", false, "岩山", "渓谷", "鉱物"),
            new ColossusDefinition("colossus.memory-crystal-dragon", "記晶機竜", "W02", false, "鉱床", "結晶地帯"),
            new ColossusDefinition("colossus.sky-tower-machine", "天塔機神", "W02", false, "高原", "大気循環", "安定気候"),
            new ColossusDefinition("colossus.crystal-rose-princess", "水晶薔薇姫", "W03", false, "花園", "肥沃な平原"),
            new ColossusDefinition("colossus.silver-sea-whale", "銀海鯨", "W03", false, "海", "湖", "雨"),
            new ColossusDefinition("colossus.heaven-tree-orochi", "天樹八岐", "W03", false, "山岳", "竹林", "滝", "紅葉林"),
            new ColossusDefinition("colossus.reenactment-yimir", "再演巨人ユミル", "W04", false, "穏やかな平地", "桜", "四季"),
            new ColossusDefinition("colossus.emerald-star-astal", "翠星アスタル", "W04", false, "夜空", "星空", "季節周期"),
            new ColossusDefinition("colossus.amber-king-serpent", "琥珀王蛇", "W05", false, "砂漠", "オアシス", "黄金色の大地"),
            new ColossusDefinition("colossus.white-divine-dragon-mother", "白神竜母", "W05", false, "温暖地域", "生命圏"),
            new ColossusDefinition("colossus.black-smoke-citadel", "黒煙城塞", "W06", false, "火山", "地熱", "温泉", "金属資源"),
            new ColossusDefinition("colossus.dead-king-megadeath", "死人王メガデス", "W06", false, "湿地", "夜の森"),
            new ColossusDefinition("colossus.final-flame-ice-phoenix", "終焔氷凰", "W07", false, "雪原", "氷河", "雪山"),
            new ColossusDefinition("colossus.newborn-asteria", "新生獣アステリア", "新天地", true, "世界統合"),
        };

        public static readonly IReadOnlyList<string> ColossusIds = new[]
        {
            "colossus.green-return-dragon", "colossus.red-crystal-tyrant", "colossus.memory-crystal-dragon",
            "colossus.sky-tower-machine", "colossus.crystal-rose-princess", "colossus.silver-sea-whale",
            "colossus.heaven-tree-orochi", "colossus.reenactment-yimir", "colossus.emerald-star-astal",
            "colossus.amber-king-serpent", "colossus.white-divine-dragon-mother", "colossus.black-smoke-citadel",
            "colossus.dead-king-megadeath", "colossus.final-flame-ice-phoenix", "colossus.newborn-asteria",
        };

        public static IReadOnlyDictionary<BookBookmark, IReadOnlyList<string>> BookSubjects =>
            new Dictionary<BookBookmark, IReadOnlyList<string>>
            {
                [BookBookmark.Colossi] = ColossusIds,
                [BookBookmark.Heroines] = new[] { "heroine.placeholder-01" },
                [BookBookmark.Gardens] = new[] { "garden.blank-star" },
                [BookBookmark.Stories] = new[] { "story.prologue" },
            };
    }
}
