using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public static class TrialBaselineResources
    {
        private static T Read<T>(string path)
        {
            var source = Resources.Load<TextAsset>(path);
            if (source == null) throw new ArgumentException("Missing trial resource: " + path);
            var value = JsonUtility.FromJson<T>(source.text);
            if (value == null) throw new ArgumentException("Empty trial resource: " + path);
            return value;
        }

        public static TrialDefinition ReadAndValidate()
        {
            var trial = Read<TrialDefinition>("Trial/plan8-baseline");
            var combat = Read<CombatDefinitionCatalog>("Combat/battle-formal");
            trial.Validate(combat, WorldCatalog.ColossusIds.ToArray());
            Read<BattleIllustrationManifest>("Illustrations/battle-formal").Validate();
            Read<FormalKinderBanner>("Economy/kinder-trial").Validate(combat.FormationIds);
            Read<FormalEngagementRules>("Economy/engagement-trial").Validate();
            return trial;
        }

        public static bool RunDiagnostic(string[] args)
        {
            if (!args.Contains("-validatePlan8Baseline")) return false;
            try
            {
                var boundary = TrialDiagnosticBoundary.Create(Option(args, "-plan8RepositoryRoot"), Application.persistentDataPath, Option(args, "-plan8RunId"));
                var trial = ReadAndValidate();
                Debug.Log("PLAN8_BASELINE_PASS schema=1 scope=development-trial heroes=" + trial.heroineIds.Length + " enemy=1 levels=1-50 strongest=45 definitions=4 measurements=10");
                Debug.Log("PLAN8_SAVE_BOUNDARY_PASS saveId=" + boundary.SaveId + " storeOpened=False saveWritten=False");
                Debug.Log("PLAN8_CONTENT_GOALS_PENDING chapters=8 events=1 poems=54");
                Application.Quit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("PLAN8_BASELINE_ERROR " + e.Message);
                Application.Quit(2);
            }
            return true;
        }

        private static string Option(string[] args, string name)
        {
            var indices = Enumerable.Range(0, args.Length).Where(i => args[i] == name).ToArray();
            if (indices.Length != 1 || indices[0] + 1 >= args.Length || args[indices[0] + 1].StartsWith("-", StringComparison.Ordinal))
                throw new ArgumentException("Exactly one diagnostic option required: " + name);
            return args[indices[0] + 1];
        }
    }
}
