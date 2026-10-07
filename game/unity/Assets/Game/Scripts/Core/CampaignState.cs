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
        public PlayableProgress Playable { get; }
        public TerraformSave Terraform { get; }
        private readonly TerraformMigration migration;

        public CampaignState(IEnumerable<string> colossusIds, CampaignSaveV2 save = null)
        {
            save = save ?? new CampaignSaveV2();
            Terraform = TerraformRules.Copy(TerraformRules.Migrate(save));
            migration = new TerraformMigration { plan11TerraformCompleted=save.migration.plan11TerraformCompleted,plan11ContentUnlocksCompleted=save.migration.plan11ContentUnlocksCompleted };
            Progress = new ProgressState(save);
            ColossusUnlocks = new ColossusUnlockState(colossusIds, save?.firstClearIds);
            Terraforming = new TerraformingState(save?.appliedColossusIds, save?.environmentTags);
            Gardens = new GardenUnlockState(save?.unlockedGardenIds);
            Playable = new PlayableProgress(save);
        }

        public CampaignSaveV2 CreateSave()
        {
            var save = new CampaignSaveV2();
            save.terraform = TerraformRules.Copy(Terraform); save.migration = new TerraformMigration {plan11TerraformCompleted=migration.plan11TerraformCompleted,plan11ContentUnlocksCompleted=migration.plan11ContentUnlocksCompleted};
            Progress.CopyTo(save); ColossusUnlocks.CopyTo(save); Terraforming.CopyTo(save); Gardens.CopyTo(save);
            Playable.CopyTo(save);
            return save;
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

            TerraformRules.Victory(Terraform, colossusId, reward.Level);

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
