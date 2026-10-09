using System;
using System.Collections.Generic;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class SkillConditionDef
    {
        public string kind;
        public string referenceId;
        public int threshold;
        public void Validate()
        {
            int max=kind=="resource-at-least"?10:kind=="job-resource-at-least"?15:kind=="hp-at-most-percent"?100:kind=="broken-parts-at-least"?4:kind=="trait-equipped" || kind=="boss-status-active"?1:-1;
            if(max<0 || threshold<0 || threshold>max) throw new ArgumentException("Unknown skill condition or threshold.");
            if(kind=="trait-equipped" && (threshold!=1 || string.IsNullOrEmpty(referenceId) || !System.Text.RegularExpressions.Regex.IsMatch(referenceId,"^[a-z0-9._-]{1,96}$"))) throw new ArgumentException("Invalid trait condition.");
            if(kind=="boss-status-active" && (threshold!=1 || !EnemyStatusState.Kinds.Contains(referenceId))) throw new ArgumentException("Invalid status condition.");
            if(kind!="trait-equipped" && kind!="boss-status-active" && !string.IsNullOrEmpty(referenceId)) throw new ArgumentException("Unexpected condition reference.");
        }
        public SkillConditionDef Copy() => new SkillConditionDef {kind=kind,threshold=threshold,referenceId=referenceId};
        public static void ValidateAll(IEnumerable<SkillConditionDef> conditions)
        {
            var items=(conditions??Array.Empty<SkillConditionDef>()).ToArray();
            if(items.Any(c=>c==null) || items.Select(c=>c.kind).Distinct().Count()!=items.Length) throw new ArgumentException("Duplicate or missing skill condition.");
            foreach(var item in items) item.Validate();
        }
        public static bool AllSatisfied(BattleState state,int actor,IEnumerable<SkillConditionDef> conditions)
        {
            var items=(conditions??Array.Empty<SkillConditionDef>()).ToArray();ValidateAll(items);
            if(state==null || actor<0 || actor>=state.Heroes.Count) throw new ArgumentException("Invalid condition context.");
            var hero=state.Heroes[actor];
            return hero.IsAlive && items.All(c=>c.kind=="resource-at-least" || c.kind=="job-resource-at-least"?hero.JobResource>=c.threshold:
                c.kind=="trait-equipped"?hero.HasVisibleTrait(c.referenceId):
                c.kind=="boss-status-active"?state.BossStatus.Active(c.referenceId):
                c.kind=="hp-at-most-percent"?(long)hero.HitPoints*100<=(long)hero.MaxHitPoints*c.threshold:
                state.Parts.Count(p=>p.IsBroken)>=c.threshold);
        }
    }
    public static class EnemyTargetSelector
    {
        public static int[] Resolve(BattleState state,bool allLiving,int preferredActor)
        {
            if(state==null) throw new ArgumentNullException(nameof(state));
            var alive=Enumerable.Range(0,state.Heroes.Count).Where(i=>state.Heroes[i].IsAlive).ToArray();
            if(allLiving || alive.Length==0) return alive;
            var forced=alive.Where(i=>state.Heroes[i].ForcedTarget).ToArray();
            return new[]{forced.Length>0?forced[0]:alive.Contains(preferredActor)?preferredActor:alive[0]};
        }
    }
}
