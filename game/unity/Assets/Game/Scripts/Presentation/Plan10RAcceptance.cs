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
        private void PreparePlan10RCapture(string[] args)
        {
            if(!args.Contains("-capturePlan10") || capturePath==null)return;
            int vi=Array.IndexOf(args,"-plan10View");string view=vi>=0?args[vi+1]:"detail";
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"plan10-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
            if(view!="recruitment")save.growth.heroines=save.growth.heroines.Concat(new[]{new FormalHeroineGrowth{heroineId="heroine.r",level=20}}).ToArray();
            save.growth.nectar=20000;save.world.unlockedGardenIds=HomeData().gardens.Take(1).Select(g=>g.id).ToArray();
            if(view!="recruitment")save.home.formationIds=new[]{"heroine.r"}.Concat(combatDefinitions.FormationIds.Skip(1)).ToArray();
            save.home.affections=view=="recruitment"?new HomeAffection[0]:new[]{new HomeAffection{heroineId="heroine.r",value=20}};
            if(view=="journey" || view=="chapter")save.world.poemIds=save.world.poemIds.Union(ProductionStoryData().chapters.Where(c=>c.ownerId=="heroine.r").SelectMany(c=>c.poems).Select(p=>p.id)).ToArray();
            save.collection.materials=HomeData().materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray();
            HomeConditions.Refresh(save,HomeData());save.Validate();AcceptanceCheck(acceptanceStore.Save(save),"R diagnostic profile saved separately");BindFormalCampaign(save);
            title=false;encounter=null;book.RequestSubject(BookBookmark.Heroines,"heroine.r");book.CompleteTransition();heroineRosterOpen=false;
            if(view=="recruitment" || view=="roster")heroineRosterOpen=true;
            else if(view=="journey"){
                foreach(var chapter in ProductionStoryData().chapters.Where(c=>c.ownerId=="heroine.r"))ReadProductionDiagnosticScene(chapter.id,false);
                for(int i=0;i<5;i++)ReadProductionDiagnosticScene("heroine.r.event."+i,i==2);
                var result=formalCampaign.Snapshot;
                AcceptanceCheck(result.home.loverHeroineIds.Contains("heroine.r") && result.home.readEventIds.Count(id=>id.StartsWith("heroine.r.event."))==5,"R full event progression persists lover and all five reads");
                AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"R unified save reloads");
                restored.home.ValidateContent(HomeData(),restored);BindFormalCampaign(restored);
                heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;book.RequestSubject(BookBookmark.Heroines,"heroine.r");book.CompleteTransition();
            }
            else if(view=="detail")growthScreen=GrowthScreen.Overview;
            else if(view=="weapon")growthScreen=GrowthScreen.Weapons;
            else if(view=="formation")formationOpen=true;
            else if(view=="battle" || view=="song" || view=="buff"){
                StartBattle(WorldCatalog.ColossusIds[0],1137);int guard=0;
                while(encounter.AvailableHero!=0 && !encounter.Ended && guard++<100)encounter.Pass();
                AcceptanceCheck(encounter.JobState(0).Id=="job.artist","R selected by ID in mixed party");
                if(view=="song"){encounter.State.Heroes[0].GainResource(10);AcceptanceCheck(encounter.StartSong(0),"R starts song");guard=0;while(encounter.AvailableHero!=0 && !encounter.Ended && guard++<100)encounter.Pass();AcceptanceCheck(encounter.JobState(0).Singing,"R singing UI at own READY");}
                if(view=="buff")AcceptanceCheck(encounter.Act(0,1,"body"),"R party support used");
                SelectNextHero();ResetBattleMenu();battlePanel=BattlePanel.Actions;encounter.DrainPresentationEvents();
            }else if(view=="garden"){
                string garden=HomeData().gardens[0].id;
                ProposeHome(new HomeOperation("occupant","heroine.r",garden:garden,x:.5f,y:.65f));ConfirmHome();
                book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();selectedResident="heroine.r";
            }else if(view.StartsWith("event")){
                int i=int.Parse(view.Substring(5));for(int n=0;n<=i;n++)ReadProductionDiagnosticScene("heroine.r.event."+n,false);
                BeginAdv("heroine.r.event."+i,true);for(int n=0;n<3;n++){AdvanceAdv();if(n<2)AdvanceAdv();}
                AcceptanceCheck(adv!=null && adv.CgId!=null,"R own event CG appears");
            }else if(view=="chapter"){
                ReadProductionDiagnosticScene("heroine.r.poem-chapter.1",false);
                BeginAdv("heroine.r.poem-chapter.1",true);AdvanceAdv();
                AcceptanceCheck(adv!=null,"R chapter replay opens after reading");
            }else throw new ArgumentException("Unknown R view "+view);
            Debug.Log("PLAN10_R_PLAYER_PASS view="+view+" isolated=true owned="+save.growth.heroines.Length);
        }
    }
}
