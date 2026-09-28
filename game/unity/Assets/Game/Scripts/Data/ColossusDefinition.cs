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
        public int PageOrder { get; }
        public string PrerequisiteColossusId { get; }
        public IReadOnlyList<string> EnvironmentTags { get; }
        public bool IsIntegrationBoss { get; }

        public ColossusDefinition(string id, string displayName, string worldLineId, int pageOrder,
            string prerequisiteColossusId, bool isIntegrationBoss, params string[] environmentTags)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An id is required.", nameof(id));
            if (pageOrder < 1 || pageOrder > 15) throw new ArgumentOutOfRangeException(nameof(pageOrder));
            if (isIntegrationBoss && !string.IsNullOrEmpty(prerequisiteColossusId))
                throw new ArgumentException("The integration boss uses the full first-clear condition.", nameof(prerequisiteColossusId));
            Id = id;
            DisplayName = displayName;
            WorldLineId = worldLineId;
            PageOrder = pageOrder;
            PrerequisiteColossusId = prerequisiteColossusId;
            IsIntegrationBoss = isIntegrationBoss;
            EnvironmentTags = environmentTags;
        }
    }
}
