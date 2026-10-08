using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void PrepareOopartCapture(string view)
        {
            var save=formalCampaign.Snapshot;var catalog=CollectionData();save.growth.nectar=300000;
            save.home.formationIds=combatDefinitions.FormationIds;save.collection.ooparts=null;save.collection.equipment=Array.Empty<CollectionEquipment>();
            save.collection.relics=catalog.relics.Select(r=>new CollectionRelic{id=r.id,contentVersion=catalog.contentVersion,level=30,attackRoll=80,hpRoll=320}).ToArray();
            OopartSaveAdapter.Migrate(save.collection,catalog,save.home.formationIds);
            foreach(var p in save.collection.ooparts.progress){p.accumulatedRandomStats.physicalDefense=80;p.accumulatedRandomStats.magicDefense=80;p.accumulatedRandomStats.speed=10;}
            OopartService.Equip(save.collection.ooparts,0,"relic.tactic.skill-two");OopartService.Equip(save.collection.ooparts,1,"relic.special.ramp");OopartService.SavePreset(save.collection.ooparts,"preset.1","第1編成");
            if(view=="oopart-empty-slot"){save.home.allowEmptyFormationSlots=true;save.home.formationIds[0]=null;save.collection.ooparts.SyncFormation(save.home.formationIds);}
            save.revision++;BindFormalCampaign(save);
            AcceptanceCheck(acceptanceStore.Save(formalCampaign.Snapshot),"Oopart fixture writes through actual Unity JSON store");
            AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Oopart five-stat equipment and nullable slots reload");
            AcceptanceCheck(restored.collection.ooparts.slots[0].equippedOopartId=="relic.tactic.skill-two" && restored.collection.ooparts.slots[0].heroineFormId==save.home.formationIds[0],"Unity JSON preserves equipped and empty slots");BindFormalCampaign(restored);title=false;encounter=null;heroineRosterOpen=false;formationOpen=true;formationSlot=0;formationLayer=1;
            OpenOoparts(0,view=="oopart-conditions"?"relic.tactic.tools":view=="oopart-turn"?"relic.special.wane":"relic.tactic.skill-two");
            if(view=="oopart-direct")ProposeOopart("direct",selectedOopart,stat:"attack");
            if(view=="oopart-level")ProposeOopart("level",selectedOopart,count:10);
            if(view=="oopart-pending"){
                ProposeOopart("direct",selectedOopart,stat:"attack");var before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);
                AcceptanceCheck(formalCampaign.CommitOopart(oopartRequest,catalog,combatDefinitions.FormationIds,s=>false,HomeData())==GrowthCommitResult.SaveFailed,"Oopart UI save failure is isolated");
                AcceptanceCheck(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot)==before,"Oopart failure publishes no material costs");oopartError="保存できませんでした。同じ内容で再試行してください。";
            }
            if(view=="oopart-presets"){oopartPanel=false;formationLayer=0;}
            if(view=="oopart-items"){oopartPanel=false;formationOpen=false;book.ChangeBookmark(BookBookmark.Items);book.CompleteTransition();collectionTab=1;collectionRelicPage=8;}
            if(view=="oopart-battle"){oopartPanel=false;formationOpen=false;StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyShangrila();encounter.DrainPresentationEvents();selectedHero=0;ResetBattleMenu();battlePanel=BattlePanel.Status;AcceptanceCheck(formalCampaign.OopartBattleActive,"Battle locks oopart changes");}
            if(view=="oopart-result"){
                oopartPanel=false;formationOpen=false;book.ChangeBookmark(BookBookmark.RelicHunt);book.CompleteTransition();selectedLevel=1;
                int seed=Enumerable.Range(0,100).First(i=>new System.Random(i^0x48554E54).Next(10000)<3500);StartBattle(WorldCatalog.ColossusIds[0],seed);
                int guard=0;while(!encounter.Ended && guard++<300){int actor=encounter.AvailableHero;if(encounter.RequiresPanzerDefense(actor))encounter.DefendPanzer(actor);else if(!encounter.Act(actor,0,"body"))encounter.Pass();encounter.DrainPresentationEvents();}
                AcceptanceCheck(encounter.State.IsVictory,"Oopart result follows actual legal battle actions");FinishCheck();
                AcceptanceCheck(lastCollectionResult!=null && lastCollectionResult.relicDrops.Length>0 && lastCollectionResult.relicDrops.All(r=>r.previousRandomStats!=null && r.resultRandomStats!=null),"Actual hunt receipt contains five-stat comparison");resultTab=3;
            }
            AcceptanceCheck(formalCampaign.Snapshot.collection.ooparts.progress.Length==42,"All42 ooparts rendered in isolated native player");
            Debug.Log("PLAN14_OOPART_PLAYER_PASS view="+view+" types=42 isolated=true physicalInput=0");
        }
    }
}
