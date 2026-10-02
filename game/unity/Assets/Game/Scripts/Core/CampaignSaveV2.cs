using System;

namespace NewAster.Core
{
    /// <summary>JSON等へ直列化する保存契約。新しい項目は既存フィールドを削らずに追加する。</summary>
    [Serializable]
    public sealed class CampaignSaveV2
    {
        public const int Version = 2;
        public int version = Version;
        public int materials;
        public int terraformingExperience;
        public int[] heroineLevels = { 1, 1, 1, 1, 1 };
        public int[] heroineAwakenings = new int[5];
        public int[] weaponBranches = new int[15];
        public int[] affections = new int[5];
        public bool[] craftedFurniture = new bool[3];
        public int[] furnitureSlots = { -1, -1, -1 };
        public int highestBattleLevel = 1;
        public int kinderStones = 10;
        public int kinderDrawCount;
        public int kinderExchangeCount;
        public int[] heroineDuplicates = new int[5];
        public int[] heroineTraitRanks = new int[5];
        public int overflowEnhancementMaterials;
        public string[] claimedBattleIds = Array.Empty<string>();
        public string[] poemIds = Array.Empty<string>();
        public string[] unlockedStoryIds = Array.Empty<string>();
        public string[] readStoryIds = Array.Empty<string>();
        public string[] unlockedMilestoneIds = Array.Empty<string>();
        public string[] firstClearIds = Array.Empty<string>();
        public string[] appliedColossusIds = Array.Empty<string>();
        public string[] environmentTags = Array.Empty<string>();
        public string[] unlockedGardenIds = Array.Empty<string>();
    }
}
