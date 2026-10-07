using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed partial class PlayableBattle
    {
        public static int LifeCost(string tool)=>tool=="heal"?2:tool=="overheal" || tool=="maxhp"?4:tool=="revive"?8:tool=="invest"?3:-1;
        public bool CanUseLifeTool(int actor,string tool,int target)
        {
            int cost=LifeCost(tool);
            if(!JobReady(actor) || !Job(actor,"healer") || cost<0 || State.Heroes[actor].JobResource<cost)return false;
            if(tool=="invest")return State.Heroes[actor].JobResourceMax<20;
            if(target<0 || target>=5)return false;var h=State.Heroes[target];
            if(tool=="revive")return !h.IsAlive;
            if(h.IsPanzer && h.ArmorActive && (tool=="heal" || tool=="overheal"))return false;
            if(!h.IsAlive || (tool=="heal" || tool=="overheal") && h.Status.Active("bleed"))return false;
            return tool=="maxhp"?h.LifeMaxHpPercent<50:tool=="overheal"?h.HitPoints<(long)h.MaxHitPoints*125/100:h.HitPoints<h.MaxHitPoints;
        }
        public bool UseLifeTool(int actor,string tool,int target)
        {
            if(!CanUseLifeTool(actor,tool,target))return false;
            var user=State.Heroes[actor];user.SpendResource(LifeCost(tool));
            if(tool=="invest")user.JobResourceMax=Math.Min(20,user.JobResourceMax+2);
            else{
                var h=State.Heroes[target];
                if(tool=="heal")h.Heal((int)((long)user.Attack*150/100));
                else if(tool=="overheal")h.Overheal((int)((long)user.Attack*200/100));
                else if(tool=="maxhp")h.LifeMaxHpPercent=Math.Min(50,h.LifeMaxHpPercent+20);
                else{h.Revive();casting[target]=null;readyAt[target]=Clock+SkillTimingDefinition.Delay(h.Speed,100);Acted[target]=true;}
                RecordPresentation(BattlePresentationKind.Healing,actor,"body","生命操作："+tool,healingTargets:new[]{target},targetIds:new[]{h.Id},standalone:true);
            }
            Chain=0;chainPending=false;chainMembers.Clear();LastActionChain=0;LastFullChain=false;
            CompleteJobUtility(actor,100,"生命操作 ／ "+tool+" ／ 消費 "+LifeCost(tool));return true;
        }
        public bool IsAllyBuff(int actor,int slot)=>commandDefinitions?[actor,slot].effectRuleId=="effect.allies-buff" && commandDefinitions[actor,slot].targetRuleId=="target.selected-allies";
        public bool CanChooseAlly(int actor,int slot,int ally)
        {
            if(ally<0 || ally>=5 || !State.Heroes[ally].IsAlive)return false;
            return IsAllyBuff(actor,slot) || PreviewHealing(actor,ally,slot)>0 || commandDefinitions?[actor,slot].cleanseAll==true && EnemyStatusState.Kinds.Any(State.Heroes[ally].Status.Active);
        }
    }
}
