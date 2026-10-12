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
        private void PreparePlan12AeonCapture(string[] args)
        {
            if(!args.Contains("-captureAeon") || capturePath==null)return;
            int vi=Array.IndexOf(args,"-aeonView");string view=vi>=0?args[vi+1]:"detail",hero="heroine.aeon";
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"aeon-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));bool recruit=view=="recruitment" || view=="recruit";
            if(!recruit)save.growth.heroines=save.growth.heroines.Concat(combatDefinitions.HeroineIds.Except(save.growth.heroines.Select(h=>h.heroineId)).Select(id=>new FormalHeroineGrowth{heroineId=id,level=20})).ToArray();
            save.growth.nectar=20000;save.world.unlockedGardenIds=HomeData().gardens.Take(1).Select(g=>g.id).ToArray();
            if(!recruit){save.home.formationIds=new[]{hero,"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna"};save.home.commanderHeroineId=hero;}
            save.home.affections=recruit?Array.Empty<HomeAffection>():new[]{new HomeAffection{heroineId=hero,value=20}};
            save.world.poemIds=save.world.poemIds.Union(ProductionStoryData().chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.poems).Select(p=>p.id)).ToArray();
            save.collection.materials=HomeData().materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray();
            AffectionSaveAdapter.Migrate(save,HomeData());
            if(!recruit){var affection=AffectionService.State(save,HomeData(),hero);affection.level=20;affection.levelCap=20;affection.exp=0;}
            AffectionEventResolver.Refresh(save,HomeData());HomeConditions.Refresh(save,HomeData());save.Validate();AcceptanceCheck(acceptanceStore.Save(save),"Aeon isolated save");BindFormalCampaign(save);
            title=false;encounter=null;book.RequestSubject(BookBookmark.Heroines,hero);book.CompleteTransition();heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;
            if(view=="recruitment"){book.ChangeBookmark(BookBookmark.Heroines);book.CompleteTransition();heroineRosterOpen=true;expansionRecruitmentPage=Math.Max(0,Array.IndexOf(UnownedExpansionForms(),hero)/5);expansionRecruitmentOpen=true;}
            else if(view=="recruit"){RecruitmentCapture(hero);AcceptanceCheck(RecruitExpansionForm(hero)==GrowthCommitResult.AlreadyCommitted,"Aeon joins once");book.ChangeBookmark(BookBookmark.Heroines);book.CompleteTransition();heroineRosterOpen=true;}
            else if(view=="detail"){}
            else if(view=="skill")growthScreen=GrowthScreen.Skill;
            else if(view=="trait")selectedTrait=0;
            else if(view=="weapon")growthScreen=GrowthScreen.Weapons;
            else if(view=="formation"){formationOpen=true;formationSlot=0;formationLayer=1;}
            else if(view=="journey"){
                foreach(var chapter in ProductionStoryData().chapters.Where(c=>c.ownerId==hero))ReadProductionDiagnosticScene(chapter.id,false);
                for(int i=0;i<5;i++)ReadProductionDiagnosticScene(hero+".event."+i,i==2);
                var relationship=AffectionService.State(formalCampaign.Snapshot,HomeData(),hero);
                AcceptanceCheck(AffectionService.IsLover(relationship) && relationship.readEventIds.Contains(hero+".event.2"),"Aeon confession read and affection relationship preserved");
                AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Aeon complete story reloads");restored.home.ValidateContent(HomeData(),restored);BindFormalCampaign(restored);
            }
            else if(view=="garden"){ProposeHome(new HomeOperation("occupant",hero,garden:HomeData().gardens[0].id,x:.5f,y:.65f));ConfirmHome();book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident=hero;}
            else if(view.StartsWith("chapter",StringComparison.Ordinal)){
                int i=int.Parse(view.Substring(7));ReadProductionDiagnosticScene(hero+".poem-chapter."+i,false);BeginAdv(hero+".poem-chapter."+i,true);AdvanceAdv();AcceptanceCheck(adv!=null,"Aeon chapter opens");
            }
            else if(view.StartsWith("event",StringComparison.Ordinal)){
                int i=int.Parse(view.Substring(5));for(int n=0;n<=i;n++)ReadProductionDiagnosticScene(hero+".event."+n,false);
                BeginAdv(hero+".event."+i,true);for(int n=0;n<5;n++)AdvanceAdv();AcceptanceCheck(adv?.CgId!=null,"Aeon dedicated CG appears");
            }
            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyJobPanelActor(0);AcceptanceCheck(encounter.JobState(0).Id=="job.general" && encounter.CommanderActor==0,"Aeon commands her own formation");
                if(view=="command"){encounter.State.Heroes[0].GainResource(15);AcceptanceCheck(encounter.ActivateGeneralCommand(0),"Aeon command activates");}
                else if(view.StartsWith("attack",StringComparison.Ordinal)){int slot=int.Parse(view.Substring(6));AcceptanceCheck(encounter.Act(0,slot,"body"),"Aeon skill executes");ReadyJobPanelActor(0);}
                else if(view!="battle")throw new ArgumentException("Unknown Aeon view "+view);
                SelectNextHero();ResetBattleMenu();battlePanel=BattlePanel.Actions;selectedHero=0;encounter.DrainPresentationEvents();playback.Reset();paused=true;
            }
            Debug.Log("PLAN12_AEON_PLAYER_PASS view="+view+" isolated=true");
        }
    }
}
