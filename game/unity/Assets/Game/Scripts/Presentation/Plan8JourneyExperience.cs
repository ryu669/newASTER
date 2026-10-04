using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool plan8JourneyCapture;
        private bool plan8JourneyExchange;
        [Serializable] private sealed class TrialJourneyPoint
        {
            public string stage;public int battles,nectar,crystals,stones,points,draws,material,furniture,weapons,readChapters;public int[] levels;
        }
        [Serializable] private sealed class TrialJourneyReport
        {
            public string scope="automated real transactions; active seconds injected for persistence contract, not elapsed play time";
            public int diagnosticActiveSeconds=60;public TrialJourneyPoint[] points;
            public int fiveHeroNectarBudgetTo120=5*FormalProgression.LevelCost(1,120);
            public int fiveHeroCrystalBudgetTo120=5*(20+60);
            public bool performanceMeasured=false,humanTimingMeasured=false;
        }
        private readonly List<TrialJourneyPoint> trialJourneyPoints=new List<TrialJourneyPoint>();
        private void RecordTrialJourney(string stage)
        {
            var s=formalCampaign.Snapshot;
            trialJourneyPoints.Add(new TrialJourneyPoint{stage=stage,battles=s.collection.receipts.Length,nectar=s.growth.nectar,crystals=s.growth.awakeningCrystals,stones=s.growth.stones,points=s.growth.kinderPoints,draws=s.growth.totalKinderDraws,material=s.collection.materials.Sum(m=>m.amount),furniture=s.home.furnitureInstances.Length,weapons=s.home.weaponEquipment.Length,readChapters=s.world.readStoryIds.Count(HasTrialText),levels=s.growth.heroines.Select(h=>h.level).ToArray()});
        }
        private void JourneyLevelAll(int level)
        {
            foreach(string hero in combatDefinitions.FormationIds){
                var request=new GrowthRequest("journey.level."+level+"."+hero,hero,formalProgression.Snapshot.revision,GrowthOperation.Level,level);
                AcceptanceCheck(formalProgression.Commit(request,SaveFormalGrowth)==GrowthCommitResult.Committed,"journey level consumes actual earned nectar and saves");
            }
            ReloadAcceptance();RecordTrialJourney("five-heroes.level."+level);
        }
        private void RunTrialJourney(string directory)
        {
            CloseAdv();RecordTrialJourney("original-reading-complete");JourneyLevelAll(10);JourneyLevelAll(20);
            foreach(string hero in combatDefinitions.FormationIds){
                AcceptanceHome(new HomeOperation("weapon",hero+".weapon.root"));AcceptanceHome(new HomeOperation("weapon",hero+".weapon.alpha"));AcceptanceHome(new HomeOperation("equip",hero+".weapon.alpha",hero));
            }
            string garden=HomeData().gardens.First(g=>!g.unmade && formalCampaign.Snapshot.world.unlockedGardenIds.Contains(g.id)).id;
            int index=0;foreach(var furniture in HomeData().furniture){
                string instance="journey.furniture."+index;AcceptanceHome(new HomeOperation("craft",furniture.id,instance),index==0);
                AcceptanceHome(new HomeOperation("place",instance,garden:garden,zone:"zone.ground",x:.3f+index*.25f,y:.7f));index++;
            }
            AcceptanceHome(new HomeOperation("occupant","heroine.slayer",garden:garden,x:.2f,y:.72f));
            AcceptanceHome(new HomeOperation("use","heroine.slayer","journey.furniture.0"));ReloadAcceptance();RecordTrialJourney("weapons-and-garden");
            for(int level=2;level<=10;level++)PlayedAcceptanceEnding(BattleEndReason.Victory,56000+level,0,level);
            int upper=0;while(formalProgression.Snapshot.nectar<5*FormalProgression.LevelCost(20,30) && upper<64){PlayedAcceptanceEnding(BattleEndReason.Victory,57000+upper++,0,10);}
            AcceptanceCheck(upper<64,"higher-level earned rewards reach next growth milestone without synthetic balances");
            JourneyLevelAll(30);
            for(int level=11;level<=50;level++)PlayedAcceptanceEnding(BattleEndReason.Victory,58000+level,0,level);
            encounter=null;result=null;RecordTrialJourney("enemy-lv50-unlocked-in-order");
            var relic=formalCampaign.Snapshot.collection.relics.First();RelicSelect(relic,RelicOperation.LevelUp);RelicCommit();
            AcceptanceCheck(relicRequest==null && formalCampaign.Snapshot.collection.relics.Single(r=>r.id==relic.id).level==2,"journey hunt relic upgrades with earned materials");
            RelicSelect(formalCampaign.Snapshot.collection.relics.Single(r=>r.id==relic.id),RelicOperation.Equip,"heroine.slayer");RelicCommit();ReloadAcceptance();RecordTrialJourney("upper-battle-and-relic");
            unsavedActiveSeconds=60;string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);formalVictoryDiagnosticFailure=true;
            AcceptanceCheck(!FlushActiveTime() && before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot) && unsavedActiveSeconds==60,"trial active-time persistence failure retains uncredited seconds");
            formalVictoryDiagnosticFailure=false;AcceptanceCheck(FlushActiveTime() && unsavedActiveSeconds==0,"trial active-time retry writes dedicated store");ReloadAcceptance();
            ConfirmEngagement(true);ReceiveEngagement();AcceptanceCheck(engagementComplete,"trial login reward uses durable dedicated writer");EngagementBack();ReloadAcceptance();
            var intro=new KinderRequest("grant.plan4-kinder-introduction",formalProgression.Snapshot.revision,KinderOperation.IntroGrant);
            AcceptanceCheck(formalProgression.CommitKinder(intro,kinderBanner,max=>0,SaveFormalGrowth)==GrowthCommitResult.Committed,"optional existing introduction grant is explicit and once-only");
            var draw=new KinderRequest("journey.draw-ten",formalProgression.Snapshot.revision,KinderOperation.StoneDraw,10);var random=new System.Random(58100);
            AcceptanceCheck(formalProgression.CommitKinder(draw,kinderBanner,max=>random.Next(max),SaveFormalGrowth)==GrowthCommitResult.Committed,"journey ten draws spend actual stones using unchanged banner");
            ReloadAcceptance();RecordTrialJourney("login-and-ten-draws");
            if(plan8JourneyExchange)RunTrialJourneyExchange();
            ValidateTrialJourneyRestart();
            File.WriteAllText(Path.Combine(directory,"journey.json"),JsonUtility.ToJson(new TrialJourneyReport{points=trialJourneyPoints.ToArray()},true));
            Debug.Log("PLAN8_JOURNEY_PLAYER_PASS battles="+formalCampaign.Snapshot.collection.receipts.Length+" upperGrowthBattles="+upper+" activeSeconds=60 mode=diagnostic-injected-not-real-time");
        }
        private void RunTrialJourneyExchange()
        {
            int farm=0;
            while(formalProgression.Snapshot.stones<90*FormalKinderBanner.StoneCost && farm<300)
                PlayedAcceptanceEnding(BattleEndReason.Victory,59000+farm++,0,50);
            AcceptanceCheck(farm<300,"ticket exchange budget comes from unlocked Lv50 victories");
            RecordTrialJourney("exchange-stone-budget-earned");
            var random=new System.Random(59100);
            for(int batch=0;batch<9;batch++){
                var request=new KinderRequest("journey.draw-extra."+batch,formalProgression.Snapshot.revision,KinderOperation.StoneDraw,10);
                AcceptanceCheck(formalProgression.CommitKinder(request,kinderBanner,max=>random.Next(max),SaveFormalGrowth)==GrowthCommitResult.Committed,"additional paid draws save actual cost and points");
                ReloadAcceptance();
            }
            RecordTrialJourney("one-hundred-paid-draws");
            var exchange=new KinderRequest("journey.exchange",formalProgression.Snapshot.revision,KinderOperation.Exchange,heroineId:"heroine.slayer");
            string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);formalVictoryDiagnosticFailure=true;
            AcceptanceCheck(formalProgression.CommitKinder(exchange,kinderBanner,null,SaveFormalGrowth)==GrowthCommitResult.SaveFailed && before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"failed exchange publishes neither point consumption nor ticket");
            formalVictoryDiagnosticFailure=false;
            AcceptanceCheck(formalProgression.CommitKinder(exchange,kinderBanner,null,SaveFormalGrowth)==GrowthCommitResult.Committed,"same failed exchange retries once");ReloadAcceptance();RecordTrialJourney("dedicated-ticket-exchanged");
            var ticket=new KinderRequest("journey.ticket",formalProgression.Snapshot.revision,KinderOperation.TicketDraw,heroineId:"heroine.slayer");
            int stones=formalProgression.Snapshot.stones,fragments=formalProgression.Snapshot.heroines.Single(h=>h.heroineId=="heroine.slayer").fragments;
            AcceptanceCheck(formalProgression.CommitKinder(ticket,kinderBanner,max=>throw new InvalidOperationException("Dedicated ticket must not roll."),SaveFormalGrowth)==GrowthCommitResult.Committed,"dedicated ticket guarantees chosen heroine without RNG");ReloadAcceptance();
            AcceptanceCheck(formalProgression.Snapshot.stones==stones && formalProgression.Snapshot.heroines.Single(h=>h.heroineId=="heroine.slayer").fragments==fragments+100 && formalProgression.CommitKinder(ticket,kinderBanner,null,SaveFormalGrowth)==GrowthCommitResult.AlreadyCommitted,"ticket survives physical reload without repeated cost or duplicate award");
            RecordTrialJourney("dedicated-ticket-used");
        }
        private void ValidateTrialJourneyRestart()
        {
            var s=formalCampaign.Snapshot;
            AcceptanceCheck(s.growth.heroines.All(h=>h.level==30),"journey five Lv30 heroes survive reload");
            AcceptanceCheck(s.home.weaponEquipment.Length==5 && s.home.furnitureInstances.Length==3 && s.home.furniturePlacements.Length==3 && s.home.occupants.Length==1,"journey equipment and garden placements survive reload");
            AcceptanceCheck(s.collection.equipment.Length==1 && s.collection.relics.Any(r=>r.level==2),"journey upgraded equipped hunt relic survives reload");
            AcceptanceCheck(s.growth.totalKinderDraws==(plan8JourneyExchange?100:10) && s.growth.kinderPoints==(plan8JourneyExchange?0:10) && s.engagement.activeSeconds==60 && s.engagement.lastLoginDay>0,"journey economic receipts and dedicated active/login ledger survive reload");
            if(plan8JourneyExchange)AcceptanceCheck(s.growth.tickets.Single(t=>t.heroineId=="heroine.slayer").count==0 && s.growth.receipts.Any(r=>r.transactionId=="journey.exchange") && s.growth.receipts.Any(r=>r.transactionId=="journey.ticket"),"used dedicated ticket and exchange receipts survive separate process restart");
        }
    }
}
