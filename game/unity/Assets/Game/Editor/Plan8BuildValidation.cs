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
        Debug.Log("PLAN8_BUILD_BASELINE_PASS trial-only, no authored-content acceptance");
    }
}
