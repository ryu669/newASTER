using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private string oopartPickerQuery="";
        private int oopartPickerPage;
        private void DrawOopartImage(Rect rect,string id)
        {
            var d=CollectionData().relics.Single(r=>r.id==id);
            if(d.jobId!=null){DrawSanctuaryIcon(rect,"resource."+d.jobId.Replace("job.",""),Color.white);return;}
            var effects=CollectionData().Oopart(id).effects;int icon=effects.Any(e=>e.effectType=="tool-uses")?12:effects.Any(e=>e.scalingType=="turn-growth")?11:effects.Any(e=>e.scalingType=="turn-decay")?13:effects.Any(e=>e.effectType=="cast-percent" || e.targetId=="speed")?15:effects.Any(e=>e.effectType=="attribute")?9:effects.Any(e=>e.targetId=="defense")?12:effects.Any(e=>e.effectType=="buff-power")?4:6;
            DrawBookEmblem(rect,icon);
        }
        private void DrawFormationOopartReplacement(bool interactive)=>DrawFormationItemPicker(interactive);
    }
}
