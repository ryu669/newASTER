using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool dailyAcceptanceFrozen;
        private void PrepareDailyInteractionCapture(string view)
        {
            PrepareAffectionCapture("affection-lover");affectionPanel=false;
            PrepareGardenLifeCapture("garden-life");
            string form="heroine.slayer";
            AcceptanceCheck(gardenLifeRuntime.Agents.Any(a=>a.heroineId==form),"Daily capture target belongs to the life scene");
            if(view=="daily-dates")dailyDateForm=form;
            else if(view=="daily-talk")BeginPlayerAffection(form,false);
            else BeginDailyInteraction(form,"date","date."+view.Substring("daily-date-".Length));
            dailyAcceptanceFrozen=!Environment.GetCommandLineArgs().Contains("-plan9ManualSmoke");
            if(affectionInteraction!=null){
                AcceptanceCheck(gardenLifeRuntime.OwnsDailySession(affectionInteraction.Id),"Daily UI acquires life participant");
                if(affectionInteraction.Kind=="date")AcceptanceCheck(affectionInteraction.Duration>=60 && affectionInteraction.Duration<=180,"Generic date lasts one to three minutes");
                gardenLifeMessage=affectionInteraction.Current.title+" ／ "+affectionInteraction.Current.description;
            }
            Debug.Log("DAILY_PLAYER_PASS view="+view+" size="+Screen.width+"x"+Screen.height+" isolated=true physicalInput=0");
        }
    }
}
