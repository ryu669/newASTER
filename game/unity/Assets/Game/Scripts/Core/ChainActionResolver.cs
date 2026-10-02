using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    public sealed class ChainActionResult
    {
        public string Target { get; }
        public IReadOnlyList<string> TargetIds { get; }
        public IReadOnlyList<int> HealingTargets { get; }
        public int Amount { get; }
        public bool PartBroken { get; }
        public ChainActionResult(string target,IEnumerable<string> targets,IEnumerable<int> healed,int amount,bool broken=false)
        {Target=target;TargetIds=Array.AsReadOnly(targets.ToArray());HealingTargets=Array.AsReadOnly(healed.ToArray());Amount=amount;PartBroken=broken;}
    }
    // Changes only target HP and part-break effects. No RNG/resource/timeline APIs.
    public static class ChainActionResolver
    {
        public static ChainActionResult Resolve(BattleState state,int actor,HeroineChainAction action)
        {
            if(state==null || action==null || actor<0 || actor>=state.Heroes.Count || state.Heroes[actor].Id!=action.HeroId) throw new ArgumentException("Chain actor ownership mismatch.");
            var hero=state.Heroes[actor];
            if(!hero.IsAlive || state.IsVictory) return Empty();
            if(action.Effect==ChainEffect.Damage) {
                var skill=new BattleSkill(action.Id,action.Power,0,damageType:action.DamageType,ignoreDefenseBp:action.IgnoreDefenseBp);
                if(action.Target==ChainTarget.BossBody) return new ChainActionResult("body",new[]{"body"},Array.Empty<int>(),state.ApplyBossDamage(BattleActionResolver.CalculateDamage(state,hero,skill,"body")));
                var part=state.Parts.Where(p=>!p.IsBroken).OrderBy(p=>p.HitPoints).FirstOrDefault();
                if(part==null) return Empty();
                int damage=BattleActionResolver.CalculateDamage(state,hero,skill,part.Id);
                int applied=Math.Min(damage,part.HitPoints);bool broken=state.BreakPart(part.Id,damage);
                return new ChainActionResult(part.Id,new[]{part.Id},Array.Empty<int>(),applied,broken);
            }
            int[] targets;
            if(action.Target==ChainTarget.Self) targets=new[]{actor};
            else if(action.Target==ChainTarget.AllLivingAllies) targets=Enumerable.Range(0,state.Heroes.Count).Where(i=>state.Heroes[i].IsAlive).ToArray();
            else {
                int candidate=-1;
                for(int i=0;i<state.Heroes.Count;i++) {
                    var h=state.Heroes[i];if(!h.IsAlive || h.HitPoints==h.MaxHitPoints) continue;
                    if(candidate<0 || (long)h.HitPoints*state.Heroes[candidate].MaxHitPoints<(long)state.Heroes[candidate].HitPoints*h.MaxHitPoints) candidate=i;
                }
                targets=candidate<0?Array.Empty<int>():new[]{candidate};
            }
            int amount=action.BaseHealing+(int)Math.Floor(hero.Attack*action.Power),total=0;
            var healed=new List<int>();
            foreach(int index in targets) {
                var h=state.Heroes[index];int before=h.HitPoints;h.Heal(amount);
                int actual=h.HitPoints-before;total+=actual;if(actual>0) healed.Add(index);
            }
            return new ChainActionResult(targets.Length==1?state.Heroes[targets[0]].Id:"allies",targets.Select(i=>state.Heroes[i].Id),healed,total);
        }
        private static ChainActionResult Empty() => new ChainActionResult("none",Array.Empty<string>(),Array.Empty<int>(),0);
    }
}
