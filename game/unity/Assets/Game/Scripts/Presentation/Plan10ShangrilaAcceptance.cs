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
        private void PreparePlan10ShangrilaCapture(string[] args)
        {
            if(!args.Contains("-captureShangrila") || capturePath==null)return;
            int vi=Array.IndexOf(args,"-shangrilaView");string view=vi>=0?args[vi+1]:"detail",hero="heroine.shangrila";
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"shangrila-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));bool recruit=view=="recruitment" || view=="recruit";
            if(!recruit)save.growth.heroines=save.growth.heroines.Concat(combatDefinitions.HeroineIds.Except(save.growth.heroines.Select(h=>h.heroineId)).Select(id=>new FormalHeroineGrowth{heroineId=id,level=20})).ToArray();
            save.growth.nectar=20000;save.world.unlockedGardenIds=HomeData().gardens.Take(1).Select(g=>g.id).ToArray();if(!recruit)save.home.formationIds=new[]{hero}.Concat(combatDefinitions.FormationIds.Skip(1)).ToArray();
            save.home.affections=recruit?new HomeAffection[0]:new[]{new HomeAffection{heroineId=hero,value=20}};
            save.world.poemIds=save.world.poemIds.Union(ProductionStoryData().chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.poems).Select(p=>p.id)).ToArray();save.collection.materials=HomeData().materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray();
            HomeConditions.Refresh(save,HomeData());save.Validate();AcceptanceCheck(acceptanceStore.Save(save),"Shangrila isolated save");BindFormalCampaign(save);
            title=false;encounter=null;book.RequestSubject(BookBookmark.Heroines,hero);book.CompleteTransition();heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;
            if(view=="recruitment"){heroineRosterOpen=true;expansionRecruitmentOpen=true;}
            else if(view=="roster")heroineRosterOpen=true;
            else if(view=="recruit"){RecruitmentCapture(hero);AcceptanceCheck(RecruitExpansionForm(hero)==GrowthCommitResult.AlreadyCommitted,"Shangrila joins once");heroineRosterOpen=true;}
            else if(view=="detail"){}
            else if(view=="weapon")growthScreen=GrowthScreen.Weapons;
            else if(view=="formation" || view=="formation-member" || view=="formation-roster"){formationOpen=true;formationSlot=0;formationLayer=view=="formation"?0:view=="formation-member"?1:2;}
            else if(view=="journey"){
                foreach(var chapter in ProductionStoryData().chapters.Where(c=>c.ownerId==hero))ReadProductionDiagnosticScene(chapter.id,false);for(int i=0;i<5;i++)ReadProductionDiagnosticScene(hero+".event."+i,i==2);
                AcceptanceCheck(formalCampaign.Snapshot.home.loverHeroineIds.Contains(hero),"Shangrila confession establishes relationship");AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Shangrila whole story reloads");restored.home.ValidateContent(HomeData(),restored);BindFormalCampaign(restored);
            }
            else if(view=="garden"){ProposeHome(new HomeOperation("occupant",hero,garden:HomeData().gardens[0].id,x:.5f,y:.65f));ConfirmHome();book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident=hero;}
            else if(view=="chapter"){ReadProductionDiagnosticScene(hero+".poem-chapter.1",false);BeginAdv(hero+".poem-chapter.1",true);AdvanceAdv();AcceptanceCheck(adv!=null,"Shangrila chapter opens");}
            else if(view.StartsWith("event",StringComparison.Ordinal)){
                int i=int.Parse(view.Substring(5));for(int n=0;n<=i;n++)ReadProductionDiagnosticScene(hero+".event."+n,false);BeginAdv(hero+".event."+i,true);for(int n=0;n<5;n++)AdvanceAdv();AcceptanceCheck(adv?.CgId!=null,"Shangrila CG appears");
            }
            else if(view.StartsWith("formation-support",StringComparison.Ordinal)){
                formationOpen=true;formationSlot=0;formationLayer=1;var operation=new HomeOperation("sniper-support",hero,save.home.formationIds[2]);ProposeHome(operation);
                if(view=="formation-support-pending"){
                    AcceptanceCheck(formalCampaign.CommitHomeOperation(homeRequest,HomeData(),homeOperation,s=>false)==GrowthCommitResult.SaveFailed,"Support failure preserves current deployment");homeError="保存できませんでした。支援対象は変更していません。";
                }
                if(view=="formation-support-saved"){
                    ConfirmHome();AcceptanceCheck(HomeState.sniperSupports.Single(s=>s.heroineId==hero).targetId==save.home.formationIds[2],"Selected support commits through production UI");
                    AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Support save reloads");BindFormalCampaign(restored);
                }
            }
            else if(view=="formation-weapon-return"){
                book.ChangeBookmark(BookBookmark.Formation);book.CompleteTransition();formationSlot=0;formationLayer=1;returnToFormationFromWeapon=true;formationOpen=false;book.RequestSubject(BookBookmark.Heroines,hero);book.CompleteTransition();growthScreen=GrowthScreen.Weapons;ReturnFromFormationWeapon();AcceptanceCheck(formationOpen && formationLayer==1 && formationSlot==0,"Weapon return preserves selected member and hierarchy");
            }
            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyShangrila();AcceptanceCheck(encounter.JobState(0).Id=="job.sniper","Shangrila uses Sniper");
                if(view=="recoil")AcceptanceCheck(encounter.Act(0,0,"body"),"Observed recoil shot executes");
                if(view=="heal"){encounter.State.Heroes[0].TakeDamage(100);AcceptanceCheck(encounter.Act(0,1,"body"),"Observed bleeding heal shot executes");}
                if(view=="ultimate")AcceptanceCheck(encounter.Act(0,2,"body"),"Observed negative-effect ultimate executes");
                if(view=="support"){
                    while(encounter.AvailableHero!=1 && !encounter.Ended)encounter.Pass();AcceptanceCheck(encounter.Act(1,0,"body"),"Selected ally triggers support shot");
                }
                if(view=="sniper-mode" || view=="cast-release" || view=="cast-cancel"){
                    encounter.State.Heroes[0].GainResource(15);string selected=view=="cast-cancel"?encounter.State.Parts.First(p=>!p.IsBroken).Id:"body";
                    AcceptanceCheck(encounter.StartSniperMode(0,selected),"Sniper mode begins");
                    if(view=="cast-cancel")encounter.State.BreakPart(selected,int.MaxValue);
                    if(view=="sniper-mode")AcceptanceCheck(encounter.IsSniping(0),"Casting supports all allies");else ReadyShangrila();
                }
                if(!encounter.Ended && view!="sniper-mode")ReadyShangrila();
            }
            if(encounter!=null){SelectNextHero();ResetBattleMenu();battlePanel=view=="status" || view=="sniper-mode"?BattlePanel.Status:BattlePanel.Actions;selectedHero=0;encounter.DrainPresentationEvents();}
            Debug.Log("PLAN10_SHANGRILA_PLAYER_PASS view="+view+" isolated=true");
        }
        private void ReadyShangrila(){int guard=0;while(encounter.AvailableHero!=0 && !encounter.Ended && guard++<250){int a=encounter.AvailableHero;if(encounter.RequiresPanzerDefense(a))encounter.DefendPanzer(a);else encounter.Pass();}AcceptanceCheck(encounter.AvailableHero==0,"Shangrila reaches READY");}
    }
}
