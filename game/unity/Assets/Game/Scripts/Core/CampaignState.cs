using System;
using System.Collections.Generic;

namespace NewAster.Core
{
    public sealed class CampaignVictoryResult
    {
        public VictoryClaimResult Reward { get; }
        public bool FirstClear { get; }
        public IReadOnlyCollection<string> NewEnvironmentTags { get; }
        public IReadOnlyCollection<string> NewGardenIds { get; }

        public CampaignVictoryResult(VictoryClaimResult reward, bool firstClear,
            IReadOnlyCollection<string> newEnvironmentTags, IReadOnlyCollection<string> newGardenIds)
        {
            Reward = reward;
            FirstClear = firstClear;
            NewEnvironmentTags = newEnvironmentTags;
            NewGardenIds = newGardenIds;
        }
    }

    /// <summary>
    /// 戦闘結果を受ける唯一の進行入口。
    /// 素材・詩・テラフォーミング・初回討伐・環境・箱庭を同じ battleId で一度だけ確定する。
    /// </summary>
    public sealed class CampaignState
    {
        public ProgressState Progress { get; }
        public ColossusUnlockState ColossusUnlocks { get; }
        public TerraformingState Terraforming { get; }
        public GardenUnlockState Gardens { get; }

        public CampaignState(IEnumerable<string> colossusIds)
        {
            Progress = new ProgressState();
            ColossusUnlocks = new ColossusUnlockState(colossusIds);
            Terraforming = new TerraformingState();
            Gardens = new GardenUnlockState();
        }

        public CampaignVictoryResult ClaimColossusVictory(
            string colossusId,
            IEnumerable<string> environmentTags,
            VictoryReward reward,
            IEnumerable<StoryRequirement> stories,
            IEnumerable<TerraformingMilestone> milestones,
            IEnumerable<GardenRequirement> gardenRequirements)
        {
            if (!ColossusUnlocks.IsUnlocked(colossusId)) throw new InvalidOperationException("Cannot claim a locked colossus.");
            var rewardResult = Progress.ClaimVictory(reward, stories, milestones);
            if (!rewardResult.Claimed)
                return new CampaignVictoryResult(rewardResult, false, Array.Empty<string>(), Array.Empty<string>());

            var firstClear = ColossusUnlocks.RecordFirstClear(colossusId);
            var environments = firstClear
                ? Terraforming.ApplyFirstClear(colossusId, environmentTags)
                : Array.Empty<string>();
            var gardens = firstClear
                ? Gardens.Refresh(Terraforming, gardenRequirements)
                : Array.Empty<string>();
            return new CampaignVictoryResult(rewardResult, firstClear, environments, gardens);
        }
    }
}
