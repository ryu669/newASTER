using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation {
 public sealed partial class PrototypeBootstrap {
  private bool plan10UiCapture;
  private void ValidateNavigationRoutes(){
   string progress=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);
   int checks=0;Action<bool,string> check=(ok,name)=>{AcceptanceCheck(ok,name);checks++;};
   Action reset=()=>{
    title=false;titlePanel=null;recoveryActive=false;recoveryConfirm=false;encounter=null;result=null;help=false;bookSystemOpen=false;modelViewer=false;
    collectionOpen=false;engagementOpen=false;kinderGarden=false;kinderScreen=KinderScreen.Entrance;
    expansionRecruitmentOpen=false;expansionRecruitRequest=null;formationOpen=false;formationLayer=0;
    heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;returnToFormationFromWeapon=false;
    affectionPanel=false;affectionShop=false;affectionConfirm=false;affectionError=null;oopartPanel=false;
    gardenLifePanel=null;gardenLifeRecords=false;gardenViewing=false;gardenDiscardConfirm=false;
    gardenMenuExpanded=false;gardenPanel=GardenPanel.None;placing=false;panzerSetupOpen=false;
    book=CreateFormalBook();book.ChangeBookmark(BookBookmark.Colossi);book.CompleteTransition();
   };
   foreach(var origin in RibbonOrder)foreach(var destination in RibbonOrder.Where(b=>b!=origin)){
    reset();book.ChangeBookmark(origin);book.CompleteTransition();RequestBookBookmark(destination);book.CompleteTransition();
    check(book.Bookmark==destination,"Forward bookmark "+destination);
    if(destination==BookBookmark.Heroines)heroineRosterOpen=true;
    HandleEscapeNavigation();
    check(book.Bookmark==origin && !title,"Back bookmark "+destination+" to "+origin);
   }
   reset();RequestBookBookmark(BookBookmark.Heroines);book.CompleteTransition();heroineRosterOpen=true;
   string subject=book.SubjectId;RequestBookBookmark(BookBookmark.Formation);book.CompleteTransition();
   formationSlot=2;formationLayer=1;panzerSetupOpen=true;
   HandleEscapeNavigation();check(!panzerSetupOpen && formationLayer==1 && formationSlot==2,"Panzer closes to same member settings");
   HandleEscapeNavigation();check(formationLayer==0 && book.Bookmark==BookBookmark.Formation,"Formation settings close to formation");
   foreach(int layer in new[]{2,3}){formationLayer=layer;HandleEscapeNavigation();check(formationLayer==0 && formationSlot==2,"Formation picker closes without moving bookmark "+layer);}
   HandleEscapeNavigation();check(book.Bookmark==BookBookmark.Heroines && heroineRosterOpen && book.SubjectId==subject,"Formation returns to original heroine roster");
   book.CompleteTransition();RequestBookBookmark(BookBookmark.Summoning);book.CompleteTransition();
   kinderOrigin=KinderScreen.Draw;kinderScreen=KinderScreen.Confirmation;HandleEscapeNavigation();check(kinderScreen==KinderScreen.Draw,"Summoning confirmation returns to selected operation");
   kinderScreen=KinderScreen.Rates;HandleEscapeNavigation();check(kinderScreen==KinderScreen.Entrance && book.Bookmark==BookBookmark.Summoning,"Summoning rates return to entrance");
   HandleEscapeNavigation();check(book.Bookmark==BookBookmark.Heroines && heroineRosterOpen,"Summoning returns to original heroine roster");
   reset();RequestBookBookmark(BookBookmark.Formation);book.CompleteTransition();formationLayer=1;
   returnToFormationFromWeapon=true;formationOpen=false;book.RequestSubject(BookBookmark.Heroines,subject);book.CompleteTransition();growthScreen=GrowthScreen.Weapons;
   HandleEscapeNavigation();check(book.Bookmark==BookBookmark.Formation && formationOpen && formationLayer==1 && !returnToFormationFromWeapon,"Weapon tree returns to member settings");
   HandleEscapeNavigation();HandleEscapeNavigation();check(book.Bookmark==BookBookmark.Colossi,"Returning from weapon tree does not create a history loop");
   reset();RequestBookBookmark(BookBookmark.Heroines);book.CompleteTransition();heroineRosterOpen=false;growthScreen=GrowthScreen.Level;
   growthOrigin=GrowthScreen.Level;growthScreen=GrowthScreen.Confirmation;
   HandleEscapeNavigation();check(growthScreen==GrowthScreen.Level && book.Bookmark==BookBookmark.Heroines,"Growth confirmation returns to operation");
   HandleEscapeNavigation();check(growthScreen==GrowthScreen.Overview && !heroineRosterOpen,"Growth operation returns to heroine detail");
   HandleEscapeNavigation();check(heroineRosterOpen,"Heroine detail returns to roster");
   help=true;bookSystemOpen=true;HandleEscapeNavigation();check(!help && bookSystemOpen && heroineRosterOpen,"Help closes before system");
   HandleEscapeNavigation();check(!bookSystemOpen && heroineRosterOpen,"System closes before roster");
   affectionPanel=true;affectionConfirm=true;HandleEscapeNavigation();check(!affectionConfirm && affectionPanel,"Ring confirmation returns to affection");
   HandleEscapeNavigation();check(!affectionPanel && heroineRosterOpen,"Affection returns to roster");
   affectionPanel=true;affectionError="diagnostic";HandleEscapeNavigation();check(affectionError==null && affectionPanel,"Affection error closes before panel");
   HandleEscapeNavigation();oopartPanel=true;HandleEscapeNavigation();check(!oopartPanel && heroineRosterOpen,"Oopart returns to roster");
   modelViewer=true;HandleEscapeNavigation();check(!modelViewer && heroineRosterOpen,"Model viewer returns to roster");
   reset();RequestBookBookmark(BookBookmark.Gardens);book.CompleteTransition();gardenMenuExpanded=true;gardenPanel=GardenPanel.Furniture;
   bookSystemOpen=true;HandleEscapeNavigation();check(!bookSystemOpen && gardenPanel==GardenPanel.Furniture,"System closes before covered garden drawer");
   HandleEscapeNavigation();check(gardenPanel==GardenPanel.None && gardenMenuExpanded,"Garden drawer closes first");
   HandleEscapeNavigation();check(!gardenMenuExpanded && book.Bookmark==BookBookmark.Gardens,"Garden toolbar closes second");
   gardenLifePanel="residents";gardenLifeRecords=true;HandleEscapeNavigation();check(!gardenLifeRecords && gardenLifePanel!=null,"Life records return to life panel");
   HandleEscapeNavigation();check(gardenLifePanel==null,"Life panel closes");
   gardenLifePanel="催事参加者";HandleEscapeNavigation();check(gardenLifePanel=="催事","Activity participants return to activity panel");
   HandleEscapeNavigation();check(gardenLifePanel==null,"Activity panel closes on next Back");
   gardenLifeRequest=new GardenLifeRequest("navigation.pending","settings",formalCampaign.Revision,"diagnostic");gardenLifeError="diagnostic";
   HandleEscapeNavigation();check(gardenLifeRequest!=null && gardenLifeError!=null && book.Bookmark==BookBookmark.Gardens,"Garden save retry is retained by Back");
   gardenLifeRequest=null;HandleEscapeNavigation();check(gardenLifeError==null && book.Bookmark==BookBookmark.Gardens,"Garden error closes before leaving garden");
   gardenDiscardConfirm=true;HandleEscapeNavigation();check(!gardenDiscardConfirm,"Garden discard confirmation cancels");
   placing=true;HandleEscapeNavigation();check(!placing && book.Bookmark==BookBookmark.Gardens,"Placement cancels without leaving garden");
   HandleEscapeNavigation();check(book.Bookmark==BookBookmark.Colossi,"Garden root returns to origin");
   reset();RequestBookBookmark(BookBookmark.Items);check(book.IsTransitioning,"Forward starts page animation");
   HandleEscapeNavigation();check(!book.IsTransitioning && book.Bookmark==BookBookmark.Items && !title,"Back during animation completes animation without falling through to title");
   HandleEscapeNavigation();check(book.Bookmark==BookBookmark.Colossi,"Next Back returns to origin");
   reset();book.ChangeBookmark(BookBookmark.Summoning);book.CompleteTransition();title=true;OpenTitlePanel("settings");
   HandleEscapeNavigation();check(title && titlePanel==null && book.Bookmark==BookBookmark.Summoning,"Title settings close without navigating covered summoning page");
   reset();collectionOpen=true;HandleEscapeNavigation();check(!collectionOpen && book.Bookmark==BookBookmark.Colossi,"Collection closes to origin");
   engagementOpen=true;HandleEscapeNavigation();check(!engagementOpen && book.Bookmark==BookBookmark.Colossi,"Engagement closes to origin");
   reset();recoveryActive=true;recoveryConfirm=true;HandleEscapeNavigation();check(recoveryActive && !recoveryConfirm && !title,"Recovery confirmation cancels first");
   HandleEscapeNavigation();check(!recoveryActive && title,"Recovery root returns to title");
   reset();book=new BookNavigationState(new[]{new BookOrderedSubject(BookBookmark.Colossi,WorldCatalog.ColossusIds[0],0)});
   HandleEscapeNavigation();check(title && !book.IsOpen,"Book root without history returns to title");
   check(progress==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"Navigation does not consume or modify progression");
   reset();heroineRosterOpen=true;book.RequestSubject(BookBookmark.Heroines,subject);book.CompleteTransition();
   Debug.Log("PLAN119_NAVIGATION_ROUTES_PASS checks="+checks+" physicalInput=0");
  }
  private void PreparePlan10UiAuditCapture(string[] args){
   if(!args.Contains("-capturePlan10Ui") || capturePath==null)return;
   plan10UiCapture=!args.Contains("-plan9ManualSmoke");
   int at=Array.IndexOf(args,"-uiView");string view=at>=0?args[at+1]:"title";
   PreparePlan10ShangrilaCapture(args.Concat(new[]{"-captureShangrila","-shangrilaView","detail"}).ToArray());
   if(view=="title" || view=="settings" || view=="credits" || view=="title-help"){title=true;if(view!="title")OpenTitlePanel(view=="title-help"?"help":view);}
   else if(view.StartsWith("save-",StringComparison.Ordinal))PrepareSaveManagementCapture(view);
   else if(view=="help")help=true;
   else if(view=="book-summoning" || view.StartsWith("book-items",StringComparison.Ordinal) || view=="book-materials" || view=="book-hunt" || view=="book-new-world" || view=="book-possible-worlds" || view=="book-system"){
    book.ChangeBookmark(view=="book-summoning"?BookBookmark.Summoning:view.StartsWith("book-items",StringComparison.Ordinal) || view=="book-materials"?BookBookmark.Items:view=="book-hunt"?BookBookmark.RelicHunt:view=="book-new-world"?BookBookmark.NewWorld:view=="book-possible-worlds"?BookBookmark.PossibleWorlds:BookBookmark.Colossi);
    heroineRosterOpen=false;formationOpen=false;collectionOpen=false;kinderGarden=false;book.CompleteTransition();collectionTab=view=="book-materials"?2:1;if(view=="book-system")bookSystemOpen=true;
    if(view.StartsWith("book-items",StringComparison.Ordinal)){var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));save.collection.relics=CollectionData().relics.Select(r=>new CollectionRelic{id=r.id,contentVersion=CollectionData().contentVersion,attackRoll=80,hpRoll=800}).ToArray();BindFormalCampaign(save);book.ChangeBookmark(BookBookmark.Items);book.CompleteTransition();if(view=="book-items-added")collectionRelicPage=4;if(view=="book-items-special")collectionRelicPage=7;if(view=="book-items-fading")collectionRelicPage=8;if(view=="book-items-equip")OpenOoparts(0,"relic.special.speed");}
    AcceptanceCheck(book.Bookmark==(view=="book-summoning"?BookBookmark.Summoning:view.StartsWith("book-items",StringComparison.Ordinal) || view=="book-materials"?BookBookmark.Items:view=="book-hunt"?BookBookmark.RelicHunt:view=="book-new-world"?BookBookmark.NewWorld:view=="book-possible-worlds"?BookBookmark.PossibleWorlds:BookBookmark.Colossi),"Capture displays requested bookmark: "+view);
   }
   else if(view.StartsWith("detail-information",StringComparison.Ordinal)){PrepareAffectionCapture(view.Contains("ring")?"affection-cap99":"affection-unread");affectionPanel=false;book.FlipPage();book.CompleteTransition();}
   else if(view=="material-exchange"){
    var save=formalCampaign.Snapshot;var resource=CollectionData().resources.First(r=>MaterialExchangeService.UnitCost(r)==500);save.growth.nectar=20000;
    var material=save.collection.materials.SingleOrDefault(m=>m.id==resource.id);if(material==null)save.collection.materials=save.collection.materials.Concat(new[]{new CollectionMaterial{id=resource.id,sourceColossusId=resource.ownerId,amount=1}}).ToArray();
    BindFormalCampaign(save);book.ChangeBookmark(BookBookmark.Items);book.CompleteTransition();collectionTab=2;ProposeExchange(resource,5);
   }
   else if(view=="model"){
    var bookmark=book.Bookmark;var subject=book.SubjectId;var face=book.Face;var screen=growthScreen;bool roster=heroineRosterOpen;
    modelViewer=true;HandleEscapeNavigation();
    AcceptanceCheck(!modelViewer && book.Bookmark==bookmark && book.SubjectId==subject && book.Face==face && growthScreen==screen && heroineRosterOpen==roster,"Full-screen model Escape preserves underlying heroine selection and screen");
    // A covered help layer must survive the first Escape; the next Escape closes only help.
    modelViewer=true;help=true;HandleEscapeNavigation();
    AcceptanceCheck(!modelViewer && help,"Model Escape closes the visible layer before covered help");
    HandleEscapeNavigation();AcceptanceCheck(!help && growthScreen==screen && heroineRosterOpen==roster,"Next Escape closes help without navigating the heroine page");
    modelViewer=true;Debug.Log("PLAN119_MODEL_NAVIGATION_PASS selected="+subject+" physicalInput=0");
   }
   else if(view=="book-colossi" || view=="book-world" || view=="book-stories"){book.ChangeBookmark(view=="book-stories"?BookBookmark.Stories:BookBookmark.Colossi);book.CompleteTransition();if(view=="book-world")book.FlipPage();}
   else if(view.StartsWith("job-",StringComparison.Ordinal)){
    string job="job."+view.Substring(4);var ids=combatDefinitions.FormationIds;string id=combatDefinitions.heroines.First(h=>h.jobId==job).id;int actor=Array.IndexOf(ids,id);if(actor<0){actor=0;ids[0]=id;}
    var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));save.home.formationIds=ids;BindFormalCampaign(save);StartBattle(WorldCatalog.ColossusIds[0],1137);int guard=0;while(encounter.AvailableHero!=actor && guard++<250)encounter.Pass();AcceptanceCheck(encounter.AvailableHero==actor,"Job reaches READY: "+job);encounter.State.Heroes[actor].GainResource(15);encounter.DrainPresentationEvents();selectedHero=actor;ResetBattleMenu();battlePanel=BattlePanel.Actions;
   }
   else if(view.StartsWith("battle-",StringComparison.Ordinal)){
    StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyShangrila();encounter.DrainPresentationEvents();selectedHero=0;ResetBattleMenu();
    if(view=="battle-targets")battlePanel=BattlePanel.Targets;else if(view=="battle-timeline")battlePanel=BattlePanel.Timeline;else if(view=="battle-pause")paused=true;else if(view=="battle-retreat"){paused=true;retreat=true;}else if(view=="battle-result")PrepareVictoryCapture(args);else throw new ArgumentException("Unknown battle UI case");
   }
   else if(view.StartsWith("ability-",StringComparison.Ordinal))PrepareAbilityCapture(view);
   else if(view.StartsWith("formation-ui",StringComparison.Ordinal))PrepareFormationUiCapture(view);
   else if(view.StartsWith("oopart-",StringComparison.Ordinal))PrepareOopartCapture(view);
   else if(view.StartsWith("daily-",StringComparison.Ordinal))PrepareDailyInteractionCapture(view);
   else if(view.StartsWith("affection-",StringComparison.Ordinal))PrepareAffectionCapture(view);
   else if(view.StartsWith("garden-life",StringComparison.Ordinal))PrepareGardenLifeCapture(view);
   else if(view.StartsWith("garden-",StringComparison.Ordinal)){
    string garden=HomeData().gardens[0].id;ProposeHome(new HomeOperation("occupant","heroine.shangrila",garden:garden,x:.5f,y:.65f));ConfirmHome();book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident="heroine.shangrila";PrepareGardenMenuCapture(view.Substring(7));
   }
   else if(view=="formation-weapon-return"){PreparePlan10ShangrilaCapture(args.Concat(new[]{"-captureShangrila","-shangrilaView","formation-weapon-return"}).ToArray());}
   else if(view=="formation-general" || view=="formation-general-confirm"){
    var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));save.home.formationIds[0]="heroine.slayer-swim";BindFormalCampaign(save);formationOpen=true;formationSlot=0;formationLayer=1;if(view.EndsWith("confirm",StringComparison.Ordinal))ProposeHome(new HomeOperation("commander","heroine.slayer-swim"));else{ProposeHome(new HomeOperation("commander","heroine.slayer-swim"));ConfirmHome();AcceptanceCheck(HomeState.commanderHeroineId=="heroine.slayer-swim","Commander commits through production UI");}
   }
   else if(view=="release-generation"){
    AcceptanceCheck(Application.version.StartsWith("1.",StringComparison.Ordinal),"Release candidate uses generation-one version");
    string root=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(capturePath),"release-generation-"+Guid.NewGuid().ToString("N"));
    var original=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
    InitializeSaveSlots(root,0);saveSlots.Save(1,original,DateTime.UtcNow);
    InitializeSaveSlots(root,1);AcceptanceCheck(saveSlots.Inspect(1).Status==SaveSlotStatus.Empty,"Release does not load development slot");
    var release=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(original));release.revision++;
    saveSlots.Save(1,release,DateTime.UtcNow);saveSlots.Save(2,original,DateTime.UtcNow);
    AcceptanceCheck(saveSlots.Load(1).revision==release.revision && saveSlots.Load(2).revision==original.revision,"Release slots save and load independently");
    InitializeSaveSlots(root,0);AcceptanceCheck(saveSlots.Load(1).revision==original.revision,"Release save preserves development data");
    InitializeSaveSlots(root,1);ResetForSlotLoad(saveSlots.Load(1));OpenSaveManagement();
    Debug.Log("COMMERCIAL_RELEASE_GENERATION_PASS version="+Application.version+" physicalInput=0");
   }
   else if(view=="navigation-routes"){ValidateNavigationRoutes();}
   else if(view=="recruitment-second"){
    var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));save.growth.heroines=save.growth.heroines.Where(h=>combatDefinitions.FormationIds.Contains(h.heroineId)).ToArray();save.home=FormalHomeProgress.Empty(HomeData().contentVersion);save.affection=null;HomeConditions.Refresh(save,HomeData());BindFormalCampaign(save);heroineRosterOpen=true;expansionRecruitmentOpen=true;expansionRecruitmentPage=1;
   }
   else if(view=="roster-second"){PrepareHeroineSanctuaryCapture(args.Concat(new[]{"-heroineView","roster","-heroineId","heroine.shangrila"}).ToArray());heroinePage=1;}
   else if(view=="formation-roster-second"){formationOpen=true;formationSlot=0;formationLayer=2;formationPage=1;}
   else if(view=="adv-backlog" || view=="adv-help"){
    ReadProductionDiagnosticScene("heroine.shangrila.poem-chapter.1",false);BeginAdv("heroine.shangrila.poem-chapter.1",true);AdvanceAdv();AdvanceAdv();advBacklog=view=="adv-backlog";advHelp=!advBacklog;
   }
   else PrepareHeroineSanctuaryCapture(args.Concat(new[]{"-heroineView",view,"-heroineId","heroine.shangrila"}).ToArray());
   if(formationOpen){book.ChangeBookmark(BookBookmark.Formation);book.CompleteTransition();}
   else if(heroineRosterOpen){book.RequestSubject(BookBookmark.Heroines,"heroine.slayer");book.CompleteTransition();}
   if(view.StartsWith("battle-",StringComparison.Ordinal)){
    var bookmark=book.Bookmark;
    if(view=="battle-targets" || view=="battle-timeline"){
     var panel=battlePanel;HandleEscapeNavigation();AcceptanceCheck(battlePanel==BattlePanel.None && !paused && encounter!=null,"Battle Back closes drawer before pausing");battlePanel=panel;
    }else if(view=="battle-pause"){
     HandleEscapeNavigation();AcceptanceCheck(!paused && encounter!=null && book.Bookmark==bookmark,"Battle pause Back resumes same battle");paused=true;
    }else if(view=="battle-retreat"){
     HandleEscapeNavigation();AcceptanceCheck(!retreat && !paused && encounter!=null,"Retreat Back cancels and resumes battle");paused=true;retreat=true;
    }else if(view=="battle-result"){
     var battle=encounter;var outcome=result;HandleEscapeNavigation();AcceptanceCheck(result==null && encounter==null && book.Bookmark==bookmark,"Committed result Back returns to originating book page");encounter=battle;result=outcome;
    }
   }
   if(view=="adv-backlog" || view=="adv-help"){
    HandleEscapeNavigation();AcceptanceCheck(adv!=null && !advBacklog && !advHelp,"ADV Back closes backlog or help before leaving story");advBacklog=view=="adv-backlog";advHelp=!advBacklog;
   }
   if(view=="recruitment-second"){
    var subject=book.SubjectId;var face=book.Face;var screen=growthScreen;int page=heroinePage;int recruitmentPage=expansionRecruitmentPage;
    AcceptanceCheck(!BookInputAllowed,"Recruitment blocks navigation behind the modal");
    RequestBookBookmark(BookBookmark.Colossi);
    AcceptanceCheck(book.Bookmark==BookBookmark.Heroines,"Recruitment cannot leave a hidden dialog on another bookmark");
    expansionRecruitRequest=new GrowthRequest("navigation-pending",subject,formalProgression.Snapshot.revision,GrowthOperation.ReceiveHeroine);
    HandleEscapeNavigation();
    AcceptanceCheck(expansionRecruitmentOpen && heroineRosterOpen && book.Bookmark==BookBookmark.Heroines,"Back preserves unresolved recruitment and underlying roster");
    expansionRecruitRequest=null;
    HandleEscapeNavigation();
    AcceptanceCheck(!expansionRecruitmentOpen && heroineRosterOpen && book.Bookmark==BookBookmark.Heroines && book.SubjectId==subject && book.Face==face && growthScreen==screen && heroinePage==page && BookInputAllowed,"Recruitment Back restores the same usable heroine roster without consuming book history");
    expansionRecruitmentOpen=true;CloseExpansionRecruitment();
    AcceptanceCheck(!expansionRecruitmentOpen && heroineRosterOpen && BookInputAllowed,"Dialog return and global Back share close behavior");
    expansionRecruitmentOpen=true;expansionRecruitmentPage=recruitmentPage;
    Debug.Log("PLAN119_RECRUITMENT_BACK_PASS physicalInput=0");
   }
   Debug.Log("PLAN10_UI_AUDIT_PLAYER_PASS view="+view+" isolated=true physicalInput=0");
  }
 }
}
