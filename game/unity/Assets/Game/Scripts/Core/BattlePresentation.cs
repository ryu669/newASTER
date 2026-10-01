using System;
using System.Collections.Generic;
using System.Linq;
namespace NewAster.Core
{
    public enum BattlePresentationKind { Attack, Healing, Support, Pass, CastStart, CastRelease, CastCanceled, Enemy }
    public sealed class BattlePresentationEvent
    {
        public long Sequence { get; }
        public long Clock { get; }
        public BattlePresentationKind Kind { get; }
        public int Actor { get; }
        public string Target { get; }
        public string Message { get; }
        public bool Major { get; }
        public bool PartBroken { get; }
        public int Damage { get; }
        public int Chain { get; }
        public int BossHp { get; }
        public int BossGauge { get; }
        public IReadOnlyList<int> HeroHp { get; }
        public IReadOnlyList<int> Resources { get; }
        public IReadOnlyList<int> PartHp { get; }
        public IReadOnlyList<int> HealingTargets { get; }
        public IReadOnlyList<bool> Casting { get; }
        public BattlePresentationEvent(long sequence,long clock,BattlePresentationKind kind,int actor,string target,string message,bool major,bool partBroken,int damage,int chain,BattleState state,IEnumerable<int> healingTargets,IEnumerable<bool> casting)
        {
            Sequence=sequence; Clock=clock; Kind=kind; Actor=actor; Target=target??"body"; Message=message??"";
            Major=major; PartBroken=partBroken; Damage=damage; Chain=chain; BossHp=state.BossHitPoints; BossGauge=state.BossGauge;
            HeroHp=Array.AsReadOnly(state.Heroes.Select(h=>h.HitPoints).ToArray());
            Resources=Array.AsReadOnly(state.Heroes.Select(h=>h.JobResource).ToArray());
            PartHp=Array.AsReadOnly(state.Parts.Select(p=>p.HitPoints).ToArray());
            HealingTargets=Array.AsReadOnly((healingTargets??Array.Empty<int>()).ToArray());
            Casting=Array.AsReadOnly(casting.ToArray());
        }
    }
    public sealed class BattlePlaybackQueue
    {
        private readonly Queue<BattlePresentationEvent> pending=new Queue<BattlePresentationEvent>();
        private float remaining;
        private long lastEnqueued;
        public BattlePresentationEvent Current { get; private set; }
        public bool Busy => Current!=null;
        public int Count => pending.Count+(Busy?1:0);
        public void Enqueue(IEnumerable<BattlePresentationEvent> events)
        {
            var batch=events.ToArray(); long sequence=lastEnqueued;
            foreach(var e in batch) { if(e==null || e.Sequence<=sequence) throw new ArgumentException("Playback requires ordered unique events."); sequence=e.Sequence; }
            foreach(var e in batch) pending.Enqueue(e);
            lastEnqueued=sequence;
            if(Current==null) Next();
        }
        private void Next()
        {
            Current=pending.Count>0?pending.Dequeue():null;
            remaining=Current==null?0:BattleVisualCue.Duration(Current.Kind,Current.Major);
        }
        public void Tick(float seconds,bool paused)
        {
            if(seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            if(paused || Current==null) return;
            // At most one transition per frame so even a slow frame does not hide events.
            remaining-=seconds; if(remaining<=0) Next();
        }
        public void Skip() { pending.Clear(); Current=null; remaining=0; }
        public void Reset() { Skip(); lastEnqueued=0; }
    }
    public sealed partial class PlayableBattle
    {
        private readonly List<BattlePresentationEvent> presentationEvents=new List<BattlePresentationEvent>();
        private long presentationSequence;
        public IReadOnlyList<BattlePresentationEvent> DrainPresentationEvents()
        {
            var result=presentationEvents.ToArray(); presentationEvents.Clear(); return result;
        }
        private void RecordPresentation(BattlePresentationKind kind,int actor,string target,string message,bool major=false,bool broken=false,int damage=0,IEnumerable<int> healingTargets=null)
        {
            presentationEvents.Add(new BattlePresentationEvent(++presentationSequence,Clock,kind,actor,target,message,major,broken,damage,LastActionChain,State,healingTargets,Enumerable.Range(0,5).Select(IsCasting)));
        }
    }
}
