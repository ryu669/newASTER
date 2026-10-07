using System;
using System.Linq;

namespace NewAster.Core
{
    public sealed class TerraformLevelDef
    {
        public int level, tpCost, requiredRecordTier, requiredDistinctColossi;
        public bool requiresIntegration;
    }
    public sealed class TerraformDomainDef
    {
        public string id, displayName;
        public TerraformLevelDef[] levels;
    }
    public sealed class WorldRecordDef
    {
        public string id, worldLineId, colossusId;
        public string[] domainIds;
        public int tier;
    }
    public sealed class DeepRecordDef
    {
        public string id, worldLineId, displayName;
        public string[] modifierTags;
    }
    public sealed class ExtremeDef
    {
        public string id, mainDomainId, fusionDomainId, displayName, presentationId;
    }
    public sealed class TerraformUnlockDef
    {
        public string domainId, gardenId, furnitureId;
        public int requiredLevel;
    }
    public static class TerraformCatalog
    {
        public static readonly TerraformDomainDef[] Domains = TerraformRules.DomainIds.Select((id, i) => new TerraformDomainDef {
            id = id, displayName = TerraformRules.DomainNames[i],
            levels = Enumerable.Range(0, 8).Select(level => new TerraformLevelDef {
                level = level, tpCost = TerraformRules.Costs[level], requiredRecordTier = level <= 5 ? level : 0,
                requiredDistinctColossi = level >= 4 && level <= 5 ? 2 : level >= 1 && level <= 3 ? 1 : 0,
                requiresIntegration = level >= 6
            }).ToArray()
        }).ToArray();
        public static readonly WorldRecordDef[] WorldRecords = TerraformRules.ColossusIds.SelectMany((id, i) => Enumerable.Range(1, 5).Select(tier => new WorldRecordDef {
            id = id + ".world-record." + tier, colossusId = id,
            worldLineId = TerraformRules.Worlds[i] == 0 ? "new-world" : "W0" + TerraformRules.Worlds[i],
            domainIds = (string[])TerraformRules.ColossusDomains[i].Clone(), tier = tier
        })).ToArray();
        private static readonly string[][] DeepTags = {
            new[] { "growth", "adaptation" }, new[] { "information", "living-matter" },
            new[] { "arcane", "natural-law" }, new[] { "recurrence", "seasons" },
            new[] { "immutable", "stasis" }, new[] { "self-rewriting", "metamorphosis" },
            new[] { "frozen", "suspended-cycle" }
        };
        public static readonly DeepRecordDef[] DeepRecords = TerraformRules.DeepIds.Select((id, i) => new DeepRecordDef {
            id = id, worldLineId = "W0" + (i + 1), displayName = TerraformRules.DeepNames[i], modifierTags = DeepTags[i]
        }).ToArray();
        public static readonly ExtremeDef[] Extremes = TerraformRules.DomainIds.SelectMany((id, i) => TerraformRules.Fusions[i].Select((fusion, j) => new ExtremeDef {
            id = id + "_" + fusion, mainDomainId = id, fusionDomainId = fusion,
            displayName = TerraformRules.ExtremeNames[i][j], presentationId = "terraform.extreme." + id + "_" + fusion
        })).ToArray();
        // Initial content correspondence: stable existing garden/furniture IDs; never replace owned content.
        public static readonly TerraformUnlockDef[] Unlocks = {
            new TerraformUnlockDef { domainId="life", requiredLevel=2, gardenId="garden.grassland-forest", furnitureId="furniture.production.planter" },
            new TerraformUnlockDef { domainId="earth", requiredLevel=2, gardenId="garden.crystal-highland", furnitureId="furniture.production.obelisk" },
            new TerraformUnlockDef { domainId="water", requiredLevel=2, gardenId="garden.flower-water", furnitureId="furniture.production.birdbath" },
            new TerraformUnlockDef { domainId="sky", requiredLevel=3, gardenId="garden.bamboo-waterfall", furnitureId="furniture.production.well" },
            new TerraformUnlockDef { domainId="night", requiredLevel=2, gardenId="garden.sakura-stargazing", furnitureId="furniture.production.telescope" },
            new TerraformUnlockDef { domainId="light", requiredLevel=2, gardenId="garden.oasis", furnitureId="furniture.production.brazier" },
            new TerraformUnlockDef { domainId="civilization", requiredLevel=2, gardenId="garden.hot-spring", furnitureId="furniture.production.lantern" },
            new TerraformUnlockDef { domainId="water", requiredLevel=4, gardenId="garden.snowfield" }
        };
        public static string DomainName(string id) => Domains.FirstOrDefault(d => d.id == id)?.displayName ?? id;
        public static string DeepName(string id) => DeepRecords.FirstOrDefault(d => d.id == id)?.displayName ?? id;
        public static string ExtremeName(string id) => Extremes.FirstOrDefault(d => d.id == id)?.displayName ?? id;
        public static string RecordRequirement(TerraformSave s, string domain, int level)
        {
            if (level > 5) return level == 6 ? "アステリア初討伐・使用可能な深層記述" : "アステリアLv50討伐・融合先の通常記述";
            int distinct = s.acquiredWorldRecords.Count(r => TerraformRules.Index(r.colossusId) >= 0 && TerraformRules.Index(r.colossusId) < 14 && TerraformRules.ColossusDomains[TerraformRules.Index(r.colossusId)].Contains(domain));
            string tier = new[] { "", "I", "II", "III", "IV", "V" }[level];
            return "対応記述 " + tier + (TerraformRules.HasRecord(s, domain, level) ? "：所持" : "：未取得") + (level >= 4 ? " ／ 異なる巨神獣 " + distinct + "/2体" : "");
        }
        public static string[] UnlockPreview(string domain, int level) => Unlocks.Where(u => u.domainId == domain && u.requiredLevel == level)
            .SelectMany(u => new[] { u.gardenId, u.furnitureId }).Where(id => id != null).ToArray();
    }
}
