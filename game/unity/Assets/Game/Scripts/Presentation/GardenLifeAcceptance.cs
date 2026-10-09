using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private float lifeMeasuredSeconds;
        private int lifeMeasuredFrames;
        private bool lifeMeasurementReported;
        private readonly System.Collections.Generic.HashSet<string> lifeMeasuredKinds=new System.Collections.Generic.HashSet<string>();
        private void MeasureLifeAcceptance()
        {
            if(!gardenLifeCapture || lifeMeasurementReported || gardenLifeEditor!=null || gardenLifeRuntime==null || gardenLifeRuntime.Paused || !Application.isFocused || help || bookSystemOpen)return;
            lifeMeasuredSeconds+=Time.unscaledDeltaTime;lifeMeasuredFrames++;
            foreach(var a in gardenLifeRuntime.Agents)lifeMeasuredKinds.Add(a.kind);
            if(lifeMeasuredSeconds<60)return;lifeMeasurementReported=true;
            Debug.Log("GARDEN_LIFE_MINUTE_PASS seconds="+lifeMeasuredSeconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" frames="+lifeMeasuredFrames+" fps="+(lifeMeasuredFrames/lifeMeasuredSeconds).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" agents="+gardenLifeRuntime.Agents.Count+" kinds="+string.Join(",",lifeMeasuredKinds.OrderBy(x=>x))+" managedBytes="+GC.GetTotalMemory(false)+" allocatedBytes="+UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
        }
        private void PrepareGardenLifeCapture(string view)
        {
            gardenLifeCapture=true;var args=Environment.GetCommandLineArgs();int manualAt=Array.IndexOf(args,"-gardenLifeManualSave");
            if(manualAt>=0){
                string path=System.IO.Path.GetFullPath(args[manualAt+1]);if(!System.IO.Path.GetFileName(path).StartsWith("manual-life-",StringComparison.Ordinal) || System.IO.Path.GetDirectoryName(path)!=System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(capturePath)))throw new ArgumentException("Manual smoke uses its isolated capture directory.");
                acceptanceStore=new FormalCampaignStore(path,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
                var loaded=acceptanceStore.Load(out var old);
                if(loaded==FormalLoadResult.Loaded){BindFormalCampaign(old);title=false;encounter=null;heroineRosterOpen=false;string id=old.gardenLife?.lastGardenId??old.home.furniturePlacements.FirstOrDefault()?.gardenId??GardenLifeCatalog.GardenIds[0];if(!plan15Manual){book.ChangeBookmark(BookBookmark.Gardens);book.RequestSubject(BookBookmark.Gardens,id);book.CompleteTransition();}if(book.Bookmark==BookBookmark.Gardens)StartLifeScene();Debug.Log("GARDEN_LIFE_MANUAL_RELOAD_PASS revision="+old.revision+" placements="+old.home.furniturePlacements.Length);return;}
                if(loaded!=FormalLoadResult.Missing)throw new InvalidOperationException("Manual life save cannot be loaded: "+loaded);
            }
            var save=formalCampaign.Snapshot;GardenLifeCatalog.Migrate(save);
            foreach(var d in save.world.terraform.domains){d.currentLevel=5;d.maxReachedLevel=7;}
            save.world.terraform.worldIntegrated=true;save.world.unlockedGardenIds=GardenLifeCatalog.GardenIds.ToArray();
            save.gardenLife.assignments=Array.Empty<GardenLifeAssignment>();save.gardenLife.unlockedRecipeIds=GardenLifeCatalog.RecipeIds.ToArray();
            save.home.furnitureInstances=new[]{"furniture.fixture.0","furniture.fixture.1","furniture.memorial.books","furniture.memorial.tea","furniture.production.telescope"}.Select((id,i)=>new HomeFurnitureInstance{instanceId="furniture.capture.life."+i,defId=id}).ToArray();
            string garden=view.Contains("level")?"garden.integrated-world":"garden.grassland-forest";
            save.home.furniturePlacements=save.home.furnitureInstances.Select((f,i)=>new HomePlacement{instanceId=f.instanceId,defId=f.defId,gardenId=garden,zoneId="zone.ground",orientationId="orientation.default",x=.17f+i*.17f,y=i%2==0?.72f:.86f}).ToArray();save.home.occupants=Array.Empty<HomeOccupant>();
            save.gardenLife.Setting(garden).autoWeather=false;save.gardenLife.Setting(garden).weather=view.Contains("snow")?"snow":"clear";
            if(view.Contains("level6") || view.Contains("level7")){int level=view.Contains("level7")?7:6;foreach(var d in save.world.terraform.domains){d.currentLevel=level;d.activeDeepRecordId=TerraformRules.DeepIds[Array.IndexOf(TerraformRules.DomainIds,d.domainId)];if(level==7)d.activeExtremeId=d.domainId+"_"+TerraformRules.Fusions[Array.IndexOf(TerraformRules.DomainIds,d.domainId)][0];}}
            GardenLifeCatalog.RefreshUnlocks(save);save.home.ValidateContent(HomeData(),save);save.gardenLife.ValidateContent(save,HomeData());BindFormalCampaign(save);title=false;encounter=null;heroineRosterOpen=false;book.ChangeBookmark(BookBookmark.Gardens);book.RequestSubject(BookBookmark.Gardens,garden);book.CompleteTransition();StartLifeScene();
            if(view.Contains("editor")){gardenLifeEditor=new GardenLifeEditor(save.home,garden,HomeData());selectedFurniture=save.home.furnitureInstances[0].instanceId;gardenLifeRuntime.Stop();}
            else if(view.Contains("activity"))gardenLifeRuntime.StartActivity("picnic");
            else if(view.Contains("social")){int guard=0;while(!gardenLifeRuntime.Agents.Any(a=>a.kind=="Social" && a.socialState=="Interaction") && guard++<5000)gardenLifeRuntime.Tick(.1f);AcceptanceCheck(guard<5000,"Native life fixture reaches Social through ordinary decisions");}
            else if(view.Contains("viewing"))gardenViewing=true;
            else if(view.Contains("furniture"))gardenLifePanel="家具";
            else if(view.Contains("settings"))gardenLifePanel="環境";
            else if(view.Contains("records")){book.ChangeBookmark(BookBookmark.NewWorld);book.CompleteTransition();gardenLifeRecords=true;}
            gardenLifeTriggers.Clear();gardenLifeFinds.Clear();
            if(manualAt>=0){AcceptanceCheck(acceptanceStore.Save(formalCampaign.Snapshot),"Initialize isolated manual life save");Debug.Log("GARDEN_LIFE_MANUAL_READY revision="+formalCampaign.Revision);}
            Debug.Log("GARDEN_LIFE_PLAYER_PASS view="+view+" agents="+gardenLifeRuntime.Agents.Count+" seed=113711 isolated=true");
        }
    }
}
