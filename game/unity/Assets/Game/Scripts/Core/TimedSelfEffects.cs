using System;
using System.Collections.Generic;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class TimedSelfEffectDef
    {
        public string kind;
        public int percent,turns;
        public void Validate()
        {
            if((kind!="attack" && kind!="physical-protection" && kind!="regen") || percent<=0 || percent>(kind=="physical-protection"?100:1000) || turns<1 || turns>10)
                throw new ArgumentException("Unsupported timed self effect or range.");
        }
        public static void ValidateAll(IEnumerable<TimedSelfEffectDef> effects)
        {
            var items=(effects??Array.Empty<TimedSelfEffectDef>()).ToArray();
            if(items.Any(e=>e==null) || items.Select(e=>e.kind).Distinct().Count()!=items.Length)
                throw new ArgumentException("Duplicate or null timed self effect.");
            foreach(var item in items) item.Validate();
        }
        public TimedSelfEffectDef Copy() => new TimedSelfEffectDef {kind=kind,percent=percent,turns=turns};
    }
    // Immutable copies are safe to place in delayed presentation events.
    public sealed class TimedSelfEffectSnapshot
    {
        public string Kind { get; }
        public int Percent { get; }
        public int RemainingCommands { get; }
        public TimedSelfEffectSnapshot(string kind,int percent,int remaining)
        {Kind=kind;Percent=percent;RemainingCommands=remaining;}
    }
    public sealed partial class BattleHero
    {
        private readonly List<TimedSelfEffectSnapshot> timedEffects=new List<TimedSelfEffectSnapshot>();
        public IReadOnlyList<TimedSelfEffectSnapshot> TimedEffects => Array.AsReadOnly(timedEffects.ToArray());
        private int EffectPercent(string kind) => timedEffects.FirstOrDefault(e=>e.Kind==kind)?.Percent??0;
        public bool ApplySelfEffects(IEnumerable<TimedSelfEffectDef> definitions)
        {
            var items=(definitions??Array.Empty<TimedSelfEffectDef>()).ToArray();TimedSelfEffectDef.ValidateAll(items);
            if(!IsAlive) return false;
            foreach(var effect in items) {
                timedEffects.RemoveAll(e=>e.Kind==effect.kind);
                timedEffects.Add(new TimedSelfEffectSnapshot(effect.kind,effect.percent,effect.turns));
            }
            return true;
        }
        // Called once at a successful command or pass, never by independent chains
        // or cast release. New self-buffs are installed after this expiration step.
        public void CompleteOwnerCommand()
        {
            var next=timedEffects.Where(e=>e.RemainingCommands>1).Select(e=>new TimedSelfEffectSnapshot(e.Kind,e.Percent,e.RemainingCommands-1)).ToArray();
            timedEffects.Clear();timedEffects.AddRange(next);
        }
        public int RegenerateAtOwnerReady()
        {
            if(!IsAlive) return 0;
            int before=HitPoints;
            Heal((int)Math.Min(int.MaxValue,(long)Attack*EffectPercent("regen")/100));
            return HitPoints-before;
        }
        public int ProtectPhysicalDamage(int damage)
        {
            if(damage<0) throw new ArgumentOutOfRangeException(nameof(damage));
            return (int)((long)damage*(100-EffectPercent("physical-protection"))/100);
        }
    }
}
