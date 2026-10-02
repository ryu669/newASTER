using System;

namespace NewAster.Core
{
    public enum SlayerMode { Standard, Reckless }

    /// <summary>動画観測に基づくスレイヤーの状態遷移。個別数値は定義データから渡す。</summary>
    public sealed class SlayerCombatState
    {
        public SlayerMode Mode { get; private set; }
        public int BoostOrbs { get; private set; }
        public int BoostOrbCap { get; }
        public int LastResortGauge { get; private set; }
        public int LastResortGaugeCap { get; }
        public bool LastResortReady => LastResortGauge >= LastResortGaugeCap;

        public SlayerCombatState(int boostOrbCap, int lastResortGaugeCap)
        {
            if (boostOrbCap <= 0 || lastResortGaugeCap <= 0) throw new ArgumentOutOfRangeException();
            BoostOrbCap = boostOrbCap;
            LastResortGaugeCap = lastResortGaugeCap;
        }

        public void ToggleMode() => Mode = Mode == SlayerMode.Standard ? SlayerMode.Reckless : SlayerMode.Standard;
        public void GainBoostOrbs(int amount) => BoostOrbs = Math.Min(BoostOrbCap, BoostOrbs + Math.Max(0, amount));
        public bool TrySpendBoostOrbs(int amount)
        {
            if (amount < 0 || amount > BoostOrbs) return false;
            BoostOrbs -= amount;
            return true;
        }
        public void GainLastResort(int amount) => LastResortGauge = Math.Min(LastResortGaugeCap, LastResortGauge + Math.Max(0, amount));
        public bool TryUseLastResort()
        {
            if (!LastResortReady) return false;
            LastResortGauge = 0;
            return true;
        }
    }
}
