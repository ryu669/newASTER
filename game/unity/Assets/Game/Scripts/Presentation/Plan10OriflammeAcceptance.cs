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
        private void PreparePlan10OriflammeCapture(string[] args)
        {
            if(!args.Contains("-captureOriflamme") || capturePath==null)return;
            int vi=Array.IndexOf(args,"-oriflammeView");string view=vi>=0?args[vi+1]:"detail",hero="heroine.oriflamme";
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"oriflamme-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));bool recruit=view=="recruitment" || view=="recruit";
            if(!recruit)save.growth.heroines=save.growth.heroines.Concat(combatDefinitions.HeroineIds.Except(save.growth.heroines.Select(h=>h.heroineId)).Select(id=>new FormalHeroineGrowth{heroineId=id,level=20})).ToArray();
            save.growth.nectar=20000;save.world.unlockedGardenIds=HomeData().gardens.Take(1).Select(g=>g.id).ToArray();if(!recruit)save.home.formationIds=new[]{hero}.Concat(combatDefinitions.FormationIds.Skip(1)).ToArray();
            save.home.affections=recruit?new HomeAffection[0]:new[]{new HomeAffection{heroineId=hero,value=20}};
            save.world.poemIds=save.world.poemIds.Union(ProductionStoryData().chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.poems).Select(p=>p.id)).ToArray();save.collection.materials=HomeData().materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray();
            HomeConditions.Refresh(save,HomeData());save.Validate();AcceptanceCheck(acceptanceStore.Save(save),"Oriflamme isolated save");BindFormalCampaign(save);
            title=false;encounter=null;book.RequestSubject(BookBookmark.Heroines,hero);book.CompleteTransition();heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;
            if(view=="recruitment"){heroineRosterOpen=true;expansionRecruitmentOpen=true;}
            else if(view=="roster")heroineRosterOpen=true;
            else if(view=="recruit"){RecruitmentCapture(hero);AcceptanceCheck(RecruitExpansionForm(hero)==GrowthCommitResult.AlreadyCommitted,"Oriflamme joins once");heroineRosterOpen=true;}
            else if(view=="detail"){}
            else if(view=="weapon")growthScreen=GrowthScreen.Weapons;
            else if(view=="formation")formationOpen=true;
            else if(view=="journey"){
                foreach(var chapter in ProductionStoryData().chapters.Where(c=>c.ownerId==hero))ReadProductionDiagnosticScene(chapter.id,false);for(int i=0;i<5;i++)ReadProductionDiagnosticScene(hero+".event."+i,i==2);
                AcceptanceCheck(formalCampaign.Snapshot.home.loverHeroineIds.Contains(hero),"Oriflamme confession establishes relationship");AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Oriflamme whole story reloads");restored.home.ValidateContent(HomeData(),restored);BindFormalCampaign(restored);
            }
            else if(view=="garden"){ProposeHome(new HomeOperation("occupant",hero,garden:HomeData().gardens[0].id,x:.5f,y:.65f));ConfirmHome();book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident=hero;}
            else if(view=="chapter"){ReadProductionDiagnosticScene(hero+".poem-chapter.1",false);BeginAdv(hero+".poem-chapter.1",true);AdvanceAdv();AcceptanceCheck(adv!=null,"Oriflamme chapter opens");}
            else if(view.StartsWith("event",StringComparison.Ordinal)){
                int i=int.Parse(view.Substring(5));for(int n=0;n<=i;n++)ReadProductionDiagnosticScene(hero+".event."+n,false);BeginAdv(hero+".event."+i,true);for(int n=0;n<5;n++)AdvanceAdv();AcceptanceCheck(adv?.CgId!=null,"Oriflamme CG appears");
            }
            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyOriflamme();AcceptanceCheck(encounter.JobState(0).Id=="job.alchemist","Oriflamme owns alchemist job");
                if(view=="input"){Array.Copy(new[]{2,2,1,0,0},alchemyUnits,5);AcceptanceCheck(encounter.CanTransmute(0,alchemyUnits,"body"),"Five-attribute input preview is executable");}
                if(view=="attack")AcceptanceCheck(encounter.Act(0,0,"body"),"Fire attack executes");
                if(view=="buff"){AcceptanceCheck(encounter.Act(0,1,"body"),"Allies fire amplification executes");ReadyOriflamme();}
                if(view=="ultimate"){encounter.State.BossStatus.Add(new EnemyStatusDef{kind="burn",amount=1000});AcceptanceCheck(encounter.Act(0,2,"body"),"Burning target ultimate executes");}
                if(view=="alchemy" || view=="weakness"){
                    long clock=encounter.Clock;int actor=encounter.AvailableHero,resource=encounter.State.Heroes[0].JobResource;
                    AcceptanceCheck(encounter.Transmute(0,view=="alchemy"?new[]{2,2,1,0,0}:new[]{2,0,0,1,0},"body",view=="weakness"),"Alchemy executes");
                    AcceptanceCheck(encounter.Clock==clock && encounter.AvailableHero==actor && encounter.State.Heroes[0].JobResource==resource-(view=="alchemy"?5:3),"Alchemy spends exactly once and preserves READY");
                }
            }
            if(encounter!=null){SelectNextHero();ResetBattleMenu();battlePanel=view=="status"?BattlePanel.Status:BattlePanel.Actions;selectedHero=0;encounter.DrainPresentationEvents();}
            Debug.Log("PLAN10_ORIFLAMME_PLAYER_PASS view="+view+" isolated=true");
        }
        private void ReadyOriflamme(){int guard=0;while(encounter.AvailableHero!=0 && !encounter.Ended && guard++<250){int a=encounter.AvailableHero;if(encounter.RequiresPanzerDefense(a))encounter.DefendPanzer(a);else encounter.Pass();}AcceptanceCheck(encounter.AvailableHero==0,"Oriflamme reaches READY");}
    }
}
