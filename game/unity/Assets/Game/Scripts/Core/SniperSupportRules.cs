using System;
using System.Linq;
namespace NewAster.Core {
 public sealed partial class PlayableBattle {
  private readonly int[] sniperTargets={-1,-1,-1,-1,-1};
  private void InitializeSniperSupport(BattleDeployment deployment){
   if(!UsesJobRulesV2)return;
   var entries=deployment?.SniperSupports??Array.Empty<HomeSniperSupport>();
   if(entries.Any(s=>s==null) || entries.Select(s=>s.heroineId).Distinct().Count()!=entries.Length)throw new ArgumentException("Duplicate sniper deployment.");
   foreach(var entry in entries){int owner=Array.IndexOf(formationIds,entry.heroineId),target=Array.IndexOf(formationIds,entry.targetId);if(owner<0 || target<0 || owner==target || !Job(owner,"sniper"))throw new ArgumentException("Support must be one deployed ally of a sniper.");sniperTargets[owner]=target;}
   for(int i=0;i<5;i++)if(Job(i,"sniper") && sniperTargets[i]<0)sniperTargets[i]=(i+1)%5;
  }
  public int SniperSupportTarget(int actor)=>actor>=0 && actor<5?sniperTargets[actor]:-1;
  public bool IsSniping(int actor)=>IsCasting(actor) && casting[actor].Sniper;
  public bool StartSniperMode(int actor,string target){
   if(!JobReady(actor) || !Job(actor,"sniper") || State.Heroes[actor].JobResource<State.Heroes[actor].JobResourceMax || EnemyAttackTargets.Resolve(State,"target.selected-enemy",target).Length==0)return false;
   var h=State.Heroes[actor];var shot=new BattleSkill("job.sniper.cast-shot",5m,0,attackSnapshot:h.Attack,criticalChanceBp:h.CriticalChanceBp,criticalMultiplierPercent:h.CriticalMultiplierPercent,damageCap:10000,bodyPartProtection:true,attributes:new[]{"銃弾"},traitResourceSpent:true);
   if(!h.SpendResource(h.JobResourceMax))return false;
   casting[actor]=new PendingCast{Sniper=true,Slot=-1,Target=target,Skill=shot};readyAt[actor]=Clock+SkillTimingDefinition.Delay(h.Speed,200);Acted[actor]=true;AvailableHero=-1;
   LastActionWasCastStart=true;LastActionChain=0;LastHealingTargets=Array.Empty<int>();LastFullChain=false;LastChainActionCount=0;LastChainChecks=Array.Empty<ChainConnection>();
   RecordPresentation(BattlePresentationKind.CastStart,actor,target,"狙撃モード：詠唱中は味方全員を支援 ／ 発動予定 "+readyAt[actor]);AdvanceTimeline();return true;
  }
  private void ResolveSniperMode(int actor,PendingCast pending){
   var outcome=BattleActionResolver.Resolve(State,State.Heroes[actor].Id,pending.Skill,pending.Target,max=>random.Next(max));
   LastCastResolvedActor=outcome.Accepted?actor:-1;LastActionChain=outcome.Accepted?1:0;LastActionWasCastStart=false;
   RecordPresentation(outcome.Accepted?BattlePresentationKind.CastRelease:BattlePresentationKind.CastCanceled,actor,pending.Target,outcome.Accepted?"狙撃完了 ／ "+outcome.Damage+"ダメージ・通常支援へ戻る":"狙撃対象消失 ／ 通常支援へ戻る",damage:outcome.Damage,broken:outcome.PartBroken,targetIds:outcome.TargetIds);
   if(outcome.Accepted)SniperSupportReaction(actor,outcome.TargetIds.ToArray());
   readyAt[actor]=Clock+SkillTimingDefinition.Delay(State.Heroes[actor].Speed,150)+FinishHeroStatusAction(actor,outcome.Accepted);Chain=0;chainPending=false;chainMembers.Clear();
  }
  private void SniperSupportReaction(int attackingActor,string[] targets){
   if(!UsesJobRulesV2 || Ended || targets==null)return;
   // One support per actual ally attack; reactions never call this method again.
   string target=targets.FirstOrDefault(t=>t=="body" || State.Parts.Any(p=>p.Id==t && !p.IsBroken));if(target==null)return;
   for(int i=0;i<5;i++){
    if(i==attackingActor || !Job(i,"sniper") || !State.Heroes[i].IsAlive || State.Heroes[i].Status.Active("stun") || State.Heroes[i].Status.Active("absent") || !IsSniping(i) && sniperTargets[i]!=attackingActor || Ended)continue;
    var h=State.Heroes[i];var shot=new BattleSkill("job.sniper.support",.45m,0,criticalChanceBp:h.CriticalChanceBp,criticalMultiplierPercent:h.CriticalMultiplierPercent,damageCap:10000,bodyPartProtection:true,attributes:new[]{"銃弾"},traitResourceSpent:true);
    var hit=BattleActionResolver.Resolve(State,h.Id,shot,target,max=>random.Next(max));
    if(hit.Accepted)RecordPresentation(BattlePresentationKind.Attack,i,target,"支援射撃 ／ 資源・通常WT消費なし",damage:hit.Damage,broken:hit.PartBroken,targetIds:hit.TargetIds,standalone:true);
   }
  }
 }
}
