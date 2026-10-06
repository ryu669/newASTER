using System;
using System.Linq;
namespace NewAster.Core
{
    // v0.2 test tuning; all state is encounter-local, never written into growth saves.
    public sealed class BattleJobState
    {
        public string Id { get; }
        public int Gauge { get; internal set; }
        public int Predation { get; internal set; }
        public bool Reckless { get; internal set; }
        public long EmpoweredUntil { get; internal set; }
        public int[] Magazines { get; } = new[]{6,6};
        public int SelectedMagazine { get; internal set; }
        public int BoostUnits { get; internal set; }
        public int CastPercent { get; internal set; } = 100;
        public int Repeat { get; internal set; } = 1;
        public bool FullVolley { get; internal set; }
        public BattleJobState(string id) { Id=id; }
        public bool Empowered(long clock) => EmpoweredUntil>clock;
    }
    public sealed partial class PlayableBattle
    {
        public bool UsesJobRulesV2 { get; private set; }
        public long BattleTurn => Clock/100;
        private BattleJobState[] jobStates;
        private int guardTarget = -1;
        public BattleJobState JobState(int actor) => jobStates?[actor];
        private bool Job(int actor,string name) => UsesJobRulesV2 && jobStates[actor].Id=="job."+name;
        private void InitializeJobRules(bool enabled,int protectedSlot)
        {
            if(!enabled)return;
            if(!IsFormal || !UsesTimeline)throw new ArgumentException("Job v2 requires formal timeline definitions.");
            if(protectedSlot<0 || protectedSlot>=5)throw new ArgumentOutOfRangeException(nameof(protectedSlot));
            UsesJobRulesV2=true;guardTarget=protectedSlot;
            jobStates=jobProfiles.Select(j=>new BattleJobState(j.id)).ToArray();
            for(int i=0;i<5;i++)State.Heroes[i].UsesBattleTurnDuration=true;
        }
        public string JobDescription(int actor)
        {
            if(!UsesJobRulesV2)return "";
            var j=jobStates[actor];var h=State.Heroes[actor];
            if(Job(actor,"fighter"))return "珠 "+h.JobResource+" / BOOST "+j.Gauge+"/100 / "+(j.Reckless?"捨て身":"通常")+(j.Empowered(Clock)?" / 全能力強化中":"");
            if(Job(actor,"berserker"))return "捕食 "+j.Predation+"/10 / ゲージ "+h.JobResource+"/"+h.JobResourceMax+(j.Empowered(Clock)?" / 二重発動中":"");
            if(Job(actor,"defender"))return "護衛："+HeroineName(guardTarget)+" / ゲージ "+h.JobResource+"/"+h.JobResourceMax+(j.Empowered(Clock)?" / 全員護衛・反撃中":"");
            if(Job(actor,"blaster"))return ResourceName(actor)+" "+h.JobResource+" / 詠唱 "+j.CastPercent+"% / ×"+j.Repeat;
            return "弾倉1 "+j.Magazines[0]+"/6　弾倉2 "+j.Magazines[1]+"/6 / ゲージ "+h.JobResource+"/"+h.JobResourceMax;
        }
        public bool SelectBoostUnits(int actor,int units)
        {
            if(!JobReady(actor) || !Job(actor,"fighter") || units<0 || units>State.Heroes[actor].JobResource)return false;
            jobStates[actor].BoostUnits=units;return true;
        }
        public bool ToggleReckless(int actor)
        {
            if(!JobReady(actor) || !Job(actor,"fighter"))return false;
            jobStates[actor].Reckless=!jobStates[actor].Reckless;UpdateJobStats(actor);return true;
        }
        public bool ActivateJobGauge(int actor)
        {
            if(!JobReady(actor))return false;
            var j=jobStates[actor];var h=State.Heroes[actor];
            if(Job(actor,"fighter")){if(j.Gauge<100)return false;j.Gauge=0;}
            else if(Job(actor,"berserker") || Job(actor,"defender")){if(h.JobResource<h.JobResourceMax)return false;h.SpendResource(h.JobResourceMax);}
            else return false;
            j.EmpoweredUntil=Clock+300;UpdateJobStats(actor);
            RecordPresentation(BattlePresentationKind.Support,actor,"body","固有ゲージ解放 / 3 Battle Turn",standalone:true);return true;
        }
        public bool SelectBlaster(int actor,int castPercent,int repeats)
        {
            if(!JobReady(actor) || !Job(actor,"blaster") || !new[]{0,50,100,200}.Contains(castPercent) || repeats<1 || repeats>3)return false;
            int cost=BlasterCost(castPercent,repeats);if(cost>State.Heroes[actor].JobResource)return false;
            jobStates[actor].CastPercent=castPercent;jobStates[actor].Repeat=repeats;return true;
        }
        public static int BlasterCost(int castPercent,int repeats) => (castPercent==0?4:castPercent==50?2:0)+(repeats-1)*3;
        public bool SelectMagazine(int actor,int magazine)
        {
            if(!JobReady(actor) || !Job(actor,"gunner") || magazine<0 || magazine>1)return false;
            jobStates[actor].SelectedMagazine=magazine;return true;
        }
        public bool Reload(int actor)
        {
            if(!JobReady(actor) || !Job(actor,"gunner"))return false;
            var j=jobStates[actor];j.Magazines[0]=j.Magazines[1]=6;j.FullVolley=false;
            CompleteJobUtility(actor,100,"全弾倉をリロード");return true;
        }
        public bool FullVolley(int actor,string target)
        {
            if(!JobReady(actor) || !Job(actor,"gunner"))return false;
            var h=State.Heroes[actor];var j=jobStates[actor];
            if(h.JobResource<h.JobResourceMax || j.Magazines[j.SelectedMagazine]==0)return false;
            j.FullVolley=true;bool accepted=Act(actor,0,target);if(!accepted)j.FullVolley=false;return accepted;
        }
        private bool JobReady(int actor) => UsesJobRulesV2 && !Ended && actor>=0 && actor<5 && AvailableHero==actor && !IsCasting(actor) && State.Heroes[actor].IsAlive && !State.Heroes[actor].Status.Active("stun") && !State.Heroes[actor].Status.Active("absent");
        private void CompleteJobUtility(int actor,int wait,string message)
        {
            Acted[actor]=true;LastHealingTargets=Array.Empty<int>();LastActionWasCastStart=false;LastActionChain=0;LastFullChain=false;
            RecordPresentation(BattlePresentationKind.Support,actor,"body",message);
            State.Heroes[actor].GainResource(jobProfiles[actor].gainOnAttack);
            readyAt[actor]=Clock+SkillTimingDefinition.Delay(State.Heroes[actor].Speed,wait)+FinishHeroStatusAction(actor,false);
            AvailableHero=-1;AdvanceTimeline();
        }
        private int JobCost(int actor,int slot)
        {
            var j=jobStates[actor];
            if(Job(actor,"fighter"))return Math.Min(j.BoostUnits,State.Heroes[actor].JobResource);
            if(Job(actor,"blaster"))return BlasterCost(j.CastPercent,j.Repeat);
            if(Job(actor,"gunner") && j.FullVolley)return State.Heroes[actor].JobResourceMax;
            return 0;
        }
        private decimal JobPower(int actor)
        {
            var j=jobStates[actor];
            if(Job(actor,"fighter"))return 1m+Math.Min(j.BoostUnits,State.Heroes[actor].JobResource)*.1m;
            if(Job(actor,"blaster") && j.CastPercent==200)return 1.5m;
            if(Job(actor,"gunner") && j.FullVolley)return Math.Max(1,j.Magazines[j.SelectedMagazine]);
            return 1m;
        }
        private int JobRepeat(int actor) => Job(actor,"berserker") && jobStates[actor].Empowered(Clock)?2:Job(actor,"blaster")?jobStates[actor].Repeat:1;
        private bool JobCanCommand(int actor,int slot) => !UsesJobRulesV2 || !Job(actor,"gunner") || !IsAttackSkill(actor,slot) || jobStates[actor].Magazines[jobStates[actor].SelectedMagazine]>0;
        private void ConsumeJobCommand(int actor,int cost,bool attack)
        {
            if(!UsesJobRulesV2)return;var j=jobStates[actor];
            if(Job(actor,"fighter")){j.Gauge=Math.Min(100,j.Gauge+cost*10);j.BoostUnits=0;}
            if(Job(actor,"gunner") && attack){int m=j.SelectedMagazine;j.Magazines[m]=j.FullVolley?0:Math.Max(0,j.Magazines[m]-1);j.FullVolley=false;}
        }
        private void UpdateJobStats(int actor)
        {
            var j=jobStates[actor];var h=State.Heroes[actor];
            int all=Job(actor,"berserker")?j.Predation*5:Job(actor,"fighter") && j.Empowered(Clock)?25:0;
            h.JobAllStatsPercent=all;h.JobAttackPercent=Job(actor,"fighter") && j.Reckless?30:0;
            h.JobSpeedPercent=Job(actor,"fighter") && j.Reckless?20:0;h.JobIncomingPercent=Job(actor,"fighter") && j.Reckless?130:100;
            h.ClampJobHitPoints();
        }
        private void JobAttackReaction(int actor)
        {
            if(!UsesJobRulesV2)return;
            if(Job(actor,"berserker")){jobStates[actor].Predation=Math.Min(10,jobStates[actor].Predation+1);UpdateJobStats(actor);}
        }
        private void GainJobCommandResource(int actor)
        {
            if(Job(actor,"blaster"))State.Heroes[actor].GainResource(jobProfiles[actor].gainAtReady);
        }
        private void ResolveRepeatedAttack(int actor,int slot,string target,int count,BattleSkill snapshot=null)
        {
            for(int n=1;n<count && !Ended && State.Heroes[actor].IsAlive;n++){
                var hit=BattleActionResolver.Resolve(State,State.Heroes[actor].Id,snapshot??AttackDefinition(actor,slot,AttackPower(actor,slot,target,1),0),target,max=>random.Next(max));
                if(!hit.Accepted)break;
                RecordPresentation(BattlePresentationKind.Attack,actor,target,"固有能力：追加発動 / "+hit.Damage+"ダメージ",damage:hit.Damage,broken:hit.PartBroken,targetIds:hit.TargetIds);
                RecordAttackFollowUps(actor,hit);ApplyCommandAttackEffects(actor,slot);ApplyAttackTimedEffects(actor,slot,false);
            }
        }
        private int GuardRecipient(int intended,int[] allTargets)
        {
            if(!UsesJobRulesV2 || allTargets.Length==5 || NextAttackIsMajor || NextColossusStep?.targetRule=="all")return intended;
            for(int i=0;i<5;i++)if(Job(i,"defender") && State.Heroes[i].IsAlive && !State.Heroes[i].Status.Active("stun") && !State.Heroes[i].Status.Active("absent") && (jobStates[i].Empowered(Clock) || !allTargets.Contains(i) && intended==guardTarget))return i;
            return intended;
        }
        private void DefenderReaction(int actor)
        {
            if(!Job(actor,"defender") || !jobStates[actor].Empowered(Clock) || !State.Heroes[actor].IsAlive || Ended)return;
            var hit=BattleActionResolver.Resolve(State,State.Heroes[actor].Id,new BattleSkill("reaction.defender",.6m,0),"body",max=>random.Next(max));
            if(hit.Accepted)RecordPresentation(BattlePresentationKind.Attack,actor,"body","護衛反撃 / "+hit.Damage,damage:hit.Damage,targetIds:hit.TargetIds);
        }
        private void AdvanceClock(long target)
        {
            if(!UsesJobRulesV2){Clock=target;return;}
            // Process every boundary before the event at that time; READY never calls this.
            while(!Ended && (Clock/100+1)*100<=target){
                Clock=(Clock/100+1)*100;
                for(int i=0;i<5;i++){UpdateJobStats(i);State.Heroes[i].TickBattleTurn();}
                State.BossStatus.Tick();foreach(var p in State.Parts)p.Status.Tick();
            }
            Clock=target;
            for(int i=0;i<5;i++)UpdateJobStats(i);
        }
    }
}
