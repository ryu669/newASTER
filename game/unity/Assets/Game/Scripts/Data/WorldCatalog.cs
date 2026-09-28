using System.Collections.Generic;
using NewAster.Core;

namespace NewAster.Data
{
    public static class WorldCatalog
    {
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
