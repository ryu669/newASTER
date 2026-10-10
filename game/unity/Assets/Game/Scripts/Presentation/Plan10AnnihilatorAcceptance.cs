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
        private void PreparePlan10AnnihilatorCapture(string[] args)
        {
            if(!args.Contains("-captureAnnihilator") || capturePath==null)return;
            int vi=Array.IndexOf(args,"-anniView"),fi=Array.IndexOf(args,"-anniForm");string view=vi>=0?args[vi+1]:"detail",form=fi>=0?args[fi+1]:"normal";
            string hero="heroine.annihilator"+(form=="holy"?"-holy":"");
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"annihilator-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
            bool recruit=view=="recruitment" || view=="recruit";
            if(!recruit)save.growth.heroines=save.growth.heroines.Concat(combatDefinitions.HeroineIds.Except(save.growth.heroines.Select(h=>h.heroineId)).Select(id=>new FormalHeroineGrowth{heroineId=id,level=20})).ToArray();
            save.growth.nectar=20000;save.world.unlockedGardenIds=HomeData().gardens.Take(1).Select(g=>g.id).ToArray();
            if(!recruit)save.home.formationIds=new[]{hero}.Concat(combatDefinitions.FormationIds.Skip(1)).ToArray();
            save.home.affections=recruit?new HomeAffection[0]:new[]{new HomeAffection{heroineId=hero,value=20}};
            if(!recruit){AffectionSaveAdapter.Migrate(save,HomeData());var affection=AffectionService.State(save,HomeData(),hero);affection.level=20;affection.exp=0;AffectionEventResolver.Refresh(save,HomeData());}
            save.world.poemIds=save.world.poemIds.Union(ProductionStoryData().chapters.Where(c=>c.ownerId.StartsWith("heroine.annihilator",StringComparison.Ordinal)).SelectMany(c=>c.poems).Select(p=>p.id)).ToArray();
            save.collection.materials=HomeData().materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray();
            HomeConditions.Refresh(save,HomeData());save.Validate();AcceptanceCheck(acceptanceStore.Save(save),"New forms use isolated save");BindFormalCampaign(save);
            title=false;encounter=null;book.RequestSubject(BookBookmark.Heroines,hero);book.CompleteTransition();heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;
            if(view=="recruitment"){heroineRosterOpen=true;expansionRecruitmentOpen=true;}
            else if(view=="roster")heroineRosterOpen=true;
            else if(view=="recruit"){
                RecruitmentCapture("heroine.annihilator");RecruitmentCapture("heroine.annihilator-holy");heroineRosterOpen=true;
                AcceptanceCheck(formalCampaign.Snapshot.growth.heroines.Count(h=>h.heroineId.StartsWith("heroine.annihilator"))==2,"Both forms join once");
            }
            else if(view=="journey"){
                foreach(string id in new[]{"heroine.annihilator","heroine.annihilator-holy"}){
                    foreach(var chapter in ProductionStoryData().chapters.Where(c=>c.ownerId==id))ReadProductionDiagnosticScene(chapter.id,false);
                    for(int i=0;i<5;i++)ReadProductionDiagnosticScene(id+".event."+i,i==2);
                }
                AcceptanceCheck(formalCampaign.Snapshot.home.loverHeroineIds.Contains("heroine.annihilator") && formalCampaign.Snapshot.home.loverHeroineIds.Contains("heroine.annihilator-holy"),"Lover state belongs to one woman across both forms");
                AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Both narrative journeys reload");restored.home.ValidateContent(HomeData(),restored);BindFormalCampaign(restored);
            }
            else if(view=="detail"){}
            else if(view=="weapon")growthScreen=GrowthScreen.Weapons;
            else if(view=="formation")formationOpen=true;
            else if(view=="garden"){
                ProposeHome(new HomeOperation("occupant",hero,garden:HomeData().gardens[0].id,x:.5f,y:.65f));ConfirmHome();book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident=hero;
            }
            else if(view=="chapter"){
                ReadProductionDiagnosticScene(hero+".poem-chapter.1",false);BeginAdv(hero+".poem-chapter.1",true);AdvanceAdv();AcceptanceCheck(adv!=null,"Chapter opens");
            }
            else if(view.StartsWith("event",StringComparison.Ordinal)){
                int i=int.Parse(view.Substring(5));for(int n=0;n<=i;n++)ReadProductionDiagnosticScene(hero+".event."+n,false);
                BeginAdv(hero+".event."+i,true);for(int n=0;n<3;n++){AdvanceAdv();if(n<2)AdvanceAdv();}AcceptanceCheck(adv?.CgId!=null,"Own costume CG appears");
            }
            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyAnnihilator();
                AcceptanceCheck(encounter.JobState(0).Id==(form=="holy"?"job.healer":"job.fighter"),"Selected form chooses job by ID");encounter.State.Heroes[0].GainResource(20);
                if(view=="heal-selection" || view=="heal-action"){
                    encounter.State.Heroes[1].TakeDamage(100);if(view=="heal-action")AcceptanceCheck(encounter.ActWithAllies(0,0,"body",new[]{1}),"Holy heal selected ally");
                }
                if(view=="buff-action")AcceptanceCheck(encounter.ActWithAllies(0,1,"body",new[]{2}),"Holy support selected full-HP ally");
                if(view=="attack")AcceptanceCheck(encounter.Act(0,2,"body"),"Form ultimate resolves");
                if(view=="declaration")AcceptanceCheck(encounter.Act(0,0,"body"),"Normal declaration resolves");
                if(view=="invest" || view=="overheal" || view=="maxhp" || view=="revive"){
                    if(view=="overheal")encounter.State.Heroes[1].TakeDamage(20);if(view=="revive")encounter.State.Heroes[1].TakeDamage(int.MaxValue);
                    AcceptanceCheck(encounter.UseLifeTool(0,view,1),"Life utility resolves "+view);ReadyAnnihilator();encounter.State.Heroes[0].GainResource(20);
                }
                SelectNextHero();ResetBattleMenu();battlePanel=BattlePanel.Actions;encounter.DrainPresentationEvents();
                if(view=="heal-selection" || view=="buff-selection")ChooseBattleSkill(view=="heal-selection"?0:1);
            }
            Debug.Log("PLAN10_ANNIHILATOR_PLAYER_PASS form="+form+" view="+view+" isolated=true");
        }
        private void RecruitmentCapture(string id)
        {
            RecruitExpansionForm(id);AcceptanceCheck(formalCampaign.Snapshot.growth.heroines.Any(h=>h.heroineId==id),"Form recruitment persists "+id);
        }
        private void ReadyAnnihilator(){int guard=0;while(encounter.AvailableHero!=0 && !encounter.Ended && guard++<250)encounter.Pass();AcceptanceCheck(encounter.AvailableHero==0,"Form reaches READY");}
    }
}
