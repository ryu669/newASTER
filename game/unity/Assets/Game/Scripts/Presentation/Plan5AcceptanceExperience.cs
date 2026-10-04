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
        [Serializable] private sealed class PreCollectionAcceptanceSave
        {
            public int version=1;public string saveId=FormalCampaignSave.Identity;public long revision;
            public CampaignSaveV2 world;public FormalGrowthSave growth;public FormalEngagementState engagement;
        }
        private FormalCampaignStore acceptanceStore;
        private int acceptanceChecks;
        private bool SaveDiagnosticCampaign(FormalCampaignSave next)
        {
            bool success=!formalVictoryDiagnosticFailure && (acceptanceStore==null || acceptanceStore.Save(next));
            ObserveTrialSave(next,success);return success;
        }
        private void AcceptanceCheck(bool ok,string description)
        {acceptanceChecks++;if(!ok)throw new InvalidOperationException("Plan5 acceptance: "+description);}
        private void ReloadAcceptance()
        {
            var expected=JsonUtility.ToJson(formalCampaign.Snapshot);
            AcceptanceCheck(acceptanceStore.Load(out var loaded)==FormalLoadResult.Loaded && JsonUtility.ToJson(loaded)==expected,"physical file restart matches complete ledger");
            BindFormalCampaign(loaded);
        }
        private void PlayedAcceptanceEnding(BattleEndReason reason,int seed,int songs,int level=0)
        {
            selectedLevel=level>0?level:reason==BattleEndReason.Defeat?50:1;
            AcceptanceCheck(!plan8JourneyCapture || selectedLevel<=campaign.Playable.HighestLevel,"journey respects the real unlocked enemy level");
            StartBattle(WorldCatalog.ColossusIds[0],seed);
            int completions=0,steps=0;var record=encounter.CompletedEnemyAction;encounter.CompletedEnemyAction=()=>{record();completions++;};
            while(!encounter.Ended && completions<songs && steps++<20000){encounter.Pass();TrialObserve("battle","diagnostic-pass",tick:encounter.Clock);}
            while(reason!=BattleEndReason.Retreat && !encounter.Ended && steps++<20000) {
                bool accepted=reason!=BattleEndReason.Defeat && encounter.Act(encounter.AvailableHero,0,"body");
                if(reason!=BattleEndReason.Defeat)TrialObserve("battle",accepted?"diagnostic-input-accepted":"diagnostic-input-rejected","skill=0;target=body",tick:encounter.Clock);
                if(!accepted){encounter.Pass();TrialObserve("battle","diagnostic-pass",tick:encounter.Clock);}
                encounter.DrainPresentationEvents();
            }
            AcceptanceCheck(steps<20000 && completions>0,"bounded real commands and completed singing");
            AcceptanceCheck(reason==BattleEndReason.Retreat?!encounter.Ended:reason==BattleEndReason.Victory?encounter.State.IsVictory:!encounter.State.Heroes.Any(h=>h.IsAlive),"natural ending reason");
            string before=JsonUtility.ToJson(formalCampaign.Snapshot);formalVictoryDiagnosticFailure=true;PrepareFormalBattleEnd(reason);
            AcceptanceCheck(formalCampaign.HasPending && JsonUtility.ToJson(formalCampaign.Snapshot)==before,"failed end excludes publication");
            var request=formalBattleEndRequest;formalVictoryDiagnosticFailure=false;PersistFormalVictory();
            AcceptanceCheck(!formalCampaign.HasPending && formalBattleEndRequest==null,"same ending retry publishes");
            ReloadAcceptance();
            AcceptanceCheck(formalCampaign.CommitBattleEnd(request,null,null,s=>throw new Exception("duplicate write"))==GrowthCommitResult.AlreadyCommitted,"restart excludes replay");
            for(int tab=0;tab<4;tab++){resultTab=tab;AcceptanceCheck(!string.IsNullOrEmpty(ResultDetail()),"result tab resolves after reload");}
            Debug.Log("PLAN5_PLAYED_END reason="+reason+" seed="+seed+" commands="+steps+" singing="+completions);
            if(plan8JourneyCapture)RecordTrialJourney("battle."+formalCampaign.Snapshot.collection.receipts.Length);
        }
        private void PreparePlan5Acceptance(string[] args)
        {
            if(!formalDiagnostic || capturePath==null)throw new InvalidOperationException("Acceptance requires isolated capture mode.");
            string root=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(capturePath)),Path.GetFileNameWithoutExtension(capturePath)+".acceptance");Directory.CreateDirectory(root);
            string path=Path.Combine(root,"formal-campaign-v1.json");
            acceptanceStore=new FormalCampaignStore(path,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            if(args.Contains("-plan5Resume")) {
                AcceptanceCheck(acceptanceStore.Load(out var resumed)==FormalLoadResult.Loaded,"separate player process loads accepted save");BindFormalCampaign(resumed);
                AcceptanceCheck(resumed.collection!=null && resumed.collection.receipts.Any(r=>r.reason==BattleEndReason.Defeat) && resumed.collection.receipts.Any(r=>r.reason==BattleEndReason.Retreat),"every ending survives process restart");
                AcceptanceCheck(resumed.world.unlockedStoryIds.Length==18 && resumed.collection.equipment.Length==1 && resumed.collection.relics.Single().level==2,"chapters and upgraded equipment survive process restart");
                selectedLevel=1;StartBattle(WorldCatalog.ColossusIds[0]);while(!encounter.Ended && lastSinging==null)encounter.Pass();encounter.DrainPresentationEvents();selectedHero=encounter.AvailableHero;
                Debug.Log("PLAN5_PROCESS_RESTART_PASS "+acceptanceChecks+" assertions file="+path);return;
            }
            AcceptanceCheck(!File.Exists(path),"acceptance never replaces an existing test save");
            bool legacy=!args.Contains("-plan5NewSave");var initial=formalCampaign.Snapshot;
            if(legacy){initial.world.poemIds=new[]{"legacy.plan4.poem"};initial.growth.stones=321;}
            if(legacy)File.WriteAllText(path,JsonUtility.ToJson(new PreCollectionAcceptanceSave{revision=initial.revision,world=initial.world,growth=initial.growth,engagement=initial.engagement},true));
            else AcceptanceCheck(acceptanceStore.Save(initial),"new formal save created");
            AcceptanceCheck(acceptanceStore.Load(out var starting)==FormalLoadResult.Loaded,"new or pre-collection formal envelope loaded");BindFormalCampaign(starting);ReloadAcceptance();
            foreach(var id in combatDefinitions.FormationIds) {
                var request=new GrowthRequest("acceptance.growth."+id,id,formalProgression.Snapshot.revision,GrowthOperation.Level,10);
                AcceptanceCheck(formalProgression.Commit(request,SaveFormalGrowth)==GrowthCommitResult.Committed,"real growth transaction before encounter");
            }
            ReloadAcceptance();
            // Complete chapter conditions through actual commands, never inject heard poems or boss HP.
            int fights=0;
            do {PlayedAcceptanceEnding(BattleEndReason.Victory,8+fights,8);fights++;}
            while(fights<128 && (formalCampaign.Snapshot.world.unlockedStoryIds.Length<18 || formalCampaign.Snapshot.collection.relics.Length==0));
            AcceptanceCheck(fights<128 && formalCampaign.Snapshot.world.unlockedStoryIds.Length==18,"all 8/6-poem chapters acquired through played singing");
            AcceptanceCheck(campaign.Gardens.UnlockedGardenIds.Contains("garden.grassland-forest") && campaign.ColossusUnlocks.IsUnlocked(WorldCatalog.ColossusIds[1]) && !ColossusCombatCatalog.CanSummon(WorldCatalog.ColossusIds[1]),"first-clear garden and next page without unmade summon");
            AcceptanceCheck(formalCampaign.Snapshot.world.firstClearIds.Length==1 && formalCampaign.Snapshot.world.environmentTags.Length==2 && (!legacy || formalCampaign.Snapshot.world.poemIds.Contains("legacy.plan4.poem")),"repeat farming preserves first-clear and old poems");
            PlayedAcceptanceEnding(BattleEndReason.Defeat,71,1);PlayedAcceptanceEnding(BattleEndReason.Retreat,73,1);
            var relic=formalCampaign.Snapshot.collection.relics.Single();collectionOpen=true;collectionTab=1;result=null;encounter=null;
            string cancelBefore=JsonUtility.ToJson(formalCampaign.Snapshot);RelicSelect(relic,RelicOperation.LevelUp);CollectionBack();AcceptanceCheck(relicRequest==null && JsonUtility.ToJson(formalCampaign.Snapshot)==cancelBefore,"upgrade confirmation cancel is inert");
            RelicSelect(relic,RelicOperation.LevelUp);formalVictoryDiagnosticFailure=true;RelicCommit();AcceptanceCheck(formalCampaign.HasPending && JsonUtility.ToJson(formalCampaign.Snapshot)==cancelBefore,"relic failed save remains pending");
            CollectionBack();AcceptanceCheck(relicRequest!=null,"pending relic cannot cancel");formalVictoryDiagnosticFailure=false;RelicCommit();ReloadAcceptance();
            RelicSelect(formalCampaign.Snapshot.collection.relics.Single(),RelicOperation.Equip,combatDefinitions.FormationIds[0]);
            var comparison=RelicEquipmentComparison(relic,relicRequest);AcceptanceCheck(comparison.Contains("→") && comparison.Contains("チェイン率"),"equip comparison provides actual before and after");RelicCommit();ReloadAcceptance();
            AcceptanceCheck(formalCampaign.Snapshot.collection.equipment.Length==1 && formalCampaign.Snapshot.collection.relics.Single().level==2,"upgrade and equip durable together");
            selectedLevel=1;StartBattle(WorldCatalog.ColossusIds[0]);var equipped=encounter.State.Heroes[0];var naked=new PlayableBattle(1,campaign.Playable,encounter.Seed,combatDefinitions:combatDefinitions,formalGrowth:formalProgression.Snapshot,colossusDefinition:ActiveColossusDefinition(activeColossus));
            AcceptanceCheck(equipped.BaseAttack>naked.State.Heroes[0].BaseAttack && encounter.ChainRate(0)==naked.ChainRate(0),"retry encounter applies equipment without chain change");
            encounter=null;collectionOpen=true;RelicSelect(formalCampaign.Snapshot.collection.relics.Single(),RelicOperation.Unequip,combatDefinitions.FormationIds[0]);
            Debug.Log("PLAN5_PLAYER_ACCEPTANCE_PASS "+acceptanceChecks+" assertions fights="+fights+" file="+path);
        }
    }
}
