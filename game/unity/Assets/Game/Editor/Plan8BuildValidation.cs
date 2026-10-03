using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using NewAster.Presentation;
using UnityEngine;

public static partial class PlayableBuild
{
    private static void ValidatePlan8()
    {
        var trial = TrialBaselineResources.ReadAndValidate();
        var roundtrip = JsonUtility.FromJson<TrialDefinition>(JsonUtility.ToJson(trial));
        var combat = JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-formal").text);
        roundtrip.Validate(combat, WorldCatalog.ColossusIds.ToArray());
        Check(roundtrip.heroineIds.SequenceEqual(trial.heroineIds) && roundtrip.measurements.Length == 10, "Unity trial JSON preserves scope and measurement references");
        roundtrip.maxEnemyLevel = 51;
        bool rejected = false; try { roundtrip.Validate(combat, WorldCatalog.ColossusIds.ToArray()); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Unity rejects invalid trial enemy level contract");
        var missing = JsonUtility.FromJson<TrialDefinition>("{\"schemaVersion\":1}");
        rejected = false; try { missing.Validate(combat, WorldCatalog.ColossusIds.ToArray()); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Unity does not accept missing trial fields through defaults");
        TrialRunEvent observed=null;double seconds=0;
        var telemetry=new TrialRunTelemetry("unity-run","unity-session",trial.contentVersion,"build-hash","diagnostic",()=>seconds,()=>DateTime.UtcNow,row=>observed=JsonUtility.FromJson<TrialRunEvent>(JsonUtility.ToJson(row)));
        seconds=2.25;telemetry.Record("event-1","battle","attack",battleId:"battle-1",seed:73491,timeTick:123,presentationSeconds:1.2);
        Check(observed!=null && observed.runId=="unity-run" && observed.seed==73491 && observed.timeTick==123 && observed.elapsedSeconds==2.25,"Unity telemetry JSON preserves identity and distinct time units");
        var failed=new TrialRunTelemetry("unity-run","unity-session",trial.contentVersion,"build-hash","diagnostic",()=>seconds,()=>DateTime.UtcNow,row=>throw new System.IO.IOException());
        failed.Record("event-2","save","failed");Check(failed.Incomplete,"Unity telemetry disk failure never escapes into gameplay");
        var story=JsonUtility.FromJson<TrialStoryContent>(Resources.Load<TextAsset>("Trial/plan8-story-content").text);story.Validate(combat,WorldCatalog.ColossusIds[0]);
        Check(story.chapters.Sum(c=>c.poems.Length)==54 && story.events.Length==1,"Unity imports original trial text and explicit poem correspondences; original trial catalog adapter validated below");
        var originalCollection=TrialStoryCatalog.Collection(combat,story);var originalHome=TrialStoryCatalog.Home(combat,story);
        Check(originalCollection.links.Length==30 && originalCollection.chapters.Count(c=>c.textId!=null)==8,"Original trial has 30 authored correspondences and eight text chapters only");
        Check(originalHome.scripts.Count(s=>s.id.StartsWith("scene.trial.plan8."))==9,"Original trial adds eight chapter scripts and one distinct event script");
        Debug.Log("PLAN8_BUILD_BASELINE_PASS trial-only, no authored-content acceptance");
    }
}
