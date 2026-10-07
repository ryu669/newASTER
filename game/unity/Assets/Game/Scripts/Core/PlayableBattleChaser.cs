using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed partial class PlayableBattle
    {
        public static int ChaserGearCost(int gear)=>gear==1?0:gear==2?2:gear==3?4:-1;
        public static int ChaserGearPercent(int gear)=>gear==1?100:gear==2?65:gear==3?35:100;
        public bool SelectChaserGear(int actor,int gear)
        {
            int cost=ChaserGearCost(gear);if(!JobReady(actor) || !Job(actor,"chaser") || cost<0 || cost>State.Heroes[actor].JobResource)return false;
            jobStates[actor].Gear=gear;jobStates[actor].NitroSelected=false;return true;
        }
        public bool SelectChaserNitro(int actor,bool selected=true)
        {
            if(!JobReady(actor) || !Job(actor,"chaser") || selected && jobStates[actor].NitroCount<5)return false;
            jobStates[actor].Gear=1;jobStates[actor].NitroSelected=selected;return true;
        }
        private bool ChaserCommandValid(int actor){if(!Job(actor,"chaser"))return true;return jobStates[actor].NitroSelected?jobStates[actor].NitroCount>=5:State.Heroes[actor].JobResource>=ChaserGearCost(jobStates[actor].Gear);}
        private int ChaserSelectedRecovery(int actor)=>jobStates[actor].NitroSelected?0:ChaserGearPercent(jobStates[actor].Gear);
        private static long ChaserDelay(long delay,int percent)=>percent==0?0:Math.Max(1,delay*percent/100);
        private void CommitChaserGear(int actor,int cost)
        {
            if(!Job(actor,"chaser"))return;var j=jobStates[actor];j.ChaserRecoveryPercent=ChaserSelectedRecovery(actor);
            if(j.NitroSelected){j.NitroCount-=5;RecordPresentation(BattlePresentationKind.Support,actor,"body","NITRO：5消費・このスキルの待機0で即READY",standalone:true);}
            else if(cost>0)j.NitroCount=Math.Min(10,j.NitroCount+1);
            j.Gear=1;j.NitroSelected=false;
        }
        private void TickChaserResources()
        {
            for(int i=0;i<5;i++)if(Job(i,"chaser") && State.Heroes[i].IsAlive)State.Heroes[i].GainResource(2);
        }
        private void ChaserIgnition(int actor,System.Collections.Generic.IEnumerable<string> targets)
        {
            if(!Job(actor,"chaser") || Ended || !State.Heroes[actor].IsAlive)return;
            bool over=false;
            foreach(string target in targets.Distinct())if(target=="body" || State.Parts.Any(p=>p.Id==target && !p.IsBroken))over=State.EnemyStatus(target).AddIgnition() || over;
            if(!over)return;
            // One reaction per resolved attack even if several targets reach three. This hit never calls ignition recursively.
            var hit=BattleActionResolver.Resolve(State,State.Heroes[actor].Id,new BattleSkill("reaction.nighthawk.over-ignition",2m,0,damageCap:10000,damageType:"physical",targetRule:"target.all-enemies",bodyPartProtection:true,attributes:new[]{"打撃","火","氷"}),"body");
            if(hit.Accepted)RecordPresentation(BattlePresentationKind.Attack,actor,"body","OVER IGNITION：敵全体へ自動攻撃 ／ 通常WTなし",damage:hit.Damage,broken:hit.PartBroken,targetIds:hit.TargetIds,standalone:true);
        }
    }
}
