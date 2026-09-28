using System;
using System.Collections.Generic;

namespace NewAster.Data
{
    [Serializable]
    public sealed class ColossusDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string WorldLineId { get; }
        public IReadOnlyList<string> EnvironmentTags { get; }
        public bool IsIntegrationBoss { get; }

        public ColossusDefinition(string id, string displayName, string worldLineId, bool isIntegrationBoss, params string[] environmentTags)
        {
            Id = id;
            DisplayName = displayName;
            WorldLineId = worldLineId;
            IsIntegrationBoss = isIntegrationBoss;
            EnvironmentTags = environmentTags;
        }
    }
}
