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
            if((kind!="speed" && kind!="fire-amplification" && kind!="attack" && kind!="attack-reduction" && kind!="physical-protection" && kind!="regen" && kind!="critical" && kind!="critical-damage" && kind!="forced-target") || percent<=0 || percent>(kind=="forced-target"?1:kind=="critical" || kind=="physical-protection" || kind=="attack-reduction"?100:1000) || turns<1 || turns>10)
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
        public static string Label(string kind) => kind=="speed"?"速度＋": kind=="fire-amplification"?"火増幅＋": kind=="attack"?"攻撃＋":kind=="attack-reduction"?"攻撃−":kind=="regen"?"再生 ":kind=="physical-protection"?"物理防護 ":kind=="critical"?"会心率＋":kind=="critical-damage"?"会心威力＋":"強制標的 ";
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
        public IReadOnlyList<TimedSelfEffectSnapshot> TimedEffects => Array.AsReadOnly(timedEffects.Concat(OopartBuffs.Where(b=>!b.kind.StartsWith("stat.",StringComparison.Ordinal)).Select(b=>new TimedSelfEffectSnapshot(b.kind,b.value,(int)Math.Max(1,(b.expiresAt-(OopartClock?.Invoke()??0)+99)/100)))).ToArray());
        private int EffectPercent(string kind) => (timedEffects.FirstOrDefault(e=>e.Kind==kind)?.Percent??0)+OopartBuffPercent(kind);
        public int CriticalChanceBp => Math.Min(10000,BaseCriticalChanceBp*(100+JobAllStatsPercent)/100+EffectPercent("critical")*100+SongCriticalBonusBp+GeneralCriticalBp);
        public int CriticalMultiplierPercent => (150+WeaponCriticalDamageBonus)*(100+JobAllStatsPercent)/100+EffectPercent("critical-damage");
        public int TimedSpeedPercent=>EffectPercent("speed");
        public int FireAmplificationPercent=>EffectPercent("fire-amplification");
        public bool ForcedTarget => IsAlive && EffectPercent("forced-target")>0;
        public bool ApplySelfEffects(IEnumerable<TimedSelfEffectDef> definitions)
        {
            var items=(definitions??Array.Empty<TimedSelfEffectDef>()).ToArray();TimedSelfEffectDef.ValidateAll(items);
            if(OopartClock!=null)return ApplyOopartBuffs(items,Id,"self",OopartValue?.Invoke("buff-power")??0);
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
            if(UsesBattleTurnDuration)return;
            TickTimedEffects();
        }
        private void TickTimedEffects()
        {
            var next=timedEffects.Where(e=>e.RemainingCommands>1).Select(e=>new TimedSelfEffectSnapshot(e.Kind,e.Percent,e.RemainingCommands-1)).ToArray();
            timedEffects.Clear();timedEffects.AddRange(next);
        }
        internal void ExtendTimedEffects(){ExtendOopartBuffs();var next=timedEffects.Select(e=>new TimedSelfEffectSnapshot(e.Kind,e.Percent,e.Kind=="attack-reduction"?e.RemainingCommands:checked(e.RemainingCommands+1))).ToArray();timedEffects.Clear();timedEffects.AddRange(next);}
        internal void ExtendPositiveTimedEffects(){var next=timedEffects.Select(e=>new TimedSelfEffectSnapshot(e.Kind,e.Percent,e.Kind=="attack-reduction"?e.RemainingCommands:checked(e.RemainingCommands+1))).ToArray();timedEffects.Clear();timedEffects.AddRange(next);}
        public int RegenerateAtOwnerReady()
        {
            if(!IsAlive) return 0;
            int before=HitPoints;
            Heal((int)Math.Min(int.MaxValue,(long)Attack*EffectPercent("regen")/100+(long)MaxHitPoints*PermanentRegenPercent/100));
            return HitPoints-before;
        }
        public int ProtectPhysicalDamage(int damage)
        {
            if(damage<0) throw new ArgumentOutOfRangeException(nameof(damage));
            return (int)((long)damage*(Math.Max(0,100-EffectPercent("physical-protection")))/100);
        }
    }
}
