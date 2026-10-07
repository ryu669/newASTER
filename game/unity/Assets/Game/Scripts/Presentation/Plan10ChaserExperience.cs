using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void DrawChaserBattleControls(int actor,bool enabled,GUIStyle style)
        {
            var j=encounter.JobState(actor);var h=encounter.State.Heroes[actor];
            for(int gear=1;gear<=3;gear++){int value=gear;if(Btn(34+(gear-1)*175,484,165,32,(j.Gear==gear && !j.NitroSelected?"◆":"")+"GEAR "+gear+" ／ "+PlayableBattle.ChaserGearCost(gear),enabled && h.JobResource>=PlayableBattle.ChaserGearCost(gear),style))encounter.SelectChaserGear(actor,value);}
            if(Btn(569,484,286,32,(j.NitroSelected?"◆":"")+"NITRO 5 ／ 次スキルWT0",enabled && (j.NitroSelected || j.NitroCount>=5),style))encounter.SelectChaserNitro(actor,!j.NitroSelected);
        }
    }
}
