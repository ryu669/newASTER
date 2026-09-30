using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    /// <summary>
    /// 巨神獣の初回討伐で新天地へ定着した環境記述。
    /// 箱庭、家具、ヒロイン行動は表示名を持たず、ここで得た環境タグを共通参照する。
    /// </summary>
    public sealed class TerraformingState
    {
        private readonly HashSet<string> _appliedColossusIds = new HashSet<string>();
        private readonly HashSet<string> _environmentTags = new HashSet<string>();

        public IReadOnlyCollection<string> EnvironmentTags => _environmentTags;

        public TerraformingState(IEnumerable<string> appliedColossusIds = null, IEnumerable<string> environmentTags = null)
        {
            _appliedColossusIds.UnionWith(appliedColossusIds ?? Enumerable.Empty<string>());
            _environmentTags.UnionWith(environmentTags ?? Enumerable.Empty<string>());
        }

        /// <returns>今回初めて定着した環境タグ。再召喚の報酬では空。</returns>
        public IReadOnlyCollection<string> ApplyFirstClear(string colossusId, IEnumerable<string> environmentTags)
        {
            if (string.IsNullOrWhiteSpace(colossusId)) throw new ArgumentException("A colossus id is required.", nameof(colossusId));
            if (!_appliedColossusIds.Add(colossusId)) return Array.Empty<string>();
            return (environmentTags ?? throw new ArgumentNullException(nameof(environmentTags)))
                .Where(tag => !string.IsNullOrWhiteSpace(tag) && _environmentTags.Add(tag))
                .ToArray();
        }

        public bool HasEnvironment(string environmentTag) => _environmentTags.Contains(environmentTag);
        public void CopyTo(CampaignSaveV2 save) { save.appliedColossusIds = _appliedColossusIds.ToArray(); save.environmentTags = _environmentTags.ToArray(); }
    }
}
