using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    /// <summary>
    /// 万物の書の巨神獣ページの初回討伐状態。
    /// No.1は開始時に読め、No.2〜14は直前の初回討伐、No.15はNo.1〜14の全初回討伐で読める。
    /// 再召喚回数や選択Lvは、この解放条件に影響しない。
    /// </summary>
    public sealed class ColossusUnlockState
    {
        private readonly IReadOnlyList<string> _orderedIds;
        private readonly HashSet<string> _clearedIds = new HashSet<string>();

        public ColossusUnlockState(IEnumerable<string> orderedIds, IEnumerable<string> firstClearIds = null)
        {
            _orderedIds = (orderedIds ?? throw new ArgumentNullException(nameof(orderedIds))).ToArray();
            if (_orderedIds.Count != 15 || _orderedIds.Any(string.IsNullOrWhiteSpace) || _orderedIds.Distinct().Count() != 15)
                throw new ArgumentException("The initial catalogue must contain exactly 15 unique ids.", nameof(orderedIds));
            var saved = new HashSet<string>(firstClearIds ?? Enumerable.Empty<string>());
            foreach (var id in _orderedIds.Where(saved.Contains)) RecordFirstClear(id);
        }

        public IReadOnlyCollection<string> FirstClearIds => _clearedIds;

        public bool IsUnlocked(string colossusId)
        {
            var index = -1;
            for (var candidate = 0; candidate < _orderedIds.Count; candidate++)
                if (_orderedIds[candidate] == colossusId) { index = candidate; break; }
            if (index < 0) return false;
            if (index == 0) return true;
            if (index < 14) return _clearedIds.Contains(_orderedIds[index - 1]);
            return _orderedIds.Take(14).All(_clearedIds.Contains);
        }

        /// <returns>初回討伐だった場合のみtrue。次のページの解放可否はIsUnlockedで参照する。</returns>
        public bool RecordFirstClear(string colossusId)
        {
            if (!IsUnlocked(colossusId)) throw new InvalidOperationException("Cannot clear a locked colossus page.");
            return _clearedIds.Add(colossusId);
        }

        public void CopyTo(CampaignSaveV2 save) => save.firstClearIds = _clearedIds.ToArray();
    }
}
