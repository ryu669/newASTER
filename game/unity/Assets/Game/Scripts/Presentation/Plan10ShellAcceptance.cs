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
        private void PreparePlan10ShellCapture(string[] args)
        {
            if(!args.Contains("-captureShell") || capturePath==null)return;
            int vi=Array.IndexOf(args,"-shellView");string view=vi>=0?args[vi+1]:"detail",hero="heroine.shell";
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"shell-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));bool recruit=view=="recruitment" || view=="recruit";
            if(!recruit)save.growth.heroines=save.growth.heroines.Concat(combatDefinitions.HeroineIds.Except(save.growth.heroines.Select(h=>h.heroineId)).Select(id=>new FormalHeroineGrowth{heroineId=id,level=20})).ToArray();
            save.growth.nectar=20000;save.world.unlockedGardenIds=HomeData().gardens.Take(1).Select(g=>g.id).ToArray();if(!recruit)save.home.formationIds=new[]{hero}.Concat(combatDefinitions.FormationIds.Skip(1)).ToArray();
            save.home.affections=recruit?new HomeAffection[0]:new[]{new HomeAffection{heroineId=hero,value=20}};
            save.world.poemIds=save.world.poemIds.Union(ProductionStoryData().chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.poems).Select(p=>p.id)).ToArray();save.collection.materials=HomeData().materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray();
            HomeConditions.Refresh(save,HomeData());save.Validate();AcceptanceCheck(acceptanceStore.Save(save),"Shell isolated save");BindFormalCampaign(save);
            title=false;encounter=null;book.RequestSubject(BookBookmark.Heroines,hero);book.CompleteTransition();heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;
            if(view=="recruitment"){heroineRosterOpen=true;expansionRecruitmentOpen=true;}
            else if(view=="roster")heroineRosterOpen=true;
            else if(view=="recruit"){RecruitmentCapture(hero);AcceptanceCheck(RecruitExpansionForm(hero)==GrowthCommitResult.AlreadyCommitted,"Shell joins once");heroineRosterOpen=true;}
            else if(view=="detail"){}
            else if(view=="weapon")growthScreen=GrowthScreen.Weapons;
            else if(view=="formation" || view=="setup" || view=="setup-pending"){
                formationOpen=true;if(view!="formation")panzerSetupOpen=true;
                if(view=="setup-pending"){
                    panzerResistance=1;panzerTool0=2;panzerTool1=3;ProposeHome(new HomeOperation("panzer-loadout",hero,"magic","rally","extend"));formalVictoryDiagnosticFailure=true;ConfirmHome();formalVictoryDiagnosticFailure=false;AcceptanceCheck(formalCampaign.HasPending && SavedPanzerLoadout().Resistance=="physical","Failed setup remains uncommitted");
                }
            }
            else if(view=="setup-save"){
                ProposeHome(new HomeOperation("panzer-loadout",hero,"magic","rally","extend"));ConfirmHome();AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Panzer setup reloads");BindFormalCampaign(restored);AcceptanceCheck(SavedPanzerLoadout().Resistance=="magic" && SavedPanzerLoadout().FirstTool=="rally","Saved setup retained");StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyShell();AcceptanceCheck(encounter.JobState(0).ArmorResistance=="magic" && encounter.JobState(0).PanzerTools[0]=="rally","Saved setup reaches encounter");
            }
            else if(view=="journey"){
                foreach(var chapter in ProductionStoryData().chapters.Where(c=>c.ownerId==hero))ReadProductionDiagnosticScene(chapter.id,false);for(int i=0;i<5;i++)ReadProductionDiagnosticScene(hero+".event."+i,i==2);
                AcceptanceCheck(formalCampaign.Snapshot.home.loverHeroineIds.Contains(hero),"Shell confession establishes relationship");AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Shell whole story reloads");restored.home.ValidateContent(HomeData(),restored);BindFormalCampaign(restored);
            }
            else if(view=="garden"){ProposeHome(new HomeOperation("occupant",hero,garden:HomeData().gardens[0].id,x:.5f,y:.65f));ConfirmHome();book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident=hero;}
            else if(view=="chapter"){ReadProductionDiagnosticScene(hero+".poem-chapter.1",false);BeginAdv(hero+".poem-chapter.1",true);AdvanceAdv();AcceptanceCheck(adv!=null,"Shell chapter opens");}
            else if(view.StartsWith("event",StringComparison.Ordinal)){
                int i=int.Parse(view.Substring(5));for(int n=0;n<=i;n++)ReadProductionDiagnosticScene(hero+".event."+n,false);BeginAdv(hero+".event."+i,true);for(int n=0;n<5;n++)AdvanceAdv();AcceptanceCheck(adv?.CgId!=null,"Shell CG appears");
            }
            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyShell();AcceptanceCheck(encounter.JobState(0).Id=="job.panzer","Shell owns panzer job");
                if(view=="attack")AcceptanceCheck(encounter.Act(0,1,"body"),"Shell fire skill executes");
                if(view=="repair"){encounter.State.Heroes[0].TakeDamage(300);AcceptanceCheck(encounter.UsePanzerTool(0,0),"Repair tool executes");ReadyShell();}
                if(view=="guard"){AcceptanceCheck(encounter.UsePanzerTool(0,1),"Guard tool executes");ReadyShell();}
                if(view=="bare" || view=="defense" || view=="call"){
                    var h=encounter.State.Heroes[0];h.TakeDamage(int.MaxValue);long due=h.ArmorCallAt;AcceptanceCheck(!h.ArmorActive && h.IsAlive,"Armor break enters flesh");
                    if(view=="defense"){AcceptanceCheck(encounter.DefendPanzer(0),"Flesh defense accepted");ReadyShell();}
                    if(view=="call"){
                        int guard=0;while(!h.ArmorActive && !encounter.Ended && guard++<500){int a=encounter.AvailableHero;if(encounter.RequiresPanzerDefense(a))encounter.DefendPanzer(a);else encounter.Pass();}
                        AcceptanceCheck(h.ArmorActive && encounter.Clock>=due,"Shell automatically calls armor");ReadyShell();
                    }
                }
            }
            if(encounter!=null){SelectNextHero();ResetBattleMenu();battlePanel=view=="status"?BattlePanel.Status:BattlePanel.Actions;selectedHero=0;encounter.DrainPresentationEvents();}
            Debug.Log("PLAN10_SHELL_PLAYER_PASS view="+view+" isolated=true");
        }
        private void ReadyShell(){int guard=0;while(encounter.AvailableHero!=0 && !encounter.Ended && guard++<250){int a=encounter.AvailableHero;if(encounter.RequiresPanzerDefense(a))encounter.DefendPanzer(a);else encounter.Pass();}AcceptanceCheck(encounter.AvailableHero==0,"Shell reaches READY");}
    }
}
