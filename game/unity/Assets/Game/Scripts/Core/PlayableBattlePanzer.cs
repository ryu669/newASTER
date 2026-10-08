using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed class PanzerLoadout
    {
        public string Resistance {get;}
        public string FirstTool {get;}
        public string SecondTool {get;}
        public PanzerLoadout(string resistance="physical",string firstTool="repair",string secondTool="guard")
        {if(!new[]{"physical","magic","fire"}.Contains(resistance) || !PlayableBattle.PanzerTools.Contains(firstTool) || !PlayableBattle.PanzerTools.Contains(secondTool) || firstTool==secondTool)throw new ArgumentException("Invalid panzer loadout.");Resistance=resistance;FirstTool=firstTool;SecondTool=secondTool;}
    }
    public sealed partial class BattleHero
    {
        public bool IsPanzer { get; private set; }
        public bool ArmorActive { get; private set; }
        public int FleshHitPoints { get; private set; }
        public long ArmorCallAt { get; private set; }
        internal bool PanzerFireResistance;
        internal Func<long> PanzerClock;
        internal Action ArmorBroke;
        private int OrdinaryMaxHp => (int)Math.Min(int.MaxValue,(long)baseMaxHitPoints*(100+JobAllStatsPercent+LifeMaxHpPercent+OopartStatPercent("hp"))/100);
        internal int OopartReferenceMaxHp=>IsPanzer?(ArmorActive?baseMaxHitPoints*3:Math.Max(1,baseMaxHitPoints/5)):baseMaxHitPoints;
        public int ArmorMaxHitPoints => (int)Math.Min(int.MaxValue,(long)OrdinaryMaxHp*3);
        public int FleshMaxHitPoints => Math.Max(1,OrdinaryMaxHp/5);
        internal void InitializePanzer(Func<long> clock){IsPanzer=true;ArmorActive=true;PanzerClock=clock;FleshHitPoints=FleshMaxHitPoints;HitPoints=ArmorMaxHitPoints;}
        private void PanzerBreak(){ArmorActive=false;ArmorCallAt=PanzerClock()+300;HitPoints=Math.Min(FleshMaxHitPoints,FleshHitPoints);ArmorBroke?.Invoke();}
        internal bool TryArmorCall(long clock){if(!IsPanzer || ArmorActive || !IsAlive || clock<ArmorCallAt)return false;FleshHitPoints=HitPoints;ArmorActive=true;HitPoints=Math.Max(1,ArmorMaxHitPoints/2);return true;}
        internal void RepairArmor(int amount){if(IsPanzer && ArmorActive && IsAlive)HitPoints=(int)Math.Min(ArmorMaxHitPoints,(long)HitPoints+Math.Max(0,amount));}
    }
    public sealed partial class PlayableBattle
    {
        public bool RequiresPanzerDefense(int actor)=>actor>=0 && actor<5 && State.Heroes[actor].IsPanzer && !State.Heroes[actor].ArmorActive;
        public bool CanPass(int actor)=>actor>=0 && actor<5 && !RequiresPanzerDefense(actor) && !(JobState(actor)?.Singing??false);
        public static readonly string[] PanzerTools={"repair","guard","rally","extend"};
        public static string PanzerToolName(string id)=>id=="repair"?"装甲修復":id=="guard"?"防護展開":id=="rally"?"味方鼓舞":"効果延長";
        public bool CanUsePanzerTool(int actor,int slot,int ally)
        {
            if(!JobReady(actor) || !Job(actor,"panzer") || RequiresPanzerDefense(actor) || slot<0 || slot>1 || jobStates[actor].ToolUses[slot]<=0)return false;
            string tool=jobStates[actor].PanzerTools[slot];
            if(tool=="repair")return State.Heroes[actor].HitPoints<State.Heroes[actor].MaxHitPoints;
            if(tool=="rally")return ally>=0 && ally<5 && State.Heroes[ally].IsAlive;
            return tool!="extend" || State.Heroes.Any(h=>h.IsAlive && (h.TimedEffects.Any(e=>e.Kind!="attack-reduction") || h.OopartBuffs.Any(b=>b.extendable && b.kind!="attack-reduction" && b.value>0)));
        }
        public bool UsePanzerTool(int actor,int slot,int ally=0)
        {
            if(!CanUsePanzerTool(actor,slot,ally))return false;
            var j=jobStates[actor];string tool=j.PanzerTools[slot];j.ToolUses[slot]--;
            if(tool=="repair")State.Heroes[actor].RepairArmor(State.Heroes[actor].ArmorMaxHitPoints*40/100);
            else if(tool=="guard")ApplySourceBuffs(actor,actor,"panzer.guard",new[]{new TimedSelfEffectDef{kind="physical-protection",percent=35,turns=3}});
            else if(tool=="rally"){var buffs=new[]{new TimedSelfEffectDef{kind="attack",percent=25,turns=3}};if(State.Heroes[ally].OopartClock!=null)State.Heroes[ally].ApplyOopartBuffs(buffs,State.Heroes[actor].Id,"panzer.rally",OopartBonus(actor,"buff-power"));else State.Heroes[ally].ApplySelfEffects(buffs);}
            else foreach(var h in State.Heroes.Where(h=>h.IsAlive))h.ExtendTimedEffects();
            Chain=0;chainPending=false;chainMembers.Clear();CompleteJobUtility(actor,100,PanzerToolName(tool)+" ／ 残り "+j.ToolUses[slot]+"回");return true;
        }
        public bool DefendPanzer(int actor)
        {
            if(!JobReady(actor) || !Job(actor,"panzer") || !RequiresPanzerDefense(actor))return false;
            jobStates[actor].DefendingUntil=Clock+SkillTimingDefinition.Delay(State.Heroes[actor].Speed,100);
            Chain=0;chainPending=false;chainMembers.Clear();CompleteJobUtility(actor,100,"生身で防御 ／ ARMOR CALLまで "+Math.Max(0,State.Heroes[actor].ArmorCallAt-Clock));return true;
        }
        private int PanzerIncomingDamage(int actor,int amount,bool magic)
        {
            if(!Job(actor,"panzer"))return amount;
            var j=jobStates[actor];var h=State.Heroes[actor];
            if(!h.ArmorActive)return j.DefendingUntil>=Clock?(int)Math.Max(1,(long)amount*(NextAttackIsMajor?75:25)/100):amount;
            bool resist=j.ArmorResistance==(magic?"magic":"physical") || j.ArmorResistance=="fire" && (NextAttackIsMajor?colossusDefinition?.majorAttributes:NextColossusStep?.attributes)?.Contains("火")==true;
            return resist?(int)Math.Max(1,(long)amount*80/100):amount;
        }
        private void TickPanzerCalls()
        {
            for(int i=0;i<5;i++)if(State.Heroes[i].TryArmorCall(Clock))RecordPresentation(BattlePresentationKind.Support,i,"body","ARMOR CALL ／ 装甲50%で復帰・ツール補充なし",standalone:true);
        }
    }
}
