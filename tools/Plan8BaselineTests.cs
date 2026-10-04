using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan8BaselineTests
{
    public static void Run(Action<bool, string> check, CombatDefinitionCatalog combat, string json)
    {
        var options = new JsonSerializerOptions { IncludeFields = true };
        Func<TrialDefinition> fresh = () => JsonSerializer.Deserialize<TrialDefinition>(json, options);
        var enemies = WorldCatalog.ColossusIds.ToArray();
        fresh().Validate(combat, enemies);
        check(fresh().heroineIds.SequenceEqual(combat.FormationIds), "Trial scope uses current formal formation");
        Action<Action<TrialDefinition>> invalid = change => {
            var trial = fresh(); change(trial); bool rejected = false;
            try { trial.Validate(combat, enemies); } catch (ArgumentException) { rejected = true; }
            check(rejected, "Invalid trial definition rejected");
        };
        invalid(t => t.schemaVersion = 2); invalid(t => t.status = "release");
        invalid(t => t.colossusId = "colossus.unknown"); invalid(t => t.heroineIds[0] = t.heroineIds[1]);
        invalid(t => t.heroineIds = t.heroineIds.Reverse().ToArray()); invalid(t => t.maxEnemyLevel = 51);
        invalid(t => t.minEnemyLevel = -1); invalid(t => t.strongestSkillLevel = 44);
        invalid(t => t.definitions[0].resourcePath = "../save"); invalid(t => t.definitions[0] = t.definitions[1]);
        invalid(t => t.definitions = null); invalid(t => t.measurements[0] = t.measurements[1]);
        invalid(t => t.requiredEvents = 0); invalid(t => t.contentVersion = "");
        bool absent = false;
        try { new TrialDefinition().Validate(combat, enemies); } catch (ArgumentException) { absent = true; }
        check(absent, "Missing trial fields never create an accepted baseline");

        var temp = Path.Combine(Path.GetTempPath(), "newaster-plan8-test-" + Guid.NewGuid().ToString("N"));
        var repo = Path.Combine(temp, "repo"); var normal = Path.Combine(temp, "player");
        var boundary = TrialDiagnosticBoundary.Create(repo, normal, "run-01");
        check(boundary.SaveId == "newaster.trial.run-01" && !Directory.Exists(boundary.DirectoryPath), "Trial path and ID are isolated without creating files");
        check(boundary.SavePath == Path.Combine(repo, "tmp", "plan8-runs", "run-01", "formal-campaign-v1.json"), "Trial save stays beneath repository tmp");
        Action<string, string, string> rejectPath = (root, player, run) => {
            bool rejected = false; try { TrialDiagnosticBoundary.Create(root, player, run); } catch (ArgumentException) { rejected = true; }
            check(rejected, "Unsafe trial save boundary rejected");
        };
        foreach (var run in new[] { "../player", "..", "a/b", "a\\b", "CON", "Lpt9", "", new string('a', 65), "run\n" }) rejectPath(repo, normal, run);
        rejectPath("relative", normal, "run"); rejectPath(repo, "relative", "run");
        rejectPath("C:relative", normal, "run"); rejectPath("\\relative", normal, "run");
        rejectPath(repo, repo, "run"); rejectPath(repo, Path.Combine(repo, "tmp", "plan8-runs", "player"), "run");
        Directory.CreateDirectory(boundary.DirectoryPath);
        try { rejectPath(repo, normal, "run-01"); }
        finally {
            foreach (var path in new[] { boundary.DirectoryPath, Path.GetDirectoryName(boundary.DirectoryPath), Path.Combine(repo, "tmp"), repo, temp }) Directory.Delete(path);
        }
    }
}
