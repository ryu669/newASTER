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
