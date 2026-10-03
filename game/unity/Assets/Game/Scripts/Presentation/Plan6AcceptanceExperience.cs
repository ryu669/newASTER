using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void AcceptanceHome(HomeOperation op,bool fail=false)
        {
            ProposeHome(op);AcceptanceCheck(homeRequest!=null,"UI builds immutable home confirmation");string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);var request=homeRequest;
            if(fail){formalVictoryDiagnosticFailure=true;ConfirmHome();AcceptanceCheck(formalCampaign.HasPending && before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"home candidate save failure publishes nothing");formalVictoryDiagnosticFailure=false;}
            ConfirmHome();AcceptanceCheck(homeRequest==null && !formalCampaign.HasPending,"home UI confirms candidate");AcceptanceCheck(formalCampaign.CommitHomeOperation(request,HomeData(),op,s=>throw new Exception("duplicate"))==GrowthCommitResult.AlreadyCommitted,"same home operation consumes once");
        }
        private void AcceptanceAdv(string source)
        {
            BeginAdv(source,false);AcceptanceCheck(adv!=null,"unlocked source starts isolated fixture ADV");adv.Tick(.3);int steps=0;while(!adv.EndReached && steps++<50){AdvanceAdv();AdvanceAdv();}
            AcceptanceCheck(adv.EndReached && !adv.Completed,"real ADV inputs reach end without publishing completion");string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);formalVictoryDiagnosticFailure=true;CompleteAdv();AcceptanceCheck(formalCampaign.HasPending && before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"end failure keeps all progress frozen");formalVictoryDiagnosticFailure=false;CompleteAdv();AcceptanceCheck(adv.Completed && !formalCampaign.HasPending,"end retry completes atomically");CloseAdv();
            before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);BeginAdv(source,true);AcceptanceCheck(adv!=null && adv.Replay,"read source opens replay");adv.Tick(.3);steps=0;while(!adv.EndReached && steps++<50){AdvanceAdv();AdvanceAdv();}CompleteAdv();CloseAdv();AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"replay all commands read-only");
        }
        private void PreparePlan6Acceptance(string[] args)
        {
            if(!formalDiagnostic || capturePath==null)throw new InvalidOperationException("Plan6 acceptance requires isolated capture.");
            int arg=Array.IndexOf(args,"-plan6Save");if(arg<0 || arg+1>=args.Length)throw new ArgumentException("Plan6 acceptance save path missing.");string path=Path.GetFullPath(args[arg+1]);Directory.CreateDirectory(Path.GetDirectoryName(path));acceptanceStore=new FormalCampaignStore(path,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);acceptanceChecks=0;homeTrial=true;
            if(args.Contains("-plan6Resume")){
                int useArg=Array.IndexOf(args,"-inspectPlan7GardenUse");string priorUse=useArg>=0 && useArg+1<args.Length?args[useArg+1]:null;int expectedPlacements=priorUse=="look"?3:priorUse=="remove"?1:2;
                AcceptanceCheck(acceptanceStore.Load(out var loaded)==FormalLoadResult.Loaded,"separate Windows process loads unified home file");BindFormalCampaign(loaded);AcceptanceCheck(loaded.home.weaponEquipment.Length==5 && loaded.home.furnitureInstances.Length==3 && loaded.home.furniturePlacements.Length==expectedPlacements && loaded.home.occupants.Length==5,"all durable home states survive process restart");AcceptanceCheck(loaded.home.loverHeroineIds.Contains(combatDefinitions.FormationIds[0]) && loaded.home.readLineKeys.Length>=12 && loaded.world.readStoryIds.Length>0,"chapter event lover read-line survive process restart");
            }else{
                // Exercise an actual pre-home save first; no heuristic migration of legacy arrays.
                var old=CreateHomeTrial();old.home=args.Contains("-plan6NewSave")?FormalHomeProgress.Empty(HomeData().contentVersion):null;old.collection=null;old.growth.nectar=10000;old.world.unlockedGardenIds=Array.Empty<string>();AcceptanceCheck(!File.Exists(path) && acceptanceStore.Save(old),"isolated initial formal file written without overwriting prior test");AcceptanceCheck(acceptanceStore.Load(out var legacy)==FormalLoadResult.Loaded && (legacy.home==null)==!args.Contains("-plan6NewSave"),"pre-home and new-home forms preserve presence");BindFormalCampaign(legacy);
                foreach(var owner in combatDefinitions.FormationIds){var r=new GrowthRequest("plan6.growth."+owner,owner,formalProgression.Snapshot.revision,GrowthOperation.Level,10);AcceptanceCheck(formalProgression.Commit(r,SaveFormalGrowth)==GrowthCommitResult.Committed,"real growth transaction before hunt");}
                int fights=0;do{PlayedAcceptanceEnding(BattleEndReason.Victory,101+fights,8);fights++;}while(fights<128 && formalCampaign.Snapshot.world.unlockedStoryIds.Length<1);AcceptanceCheck(fights<128,"chapters obtained from actual battle singing");PlayedAcceptanceEnding(BattleEndReason.Defeat,202,1);PlayedAcceptanceEnding(BattleEndReason.Retreat,303,1);encounter=null;result=null;
                string hero=combatDefinitions.FormationIds[0];var c=HomeData();
                // Material reserve is an explicitly isolated fixture, added after normal hunt verification.
                var trial=formalCampaign.Snapshot;trial.revision=checked(trial.revision+1);trial.collection.materials=c.materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=100}).ToArray();trial.world.unlockedGardenIds=c.gardens.Take(2).Select(g=>g.id).ToArray();trial.home=FormalHomeProgress.Empty(c.contentVersion);HomeConditions.Refresh(trial,c);AcceptanceCheck(acceptanceStore.Save(trial),"test-only material reserve written to isolated file");BindFormalCampaign(trial);
                foreach(var owner in combatDefinitions.FormationIds){foreach(var n in c.weaponNodes.Where(n=>n.heroineId==owner))AcceptanceHome(new HomeOperation("weapon",n.id));AcceptanceHome(new HomeOperation("equip",owner+".weapon.gamma",owner),owner==hero);}
                for(int i=0;i<3;i++)AcceptanceHome(new HomeOperation("craft",c.furniture[i].id,"acceptance.furniture."+i),i==0);
                string garden=c.gardens[0].id;AcceptanceHome(new HomeOperation("place","acceptance.furniture.0",garden:garden,zone:"zone.ground",x:.3f,y:.7f));AcceptanceHome(new HomeOperation("place","acceptance.furniture.1",garden:garden,zone:"zone.ground",x:.6f,y:.7f));
                string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);selectedFurniture="acceptance.furniture.0";placing=true;previewX=.9f;placing=false;AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"placement preview cancel changes no save");
                foreach(var owner in combatDefinitions.FormationIds)AcceptanceHome(new HomeOperation("occupant",owner,garden:garden,x:.15f+Array.IndexOf(combatDefinitions.FormationIds,owner)*.16f,y:.8f));AcceptanceHome(new HomeOperation("occupant",hero,garden:c.gardens[1].id,x:.5f,y:.5f));AcceptanceHome(new HomeOperation("occupant",hero,garden:garden,x:.15f,y:.8f));AcceptanceHome(new HomeOperation("use",hero,"acceptance.furniture.0"));AcceptanceHome(new HomeOperation("talk",hero),true);
                book.ChangeBookmark(BookBookmark.Gardens);foreach(var ev in c.events.Where(e=>e.heroineId==hero).Take(3))AcceptanceAdv(ev.id);
                book.ChangeBookmark(BookBookmark.Stories);var chapter=c.chapters.First(ch=>formalCampaign.Snapshot.world.unlockedStoryIds.Contains(ch.id));while(book.SubjectId!=chapter.ownerId && book.CanTurnNext)book.TurnPage(1);AcceptanceAdv(chapter.id);AcceptanceCheck(book.SubjectId==chapter.ownerId && book.Reading==null,"reading closes to same book owner");
                ReloadAcceptance();selectedLevel=1;StartBattle(WorldCatalog.ColossusIds[0]);AcceptanceCheck(encounter.SkillName(0,0).Contains("γ"),"real next battle uses equipped weapon skill");encounter=null;
                // The physical store's restore flow is exercised only on a separate disposable copy.
                string recoveryPath=path+".recovery-test";var recoveryStore=new FormalCampaignStore(recoveryPath,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);var complete=formalCampaign.Snapshot;File.Copy(path,recoveryPath);AcceptanceCheck(recoveryStore.Load(out _)==FormalLoadResult.Loaded,"complete home copied for recovery test");var newer=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(complete));newer.revision++;AcceptanceCheck(recoveryStore.Save(newer),"recovery backup created");File.WriteAllText(recoveryPath,"broken-home");var offer=recoveryStore.InspectRecovery();AcceptanceCheck(offer.CanRestore,"damaged home file offers valid backup");var recovered=recoveryStore.RestoreConfirmed(offer,out var preserved);AcceptanceCheck(File.ReadAllText(preserved)=="broken-home" && recovered.home.weaponEquipment.Length==5,"recovery preserves damaged original and full home");// Construct a future header independently of pretty-JSON whitespace.
                string future="{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"home\":{\"version\":2,\"contentVersion\":\"future\"}}";File.WriteAllText(recoveryPath,future);AcceptanceCheck(recoveryStore.Load(out _)==FormalLoadResult.Blocked && recoveryStore.InspectRecovery().Status==FormalRecoveryStatus.Unsupported && File.ReadAllText(recoveryPath)==future,"Windows future home blocks backup replacement");
            }
            var sceneCase=Array.IndexOf(args,"-homeCase");string display=sceneCase<0?"Garden":args[sceneCase+1];encounter=null;result=null;title=false;
            int gardenUseArg=Array.IndexOf(args,"-inspectPlan7GardenUse");
            if(gardenUseArg>=0){
                if(display!="Garden" || gardenUseArg+1>=args.Length)throw new ArgumentException("Garden use capture requires Garden and scenario");
                string scenario=args[gardenUseArg+1];if(!new[]{"sit","work","look","move","remove","idle"}.Contains(scenario))throw new ArgumentException("Unknown garden scenario");
                var catalog=HomeData();string hero=combatDefinitions.FormationIds[0],garden=catalog.gardens[0].id;
                AcceptanceHome(new HomeOperation("occupant",hero,garden:garden,x:.15f,y:.8f));
                if(scenario!="idle"){
                    int index=scenario=="work"?1:scenario=="look"?2:0;string instance="acceptance.furniture."+index;
                    AcceptanceHome(new HomeOperation("place",instance,garden:garden,zone:"zone.ground",x:new[]{.3f,.6f,.8f}[index],y:.7f));
                    AcceptanceHome(new HomeOperation("use",hero,instance));
                    if(scenario=="move")AcceptanceHome(new HomeOperation("place",instance,garden:garden,zone:"zone.ground",x:.45f,y:.72f));
                    if(scenario=="remove")AcceptanceHome(new HomeOperation("remove",instance));
                }
                ReloadAcceptance();var occupant=HomeState.occupants.Single(o=>o.heroineId==hero);
                bool usingFurniture=new[]{"sit","work","look"}.Contains(scenario);
                AcceptanceCheck(usingFurniture?occupant.actionId=="action."+scenario && GardenUsePlacement(occupant,HomeState)!=null:string.IsNullOrEmpty(occupant.actionId) && string.IsNullOrEmpty(occupant.furnitureInstanceId) && GardenUsePlacement(occupant,HomeState)==null,"garden composition survives reload and release");
                Debug.Log("PLAN7_GARDEN_USE_CAPTURE "+scenario+" action="+(occupant.actionId??"idle")+" / isolated");
            }
            if(display=="Weapons"){book.ChangeBookmark(BookBookmark.Heroines);growthScreen=GrowthScreen.Weapons;selectedNode=combatDefinitions.FormationIds[0]+".weapon.gamma";}
            else if(display=="Events"){book.ChangeBookmark(BookBookmark.Gardens);if(book.Face==BookFace.Overview)book.FlipPage();selectedResident=combatDefinitions.FormationIds[0];}
            else if(display=="Adv" || display=="Backlog" || display=="Cg"){book.ChangeBookmark(BookBookmark.Gardens);BeginAdv(HomeData().events[0].id,true);adv.Tick(.3);if(display=="Cg"){AdvanceAdv();AdvanceAdv();adv.Advance();}else{adv.Advance();if(display=="Backlog"){advBacklog=true;adv.Pause();}else adv.Pause();}}
            else{book.ChangeBookmark(BookBookmark.Gardens);selectedResident=combatDefinitions.FormationIds[0];}
            int artArg=Array.IndexOf(args,"-inspectPlan7Expression");
            if(artArg>=0){
                if(display!="Adv" || artArg+1>=args.Length)throw new ArgumentException("Expression capture requires ADV and expression name");
                var expression=args[artArg+1];if(!new[]{"normal","joy","puzzled","determined"}.Contains(expression))throw new ArgumentException("Unknown candidate expression");
                adv=new AdvSession(HomeData(),"scene.art-candidate.slayer",HomeData().events[0].id,true,formalCampaign.Snapshot.home.readLineKeys);
                for(int line=0;line<Array.IndexOf(new[]{"normal","joy","puzzled","determined"},expression);line++){adv.Advance();adv.Advance();}
                adv.Advance();adv.Pause();Debug.Log("PLAN7_EXPRESSION_CAPTURE "+expression+" asset="+adv.Actors.Single().ExpressionAsset);
            }
            Debug.Log("PLAN6_HOME_PLAYER_PASS checks="+acceptanceChecks+" resumed="+args.Contains("-plan6Resume")+" case="+display);
        }
    }
}
