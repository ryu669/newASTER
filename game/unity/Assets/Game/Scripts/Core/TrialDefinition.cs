using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NewAster.Core
{
    [Serializable] public sealed class TrialDefinitionReference
    {
        public string role, resourcePath;
    }

    /// <summary>Plan8 development scope. This does not authorize release content or change balance.</summary>
    [Serializable] public sealed class TrialDefinition
    {
        public int schemaVersion;
        public string id, contentVersion, status, colossusId;
        public string[] heroineIds, measurements;
        public int minEnemyLevel, maxEnemyLevel, strongestSkillLevel;
        public int requiredColossusChapters, requiredHeroineFirstChapters, requiredEvents;
        public TrialDefinitionReference[] definitions;

        public void Validate(CombatDefinitionCatalog combat, string[] knownColossusIds)
        {
            if (combat == null || knownColossusIds == null) throw new ArgumentNullException();
            combat.Validate();
            if (schemaVersion != 1 || id != "trial.plan8.vertical-slice" || status != "development-trial" ||
                string.IsNullOrWhiteSpace(contentVersion)) throw new ArgumentException("Unknown trial identity, version or adoption scope.");
            if (!knownColossusIds.Contains(colossusId) || colossusId != knownColossusIds.FirstOrDefault() ||
                heroineIds == null || !heroineIds.SequenceEqual(combat.FormationIds))
                throw new ArgumentException("Trial must reference the selected enemy and five formal heroines in formation order.");
            if (minEnemyLevel != 1 || maxEnemyLevel != 50 || strongestSkillLevel != 45)
                throw new ArgumentException("Trial cannot change the enemy level or strongest skill contract.");
            if (requiredColossusChapters != 3 || requiredHeroineFirstChapters != 5 || requiredEvents != 1)
                throw new ArgumentException("Missing Plan8 authored-content goals; counts do not certify delivery.");
            var roles = new[] { "combat", "illustrations", "kinder", "engagement" };
            var paths = new[] { "Combat/battle-formal", "Illustrations/battle-formal", "Economy/kinder-trial", "Economy/engagement-trial" };
            if (definitions == null || definitions.Length != roles.Length || definitions.Any(d => d == null) ||
                !definitions.Select(d => d.role).SequenceEqual(roles) || !definitions.Select(d => d.resourcePath).SequenceEqual(paths))
                throw new ArgumentException("Missing, duplicate or unknown trial definition reference.");
            var required = new[] { "timing", "battle", "collection", "economy", "garden", "reading", "navigation", "save", "audio", "performance" };
            if (measurements == null || measurements.Length != required.Length ||
                measurements.Distinct().Count() != measurements.Length || required.Except(measurements).Any())
                throw new ArgumentException("Missing, duplicate or unknown trial measurement category.");
        }
    }

    public sealed class TrialDiagnosticBoundary
    {
        public string DirectoryPath { get; }
        public string SavePath { get; }
        public string SaveId { get; }
        private TrialDiagnosticBoundary(string directory, string id)
        {
            DirectoryPath = directory; SavePath = Path.Combine(directory, "formal-campaign-v1.json"); SaveId = "newaster.trial." + id;
        }

        // Only derives a path. Never creates a directory, opens a store or reads a player save.
        public static TrialDiagnosticBoundary Create(string repositoryRoot, string normalSaveRoot, string runId)
        {
            if (string.IsNullOrWhiteSpace(repositoryRoot) || string.IsNullOrWhiteSpace(normalSaveRoot) ||
                !Path.IsPathFullyQualified(repositoryRoot) || !Path.IsPathFullyQualified(normalSaveRoot))
                throw new ArgumentException("Absolute repository and normal-save roots are required.");
            if (runId == null || !Regex.IsMatch(runId, "\\A[A-Za-z0-9][A-Za-z0-9_-]{0,63}\\z") ||
                Regex.IsMatch(runId, "\\A(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])\\z", RegexOptions.IgnoreCase))
                throw new ArgumentException("Invalid or reserved trial run ID.");
            string root = Path.GetFullPath(Path.Combine(repositoryRoot, "tmp", "plan8-runs"));
            string normal = Path.GetFullPath(normalSaveRoot);
            if (Within(root, normal) || Within(normal, root)) throw new ArgumentException("Trial and normal save roots must not overlap.");
            for (var parent = new DirectoryInfo(root); parent != null; parent = parent.Parent)
                if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("Trial root cannot traverse a junction or symbolic link.");
            string directory = Path.GetFullPath(Path.Combine(root, runId));
            if (Directory.Exists(directory) || File.Exists(directory)) throw new ArgumentException("Trial run ID already exists; use a fresh run.");
            return new TrialDiagnosticBoundary(directory, runId);
        }

        private static bool Within(string path, string root)
        {
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), prefix, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(prefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
