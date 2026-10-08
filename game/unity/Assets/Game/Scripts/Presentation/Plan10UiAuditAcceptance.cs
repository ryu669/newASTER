using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation {
 public sealed partial class PrototypeBootstrap {
  private bool plan10UiCapture;
  private void PreparePlan10UiAuditCapture(string[] args){
   if(!args.Contains("-capturePlan10Ui") || capturePath==null)return;
   plan10UiCapture=!args.Contains("-plan9ManualSmoke");
   int at=Array.IndexOf(args,"-uiView");string view=at>=0?args[at+1]:"title";
   PreparePlan10ShangrilaCapture(args.Concat(new[]{"-captureShangrila","-shangrilaView","detail"}).ToArray());
   if(view=="title" || view=="settings" || view=="credits" || view=="title-help"){title=true;if(view!="title")OpenTitlePanel(view=="title-help"?"help":view);}
   else if(view=="help")help=true;
   else if(view=="book-summoning" || view.StartsWith("book-items",StringComparison.Ordinal) || view=="book-materials" || view=="book-hunt" || view=="book-new-world" || view=="book-possible-worlds" || view=="book-system"){
    book.ChangeBookmark(view=="book-summoning"?BookBookmark.Summoning:view.StartsWith("book-items",StringComparison.Ordinal) || view=="book-materials"?BookBookmark.Items:view=="book-hunt"?BookBookmark.RelicHunt:view=="book-new-world"?BookBookmark.NewWorld:view=="book-possible-worlds"?BookBookmark.PossibleWorlds:BookBookmark.Colossi);
    heroineRosterOpen=false;formationOpen=false;collectionOpen=false;kinderGarden=false;book.CompleteTransition();collectionTab=view=="book-materials"?2:1;if(view=="book-system")bookSystemOpen=true;
    if(view.StartsWith("book-items",StringComparison.Ordinal)){var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));save.collection.relics=CollectionData().relics.Select(r=>new CollectionRelic{id=r.id,contentVersion=CollectionData().contentVersion,attackRoll=80,hpRoll=800}).ToArray();BindFormalCampaign(save);book.ChangeBookmark(BookBookmark.Items);book.CompleteTransition();if(view=="book-items-added")collectionRelicPage=4;if(view=="book-items-special")collectionRelicPage=7;if(view=="book-items-fading")collectionRelicPage=8;if(view=="book-items-equip")OpenOoparts(0,"relic.special.speed");}
    AcceptanceCheck(book.Bookmark==(view=="book-summoning"?BookBookmark.Summoning:view.StartsWith("book-items",StringComparison.Ordinal) || view=="book-materials"?BookBookmark.Items:view=="book-hunt"?BookBookmark.RelicHunt:view=="book-new-world"?BookBookmark.NewWorld:view=="book-possible-worlds"?BookBookmark.PossibleWorlds:BookBookmark.Colossi),"Capture displays requested bookmark: "+view);
   }
   else if(view=="model")modelViewer=true;
   else if(view=="book-colossi" || view=="book-world" || view=="book-stories"){book.ChangeBookmark(view=="book-stories"?BookBookmark.Stories:BookBookmark.Colossi);book.CompleteTransition();if(view=="book-world")book.FlipPage();}
   else if(view.StartsWith("job-",StringComparison.Ordinal)){
    string job="job."+view.Substring(4);var ids=combatDefinitions.FormationIds;string id=combatDefinitions.heroines.First(h=>h.jobId==job).id;int actor=Array.IndexOf(ids,id);if(actor<0){actor=0;ids[0]=id;}
    var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));save.home.formationIds=ids;BindFormalCampaign(save);StartBattle(WorldCatalog.ColossusIds[0],1137);int guard=0;while(encounter.AvailableHero!=actor && guard++<250)encounter.Pass();AcceptanceCheck(encounter.AvailableHero==actor,"Job reaches READY: "+job);encounter.State.Heroes[actor].GainResource(15);encounter.DrainPresentationEvents();selectedHero=actor;ResetBattleMenu();battlePanel=BattlePanel.Actions;
   }
   else if(view.StartsWith("battle-",StringComparison.Ordinal)){
    StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyShangrila();encounter.DrainPresentationEvents();selectedHero=0;ResetBattleMenu();
    if(view=="battle-targets")battlePanel=BattlePanel.Targets;else if(view=="battle-timeline")battlePanel=BattlePanel.Timeline;else if(view=="battle-pause")paused=true;else if(view=="battle-retreat"){paused=true;retreat=true;}else if(view=="battle-result")PrepareVictoryCapture(args);else throw new ArgumentException("Unknown battle UI case");
   }
   else if(view.StartsWith("formation-ui",StringComparison.Ordinal))PrepareFormationUiCapture(view);
   else if(view.StartsWith("oopart-",StringComparison.Ordinal))PrepareOopartCapture(view);
   else if(view.StartsWith("affection-",StringComparison.Ordinal))PrepareAffectionCapture(view);
   else if(view.StartsWith("garden-life",StringComparison.Ordinal))PrepareGardenLifeCapture(view);
   else if(view.StartsWith("garden-",StringComparison.Ordinal)){
    string garden=HomeData().gardens[0].id;ProposeHome(new HomeOperation("occupant","heroine.shangrila",garden:garden,x:.5f,y:.65f));ConfirmHome();book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident="heroine.shangrila";PrepareGardenMenuCapture(view.Substring(7));
   }
   else if(view=="formation-general" || view=="formation-general-confirm"){
    var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));save.home.formationIds[0]="heroine.slayer-swim";BindFormalCampaign(save);formationOpen=true;formationSlot=0;formationLayer=1;if(view.EndsWith("confirm",StringComparison.Ordinal))ProposeHome(new HomeOperation("commander","heroine.slayer-swim"));else{ProposeHome(new HomeOperation("commander","heroine.slayer-swim"));ConfirmHome();AcceptanceCheck(HomeState.commanderHeroineId=="heroine.slayer-swim","Commander commits through production UI");}
   }
   else if(view=="recruitment-second"){
    var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));save.growth.heroines=save.growth.heroines.Where(h=>combatDefinitions.FormationIds.Contains(h.heroineId)).ToArray();save.home=FormalHomeProgress.Empty(HomeData().contentVersion);HomeConditions.Refresh(save,HomeData());BindFormalCampaign(save);heroineRosterOpen=true;expansionRecruitmentOpen=true;expansionRecruitmentPage=1;
   }
   else if(view=="roster-second"){PrepareHeroineSanctuaryCapture(args.Concat(new[]{"-heroineView","roster","-heroineId","heroine.shangrila"}).ToArray());heroinePage=1;}
   else if(view=="formation-roster-second"){formationOpen=true;formationSlot=0;formationLayer=2;formationPage=1;}
   else if(view=="adv-backlog" || view=="adv-help"){
    ReadProductionDiagnosticScene("heroine.shangrila.poem-chapter.1",false);BeginAdv("heroine.shangrila.poem-chapter.1",true);AdvanceAdv();AdvanceAdv();advBacklog=view=="adv-backlog";advHelp=!advBacklog;
   }
   else PrepareHeroineSanctuaryCapture(args.Concat(new[]{"-heroineView",view,"-heroineId","heroine.shangrila"}).ToArray());
   if(formationOpen){book.ChangeBookmark(BookBookmark.Formation);book.CompleteTransition();}
   else if(heroineRosterOpen){book.RequestSubject(BookBookmark.Heroines,"heroine.slayer");book.CompleteTransition();}
   Debug.Log("PLAN10_UI_AUDIT_PLAYER_PASS view="+view+" isolated=true physicalInput=0");
  }
 }
}
