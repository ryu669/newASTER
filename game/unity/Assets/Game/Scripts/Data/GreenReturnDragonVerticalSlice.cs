using System.Collections.Generic;
using System.Linq;
using NewAster.Core;

namespace NewAster.Data
{
    /// <summary>緑還竜の縦切り用戦闘データ。個別物語本文・最終数値・行動表は正式制作で差し替える。</summary>
    public static class GreenReturnDragonVerticalSlice
    {
        public const string ColossusId = "colossus.green-return-dragon";
        public static readonly IReadOnlyList<BattlePart> Parts = new[]
        {
            new BattlePart("crystal-horn-crown", 25, "gauge-down"),
            new BattlePart("left-wing-root", 25, ""),
            new BattlePart("right-wing-root", 25, ""),
            new BattlePart("vine-wrapped-tail", 25, ""),
        };

        public static readonly IReadOnlyList<string> PoemIds = new[]
        {
            "green-return-dragon-c1-p1", "green-return-dragon-c1-p2", "green-return-dragon-c1-p3", "green-return-dragon-c1-p4",
            "green-return-dragon-c1-p5", "green-return-dragon-c1-p6", "green-return-dragon-c1-p7", "green-return-dragon-c1-p8",
            "green-return-dragon-c2-p1", "green-return-dragon-c2-p2", "green-return-dragon-c2-p3", "green-return-dragon-c2-p4",
            "green-return-dragon-c2-p5", "green-return-dragon-c2-p6", "green-return-dragon-c2-p7", "green-return-dragon-c2-p8",
            "green-return-dragon-c3-p1", "green-return-dragon-c3-p2", "green-return-dragon-c3-p3", "green-return-dragon-c3-p4",
            "green-return-dragon-c3-p5", "green-return-dragon-c3-p6", "green-return-dragon-c3-p7", "green-return-dragon-c3-p8",
        };

        public static readonly IReadOnlyList<StoryRequirement> StoryChapters = new[]
        {
            new StoryRequirement("story.green-return-dragon.chapter-1", PoemIds.Take(8)),
            new StoryRequirement("story.green-return-dragon.chapter-2", PoemIds.Skip(8).Take(8)),
            new StoryRequirement("story.green-return-dragon.chapter-3", PoemIds.Skip(16).Take(8)),
        };
    }
}
