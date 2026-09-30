using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public static class InheritedJobIds
    {
        public static readonly IReadOnlyCollection<string> All = new[]
        {
            "slayer", "defender", "chaser", "berserker", "gunner", "sniper", "blaster",
            "healer", "artist", "gambler", "general", "alchemist", "panzer"
        };
    }

    public sealed class JobDefinition
    {
        public string Id { get; }
        public string ResourceOrCommandRuleId { get; }
        public JobDefinition(string id, string resourceOrCommandRuleId)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(resourceOrCommandRuleId))
                throw new ArgumentException("A job id and a resource or command rule are required.");
            Id = id;
            ResourceOrCommandRuleId = resourceOrCommandRuleId;
        }
    }

    public sealed class HeroineCombatDefinition
    {
        public string Id { get; }
        public string JobId { get; }
        public IReadOnlyCollection<string> SkillIds { get; }
        public IReadOnlyCollection<string> PermanentEffectKinds { get; }

        public HeroineCombatDefinition(string id, string jobId, IEnumerable<string> skillIds, IEnumerable<string> permanentEffectKinds)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(jobId)) throw new ArgumentException("Heroine and job ids are required.");
            Id = id;
            JobId = jobId;
            SkillIds = (skillIds ?? throw new ArgumentNullException(nameof(skillIds))).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
            PermanentEffectKinds = (permanentEffectKinds ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
        }
    }

    public sealed class ColossusCombatDefinition
    {
        public string Id { get; }
        public int PartCount { get; }
        public int UltimateAvailableLevel { get; }
        public int PoemCount { get; }
        public ColossusCombatDefinition(string id, int partCount, int ultimateAvailableLevel, int poemCount)
        {
            Id = id;
            PartCount = partCount;
            UltimateAvailableLevel = ultimateAvailableLevel;
            PoemCount = poemCount;
        }
    }

    /// <summary>正式データの取込み時に仕様違反を止める。チェイン率は人物・装備・特性データへ持ち込ませない。</summary>
    public static class GameDataValidator
    {
        public static void ValidateJobs(IEnumerable<JobDefinition> jobs)
        {
            var values = (jobs ?? throw new ArgumentNullException(nameof(jobs))).ToArray();
            if (values.Length != 13 || values.Select(job => job.Id).Distinct().Count() != 13
                || !InheritedJobIds.All.All(expected => values.Any(job => job.Id == expected)))
                throw new InvalidOperationException("The game requires exactly the 13 inherited jobs.");
        }

        public static void ValidateHeroines(IEnumerable<HeroineCombatDefinition> heroines, IEnumerable<JobDefinition> jobs)
        {
            var jobIds = new HashSet<string>((jobs ?? throw new ArgumentNullException(nameof(jobs))).Select(job => job.Id));
            foreach (var heroine in heroines ?? throw new ArgumentNullException(nameof(heroines)))
            {
                if (heroine == null || !jobIds.Contains(heroine.JobId) || heroine.SkillIds.Count != 3)
                    throw new InvalidOperationException("Each heroine requires a valid job and exactly three unique skills.");
                if (heroine.PermanentEffectKinds.Any(effect => effect == "chain-rate"))
                    throw new InvalidOperationException("Heroine-specific chain-rate effects are prohibited.");
            }
        }

        public static void ValidateColossi(IEnumerable<ColossusCombatDefinition> colossi)
        {
            var values = (colossi ?? throw new ArgumentNullException(nameof(colossi))).ToArray();
            if (values.Length != 15 || values.Select(colossus => colossus.Id).Distinct().Count() != 15)
                throw new InvalidOperationException("The initial catalogue requires 15 unique colossi.");
            if (values.Any(colossus => colossus.PartCount < 4 || colossus.PartCount > 6
                || colossus.UltimateAvailableLevel != BattleState.UltimateUnlockLevel || colossus.PoemCount != 24))
                throw new InvalidOperationException("Colossi require 4–6 parts, 24 poems, and a Lv45 ultimate unlock.");
        }
    }
}
