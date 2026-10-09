using System;
using System.Collections;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void PrepareAbilityCapture(string view)
        {
            if(view.StartsWith("ability-battle",StringComparison.Ordinal)){StartCoroutine(VerifyAddedFormationBattles());return;}
            string id=view=="ability-trait"?"heroine.echidna":"heroine.slayer";
            heroineRosterOpen=false;formationOpen=false;book.RequestSubject(BookBookmark.Heroines,id);book.CompleteTransition();
            growthScreen=view=="ability-trait"?GrowthScreen.Overview:GrowthScreen.Weapons;selectedTrait=view=="ability-trait"?1:-1;
            selectedNode=id+".weapon.root";
            if(view=="ability-tree")selectedNode=HomeData().weaponNodes.First(n=>n.heroineId==id && n.uniqueAbilityKind=="regen").id;
            AcceptanceCheck(HomeState.weaponNodeIds.Contains(id+".weapon.root") && HomeState.WeaponLevel(id+".weapon.root")==0,"Root is available without Lv in production UI");
        }
        private IEnumerator VerifyAddedFormationBattles()
        {
            yield return null;
            var baseline=formalCampaign.Snapshot;int count=0;
            foreach(string id in combatDefinitions.HeroineIds)for(int slot=0;slot<5;slot++){
                var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(baseline));
                var ids=combatDefinitions.FormationIds.Where(x=>combatDefinitions.PersonId(x)!=combatDefinitions.PersonId(id)).Take(4).ToList();ids.Insert(slot,id);save.home.formationIds=ids.ToArray();
                BindFormalCampaign(save);book.ChangeBookmark(BookBookmark.Colossi);book.CompleteTransition();heroineRosterOpen=false;formationOpen=false;collectionOpen=false;kinderGarden=false;
                StartBattle(WorldCatalog.ColossusIds[0],882);AcceptanceCheck(encounter!=null && encounter.State.Heroes[slot].Id==id && encounter.AvailableHero>=0,"Native stage starts battle: "+id+" slot "+slot);count++;
            }
            selectedHero=0;paused=true;battlePanel=BattlePanel.Status;
            Debug.Log("ADDED_FORMATION_BATTLES_PASS "+count+" renderer=2d");
        }
    }
}
