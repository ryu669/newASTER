using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void PrepareProductionGardenCapture(string view)
        {
            var parts=view.Split('.');if(parts.Length!=2)throw new ArgumentException("Unknown production garden view");
            var data=HomeData();int index=0;bool panel=parts[1]=="audio" || parts[1]=="furniture" || parts[1]=="residents" || parts[1]=="placement" || parts[1]=="confirmation";
            if(!panel && (!int.TryParse(parts[1],out index) || index<0 || index>=data.gardens.Length))throw new ArgumentException("Unknown production garden index");
            var snapshot=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
            snapshot.world.unlockedGardenIds=data.gardens.Select(g=>g.id).ToArray();snapshot.collection.materials=data.materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=100}).ToArray();snapshot.revision++;
            TerraformRules.Migrate(snapshot.world);
            snapshot.world.terraform.unlockedFurnitureIds=snapshot.world.terraform.unlockedFurnitureIds.Union(data.furniture.Take(10).Select(f=>f.id)).ToArray();
            AcceptanceCheck(acceptanceStore.Save(snapshot),"garden diagnostic material fixture is isolated");BindFormalCampaign(snapshot);
            string garden=data.gardens[index].id;
            for(int i=0;i<Math.Min(10,data.furniture.Length);i++){
                string instance="garden.capture."+i;ProposeHome(new HomeOperation("craft",data.furniture[i].id,instance));
                if(i==0){string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);formalVictoryDiagnosticFailure=true;ConfirmHome();AcceptanceCheck(formalCampaign.HasPending && UnityFormalCampaignJson.Encode(formalCampaign.Snapshot)==before,"garden crafting save failure retains original contents");formalVictoryDiagnosticFailure=false;}
                ConfirmHome();AcceptanceCheck(homeRequest==null,"production furniture craft commits");
                ProposeHome(new HomeOperation("place",instance,garden:garden,zone:"zone.ground",x:.13f+.23f*(i%4),y:.5f+.14f*(i/4)));ConfirmHome();AcceptanceCheck(homeRequest==null,"production furniture placement commits");
            }
            for(int i=0;i<combatDefinitions.FormationIds.Length;i++){
                string hero=combatDefinitions.FormationIds[i];ProposeHome(new HomeOperation("occupant",hero,garden:garden,x:.5f,y:.7f));ConfirmHome();ProposeHome(new HomeOperation("use",hero,"garden.capture."+i));ConfirmHome();AcceptanceCheck(formalCampaign.Snapshot.home.occupants.Single(o=>o.heroineId==hero).furnitureInstanceId=="garden.capture."+i,"production heroine uses selected furniture");
            }
            ReloadAcceptance();AcceptanceCheck(HomeState.furniturePlacements.Length==10 && HomeState.occupants.Length==5,"production garden contents survive physical reload");
            book.RequestSubject(BookBookmark.Gardens,garden);book.CompleteTransition();ResetGardenMenu();
            if(parts[1]=="furniture")OpenGardenPanel(GardenPanel.Furniture);
            if(parts[1]=="residents")OpenGardenPanel(GardenPanel.Residents);
            if(parts[1]=="placement"){OpenGardenPanel(GardenPanel.Furniture);selectedFurniture="garden.capture.0";BeginGardenPlacement();}
            if(parts[1]=="confirmation"){OpenGardenPanel(GardenPanel.Furniture);ProposeHome(new HomeOperation("craft",data.furniture[9].id,"garden.capture.extra"));}
            Debug.Log("PLAN9_GARDEN_PLAYER_PASS garden="+garden+" furniture=10 residents=5 view="+parts[1]+" isolated=true fixture-not-earned-progression");
        }
    }
}
