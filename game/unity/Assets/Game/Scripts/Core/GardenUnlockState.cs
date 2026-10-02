using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public sealed class GardenRequirement
    {
        public string GardenId { get; }
        public IReadOnlyCollection<string> RequiredEnvironmentTags { get; }

        public GardenRequirement(string gardenId, IEnumerable<string> requiredEnvironmentTags)
        {
            if (string.IsNullOrWhiteSpace(gardenId)) throw new ArgumentException("A garden id is required.", nameof(gardenId));
            GardenId = gardenId;
            RequiredEnvironmentTags = (requiredEnvironmentTags ?? throw new ArgumentNullException(nameof(requiredEnvironmentTags)))
                .Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct().ToArray();
            if (RequiredEnvironmentTags.Count == 0) throw new ArgumentException("At least one environment is required.", nameof(requiredEnvironmentTags));
        }
    }

    /// <summary>テラフォーミング済み環境から箱庭区画を一度だけ解放する。</summary>
    public sealed class GardenUnlockState
    {
        private readonly HashSet<string> _unlockedGardenIds = new HashSet<string>();
        public IReadOnlyCollection<string> UnlockedGardenIds => _unlockedGardenIds;
        public GardenUnlockState(IEnumerable<string> unlockedGardenIds = null) => _unlockedGardenIds.UnionWith(unlockedGardenIds ?? Enumerable.Empty<string>());

        public IReadOnlyCollection<string> Refresh(TerraformingState terraforming, IEnumerable<GardenRequirement> requirements)
        {
            if (terraforming == null) throw new ArgumentNullException(nameof(terraforming));
            return (requirements ?? throw new ArgumentNullException(nameof(requirements)))
                .Where(requirement => requirement != null
                    && requirement.RequiredEnvironmentTags.All(terraforming.HasEnvironment)
                    && _unlockedGardenIds.Add(requirement.GardenId))
                .Select(requirement => requirement.GardenId)
                .ToArray();
        }

        public bool IsUnlocked(string gardenId) => _unlockedGardenIds.Contains(gardenId);
        public void CopyTo(CampaignSaveV2 save) => save.unlockedGardenIds = _unlockedGardenIds.ToArray();
    }
}
