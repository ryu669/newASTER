using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void DrawChaserBattleControls(int actor,bool enabled,GUIStyle style)
        {
            var j=encounter.JobState(actor);var h=encounter.State.Heroes[actor];
            for(int gear=1;gear<=3;gear++){int value=gear;if(JobCommand(gear-1,0,(j.Gear==gear && !j.NitroSelected?"◆":"")+"GEAR "+gear,enabled && h.JobResource>=PlayableBattle.ChaserGearCost(gear),3))encounter.SelectChaserGear(actor,value);}
            if(JobCommand(0,1,(j.NitroSelected?"◆ ":"")+"NITRO 5 ／ 次スキルWT0",enabled && (j.NitroSelected || j.NitroCount>=5),2,2))encounter.SelectChaserNitro(actor,!j.NitroSelected);
        }
    }
}
