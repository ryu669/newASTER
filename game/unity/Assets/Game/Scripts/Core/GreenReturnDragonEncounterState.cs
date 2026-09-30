using System;

namespace NewAster.Core
{
    /// <summary>緑還竜の大技予告と、角冠破壊による一回限りの中断処理。</summary>
    public sealed class GreenReturnDragonEncounterState
    {
        public bool UltimateTelegraphed { get; private set; }
        public bool UltimateCancelled { get; private set; }
        public void TelegraphUltimate(BattleState battle)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (battle.BossGauge >= battle.BossGaugeMax) UltimateTelegraphed = true;
        }
        public void OnPartBroken(string partId)
        {
            if (partId == "crystal-horn-crown" && UltimateTelegraphed && !UltimateCancelled) UltimateCancelled = true;
        }
        public bool TryExecuteUltimate(BattleState battle)
        {
            if (!UltimateTelegraphed || UltimateCancelled || !battle.TryConsumeUltimateGauge()) return false;
            UltimateTelegraphed = false;
            return true;
        }
    }
}
