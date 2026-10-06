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
            if(Btn(34,484,130,32,attributes[alchemyAttribute]+" ↻",enabled,style))alchemyAttribute=(alchemyAttribute+1)%5;
            if(Btn(170,484,55,32,"−",enabled && alchemyUnits[alchemyAttribute]>0,style))alchemyUnits[alchemyAttribute]--;
            if(Btn(230,484,55,32,"＋",enabled && alchemyUnits.Sum()<encounter.State.Heroes[actor].JobResource,style))alchemyUnits[alchemyAttribute]++;
            if(Btn(295,484,230,32,"錬成・READY維持",enabled && encounter.CanTransmute(actor,alchemyUnits,target),style)){encounter.Transmute(actor,alchemyUnits,target);Array.Clear(alchemyUnits,0,5);QueueBattleEvents();}
            if(Btn(535,484,220,32,"火弱点・火2＋光1",enabled && encounter.CanTransmute(actor,alchemyUnits,target,true),style)){encounter.Transmute(actor,alchemyUnits,target,true);Array.Clear(alchemyUnits,0,5);QueueBattleEvents();}
            if(Btn(765,484,90,32,"クリア",enabled,style))Array.Clear(alchemyUnits,0,5);
        }
    }
}
