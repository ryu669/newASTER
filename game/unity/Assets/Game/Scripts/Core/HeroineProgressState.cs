using System;

namespace NewAster.Core
{
    public enum AwakeningStage { Base = 0, First = 1, Second = 2 }

    /// <summary>全ヒロイン共通の★6育成状態。数値成長・覚醒素材量は定義データから別途与える。</summary>
    public sealed class HeroineProgressState
    {
        public const int BaseLevelCap = 50;
        public const int FirstAwakeningLevelCap = 80;
        public const int SecondAwakeningLevelCap = 120;
        public const int BaseRarity = 6;

        public string HeroineId { get; }
        public int Rarity => BaseRarity;
        public int Level { get; private set; } = 1;
        public AwakeningStage Awakening { get; private set; }
        public int TraitRank { get; private set; }
        public int LevelCap => Awakening == AwakeningStage.Base ? BaseLevelCap
            : Awakening == AwakeningStage.First ? FirstAwakeningLevelCap : SecondAwakeningLevelCap;

        public HeroineProgressState(string heroineId)
        {
            if (string.IsNullOrWhiteSpace(heroineId)) throw new ArgumentException("A heroine id is required.", nameof(heroineId));
            HeroineId = heroineId;
        }

        /// <returns>実際に上がったLv。上限を超える分は失わず呼び出し側で経験値として保持する。</returns>
        public int RaiseLevel(int requestedLevels)
        {
            if (requestedLevels < 0) throw new ArgumentOutOfRangeException(nameof(requestedLevels));
            var raised = Math.Min(requestedLevels, LevelCap - Level);
            Level += raised;
            return raised;
        }

        /// <summary>各覚醒は直前の上限Lv到達時だけ可能。素材消費の検証は呼び出し側で原子的に行う。</summary>
        public bool TryAwaken()
        {
            if (Awakening == AwakeningStage.Second || Level != LevelCap) return false;
            Awakening = Awakening == AwakeningStage.Base ? AwakeningStage.First : AwakeningStage.Second;
            return true;
        }

        /// <returns>最大済みならtrue。重複本人を削除せず汎用超過強化素材へ変換する契機。</returns>
        public bool ApplyDuplicate(int maximumTraitRank)
        {
            if (maximumTraitRank < 0) throw new ArgumentOutOfRangeException(nameof(maximumTraitRank));
            if (TraitRank >= maximumTraitRank) return true;
            TraitRank++;
            return false;
        }
    }
}
