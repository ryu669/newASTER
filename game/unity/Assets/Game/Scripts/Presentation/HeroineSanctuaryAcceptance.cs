using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void PrepareHeroineSanctuaryCapture(string[] args)
        {
            string Arg(string key,string fallback){int index=Array.IndexOf(args,key);return index>=0 && index+1<args.Length?args[index+1]:fallback;}
            if(!ProductionStoryActive)throw new ArgumentException("Sanctuary capture requires production entry and isolated profile.");
            string view=Arg("-heroineView","roster"),id=Arg("-heroineId","heroine.slayer");if(!combatDefinitions.HeroineIds.Contains(id))throw new ArgumentException("Unknown heroine capture target.");
            acceptanceStore=new FormalCampaignStore(Path.Combine(Path.GetDirectoryName(capturePath),"heroine-"+Guid.NewGuid().ToString("N")+".json"),UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
            save.growth.nectar=20000;save.collection.materials=HomeData().materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray();
            if(view=="trait-rank5")foreach(var h in save.growth.heroines)h.duplicateRank=4;
            if(view=="skillmax" || view=="tree-grown")foreach(var h in save.growth.heroines)h.skillLevels=new[]{7,7,7};
            if(view=="tree-grown") {save.home.weaponNodeIds=HomeData().weaponNodes.Select(n=>n.id).ToArray();save.home.weaponLevels=HomeData().weaponNodes.Select(n=>new HomeWeaponLevel{nodeId=n.id,level=7}).ToArray();save.home.weaponEquipment=new[]{new HomeWeaponEquipment{heroineId=id,nodeId=id+".weapon.alpha.tier4"}};}
            if(view=="formation-battle")save.home.formationIds=combatDefinitions.FormationIds.Reverse().ToArray();
            save.revision=0;AcceptanceCheck(acceptanceStore.Save(save),"sanctuary fixture uses separate durable profile");BindFormalCampaign(save);
            encounter=null;title=false;book.RequestSubject(BookBookmark.Heroines,id);book.CompleteTransition();heroineRosterOpen=view=="roster" || view=="empty";growthScreen=GrowthScreen.Overview;selectedTrait=-1;selectedNode=null;selectedSkillSlot=0;
            if(view=="battle-idle" || view=="battle-boost" || view=="battle-status" || view=="battle-attack" || view=="battle-targets"){StartBattle(NewAster.Data.WorldCatalog.Colossi[0].Id);int actor=encounter.AvailableHero;if(view=="battle-targets")battlePanel=BattlePanel.Targets;encounter.ResourceBoostSelected=view=="battle-boost";if(view=="battle-status"){encounter.State.Heroes[actor].AddStatus(new EnemyStatusDef{kind="burn",amount=150});selectedHero=actor;battlePanel=BattlePanel.Status;}if(view=="battle-attack"){ChooseBattleSkill(0);paused=true;}}
            else if(view=="formation-battle"){StartBattle(NewAster.Data.WorldCatalog.Colossi[0].Id);selectedHero=0;battlePanel=BattlePanel.Status;}
            else if(view=="formation" || view=="formation-confirm" || view=="formation-swapped" || view=="formation-pending"){formationOpen=true;formationSlot=0;if(view=="formation-confirm" || view=="formation-pending")ProposeHome(new HomeOperation("formation",combatDefinitions.FormationIds[1],"0"));if(view=="formation-pending"){AcceptanceCheck(formalCampaign.CommitHomeOperation(homeRequest,HomeData(),homeOperation,s=>false)==GrowthCommitResult.SaveFailed,"formation save failure leaves current party active");homeError="保存できませんでした。編成は変更していません。";}if(view=="formation-swapped"){ProposeHome(new HomeOperation("formation",combatDefinitions.FormationIds[1],"0"));ConfirmHome();}}
            else if(view=="materials"){collectionOpen=true;collectionTab=2;}
            else if(view=="empty")heroineQuery="該当なし";
            else if(view=="trait" || view=="trait-second" || view=="trait-rank5")selectedTrait=view=="trait-second"?1:0;
            else if(view=="skill" || view=="skillmax" || view=="skill-pending")growthScreen=GrowthScreen.Skill;
            else if(view.StartsWith("tree")){growthScreen=GrowthScreen.Weapons;if(view=="tree-alpha" || view=="tree-beta" || view=="tree-gamma")selectedNode=id+".weapon."+view.Substring(5)+".tier4";}
            else if(view.StartsWith("train")){PrepareKinderCapture(args.Concat(new[]{"-captureKinderResult"}).ToArray());kinderScreen=KinderScreen.Revealing;kinderTrainCapture=view=="train-arrival"?.8f:view=="train-doors"?2.15f:4.5f;}
            else if(view.StartsWith("kinder")){PrepareKinderCapture(args);kinderScreen=view=="kinder-draw"?KinderScreen.Draw:view=="kinder-exchange"?KinderScreen.Exchange:view=="kinder-tickets"?KinderScreen.Tickets:view=="kinder-rates"?KinderScreen.Rates:view=="kinder-targets"?KinderScreen.Targets:KinderScreen.Entrance;
                if(view.StartsWith("kinder-confirm",StringComparison.Ordinal)){
                    var before=JsonUtility.ToJson(formalProgression.Snapshot);
                    AcceptanceCheck(BeginKinderStoneDraw(1) && !BeginKinderStoneDraw(10),"Direct summon opens one confirmation and rejects repeated click");KinderBack();AcceptanceCheck(kinderScreen==KinderScreen.Entrance && JsonUtility.ToJson(formalProgression.Snapshot)==before,"Cancel direct summon preserves balances and returns to landing");
                    if(view=="kinder-confirm-exchange"){kinderScreen=KinderScreen.Exchange;ConfirmKinder(KinderOperation.Exchange,formalProgression.Snapshot,kinderBanner.heroineIds[kinderSelection]);}
                    else if(view=="kinder-confirm-ticket"){kinderScreen=KinderScreen.Tickets;ConfirmKinder(KinderOperation.TicketDraw,formalProgression.Snapshot,kinderBanner.heroineIds[kinderSelection]);}
                    else BeginKinderStoneDraw(view=="kinder-confirm-one"?1:10);
                }}
            else if(view=="collection" || view=="relics"){collectionOpen=true;collectionTab=view=="relics"?1:0;}
            else if(view=="engagement")engagementOpen=true;
            else if(view=="awakening")GrowthSelect(GrowthScreen.Awakening,formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id));
            else if(view=="duplicate")GrowthSelect(GrowthScreen.Duplicate,formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id));
            else if(view=="level")GrowthSelect(GrowthScreen.Level,formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id));
            else if(view=="level-confirm"){growthScreen=GrowthScreen.Level;var snapshot=formalProgression.Snapshot;var heroine=snapshot.heroines.Single(h=>h.heroineId==id);GrowthConfirm(GrowthOperation.Level,id,snapshot,Math.Min(heroine.LevelCap,heroine.level+10));}
            else if(view!="detail" && view!="roster" && view!="empty")throw new ArgumentException("Unknown sanctuary UI case");
            if(view=="tree-confirm")ProposeHome(new HomeOperation("weapon",id+".weapon.root"));
            if(view=="skill-pending"){
                growthRequest=new GrowthRequest("sanctuary.pending",id,formalProgression.Snapshot.revision,GrowthOperation.Skill,2,skillSlot:0);
                AcceptanceCheck(formalProgression.Commit(growthRequest,s=>false)==GrowthCommitResult.SaveFailed && formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id).SkillLevel(0)==1,"skill pending leaves current level unchanged");growthOutcome="保存できませんでした。Lvと費用は変更していません。";
            }
            Debug.Log("HEROINE_SANCTUARY_CAPTURE_PASS view="+view+" heroine="+id+" isolated=true fixture-not-earned-progression physicalInput=0");
        }
    }
}
