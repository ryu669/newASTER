using System;
namespace NewAster.Core
{
    // Cosmetic timing only. This never changes scheduled battle time or outcomes.
    public static class BattleVisualCue
    {
        public static float Duration(BattlePresentationKind kind,bool major) => kind==BattlePresentationKind.CastStart?.7f:major?1.1f:.8f;
        public static float Progress(float elapsed,BattlePresentationKind kind,bool major) => Math.Max(0,Math.Min(1,elapsed/Duration(kind,major)));
        public static float Travel(float progress) => Math.Max(0,Math.Min(1,(progress-.16f)/.40f));
        public static float Impact(float progress) => progress<.56f?0:Math.Max(0,1-(progress-.56f)/.44f);
    }
}
