using System;
using System.Linq;
namespace NewAster.Core
{
    // v0.2 test tuning; all state is encounter-local, never written into growth saves.
    public sealed class BattleJobState
    {
        public string Id { get; }
        public int Gear {get;internal set;}=1;
        public int NitroCount {get;internal set;}
        public bool NitroSelected {get;internal set;}
        public int ChaserRecoveryPercent {get;internal set;}=100;
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
        public bool Singing { get; internal set; }
        public int SongStage { get; internal set; }
        internal int SongOopartBoost;
        internal int SongCostRemainderBp;
        public string ArmorResistance {get;internal set;}="physical";
        public string[] PanzerTools {get;}=new[]{"repair","guard"};
        public int[] ToolUses {get;}=new[]{2,2};
        public bool PanzerConfigured {get;internal set;}
        public long DefendingUntil {get;internal set;}=-1;
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
        private void InitializeJobRules(bool enabled,int protectedSlot,PanzerLoadout loadout)
        {
            if(!enabled)return;
            if(!IsFormal || !UsesTimeline)throw new ArgumentException("Job v2 requires formal timeline definitions.");
            if(protectedSlot<0 || protectedSlot>=5)throw new ArgumentOutOfRangeException(nameof(protectedSlot));
            UsesJobRulesV2=true;guardTarget=protectedSlot;
            jobStates=jobProfiles.Select(j=>new BattleJobState(j.id)).ToArray();
            for(int i=0;i<5;i++){State.Heroes[i].UsesBattleTurnDuration=true;if(Job(i,"panzer")){var l=loadout??new PanzerLoadout();var j=jobStates[i];j.ArmorResistance=l.Resistance;j.PanzerTools[0]=l.FirstTool;j.PanzerTools[1]=l.SecondTool;j.PanzerConfigured=true;State.Heroes[i].InitializePanzer(()=>Clock);State.Heroes[i].PanzerFireResistance=l.Resistance=="fire";int actor=i;State.Heroes[i].ArmorBroke=()=>RecordPresentation(BattlePresentationKind.Support,actor,"body","ARMOR BREAK ／ 生身で防御・300 Clock生存で再召喚",standalone:true);}}
        }
        public string JobDescription(int actor)
        {
            if(!UsesJobRulesV2)return "";
            var j=jobStates[actor];var h=State.Heroes[actor];
            if(Job(actor,"sniper"))return "狙撃 "+h.JobResource+"/"+h.JobResourceMax+(IsSniping(actor)?" ／ 詠唱中・全員支援":" ／ 支援対象："+HeroineName(sniperTargets[actor]));
            if(Job(actor,"gambler"))return "SLOT ／ 資源なし・3×3・5ライン"+(lastSlotSymbols.Length==9?" ／ "+"結果は下の9マス ／ "+LastSlotTriggerCount+"発動":"");
            if(Job(actor,"general"))return actor==CommanderActor?"指揮 "+h.JobResource+"/"+h.JobResourceMax+(JobEmpowered(actor)?" ／ 5枠強化中":" ／ 指揮官・5枠の固有効果"):"非指揮官 ／ 3スキルのみ";
            if(Job(actor,"chaser"))return "駆動 "+h.JobResource+"/"+h.JobResourceMax+" ／ GEAR "+j.Gear+" ／ NITRO "+j.NitroCount+"/10"+(j.NitroSelected?"・次回WT0":"");
            if(Job(actor,"alchemist"))return "錬成 "+h.JobResource+"/"+h.JobResourceMax+" ／ 5属性を投入・行動消費なし";
            if(Job(actor,"panzer"))return (h.ArmorActive?"ARMOR "+h.HitPoints+"/"+h.MaxHitPoints+" ／ 耐性 "+(j.ArmorResistance=="physical"?"物理":j.ArmorResistance=="magic"?"魔法":"火"):"生身 "+h.HitPoints+"/"+h.MaxHitPoints+" ／ CALLまで "+Math.Max(0,h.ArmorCallAt-Clock))+" ／ ツール "+j.ToolUses[0]+"・"+j.ToolUses[1];
            if(Job(actor,"fighter"))return "珠 "+h.JobResource+" / BOOST "+j.Gauge+"/100 / "+(j.Reckless?"捨て身":"通常")+(JobEmpowered(actor)?" / 全能力強化中":"");
            if(Job(actor,"berserker"))return "捕食 "+j.Predation+"/10 / ゲージ "+h.JobResource+"/"+h.JobResourceMax+(JobEmpowered(actor)?" / 二重発動中":"");
            if(Job(actor,"defender"))return "護衛："+HeroineName(guardTarget)+" / ゲージ "+h.JobResource+"/"+h.JobResourceMax+(JobEmpowered(actor)?" / 全員護衛・反撃中":"");
            if(Job(actor,"blaster"))return ResourceName(actor)+" "+h.JobResource+" / 詠唱 "+j.CastPercent+"% / ×"+j.Repeat;
            if(Job(actor,"artist"))return "歌唱ゲージ "+h.JobResource+"/"+h.JobResourceMax+(j.Singing?" / 歌唱中・共鳴 "+j.SongStage+"/5 / 毎ターン2消費":" / 通常行動で＋2、3以上で歌唱開始");
            if(Job(actor,"healer"))return "生命 "+h.JobResource+"/"+h.JobResourceMax+" / 固有操作：回復・超過・最大HP・蘇生・投資";
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
            j.EmpoweredUntil=Clock+300;if(UsesOoparts)h.AddOopartBuff(new BuffInstance{sourceActorId=h.Id,sourceId="job.gauge",kind="job.empowered",initialValue=25*(100+OopartBonus(actor,"buff-power"))/100,value=25*(100+OopartBonus(actor,"buff-power"))/100,expiresAt=Clock+300});UpdateJobStats(actor);
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
            var h=State.Heroes[actor];var j=jobStates[actor];UpdateGeneralFormationStats(actor);
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
            if(Job(actor,"fighter")){int requested=Math.Min(j.BoostUnits,State.Heroes[actor].JobResource);return State.Heroes[actor].Id=="heroine.annihilator"?Math.Min(requested,commandDefinitions[actor,slot].chargeConsumeMax):requested;}
            if(Job(actor,"chaser"))return jobStates[actor].NitroSelected?0:ChaserGearCost(jobStates[actor].Gear);
            if(Job(actor,"healer") || Job(actor,"artist")){int cost=commandDefinitions[actor,slot].resourceCost;return cost*(10000-State.Heroes[actor].TraitEffect("resource-discount-bp"))/10000;}
            if(Job(actor,"blaster"))return BlasterCost(j.CastPercent,j.Repeat);
            if(Job(actor,"gunner") && j.FullVolley)return State.Heroes[actor].JobResourceMax;
            return 0;
        }
        private decimal JobPower(int actor,int slot)
        {
            var j=jobStates[actor];
            if(Job(actor,"fighter"))return 1m+JobCost(actor,slot)*(State.Heroes[actor].Id=="heroine.annihilator"?commandDefinitions[actor,slot].chargeBonusPercent/100m:.1m);
            if(Job(actor,"blaster") && j.CastPercent==200)return 1.5m;
            if(Job(actor,"gunner") && j.FullVolley)return Math.Max(1,j.Magazines[j.SelectedMagazine]);
            return 1m;
        }
        private int JobRepeat(int actor) => Job(actor,"berserker") && JobEmpowered(actor)?2:Job(actor,"blaster")?jobStates[actor].Repeat:1;
        private bool JobCanCommand(int actor,int slot) => !UsesJobRulesV2 || (!Job(actor,"gambler") || resolvingGamblerSlot && gamblerSlotActor==actor) && !RequiresPanzerDefense(actor) && ChaserCommandValid(actor) && !(Job(actor,"artist") && jobStates[actor].Singing) && (!Job(actor,"gunner") || !IsAttackSkill(actor,slot) || jobStates[actor].Magazines[jobStates[actor].SelectedMagazine]>0);
        private void ConsumeJobCommand(int actor,int cost,bool attack)
        {
            if(!UsesJobRulesV2)return;var j=jobStates[actor];CommitChaserGear(actor,cost);
            if(Job(actor,"fighter")){j.Gauge=Math.Min(100,j.Gauge+cost*10);j.BoostUnits=0;}
            if(Job(actor,"gunner") && attack){int m=j.SelectedMagazine;j.Magazines[m]=j.FullVolley?0:Math.Max(0,j.Magazines[m]-1);j.FullVolley=false;}
        }
        private void UpdateJobStats(int actor)
        {
            var j=jobStates[actor];var h=State.Heroes[actor];
            UpdateGeneralFormationStats(actor);
            int all=Job(actor,"berserker")?j.Predation*(5+h.TraitEffect("predation-power")):Job(actor,"fighter") && JobEmpowered(actor)?(UsesOoparts?h.OopartBuffs.First(b=>b.kind=="job.empowered").value:25):0;
            int song=jobStates.Where((s,i)=>s.Singing && State.Heroes[i].IsAlive).Select(s=>s.SongStage*5*(100+s.SongOopartBoost)/100).DefaultIfEmpty(0).Max();
            h.JobAllStatsPercent=all;h.JobAttackPercent=(Job(actor,"fighter") && j.Reckless?30:0)+song;
            h.SongCriticalBonusBp=song*20;
            h.JobSpeedPercent=Job(actor,"fighter") && j.Reckless?20:0;h.JobIncomingPercent=Job(actor,"fighter") && j.Reckless?130:100;
            h.ClampJobHitPoints();
        }
        private void JobAttackReaction(int actor)
        {
            if(!UsesJobRulesV2)return;
            if(Job(actor,"gunner") && State.Heroes[actor].TraitEffect("reload-bp")>0 && random.Next(10000)<State.Heroes[actor].TraitEffect("reload-bp"))jobStates[actor].Magazines[jobStates[actor].SelectedMagazine]=Math.Min(6,jobStates[actor].Magazines[jobStates[actor].SelectedMagazine]+1);
            if(Job(actor,"berserker")){jobStates[actor].Predation=Math.Min(10+State.Heroes[actor].TraitEffect("predation-cap"),jobStates[actor].Predation+1);UpdateJobStats(actor);}
        }
        private void GainJobCommandResource(int actor)
        {
            if(Job(actor,"healer") && State.Heroes[actor].TraitEffect("heart-bp")>0 && random.Next(10000)<State.Heroes[actor].TraitEffect("heart-bp"))State.Heroes[actor].GainResource(1);
            if(Job(actor,"sniper"))State.Heroes[actor].GainResource(3);
            if(Job(actor,"general") && actor==CommanderActor)State.Heroes[actor].GainResource(jobProfiles[actor].gainAtReady);
            if(Job(actor,"alchemist") || Job(actor,"blaster") || Job(actor,"healer") || Job(actor,"artist") && !jobStates[actor].Singing)State.Heroes[actor].GainResource(jobProfiles[actor].gainAtReady);
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
            for(int i=0;i<5;i++)if(Job(i,"defender") && State.Heroes[i].IsAlive && !State.Heroes[i].Status.Active("stun") && !State.Heroes[i].Status.Active("absent") && (JobEmpowered(i) || !allTargets.Contains(i) && intended==guardTarget))return i;
            return intended;
        }
        private void DefenderReaction(int actor)
        {
            if(!Job(actor,"defender") || !JobEmpowered(actor) || !State.Heroes[actor].IsAlive || Ended)return;
            var hit=BattleActionResolver.Resolve(State,State.Heroes[actor].Id,new BattleSkill("reaction.defender",.6m,0),"body",max=>random.Next(max));
            if(hit.Accepted)RecordPresentation(BattlePresentationKind.Attack,actor,"body","護衛反撃 / "+hit.Damage,damage:hit.Damage,targetIds:hit.TargetIds);
        }
        private void AdvanceClock(long target)
        {
            if(!UsesJobRulesV2){Clock=target;foreach(var h in State.Heroes)h.TickOopartBuffs();return;}
            // Process every boundary before the event at that time; READY never calls this.
            while(!Ended){
                long boundary=(Clock/100+1)*100;
                long call=State.Heroes.Where(h=>h.IsPanzer && !h.ArmorActive && h.IsAlive && h.ArmorCallAt>Clock).Select(h=>h.ArmorCallAt).DefaultIfEmpty(long.MaxValue).Min();
                long next=Math.Min(boundary,call);if(next>target)break;Clock=next;foreach(var hero in State.Heroes)hero.TickOopartBuffs();TickPanzerCalls();
                if(next==boundary){TickChaserResources();TickSongs();for(int i=0;i<5;i++){UpdateJobStats(i);State.Heroes[i].TickBattleTurn();State.Heroes[i].TickCombatTraits();}State.BossStatus.Tick();foreach(var p in State.Parts)p.Status.Tick();}
            }
            Clock=target;foreach(var hero in State.Heroes)hero.TickOopartBuffs();
            TickPanzerCalls();
            for(int i=0;i<5;i++)if(jobStates[i].Singing && !State.Heroes[i].IsAlive){jobStates[i].Singing=false;jobStates[i].SongStage=0;}
            for(int i=0;i<5;i++)UpdateJobStats(i);
        }
        public bool StartSong(int actor)
        {
            if(!JobReady(actor) || !Job(actor,"artist") || jobStates[actor].Singing || State.Heroes[actor].JobResource<3)return false;
            jobStates[actor].Singing=true;jobStates[actor].SongStage=1;jobStates[actor].SongOopartBoost=OopartBonus(actor,"buff-power")+State.Heroes[actor].TraitEffect("resource-buff");
            RefreshSongStats();CompleteJobUtility(actor,100,"Rの歌唱開始：味方の攻撃と会心を支援");return true;
        }
        public bool ContinueSong(int actor)
        {
            if(!JobReady(actor) || !Job(actor,"artist") || !jobStates[actor].Singing)return false;
            CompleteJobUtility(actor,100,"歌唱を継続");return true;
        }
        public bool StopSong(int actor)
        {
            if(!JobReady(actor) || !Job(actor,"artist") || !jobStates[actor].Singing)return false;
            jobStates[actor].Singing=false;jobStates[actor].SongStage=0;RefreshSongStats();
            RecordPresentation(BattlePresentationKind.Support,actor,"body","歌唱解除：同じREADYでスキルを選べます",standalone:true);return true;
        }
        private void RefreshSongStats(){for(int i=0;i<5;i++)UpdateJobStats(i);}
        private void TickSongs()
        {
            for(int i=0;i<5;i++)if(jobStates[i].Singing){
                var j=jobStates[i];var h=State.Heroes[i];
                if(!h.IsAlive || h.Status.Active("stun") || h.Status.Active("absent") || !h.SpendResource(SongResourceCost(i))){j.Singing=false;j.SongStage=0;RecordPresentation(BattlePresentationKind.Support,i,"body","歌唱終了：ゲージ不足または行動不能",standalone:true);}
                else j.SongStage=Math.Min(5,j.SongStage+1);
            }
        }
        internal int SongResourceCost(int actor)
        {
            int raw=2*(10000-State.Heroes[actor].TraitEffect("resource-discount-bp"))+jobStates[actor].SongCostRemainderBp;
            jobStates[actor].SongCostRemainderBp=raw%10000;return raw/10000;
        }
    }
}
