using System;
using System.Collections.Generic;
using System.Linq;
namespace NewAster.Core {
 public readonly struct GamblerSkillTrigger {
  public int Skill {get;} public int Line {get;} public bool Enhanced {get;}
  public GamblerSkillTrigger(int skill,int line,bool enhanced){Skill=skill;Line=line;Enhanced=enhanced;}
 }
 public static class GamblerSlotRules {
  // 0/1 fruit, 2/3/4 skills 1/2/3, 5 seven. Fixed 3 rows + 2 diagonals.
  public const int SymbolCount=6;
  private static readonly int[][] Lines={new[]{0,1,2},new[]{3,4,5},new[]{6,7,8},new[]{0,4,8},new[]{6,4,2}};
  public static IReadOnlyList<GamblerSkillTrigger> Evaluate(int[] board){
   if(board==null || board.Length!=9 || board.Any(s=>s<0 || s>=SymbolCount))throw new ArgumentException("SLOT requires nine valid symbols.");
   var result=new List<GamblerSkillTrigger>();
   for(int line=0;line<Lines.Length;line++){
    var p=Lines[line];int a=board[p[0]],b=board[p[1]],c=board[p[2]];
    if(a==5 && b==5 && c==5){for(int skill=0;skill<3;skill++)for(int n=0;n<2;n++)result.Add(new GamblerSkillTrigger(skill,line,false));}
    else if(a>=2 && a<=4 && a==b)result.Add(new GamblerSkillTrigger(a-2,line,a==c));
   }
   return result.AsReadOnly();
  }
  public static string Label(int symbol)=>symbol==0?"●":symbol==1?"◆":symbol==5?"7":"S"+(symbol-1);
 }
 public sealed partial class PlayableBattle {
  private bool resolvingGamblerSlot;
  private int gamblerSlotActor=-1;
  private decimal gamblerSlotMultiplier=1m;
  private int[] lastSlotSymbols=Array.Empty<int>();
  public int[] LastSlotSymbols=>(int[])lastSlotSymbols.Clone();
  public int LastSlotTriggerCount {get;private set;}
  public bool SpinGamblerSlot(int actor,string selectedTarget)=>SpinGamblerSlot(actor,selectedTarget,max=>random.Next(max));
  internal bool SpinGamblerSlot(int actor,string selectedTarget,Func<int,int> draw){
   if(!JobReady(actor) || !Job(actor,"gambler") || EnemyAttackTargets.Resolve(State,"target.selected-enemy",selectedTarget).Length==0)return false;
   if(draw==null)throw new ArgumentNullException(nameof(draw));
   int seven=State.Heroes[actor].TraitEffect("seven-bp");
   var board=Enumerable.Range(0,9).Select(_=>{if(seven==0)return draw(GamblerSlotRules.SymbolCount);int roll=draw(60000);return roll<10000+seven*6?5:(roll-10000-seven*6)*5/(50000-seven*6);}).ToArray();
   var triggers=GamblerSlotRules.Evaluate(board);lastSlotSymbols=board;LastSlotTriggerCount=triggers.Count;
   LastHealingTargets=Array.Empty<int>();LastActionWasCastStart=false;LastFullChain=false;LastActionChain=0;LastChainActionCount=0;LastChainChecks=Array.Empty<ChainConnection>();
   Log="SLOT ／ "+string.Join(" ",board.Select(GamblerSlotRules.Label));RecordPresentation(BattlePresentationKind.Support,actor,"body",Log,standalone:true);
   int lastSkill=-1,wait=0;resolvingGamblerSlot=true;gamblerSlotActor=actor;
   try{
    foreach(var trigger in triggers){
     if(Ended || !State.Heroes[actor].IsAlive)break;
     gamblerSlotMultiplier=trigger.Enhanced?1.5m:1m;AvailableHero=actor;Acted[actor]=false;
     if(Act(actor,trigger.Skill,selectedTarget)){lastSkill=trigger.Skill;wait=Math.Max(wait,Timing(actor,trigger.Skill).RecoveryPercent);}
    }
   }finally{resolvingGamblerSlot=false;gamblerSlotActor=-1;gamblerSlotMultiplier=1m;}
   if(lastSkill>=0 && !Ended && State.Heroes[actor].IsAlive && ChainEligible(actor,lastSkill))ResolveAutomaticChain(actor,skillChainBonuses[lastSkill],(bool[])cumulativeChainActors.Clone());
   int statusWait=FinishHeroStatusAction(actor,lastSkill>=0);
   if(triggers.Count==0 && statusWait==0 && State.Heroes[actor].IsAlive && !Ended){Log="SLOT ／ 全外れ・WT0 ／ すぐにもう一度SLOT";RecordPresentation(BattlePresentationKind.Support,actor,"body",Log,standalone:true);Acted[actor]=false;AvailableHero=actor;readyAt[actor]=Clock;return true;}
   Acted[actor]=true;readyAt[actor]=Clock+SkillTimingDefinition.Delay(State.Heroes[actor].Speed,Math.Max(1,wait))+statusWait;AvailableHero=-1;AdvanceTimeline();return true;
  }
 }
}
