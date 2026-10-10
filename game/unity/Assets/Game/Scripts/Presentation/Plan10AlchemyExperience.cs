using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private readonly int[] alchemyUnits=new int[5];
        private int alchemyAttribute;
        private void DrawAlchemyBattleControls(int actor,bool enabled,GUIStyle style)
        {
            var attributes=encounter.AlchemyAttributes(actor);if(attributes.Length!=5)return;
            for(int i=0;i<5;i++)if(JobCommand(i,0,attributes[i]+alchemyUnits[i],enabled,5))alchemyAttribute=i;
            if(JobCommand(0,1,"−投入",enabled && alchemyUnits[alchemyAttribute]>0,3))alchemyUnits[alchemyAttribute]--;
            if(JobCommand(1,1,"＋投入",enabled && alchemyUnits.Sum()<encounter.State.Heroes[actor].JobResource,3))alchemyUnits[alchemyAttribute]++;
            if(JobCommand(2,1,"クリア",enabled,3))Array.Clear(alchemyUnits,0,5);
            if(JobCommand(0,2,"錬成（維持）",enabled && encounter.CanTransmute(actor,alchemyUnits,target))){encounter.Transmute(actor,alchemyUnits,target);Array.Clear(alchemyUnits,0,5);QueueBattleEvents();}
            if(JobCommand(1,2,"火弱点（維持）",enabled && encounter.CanTransmute(actor,alchemyUnits,target,true))){encounter.Transmute(actor,alchemyUnits,target,true);Array.Clear(alchemyUnits,0,5);QueueBattleEvents();}
        }
    }
}
