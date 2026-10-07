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
        private void PreparePlan10ArcaneAcademyCapture(string[] args)
        {
            if(!args.Contains("-captureArcaneAcademy") || capturePath==null)return;
            int vi=Array.IndexOf(args,"-arcane-academyView");string view=vi>=0?args[vi+1]:"detail",hero="heroine.arcane-academy";
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"arcane-academy-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));bool recruit=view=="recruitment" || view=="recruit";
            if(!recruit)save.growth.heroines=save.growth.heroines.Concat(combatDefinitions.HeroineIds.Except(save.growth.heroines.Select(h=>h.heroineId)).Select(id=>new FormalHeroineGrowth{heroineId=id,level=20})).ToArray();
            save.growth.nectar=20000;save.world.unlockedGardenIds=HomeData().gardens.Take(1).Select(g=>g.id).ToArray();if(!recruit)save.home.formationIds=new[]{hero}.Concat(combatDefinitions.FormationIds.Skip(1)).ToArray();
            save.home.affections=recruit?new HomeAffection[0]:new[]{new HomeAffection{heroineId=hero,value=20}};
            save.world.poemIds=save.world.poemIds.Union(ProductionStoryData().chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.poems).Select(p=>p.id)).ToArray();save.collection.materials=HomeData().materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray();
            HomeConditions.Refresh(save,HomeData());save.Validate();AcceptanceCheck(acceptanceStore.Save(save),"ArcaneAcademy isolated save");BindFormalCampaign(save);
            title=false;encounter=null;book.RequestSubject(BookBookmark.Heroines,hero);book.CompleteTransition();heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;
            if(view=="recruitment"){heroineRosterOpen=true;expansionRecruitmentOpen=true;}
            else if(view=="roster")heroineRosterOpen=true;
            else if(view=="recruit"){RecruitmentCapture(hero);AcceptanceCheck(RecruitExpansionForm(hero)==GrowthCommitResult.AlreadyCommitted,"ArcaneAcademy joins once");heroineRosterOpen=true;}
            else if(view=="detail"){}
            else if(view=="weapon")growthScreen=GrowthScreen.Weapons;
            else if(view=="formation" || view=="formation-member" || view=="formation-roster"){formationOpen=true;formationSlot=0;formationLayer=view=="formation"?0:view=="formation-member"?1:2;}
            else if(view=="journey"){
                foreach(var chapter in ProductionStoryData().chapters.Where(c=>c.ownerId==hero))ReadProductionDiagnosticScene(chapter.id,false);for(int i=0;i<5;i++)ReadProductionDiagnosticScene(hero+".event."+i,i==2);
                AcceptanceCheck(formalCampaign.Snapshot.home.loverHeroineIds.Contains(hero),"ArcaneAcademy confession establishes relationship");AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"ArcaneAcademy whole story reloads");restored.home.ValidateContent(HomeData(),restored);BindFormalCampaign(restored);
            }
            else if(view=="garden"){ProposeHome(new HomeOperation("occupant",hero,garden:HomeData().gardens[0].id,x:.5f,y:.65f));ConfirmHome();book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident=hero;}
            else if(view=="chapter"){ReadProductionDiagnosticScene(hero+".poem-chapter.1",false);BeginAdv(hero+".poem-chapter.1",true);AdvanceAdv();AcceptanceCheck(adv!=null,"ArcaneAcademy chapter opens");}
            else if(view.StartsWith("event",StringComparison.Ordinal)){
                int i=int.Parse(view.Substring(5));for(int n=0;n<=i;n++)ReadProductionDiagnosticScene(hero+".event."+n,false);BeginAdv(hero+".event."+i,true);for(int n=0;n<5;n++)AdvanceAdv();AcceptanceCheck(adv?.CgId!=null,"ArcaneAcademy CG appears");
            }
            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyArcaneAcademy();AcceptanceCheck(encounter.JobState(0).Id=="job.gambler","Academy uses Gambler");
                if(view=="slot-miss")AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>0),"Fruit miss WT0");
                if(view=="slot-match")AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>2),"Five enhanced skill-one lines");
                if(view=="slot-jackpot")AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>5),"All five 777 lines execute");
                if(view=="status-extension"){
                    encounter.State.BossStatus.Add(new EnemyStatusDef{kind="burn",amount=1000});
                    int[] symbols={3,3,0,0,1,0,1,0,1};int at=0;
                    AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>symbols[at++]),"Observed second skill extends active statuses");
                }
                if(view=="buff-extension"){
                    encounter.State.Heroes[1].ApplySelfEffects(new[]{new TimedSelfEffectDef{kind="attack",percent=20,turns=3}});
                    int[] symbols={4,4,0,0,1,0,1,0,1};int at=0;
                    AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>symbols[at++]),"Observed third skill extends allied buffs");
                }
                if(!encounter.Ended)ReadyArcaneAcademy();
            }
            if(encounter!=null){SelectNextHero();ResetBattleMenu();battlePanel=view=="status"?BattlePanel.Status:BattlePanel.Actions;selectedHero=0;encounter.DrainPresentationEvents();}
            Debug.Log("PLAN10_ARCANE_ACADEMY_PLAYER_PASS view="+view+" isolated=true");
        }
        private void ReadyArcaneAcademy(){int guard=0;while(encounter.AvailableHero!=0 && !encounter.Ended && guard++<250){int a=encounter.AvailableHero;if(encounter.RequiresPanzerDefense(a))encounter.DefendPanzer(a);else encounter.Pass();}AcceptanceCheck(encounter.AvailableHero==0,"ArcaneAcademy reaches READY");}
    }
}
