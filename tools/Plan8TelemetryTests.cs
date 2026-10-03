using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan8TelemetryTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        double now=10;var rows=new List<TrialRunEvent>();
        var telemetry=new TrialRunTelemetry("run","session","definition","hash","test",()=>now,()=>new DateTime(2026,10,4,0,0,0,DateTimeKind.Utc),rows.Add);
        now=12;telemetry.Record("first","timing","start");
        telemetry.Record("first","timing","duplicate");
        now=15;telemetry.SetInactive(true);now=115;telemetry.Record("paused","timing","pause");
        telemetry.SetInactive(false);now=118;telemetry.Record("last","timing","resume",timeTick:700,presentationSeconds:2.5);
        check(rows.Count==3 && telemetry.RecordedCount==3,"Telemetry deduplicates event IDs");
        check(rows[2].elapsedSeconds==108 && rows[2].activeSeconds==8,"Monotonic elapsed separates 100 second inactive interval");
        check(rows[2].timeTick==700 && rows[2].presentationSeconds==2.5,"Timeline ticks and planned presentation seconds remain separate");
        check(rows[0].elapsedSeconds==2 && rows[0].activeSeconds==2,"Previously emitted events stay unchanged");
        check(rows.All(r=>r.runId=="run" && r.sessionId=="session" && r.buildHash=="hash" && r.definitionVersion=="definition"),"Every event carries run provenance");
        now=117;telemetry.Record("bad-clock","timing","clock");check(telemetry.Incomplete,"Regressing monotonic clock makes run incomplete without throwing");
        var options=new JsonSerializerOptions{IncludeFields=true};
        Func<int,int,string> simulate=(seed,mode)=>{
            var state=new PlayableProgress();var battle=new PlayableBattle(10,state,seed,combatDefinitions:combat,colossusDefinition:ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]));
            var catalog=CollectionContractFixture.Create(combat);var owner=catalog.owners[0];
            var initial=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth"}};
            var session=new BattleCollectionSession(catalog,"telemetry-battle-"+seed,owner.id,10,0,combat.FormationIds,seed);
            var singing=new Random(seed^0x534F4E47);
            battle.CompletedEnemyAction=()=>session.RecordCompletedSinging(owner.poemIds[singing.Next(owner.poemIds.Length)]);
            int writes=0;double seconds=0;
            var recorder=mode==0?null:new TrialRunTelemetry("run","session","definition","hash","test",()=>seconds,()=>DateTime.UtcNow,row=>{writes++;if(mode==2)throw new System.IO.IOException("disk full");});
            int commands=0;var trace=new List<string>();
            while(!battle.Ended && commands<20000){
                bool accepted=battle.Act(battle.AvailableHero,0,"body");if(!accepted)battle.Pass();
                seconds+=.1;recorder?.Record("command-"+commands,"battle",accepted?"accepted":"rejected",seed:seed,timeTick:battle.Clock);
                foreach(var e in battle.DrainPresentationEvents()){
                    trace.Add(JsonSerializer.Serialize(new{e.Sequence,e.Clock,e.Kind,e.Actor,e.Target,e.Damage,e.Chain,e.PartBroken,e.Major,e.BossHp,e.HeroHp,e.Resources,e.PartHp},options));
                    recorder?.Record("presentation-"+e.Sequence,"battle",e.Kind.ToString(),seed:seed,timeTick:e.Clock,presentationSeconds:BattleVisualCue.Duration(e.Kind,e.Major));
                }
                commands++;
            }
            check(battle.Ended,"Telemetry comparison reaches battle end");
            if(mode!=0)check(writes>0 && recorder.Incomplete==(mode==2),"Sink failure marks incomplete and leaves battle running");
            Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
            var journal=new FormalCampaignJournal(initial,encode,s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options));
            var request=new FormalBattleEndRequest(session.Finish(battle.State.IsVictory?BattleEndReason.Victory:BattleEndReason.Defeat,initial.world.poemIds,initial.world.unlockedStoryIds),0);
            string saved=null;
            var committed=journal.CommitBattleEnd(request,catalog,w=>{var next=new CampaignState(WorldCatalog.ColossusIds,w);next.ClaimColossusVictory(owner.id,WorldCatalog.Colossi[0].EnvironmentTags,new VictoryReward(request.Id,10,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return next.CreateSave();},s=>{saved=encode(s);recorder?.Record("saved","save","committed");return true;});
            check(committed==GrowthCommitResult.Committed && saved==encode(journal.Snapshot),"Observed battle reward and poems publish exact serialized save");
            return JsonSerializer.Serialize(new{commands,battle.Clock,battle.State.IsVictory,battle.State.BossHitPoints,heroes=battle.State.Heroes.Select(h=>new{h.HitPoints,h.JobResource}),parts=battle.State.Parts.Select(p=>new{p.HitPoints,p.IsBroken}),progress=state,trace,saved},options);
        };
        foreach(int seed in new[]{0,1,8,19}){
            string off=simulate(seed,0);check(simulate(seed,1)==off,"Enabled telemetry preserves exact battle and progress snapshot");
            check(simulate(seed,2)==off,"Failed telemetry preserves exact battle and progress snapshot");
        }
    }
}
