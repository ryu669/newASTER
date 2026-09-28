using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    /// <summary>戦闘終了時に一度だけ確定する、保存可能な報酬入力。</summary>
    public sealed class VictoryReward
    {
        public string BattleId { get; }
        public int Level { get; }
        public int Materials { get; }
        public int Terraforming { get; }
        public IReadOnlyCollection<string> PoemIds { get; }

        public VictoryReward(string battleId, int level, int materials, int terraforming, IEnumerable<string> poemIds)
        {
            if (string.IsNullOrWhiteSpace(battleId)) throw new ArgumentException("A battle id is required.", nameof(battleId));
            if (level < 1 || level > 50) throw new ArgumentOutOfRangeException(nameof(level));
            if (materials < 0) throw new ArgumentOutOfRangeException(nameof(materials));
            if (terraforming < 0) throw new ArgumentOutOfRangeException(nameof(terraforming));
            BattleId = battleId;
            Level = level;
            Materials = materials;
            Terraforming = terraforming;
            PoemIds = (poemIds ?? Enumerable.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        }
    }

    public sealed class StoryRequirement
    {
        public string StoryId { get; }
        public IReadOnlyCollection<string> RequiredPoemIds { get; }

        public StoryRequirement(string storyId, IEnumerable<string> requiredPoemIds)
        {
            if (string.IsNullOrWhiteSpace(storyId)) throw new ArgumentException("A story id is required.", nameof(storyId));
            StoryId = storyId;
            RequiredPoemIds = (requiredPoemIds ?? throw new ArgumentNullException(nameof(requiredPoemIds)))
                .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        }
    }

    public sealed class TerraformingMilestone
    {
        public string Id { get; }
        public int RequiredExperience { get; }

        public TerraformingMilestone(string id, int requiredExperience)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A milestone id is required.", nameof(id));
            if (requiredExperience < 0) throw new ArgumentOutOfRangeException(nameof(requiredExperience));
            Id = id;
            RequiredExperience = requiredExperience;
        }
    }

    public sealed class VictoryClaimResult
    {
        public bool Claimed { get; }
        public int Materials { get; }
        public int Terraforming { get; }
        public IReadOnlyCollection<string> NewPoemIds { get; }
        public IReadOnlyCollection<string> NewStoryIds { get; }
        public IReadOnlyCollection<string> NewMilestoneIds { get; }

        public VictoryClaimResult(bool claimed, int materials, int terraforming, IEnumerable<string> poems, IEnumerable<string> stories, IEnumerable<string> milestones)
        {
            Claimed = claimed;
            Materials = materials;
            Terraforming = terraforming;
            NewPoemIds = (poems ?? Enumerable.Empty<string>()).ToArray();
            NewStoryIds = (stories ?? Enumerable.Empty<string>()).ToArray();
            NewMilestoneIds = (milestones ?? Enumerable.Empty<string>()).ToArray();
        }
    }

    /// <summary>
    /// 戦闘結果だけが更新する進行状態。UIはこのAPIを介し、状態を直接変更しない。
    /// 同一 battleId の二重確定を防ぎ、詩・章・テラフォーミングを同じ確定単位にする。
    /// </summary>
    public sealed class ProgressState
    {
        private readonly HashSet<string> _claimedBattleIds = new HashSet<string>();
        private readonly HashSet<string> _poemIds = new HashSet<string>();
        private readonly HashSet<string> _unlockedStoryIds = new HashSet<string>();
        private readonly HashSet<string> _readStoryIds = new HashSet<string>();
        private readonly HashSet<string> _unlockedMilestoneIds = new HashSet<string>();

        public int Materials { get; private set; }
        public int TerraformingExperience { get; private set; }
        public IReadOnlyCollection<string> CollectedPoemIds => _poemIds;
        public IReadOnlyCollection<string> UnlockedStoryIds => _unlockedStoryIds;
        public IReadOnlyCollection<string> ReadStoryIds => _readStoryIds;
        public IReadOnlyCollection<string> UnlockedMilestoneIds => _unlockedMilestoneIds;

        public VictoryClaimResult ClaimVictory(
            VictoryReward reward,
            IEnumerable<StoryRequirement> stories,
            IEnumerable<TerraformingMilestone> milestones)
        {
            if (reward == null) throw new ArgumentNullException(nameof(reward));
            if (!_claimedBattleIds.Add(reward.BattleId))
                return new VictoryClaimResult(false, 0, 0, null, null, null);

            var levelBonus = (reward.Level - 1) / 10;
            var materials = reward.Materials + levelBonus;
            var terraforming = reward.Terraforming + levelBonus;
            Materials += materials;
            TerraformingExperience += terraforming;

            var newPoems = reward.PoemIds.Where(id => _poemIds.Add(id)).ToArray();
            var newStories = (stories ?? Enumerable.Empty<StoryRequirement>())
                .Where(story => story != null && story.RequiredPoemIds.All(id => _poemIds.Contains(id)) && _unlockedStoryIds.Add(story.StoryId))
                .Select(story => story.StoryId)
                .ToArray();
            var newMilestones = (milestones ?? Enumerable.Empty<TerraformingMilestone>())
                .Where(milestone => milestone != null && milestone.RequiredExperience <= TerraformingExperience && _unlockedMilestoneIds.Add(milestone.Id))
                .Select(milestone => milestone.Id)
                .ToArray();

            return new VictoryClaimResult(true, materials, terraforming, newPoems, newStories, newMilestones);
        }

        public bool MarkStoryRead(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId)) throw new ArgumentException("A story id is required.", nameof(storyId));
            if (!_unlockedStoryIds.Contains(storyId)) throw new InvalidOperationException("The story is not unlocked.");
            return _readStoryIds.Add(storyId);
        }
    }
}
