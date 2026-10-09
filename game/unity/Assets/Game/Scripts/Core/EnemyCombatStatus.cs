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
        public static readonly System.Collections.Generic.IReadOnlyList<string> Kinds=Array.AsReadOnly(new[]{"burn","bleed","poison","stun","sickness","fracture","frostbite","jamming","absent","electrified"});
        private readonly Dictionary<string,int> meter=new Dictionary<string,int>();
        private readonly Dictionary<string,int> duration=new Dictionary<string,int>();
        private readonly Dictionary<string,int> resistances=new Dictionary<string,int>();
        public EnemyStatusState(IEnumerable<EnemyStatusResistanceDef> resistance=null)
        {
            var items=(resistance??Array.Empty<EnemyStatusResistanceDef>()).ToArray();
            if(items.Any(r=>r==null) || items.Select(r=>r.kind).Distinct().Count()!=items.Length) throw new ArgumentException("Duplicate or null status resistance.");
            foreach(var r in items) {r.Validate();resistances.Add(r.kind,r.resistanceBp);}
        }
        public int AttackReductionPercent {get;private set;}
        internal void AddResistance(string kind,int amount)
        {if(!Kinds.Contains(kind) || amount<0)throw new ArgumentException("Invalid additional resistance.");resistances[kind]=Math.Min(10000,(resistances.TryGetValue(kind,out var value)?value:0)+amount);}
        public int AttackReductionTurns {get;private set;}
        public void SetAttackReduction(int percent,int turns){if(percent<1 || percent>50 || turns<1 || turns>10)throw new ArgumentException("Invalid attack reduction.");AttackReductionPercent=percent;AttackReductionTurns=turns;}
        public int IgnitionFlags {get;private set;}
        public bool AddIgnition(){if(++IgnitionFlags<3)return false;IgnitionFlags=0;return true;}
        public int FireVulnerabilityPercent {get;private set;}
        public int FireVulnerabilityTurns {get;private set;}
        public void SetFireVulnerability(int percent,int turns){if(percent<1 || percent>100 || turns<1 || turns>10)throw new ArgumentException("Invalid fire vulnerability.");FireVulnerabilityPercent=percent;FireVulnerabilityTurns=turns;}
        public int Meter(string kind) => meter.TryGetValue(kind,out var n)?n:0;
        public int Remaining(string kind) => duration.TryGetValue(kind,out var n)?n:0;
        public void ExtendActive(string[] kinds,int turns){if(turns<1 || turns>3 || kinds==null || kinds.Any(k=>k!="burn" && k!="bleed" && k!="poison"))throw new ArgumentException("Invalid active status extension.");foreach(var k in kinds.Distinct())if(Active(k))duration[k]=Math.Min(10,duration[k]+turns);}
        public bool Active(string kind) => Remaining(kind)>0;
        public bool Add(EnemyStatusDef effect)
        {
            effect.Validate();int resistance=resistances.TryGetValue(effect.kind,out var n)?n:0;
            int gain=(int)((long)effect.amount*(10000-resistance)/10000);if(gain==0)return false;
            int value=Meter(effect.kind)+gain;bool activated=value>=100;
            if(value>=100) {duration[effect.kind]=effect.kind=="stun"?1:3;value%=100;}
            meter[effect.kind]=value;return activated;
        }
        public int Dot(int maxHp) => (int)Math.Min(int.MaxValue,(long)maxHp*((Active("burn")?2:0)+(Active("poison")?3:0)+(Active("bleed")?2:0)+(Active("frostbite")?2:0))/100);
        public void Tick() {if(AttackReductionTurns>0 && --AttackReductionTurns==0)AttackReductionPercent=0;if(FireVulnerabilityTurns>0 && --FireVulnerabilityTurns==0)FireVulnerabilityPercent=0;foreach(var kind in duration.Keys.ToArray()) duration[kind]=Math.Max(0,duration[kind]-1);}
        public void Remove(string kind) {duration[kind]=0;}
        public void ClearAll(){meter.Clear();duration.Clear();FireVulnerabilityPercent=FireVulnerabilityTurns=IgnitionFlags=AttackReductionPercent=AttackReductionTurns=0;}
        public int ActivationDamage(string kind,int maxHp)=>kind=="poison"?Math.Max(1,(int)((long)maxHp*3/100)):kind=="burn" || kind=="bleed" || kind=="frostbite"?Math.Max(1,(int)((long)maxHp*2/100)):0;
        public static string EffectDescription(string kind)=>kind=="poison"?"発症時と行動終了時に最大HPの3%ダメージ。":kind=="burn"?"発症時・行動終了時に2%ダメージ、攻撃力20%低下。":kind=="frostbite"?"発症時・行動終了時に2%ダメージ、速度20%低下。":kind=="bleed"?"発症時・行動終了時に2%ダメージ。回復を受けられない。":kind=="stun"?"次の行動を自動で1回飛ばす。":kind=="jamming"?"選択可能な対象からランダムに対象を選ぶ。":kind=="absent"?"行動不可。行動終了時にHP5%回復。被会心率+25%。ダメージを受けると解除。":kind=="sickness"?"攻撃力20%低下、受ける最終ダメージ25%増加。":kind=="fracture"?"攻撃後に自身の最大HP5%ダメージ。攻撃しないスキルでは発生しない。":"雷の最終ダメージ25%増加。発症時・行動終了時に次行動待機+20。";
        public static string Label(string kind) => kind=="burn"?"火傷":kind=="bleed"?"出血":kind=="poison"?"毒":kind=="stun"?"スタン":kind=="sickness"?"病弱":kind=="fracture"?"骨折":kind=="frostbite"?"凍傷":kind=="jamming"?"ジャミング":kind=="absent"?"うわの空":"帯電";
        public string Description => (AttackReductionTurns>0?"攻撃−"+AttackReductionPercent+"%（残り"+AttackReductionTurns+"） / ":"")+ (IgnitionFlags>0?"IGNITION "+IgnitionFlags+"/3 / ":"")+ (FireVulnerabilityTurns>0?"火弱点＋"+FireVulnerabilityPercent+"%（残り"+FireVulnerabilityTurns+"） / ":"")+string.Join(" / ",Kinds.Where(k=>Meter(k)>0 || Active(k)).Select(k=>Label(k)+":"+Meter(k)+(Active(k)?"（残り"+Remaining(k)+"）":"")));
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
