using System;
using System.Collections.Generic;

namespace NewAster.Core
{
    /// <summary>第5段階のプレイテストで、戦闘理解度とテンポを比較するための匿名ローカル計測。</summary>
    public sealed class PlaytestBattleRecord
    {
        public string BattleId { get; }
        public DateTime StartedAtUtc { get; }
        public DateTime? EndedAtUtc { get; private set; }
        public bool? Victory { get; private set; }
        public int AcceptedCommandCount { get; private set; }
        public int PartBreakCount { get; private set; }

        public PlaytestBattleRecord(string battleId, DateTime startedAtUtc)
        {
            BattleId = battleId ?? throw new ArgumentNullException(nameof(battleId));
            StartedAtUtc = startedAtUtc;
        }

        public void RecordCommand(bool accepted, bool partBroken)
        {
            if (EndedAtUtc.HasValue) return;
            if (accepted) AcceptedCommandCount++;
            if (partBroken) PartBreakCount++;
        }

        public void End(bool victory, DateTime endedAtUtc)
        {
            if (EndedAtUtc.HasValue) return;
            EndedAtUtc = endedAtUtc;
            Victory = victory;
        }

        public TimeSpan? Duration => EndedAtUtc.HasValue ? EndedAtUtc.Value - StartedAtUtc : null;
    }

    public sealed class PlaytestTelemetry
    {
        private readonly List<PlaytestBattleRecord> _records = new List<PlaytestBattleRecord>();
        public IReadOnlyList<PlaytestBattleRecord> Records => _records;
        public PlaytestBattleRecord StartBattle(string battleId)
        {
            var record = new PlaytestBattleRecord(battleId, DateTime.UtcNow);
            _records.Add(record);
            return record;
        }
    }
}
