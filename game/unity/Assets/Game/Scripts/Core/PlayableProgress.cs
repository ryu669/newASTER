using System;
using System.Linq;

namespace NewAster.Core
{
    public sealed class PlayableProgress
    {
        public int[] Levels { get; private set; }
        public int[] Branches { get; private set; }
        public int[] Affections { get; private set; }
        public bool[] Furniture { get; private set; }
        public int[] Slots { get; private set; }
        public int[] Duplicates { get; private set; }
        public int HighestLevel { get; private set; }
        public int KinderStones { get; private set; }
        public int KinderDrawCount { get; private set; }
        public int KinderExchangeCount { get; private set; }
        public int AvailableKinderExchanges => KinderDrawCount / KinderGardenBanner.ExchangeThreshold - KinderExchangeCount;
        private static int Clamp(int n, int min, int max) => Math.Max(min, Math.Min(max, n));
        public PlayableProgress(CampaignSaveV2 save = null)
        {
            Levels = Enumerable.Range(0, 5).Select(i => Clamp(Read(save?.heroineLevels, i, 1), 1, 50)).ToArray();
            Branches = Enumerable.Range(0, 15).Select(i => Clamp(Read(save?.weaponBranches, i, 0), 0, 3)).ToArray();
            Affections = Enumerable.Range(0, 5).Select(i => Clamp(Read(save?.affections, i, 0), 0, 100)).ToArray();
            Furniture = Enumerable.Range(0, 3).Select(i => save?.craftedFurniture != null && i < save.craftedFurniture.Length && save.craftedFurniture[i]).ToArray();
            Slots = Enumerable.Range(0, 3).Select(i => Clamp(Read(save?.furnitureSlots, i, -1), -1, 2)).ToArray();
            Duplicates = Enumerable.Range(0, 5).Select(i => Math.Max(0, Read(save?.heroineDuplicates, i, 0))).ToArray();
            for (int i = 0; i < Slots.Length; i++) if (Slots[i] >= 0 && !Furniture[Slots[i]]) Slots[i] = -1;
            HighestLevel = Clamp(save?.highestBattleLevel ?? 1, 1, 50);
            KinderStones = Math.Max(0, save == null ? 10 : save.kinderStones);
            KinderDrawCount = Math.Max(0, save?.kinderDrawCount ?? 0);
            KinderExchangeCount = Math.Max(0, save?.kinderExchangeCount ?? 0);
        }
        private static int Read(int[] values, int i, int fallback) => values != null && i < values.Length ? values[i] : fallback;
        public void CopyTo(CampaignSaveV2 save)
        {
            save.heroineLevels = (int[])Levels.Clone(); save.weaponBranches = (int[])Branches.Clone();
            save.affections = (int[])Affections.Clone(); save.craftedFurniture = (bool[])Furniture.Clone();
            save.furnitureSlots = (int[])Slots.Clone(); save.highestBattleLevel = HighestLevel;
            save.kinderStones = KinderStones; save.kinderDrawCount = KinderDrawCount; save.kinderExchangeCount = KinderExchangeCount;
            save.heroineDuplicates = (int[])Duplicates.Clone();
        }
        public bool Train(ProgressState wallet, int hero)
        {
            if (hero < 0 || hero >= 5 || Levels[hero] >= 50 || !wallet.TrySpendMaterials(2 + Levels[hero] / 5)) return false;
            Levels[hero]++; return true;
        }
        public bool Grow(ProgressState wallet, int hero, int branch)
        {
            if (hero < 0 || hero >= 5 || branch < 0 || branch >= 3) return false;
            int i = hero * 3 + branch;
            if (Branches[i] >= 3 || !wallet.TrySpendMaterials(3 + Branches[i] * 3)) return false;
            Branches[i]++; return true;
        }
        public bool Craft(ProgressState wallet, int furniture)
        {
            if (furniture < 0 || furniture >= 3 || Furniture[furniture] || !wallet.TrySpendMaterials(4 + furniture * 2)) return false;
            Furniture[furniture] = true; return true;
        }
        public bool Place(int furniture, int slot)
        {
            if (furniture < -1 || furniture > 2 || slot < 0 || slot > 2 || furniture >= 0 && !Furniture[furniture]) return false;
            if (furniture >= 0) for (int i = 0; i < Slots.Length; i++) if (Slots[i] == furniture) Slots[i] = -1;
            Slots[slot] = furniture; return true;
        }
        public bool Visit(ProgressState wallet, int hero)
        {
            if (hero < 0 || hero >= 5 || Affections[hero] >= 100 || !wallet.TrySpendMaterials(1)) return false;
            Affections[hero] = Math.Min(100, Affections[hero] + 5); return true;
        }
        public void RecordVictory(int level)
        {
            HighestLevel = Math.Max(HighestLevel, Math.Min(50, level + 5));
            KinderStones += 2 + Math.Max(0, level - 1) / 10;
        }
        public bool TryKinderDraw(decimal heroineRoll, decimal targetRoll, out bool heroine, out int rewardIndex)
        {
            heroine = false; rewardIndex = -1;
            if (heroineRoll < 0m || heroineRoll >= 1m || targetRoll < 0m || targetRoll >= 1m || KinderStones < 1) return false;
            KinderStones--; KinderDrawCount++;
            heroine = heroineRoll < KinderGardenBanner.HeroineTotalRate;
            rewardIndex = heroine ? Math.Min(4, (int)(targetRoll * 5m)) : Math.Min(3, (int)(targetRoll * 4m));
            if (heroine) Duplicates[rewardIndex]++;
            return true;
        }
        public bool TryKinderExchange(int heroineIndex)
        {
            if (heroineIndex < 0 || heroineIndex >= 5 || AvailableKinderExchanges <= 0) return false;
            KinderExchangeCount++; Duplicates[heroineIndex]++; return true;
        }
    }
}
