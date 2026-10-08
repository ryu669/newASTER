using System;
using System.Linq;
namespace NewAster.Core {
 public static class ExtendedSkillEffects {
  public static void Validate(SkillCombatDef s){
   if(s.bodyDamageBonusPercent<0 || s.bodyDamageBonusPercent>200 || s.enemyStatusExtensionTurns<0 || s.enemyStatusExtensionTurns>3 || s.alliesEffectExtensionTurns<0 || s.alliesEffectExtensionTurns>3)throw new ArgumentException("Invalid source extension or body bonus.");
   var kinds=s.enemyStatusExtensionKinds??Array.Empty<string>();if(kinds.Any(k=>k!="poison" && k!="burn" && k!="bleed") || kinds.Distinct().Count()!=kinds.Length || (s.enemyStatusExtensionTurns>0)!=(kinds.Length>0))throw new ArgumentException("Status extension requires explicit poison/burn/bleed kinds.");
   TimedSelfEffectDef.ValidateAll(s.postAttackAlliesEffects);
   if(s.effectRuleId!="effect.damage" && (s.bodyDamageBonusPercent>0 || s.enemyStatusExtensionTurns>0 || s.alliesEffectExtensionTurns>0 || (s.postAttackAlliesEffects?.Length??0)>0))throw new ArgumentException("Attack extensions require an attack skill.");
  }
 }
 public sealed partial class PlayableBattle {
  private void ApplyExtendedSkillEffects(int actor,int slot){
   var d=commandDefinitions?[actor,slot];if(d==null)return;
   if(d.enemyStatusExtensionTurns>0)foreach(var target in alchemyHitTargets.Distinct())State.EnemyStatus(target).ExtendActive(d.enemyStatusExtensionKinds,d.enemyStatusExtensionTurns);
   if(d.postAttackAlliesEffects!=null)foreach(var hero in State.Heroes.Where(h=>h.IsAlive))ApplySourceBuffs(actor,State.Heroes.ToList().IndexOf(hero),d.id,d.postAttackAlliesEffects);
   for(int n=0;!UsesOoparts && n<d.alliesEffectExtensionTurns;n++)foreach(var hero in State.Heroes.Where(h=>h.IsAlive))hero.ExtendPositiveTimedEffects();
  }
 }
}
