using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    public sealed class SkillTimingDefinition
    {
        public int RecoveryPercent { get; }
        public int CastPercent { get; }
        public SkillTimingDefinition(int recoveryPercent, int castPercent=0)
        {
            if(recoveryPercent<=0 || castPercent<0) throw new ArgumentOutOfRangeException();
            RecoveryPercent=recoveryPercent; CastPercent=castPercent;
        }
        // Simulation ticks, not video seconds. Final tuning/formula is TBD.
        public static long Delay(int speed,int percent)
        {
            if(speed<=0 || percent<0) throw new ArgumentOutOfRangeException();
            return percent==0?0:Math.Max(1,((long)10000*percent+speed*100L-1)/(speed*100L));
        }
    }
    public readonly struct BattleOrderEntry
    {
        public int Actor { get; }
        public long At { get; }
        public bool IsCast { get; }
        public BattleOrderEntry(int actor,long at,bool isCast) { Actor=actor; At=at; IsCast=isCast; }
    }
    public sealed partial class PlayableBattle
    {
        public bool UsesTimeline { get; }
        public long Clock { get; private set; }
        public int AvailableHero { get; private set; }=-1;
        public int LastCastResolvedActor { get; private set; }=-1;
        public bool LastActionWasCastStart { get; private set; }
        public int EnemyActionCount { get; private set; }
        private readonly long[] readyAt=new long[5];
        private readonly bool[] hadCommand=new bool[5];
        private readonly HashSet<int> chainMembers=new HashSet<int>();
        private readonly PendingCast[] casting=new PendingCast[5];
        private long bossAt;
        private int commandCount;
        private readonly SkillTimingDefinition[,] timings;
        private sealed class PendingCast { public int Slot; public string Target; public BattleSkill Skill; public int ChainBonus; public bool[] ChainActors; public int Repeats; }
        public SkillTimingDefinition Timing(int actor,int slot)
        {
            // Vertical-slice profiles only; each skill has independent casting/recovery fields.
            if(actor<0 || actor>=5 || slot<0 || slot>=3) throw new ArgumentOutOfRangeException();
            if(Job(actor,"blaster"))return new SkillTimingDefinition(new[]{75,100,125}[slot],new[]{50,100,150}[slot]*jobStates[actor].CastPercent/100);
            return timings[actor,slot];
        }
        public static SkillTimingDefinition[,] DefaultTimings()
        {
            var result=new SkillTimingDefinition[5,3];
            for(int i=0;i<5;i++) for(int slot=0;slot<3;slot++) result[i,slot]=new SkillTimingDefinition(slot==0?100:slot==1?125:150,i==4 && slot==1?150:0);
            return result;
        }
        public long RecoveryDelay(int actor,int slot) => Job(actor,"chaser")?ChaserDelay(SkillTimingDefinition.Delay(State.Heroes[actor].Speed,Timing(actor,slot).RecoveryPercent),ChaserSelectedRecovery(actor)):SkillTimingDefinition.Delay(State.Heroes[actor].Speed,Timing(actor,slot).RecoveryPercent);
        public long CastDelay(int actor,int slot) => SkillTimingDefinition.Delay(State.Heroes[actor].Speed,Timing(actor,slot).CastPercent);
        public bool IsCasting(int actor) => actor>=0 && actor<5 && casting[actor]!=null;
        public long NextAt(int actor) => readyAt[actor];
        public string TimingDescription(int actor,int slot)
        {
            var d=Timing(actor,slot);
            return (d.CastPercent>0?"詠唱："+(d.CastPercent>=150?"長い":d.CastPercent>=100?"普通":"短い")+" "+CastDelay(actor,slot)+" / ":"")+"待機："+(d.RecoveryPercent>=150?"長い":d.RecoveryPercent>=125?"やや長い":d.RecoveryPercent<100?"短い":"標準")+" "+RecoveryDelay(actor,slot)+((commandDefinitions?[actor,slot].selfWaitReductionPercent??0)>0?"（短縮後 "+CommandRecoveryDelay(actor,slot)+"）":"");
        }
        private int NextChain(int actor) => !UsesTimeline && chainPending && Chain<5?Chain+1:1;
        private void InitializeTimeline()
        {
            for(int i=0;i<5;i++) readyAt[i]=SkillTimingDefinition.Delay(State.Heroes[i].Speed,100);
            bossAt=SkillTimingDefinition.Delay(EffectiveEnemySpeed,100);
            AdvanceTimeline();
        }
        public IReadOnlyList<BattleOrderEntry> UpcomingOrder()
        {
            // Exact currently committed events, not guesses about future unselected skills.
            return Enumerable.Range(0,5).Where(i=>State.Heroes[i].IsAlive)
                .Select(i=>new BattleOrderEntry(i,readyAt[i],casting[i]!=null))
                .Concat(Ended?Array.Empty<BattleOrderEntry>():new[]{new BattleOrderEntry(-1,bossAt,false)})
                .OrderBy(e=>e.At).ThenBy(e=>e.IsCast?0:e.Actor<0?1:2).ThenBy(e=>e.Actor).ToArray();
        }
        public void Pass()
        {
            if(!UsesTimeline || Ended || AvailableHero<0) return;
            if(!CanPass(AvailableHero))return;
            LastActionWasCastStart=false; LastCastResolvedActor=-1;
            LastFullChain=false; LastChainActionCount=0;LastActionChain=0;LastChainChecks=Array.Empty<ChainConnection>();
            int actor=AvailableHero; Acted[actor]=true; readyAt[actor]=Clock+SkillTimingDefinition.Delay(State.Heroes[actor].Speed,UsesJobRulesV2?25:Timing(actor,0).RecoveryPercent);
            State.Heroes[actor].CompleteOwnerCommand();
            if(optionalResourceBoost)readyAt[actor]+=FinishHeroStatusAction(actor,false);
            ResourceBoostSelected=false;
            Chain=0; chainPending=false; chainMembers.Clear(); LastHealingTargets=Array.Empty<int>();
            Log="味方"+(actor+1)+"はパス。次回まで待機。";
            RecordPresentation(BattlePresentationKind.Pass,actor,"body",Log);
            AvailableHero=-1; AdvanceTimeline();
        }
        private bool StartCasting(int actor,int slot,string target)
        {
            if(HealingSkill(actor,slot)!=null || PreviewDamage(actor,slot,target)==0) { Log="詠唱対象または資源を確認してください。"; return false; }
            int cost=SkillResourceCost(actor,slot);int repeats=UsesJobRulesV2?JobRepeat(actor):1;long castDelay=CastDelay(actor,slot);
            var reservedSkill=AttackDefinition(actor,slot,AttackPower(actor,slot,target,1),0,State.Heroes[actor].Attack);
            if(!State.Heroes[actor].SpendResource(cost)) return false;
            ResourceBoostSelected=false;
            casting[actor]=new PendingCast { Slot=slot,Target=target,Skill=reservedSkill,ChainBonus=skillChainBonuses[slot],ChainActors=(bool[])cumulativeChainActors.Clone(),Repeats=repeats };
            ConsumeJobCommand(actor,cost,true);
            State.Heroes[actor].CompleteOwnerCommand();
            readyAt[actor]=Clock+castDelay; Acted[actor]=true; AvailableHero=-1;
            Chain=0; chainPending=false; chainMembers.Clear(); LastActionChain=0;
            LastHealingTargets=Array.Empty<int>(); LastActionWasCastStart=true; LastCastResolvedActor=-1;
            Log="味方"+(actor+1)+"：詠唱開始（発動予定 "+readyAt[actor]+"）。";
            RecordPresentation(BattlePresentationKind.CastStart,actor,target,Log);
            AdvanceTimeline(); return true;
        }
        private void AdvanceTimeline()
        {
            while(!Ended) {
                for(int i=0;i<5;i++) if(!State.Heroes[i].IsAlive && casting[i]!=null) {
                    casting[i]=null; RecordPresentation(BattlePresentationKind.CastCanceled,i,"body","戦闘不能により詠唱中断。");
                }
                var next=UpcomingOrder().First(); AdvanceClock(next.At);if(Ended)break;
                if(next.Actor<0) {
                    ResolveEnemyAction(); EnemyActionCount++; Turn++;
                    Guarded=false; Chain=0; chainPending=false; chainMembers.Clear();
                    bossAt=Clock+SkillTimingDefinition.Delay(EffectiveEnemySpeed,EnemyWaitPercent)+State.EnemyWaitPenalty;State.EnemyWaitPenalty=0;
                    continue;
                }
                int actor=next.Actor;
                if(optionalResourceBoost && (State.Heroes[actor].Status.Active("stun") || State.Heroes[actor].Status.Active("absent"))){var h=State.Heroes[actor];bool canceled=casting[actor]!=null;casting[actor]=null;int wait=FinishHeroStatusAction(actor,false);readyAt[actor]=Clock+SkillTimingDefinition.Delay(h.Speed,Timing(actor,0).RecoveryPercent)+wait;RecordPresentation(canceled?BattlePresentationKind.CastCanceled:BattlePresentationKind.Pass,actor,"body","状態異常により行動をスキップ。",standalone:true);continue;}
                var pending=casting[actor];
                if(pending!=null) {
                    casting[actor]=null;
                    // An already broken target cancels this spell; no silent retarget/refund.
                    var outcome=BattleActionResolver.Resolve(State,State.Heroes[actor].Id,pending.Skill,pending.Target,max=>random.Next(max));
                    Log+="\n味方"+(actor+1)+(outcome.Accepted?"：詠唱発動 / "+outcome.Damage+"ダメージ"+(outcome.PartBroken?" / 部位破壊":""):"：対象消失により詠唱不発（消費済み）");
                    LastCastResolvedActor=outcome.Accepted?actor:-1;
                    LastFullChain=false;LastChainActionCount=0;LastActionChain=outcome.Accepted?1:0;LastChainChecks=Array.Empty<ChainConnection>();
                    RecordPresentation(outcome.Accepted?BattlePresentationKind.CastRelease:BattlePresentationKind.CastCanceled,actor,pending.Target,outcome.Accepted?"詠唱発動 / "+outcome.Damage+"ダメージ"+(outcome.Critical?" / CRITICAL":"")+(outcome.CriticalRoll>=0?"（会心判定 "+outcome.CriticalRoll+"）":""):"対象消失により詠唱不発（消費済み）",broken:outcome.PartBroken,damage:outcome.Damage,targetIds:outcome.TargetIds);
                    if(outcome.Accepted) RecordAttackFollowUps(actor,outcome);
                    if(outcome.Accepted) ApplyAttackTimedEffects(actor,pending.Slot,false);
                    if(outcome.Accepted) ApplyCommandAttackEffects(actor,pending.Slot);
                    if(outcome.Accepted && UsesJobRulesV2)ResolveRepeatedAttack(actor,pending.Slot,pending.Target,pending.Repeats,pending.Skill);
                    if(outcome.Accepted && ChainEligible(actor,pending.Slot)) ResolveAutomaticChain(actor,pending.ChainBonus,pending.ChainActors);
                    if(outcome.Accepted)GainJobCommandResource(actor);
                    readyAt[actor]=Clock+CommandRecoveryDelay(actor,pending.Slot)+(optionalResourceBoost?FinishHeroStatusAction(actor,outcome.Accepted):0);
                    Chain=0; chainPending=false; chainMembers.Clear();
                    continue;
                }
                AvailableHero=actor;
                for(int i=0;i<5;i++) Acted[i]=i!=actor;
                int regeneration=UsesJobRulesV2?0:State.Heroes[actor].RegenerateAtOwnerReady();
                if(regeneration>0) RecordPresentation(BattlePresentationKind.Healing,actor,"body","再生 / HP ＋"+regeneration,healingTargets:new[]{actor},standalone:true);
                if(hadCommand[actor] && !UsesJobRulesV2) State.Heroes[actor].GainResource(IsFormal?jobProfiles[actor].gainAtReady:3);
                hadCommand[actor]=true;
                State.BeginTurn(unchecked(Seed+(++commandCount)*97+State.SelectedLevel),.25m);
                GenerateChainModifiers();
                return;
            }
            AvailableHero=-1;
            for(int i=0;i<5;i++) { Acted[i]=true; casting[i]=null; }
        }
    }
}
