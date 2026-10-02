using System;
using System.Collections.Generic;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class EnemyStatusDef
    {
        public string kind;
        public int amount;
        public EnemyStatusDef Copy() => new EnemyStatusDef {kind=kind,amount=amount};
        public void Validate() { if(!EnemyStatusState.Kinds.Contains(kind) || amount<1 || amount>1000) throw new ArgumentException("Unknown enemy status or accumulation."); }
    }
    [Serializable] public sealed class EnemyStatusResistanceDef
    {
        public string kind;
        public int resistanceBp;
        public void Validate() {if(!EnemyStatusState.Kinds.Contains(kind) || resistanceBp<0 || resistanceBp>10000) throw new ArgumentException("Invalid enemy status resistance.");}
    }
    // newASTER rules: threshold 100; local meters; three enemy-action duration.
    public sealed class EnemyStatusState
    {
        public static readonly System.Collections.Generic.IReadOnlyList<string> Kinds=Array.AsReadOnly(new[]{"burn","bleed","poison","stun","sickness","fracture"});
        private readonly Dictionary<string,int> meter=new Dictionary<string,int>();
        private readonly Dictionary<string,int> duration=new Dictionary<string,int>();
        private readonly Dictionary<string,int> resistances=new Dictionary<string,int>();
        public EnemyStatusState(IEnumerable<EnemyStatusResistanceDef> resistance=null)
        {
            var items=(resistance??Array.Empty<EnemyStatusResistanceDef>()).ToArray();
            if(items.Any(r=>r==null) || items.Select(r=>r.kind).Distinct().Count()!=items.Length) throw new ArgumentException("Duplicate or null status resistance.");
            foreach(var r in items) {r.Validate();resistances.Add(r.kind,r.resistanceBp);}
        }
        public int Meter(string kind) => meter.TryGetValue(kind,out var n)?n:0;
        public int Remaining(string kind) => duration.TryGetValue(kind,out var n)?n:0;
        public bool Active(string kind) => Remaining(kind)>0;
        public void Add(EnemyStatusDef effect)
        {
            effect.Validate();int resistance=resistances.TryGetValue(effect.kind,out var n)?n:0;
            int gain=(int)((long)effect.amount*(10000-resistance)/10000);if(gain==0)return;
            int value=Meter(effect.kind)+gain;
            if(value>=100) {duration[effect.kind]=effect.kind=="stun"?1:3;value%=100;}
            meter[effect.kind]=value;
        }
        public int Dot(int maxHp) => (int)Math.Min(int.MaxValue,(long)maxHp*((Active("burn")?2:0)+(Active("poison")?3:0)+(Active("bleed")?2:0))/100);
        public void Tick() {foreach(var kind in duration.Keys.ToArray()) duration[kind]=Math.Max(0,duration[kind]-1);}
        public static string Label(string kind) => kind=="burn"?"火傷":kind=="bleed"?"出血":kind=="poison"?"毒":kind=="stun"?"スタン":kind=="sickness"?"病気":"骨折";
        public string Description => string.Join(" / ",Kinds.Where(k=>Meter(k)>0 || Active(k)).Select(k=>Label(k)+":"+Meter(k)+(Active(k)?"（残り"+Remaining(k)+"）":"")));
    }
    public static class EnemyAttackTargets
    {
        public static string[] Resolve(BattleState state,string rule,string selected)
        {
            if(rule=="target.all-enemies") return new[]{"body"}.Concat(state.Parts.Where(p=>!p.IsBroken).Select(p=>p.Id)).ToArray();
            bool valid=selected=="body" || state.Parts.Any(p=>p.Id==selected && !p.IsBroken);
            if(!valid) return Array.Empty<string>();
            if(rule=="target.selected-enemy") return new[]{selected};
            if(rule!="target.enemy-range") throw new ArgumentException("Unknown attack target rule.");
            // The book's four parts have stable adjacency. Dead parts are excluded, not replaced.
            if(selected=="body") return new[]{"body"}.Concat(state.Parts.Take(2).Where(p=>!p.IsBroken).Select(p=>p.Id)).ToArray();
            int index=state.Parts.ToList().FindIndex(p=>p.Id==selected);
            return state.Parts.Where((p,i)=>Math.Abs(i-index)<=1 && !p.IsBroken).Select(p=>p.Id).ToArray();
        }
    }
}
