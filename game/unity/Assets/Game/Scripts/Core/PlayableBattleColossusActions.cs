using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed partial class PlayableBattle
    {
        private ColossusActionCombatDef NextColossusStep
        {
            get {
                var cycle=colossusDefinition?.actionCycle;if(cycle==null || cycle.Length==0)return null;
                var step=cycle[EnemyActionCount%cycle.Length];
                if(string.IsNullOrEmpty(step.requiredPartId) || State.Parts.Any(p=>p.Id==step.requiredPartId && !p.IsBroken && !p.Status.Active("stun")))return step;
                return cycle.First(a=>string.IsNullOrEmpty(a.requiredPartId));
            }
        }
        private int NextColossusGaugeGain=>NextColossusStep?.gaugeGain??1;
        private int EnemyWaitPercent=>lastEnemyWasMajor?colossusDefinition?.majorWaitPercent??150:lastColossusWaitPercent;
        private int lastColossusWaitPercent=100;
        public int[] NextEnemyTargets
        {
            get {
                string rule=NextColossusStep?.targetRule??"single";
                if(!IsFormal || NextAttackIsMajor || rule=="all")return EnemyTargetSelector.Resolve(State,true,0);
                int preferred=EnemyActionCount%5;
                var alive=Enumerable.Range(0,State.Heroes.Count).Where(i=>State.Heroes[i].IsAlive).ToArray();
                if(alive.Length>0 && rule=="lowest-hp")preferred=alive.OrderBy(i=>(decimal)State.Heroes[i].HitPoints/State.Heroes[i].MaxHitPoints).ThenBy(i=>i).First();
                if(alive.Length>0 && rule=="highest-resource")preferred=alive.OrderByDescending(i=>State.Heroes[i].JobResource).ThenBy(i=>i).First();
                return EnemyTargetSelector.Resolve(State,false,preferred);
            }
        }
    }
}
