using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool plan7ActiveCombat;
        private readonly bool plan7FullCombat=Environment.GetCommandLineArgs().Contains("-validatePlan7FullCombat");
        private bool plan7FullCombatComplete;
        private string plan7ReferenceSignature,plan7SaveBefore;
        private readonly StringBuilder plan7RenderedTrace=new StringBuilder();
        private readonly HashSet<long> plan7RepaintedEvents=new HashSet<long>();
        private int plan7QueuedEvents;
        private void RecordPlan7RenderedEvent()
        {if(plan7FullCombat && Event.current.type==EventType.Repaint && playback.Current!=null)plan7RepaintedEvents.Add(playback.Current.Sequence);}
        private string PlaybackSignature(PlayableBattle battle,StringBuilder trace)
        {
            var receipt=collectionSession.Finish(battle.State.IsVictory?BattleEndReason.Victory:BattleEndReason.Defeat,formalCampaign.Snapshot.world.poemIds,formalCampaign.Snapshot.world.unlockedStoryIds);
            var shadow=new FormalCampaignJournal(formalCampaign.Snapshot,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode);
            var previousRequest=formalVictoryRequest;string previousSummary=formalVictorySummary;int writes=0;
            try{
                formalVictoryRequest=battle.State.IsVictory?new FormalVictoryRequest(battleId,activeColossus,selectedLevel,shadow.Snapshot.revision):null;
                FocusCheck(shadow.CommitBattleEnd(new FormalBattleEndRequest(receipt,shadow.Snapshot.revision),collectionCatalog,BuildVictoryWorld,s=>{writes++;return true;},HomeData())==GrowthCommitResult.Committed && writes==1,"shadow reward commit succeeds once without file writes");
            }finally{formalVictoryRequest=previousRequest;formalVictorySummary=previousSummary;}
            return PlaybackState(battle)+"/"+trace+"/"+UnityFormalCampaignJson.Encode(shadow.Snapshot);
        }
        private void AppendPlaybackEvent(StringBuilder trace,BattlePresentationEvent e)
        {trace.Append(e.Sequence).Append('/').Append(e.Clock).Append('/').Append(e.Kind).Append('/').Append(e.Damage).Append('/').Append(e.Target).Append(';');}
        private int plan7ScriptCommands,plan7ScriptRuns,plan7TotalCommands,plan7ActiveFrames,plan7MajorFrames,plan7BreakFrames;
        private string PlaybackState(PlayableBattle battle)
        {return battle.Clock+"/"+battle.State.BossHitPoints+"/"+battle.State.BossGauge+"/"+string.Join(",",battle.State.Heroes.Select(h=>h.HitPoints+":"+h.JobResource))+"/"+string.Join(",",battle.State.Parts.Select(p=>p.HitPoints));}
        private PlayableBattle PlaybackFixture(int seed)
        {
            var growth=formalProgression.Snapshot;foreach(var hero in growth.heroines){hero.level=30;hero.awakeningStage=0;}
            return new PlayableBattle(1,campaign.Playable,seed,combatDefinitions:combatDefinitions,formalGrowth:growth,colossusDefinition:ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]),collectionGrowth:formalCampaign.Snapshot.collection,homeProgress:formalCampaign.Snapshot.home,homeCatalog:HomeData());
        }
        private void PlaybackCommand(PlayableBattle battle,int index)
        {
            if(index<20){battle.Pass();return;}
            string part=battle.State.Parts.FirstOrDefault(p=>!p.IsBroken)?.Id??"body";
            if(!battle.Act(battle.AvailableHero,0,part))battle.Pass();
        }
        private void PreparePlan7Playback()
        {
            if(!formalDiagnostic || capturePath==null)throw new InvalidOperationException("Playback acceptance requires isolated capture mode");
            string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),reference=null;
            activeColossus=WorldCatalog.ColossusIds[0];selectedLevel=1;
            foreach(string mode in new[]{"normal","shortened","skip","paused"}){
                encounter=PlaybackFixture(73491);battleId="plan7.playback.fixture";StartCollection();
                var queue=new BattlePlaybackQueue();var trace=new StringBuilder();int commands=0,events=0,major=0,broken=0;
                while(!encounter.Ended && commands<20000){
                    PlaybackCommand(encounter,commands++);var batch=encounter.DrainPresentationEvents();queue.Enqueue(batch);
                    foreach(var e in batch){events++;if(e.Major)major++;if(e.PartBroken)broken++;AppendPlaybackEvent(trace,e);}
                    string state=PlaybackState(encounter);
                    if(mode=="paused"){var current=queue.Current;for(int i=0;i<60;i++)queue.Tick(1,true);FocusCheck(queue.Current==current && PlaybackState(encounter)==state,"paused playback leaves queue and battle frozen");}
                    if(mode=="skip")queue.Skip();else {int frames=0;while(queue.Busy && frames++<10000)queue.Tick(mode=="shortened"?.2f:.1f,false);FocusCheck(!queue.Busy,"bounded playback");}
                    FocusCheck(PlaybackState(encounter)==state,"presentation does not change combat state");
                }
                FocusCheck(encounter.Ended && commands<20000 && events>0 && major>0 && broken>0,"complete scripted battle includes major and break");
                string signature=PlaybackSignature(encounter,trace);
                if(reference==null)reference=signature;else FocusCheck(reference==signature,"same seed/input HP, timeline, events, poems and reward receipt: "+mode);
                Debug.Log("PLAN7_PLAYBACK_MODE_PASS "+mode+" commands="+commands+" events="+events+" major="+major+" breaks="+broken+" victory="+encounter.State.IsVictory);
            }
            FocusCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"replay validation leaves durable save unchanged");
            plan7ReferenceSignature=reference;plan7SaveBefore=before;
            using(var hash=SHA256.Create())Debug.Log("PLAN7_PLAYBACK_EQUIVALENCE_PASS modes=4 seed=73491 sha256="+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(reference))).Replace("-",""));
            plan7ActiveCombat=true;artSample=false;title=false;paused=false;result=null;playback.Reset();shownEvent=0;
            encounter=PlaybackFixture(73491);battleId=plan7FullCombat?"plan7.playback.fixture":"plan7.performance.fixture";StartCollection();selectedHero=encounter.AvailableHero;target="body";
        }
        private void UpdatePlan7ActiveCombat()
        {
            if(plan7ActiveCombat && !paused && Application.isFocused && Time.realtimeSinceStartup>8 && playback.Busy){plan7ActiveFrames++;if(playback.Current.Major)plan7MajorFrames++;if(playback.Current.PartBroken)plan7BreakFrames++;}
            if(!plan7ActiveCombat || paused || help || retreat || playback.Busy || !Application.isFocused || plan7FullCombatComplete || plan7FullCombat && Time.realtimeSinceStartup<=8)return;
            if(plan7FullCombat && encounter.Ended){
                FocusCheck(encounter.State.IsVictory && plan7ScriptCommands==32,"rendered fixture reaches victory after all presentation events");
                FocusCheck(plan7QueuedEvents==66 && plan7RepaintedEvents.Count==plan7QueuedEvents,"every queued event reaches a battle GUI repaint");
                FocusCheck(PlaybackSignature(encounter,plan7RenderedTrace)==plan7ReferenceSignature,"fully rendered battle matches four playback modes including rewards");
                FocusCheck(plan7SaveBefore==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"rendered battle leaves durable save unchanged");
                plan7ScriptRuns=1;plan7FullCombatComplete=true;
                Debug.Log("PLAN7_FULL_COMBAT_PASS commands="+plan7ScriptCommands+" repaintedEvents="+plan7RepaintedEvents.Count+" victory=True queueEmpty=True rewardSignatureMatches=True saveUnchanged=True playbackRate=1");return;
            }
            if(encounter.Ended){plan7ScriptRuns++;encounter=PlaybackFixture(73491+plan7ScriptRuns);battleId="plan7.performance.fixture."+plan7ScriptRuns;StartCollection();playback.Reset();shownEvent=0;result=null;plan7ScriptCommands=0;}
            PlaybackCommand(encounter,plan7ScriptCommands++);plan7TotalCommands++;selectedHero=encounter.AvailableHero;var batch=encounter.DrainPresentationEvents();if(plan7FullCombat)foreach(var e in batch){AppendPlaybackEvent(plan7RenderedTrace,e);plan7QueuedEvents++;}playback.Enqueue(batch);
        }
        private void ReportPlan7ActiveCombat()
        {if(!plan7ActiveCombat)return;FocusCheck(plan7TotalCommands>5 && plan7ActiveFrames>30,"active combat progresses during capture");if(measureArt)FocusCheck(plan7MajorFrames>0 && plan7BreakFrames>0,"measurement includes actual major and break frames");Debug.Log("PLAN7_ACTIVE_COMBAT_PROGRESS_PASS commands="+plan7TotalCommands+" busyFrames="+plan7ActiveFrames+" majorFrames="+plan7MajorFrames+" breakFrames="+plan7BreakFrames+" completedRuns="+plan7ScriptRuns+" fixture=hero-level30/boss-level1/basic-attacks-after20passes");}
    }
}
