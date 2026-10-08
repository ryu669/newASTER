using System;
using System.Linq;
using NewAster.Core;
namespace NewAster.Presentation
{
 public sealed partial class PrototypeBootstrap
 {
  private void PrepareFormationUiCapture(string view)
  {
   PrepareOopartCapture("oopart-presets");oopartPanel=false;book.ChangeBookmark(BookBookmark.Formation);book.CompleteTransition();formationLayer=0;
   if(view.Contains("hero")){OpenFormationHeroReplacement(0);formationCandidateHero=formalProgression.Snapshot.heroines.First(h=>!CurrentFormation().Contains(h.heroineId)).heroineId;FormationCandidateStats(formationCandidateHero);}
   if(view.Contains("oopart")){OpenFormationOopartReplacement(0);formationCandidateOopart=OopartSnapshot().collection.ooparts.progress.First(p=>!OopartSnapshot().collection.ooparts.slots.Any(s=>s.equippedOopartId==p.oopartId)).oopartId;}
   if(view.EndsWith("saved",StringComparison.Ordinal)){
    if(formationLayer==2){string target=formationCandidateHero;string gear=OopartSnapshot().collection.ooparts.slots[0].equippedOopartId;ProposeHome(new HomeOperation("formation",target,"0"));ConfirmHome();AcceptanceCheck(homeRequest==null && CurrentFormation()[0]==target && formationLayer==0,"Hero replacement commits and returns to formation");AcceptanceCheck(OopartSnapshot().collection.ooparts.slots[0].equippedOopartId==gear,"Hero replacement keeps slot equipment");}
    else{string target=formationCandidateOopart;ProposeOopart("equip",target);CommitOopart();AcceptanceCheck(oopartRequest==null && OopartSnapshot().collection.ooparts.slots[0].equippedOopartId==target && formationLayer==0,"Item replacement commits and returns to formation");}
    AcceptanceCheck(acceptanceStore.Load(out var loaded)==FormalLoadResult.Loaded && loaded.revision==formalCampaign.Revision,"Replacement reloads from actual saved store");
   }
   if(view.EndsWith("last",StringComparison.Ordinal)){oopartPickerPage=5;formationSelectionPagePending=false;}
   if(view.EndsWith("help",StringComparison.Ordinal)){help=true;AcceptanceCheck(BookHelpText().Length>20,"Picker provides contextual help");}
  }
 }
}
