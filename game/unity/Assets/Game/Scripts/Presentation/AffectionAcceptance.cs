using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void PrepareAffectionCapture(string view)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-affectionManualSave");string form="heroine.slayer";
            if(at>=0){
                string path=Path.GetFullPath(args[at+1]);if(!Path.GetFileName(path).StartsWith("manual-affection-",StringComparison.Ordinal) || Path.GetDirectoryName(path)!=Path.GetDirectoryName(Path.GetFullPath(capturePath)))throw new ArgumentException("Affection manual smoke requires an isolated capture path.");
                acceptanceStore=new FormalCampaignStore(path,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
                var loaded=acceptanceStore.Load(out var previous);
                if(loaded==FormalLoadResult.Loaded){BindFormalCampaign(previous);title=false;encounter=null;heroineRosterOpen=false;book.RequestSubject(BookBookmark.Gardens,GardenLifeCatalog.GardenIds[0]);book.CompleteTransition();gardenLifeCapture=true;StartLifeScene();gardenLifeTriggers.Clear();gardenLifeFinds.Clear();Debug.Log("AFFECTION_MANUAL_RELOAD_PASS revision="+previous.revision+" level="+AffectionService.State(previous,HomeData(),form).level+" rings="+previous.growth.eternalRings);return;}
                if(loaded!=FormalLoadResult.Missing)throw new InvalidOperationException("Affection smoke save cannot be loaded.");
            }
            var save=formalCampaign.Snapshot;AffectionSaveAdapter.Migrate(save,HomeData());var a=AffectionService.State(save,HomeData(),form);
            a.level=view.Contains("cap") || view.Contains("unread") || view.Contains("confirm")?20:view.Contains("lover")?10:view.Contains("max99")?99:9;
            a.levelCap=view.Contains("cap99") || view.Contains("max99")?99:20;a.exp=a.level==a.levelCap?0:95;save.growth.stones=20000;save.growth.eternalRings=1;
            if(view.Contains("max99"))save.affection.pendingMaxLevelPersonIds=new[]{a.personId};
            if(at>=0){a.level=19;a.exp=95;a.levelCap=20;save.growth.eternalRings=0;}
            AffectionEventResolver.Refresh(save,HomeData());BindFormalCampaign(save);title=false;encounter=null;heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;
            book.RequestSubject(BookBookmark.Heroines,form);book.CompleteTransition();affectionForm=form;affectionPanel=!view.Contains("max99");
            if(view.Contains("shop")){affectionPanel=false;affectionShop=true;kinderGarden=true;book.ChangeBookmark(BookBookmark.Summoning);book.CompleteTransition();}
            if(view.Contains("confirm")){affectionConfirm=true;affectionConfirmKind="ring-use";}
            if(at>=0){
                affectionPanel=false;PrepareGardenLifeCapture("garden-life");var manual=formalCampaign.Snapshot;manual.gardenLife.Setting(GardenLifeCatalog.GardenIds[0]).autoLife=false;BindFormalCampaign(manual);book.RequestSubject(BookBookmark.Gardens,GardenLifeCatalog.GardenIds[0]);book.CompleteTransition();StartLifeScene();gardenLifeTriggers.Clear();gardenLifeFinds.Clear();
                AcceptanceCheck(acceptanceStore.Save(formalCampaign.Snapshot),"Initialize isolated affection smoke save");Debug.Log("AFFECTION_MANUAL_READY level=19 exp=95 rings=0 stones=20000 isolated=true");
            }
            Debug.Log("AFFECTION_PLAYER_PASS view="+view+" level="+AffectionService.State(formalCampaign.Snapshot,HomeData(),form).level+" isolated=true");
        }
    }
}
