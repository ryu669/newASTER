using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public enum HeroineEventKind { Affinity, Lover }

    public sealed class HeroineEventDefinition
    {
        public string Id { get; }
        public string HeroineId { get; }
        public HeroineEventKind Kind { get; }
        public int RequiredAffection { get; }
        public bool EstablishesLoverStatus { get; }
        public IReadOnlyCollection<string> PrerequisiteEventIds { get; }

        public HeroineEventDefinition(string id, string heroineId, HeroineEventKind kind, int requiredAffection,
            bool establishesLoverStatus, IEnumerable<string> prerequisiteEventIds)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(heroineId)) throw new ArgumentException("Event and heroine ids are required.");
            if (requiredAffection < 0) throw new ArgumentOutOfRangeException(nameof(requiredAffection));
            if (establishesLoverStatus && kind != HeroineEventKind.Affinity)
                throw new ArgumentException("Only an explicit affinity event may establish lover status.", nameof(establishesLoverStatus));
            Id = id;
            HeroineId = heroineId;
            Kind = kind;
            RequiredAffection = requiredAffection;
            EstablishesLoverStatus = establishesLoverStatus;
            PrerequisiteEventIds = (prerequisiteEventIds ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
        }
    }

    /// <summary>好感度・イベントは詩、物語章、家具とは別保存。回想はこの状態を変更しない。</summary>
    public sealed class HeroineAffectionState
    {
        private readonly Dictionary<string, int> _affectionValues = new Dictionary<string, int>();
        private readonly HashSet<string> _unlockedEventIds = new HashSet<string>();
        private readonly HashSet<string> _readEventIds = new HashSet<string>();
        private readonly HashSet<string> _loverHeroineIds = new HashSet<string>();

        public IReadOnlyCollection<string> UnlockedEventIds => _unlockedEventIds;
        public IReadOnlyCollection<string> ReadEventIds => _readEventIds;

        public int GetAffection(string heroineId) => _affectionValues.TryGetValue(heroineId, out var value) ? value : 0;
        public bool IsLover(string heroineId) => _loverHeroineIds.Contains(heroineId);

        public void AddAffection(string heroineId, int amount)
        {
            if (string.IsNullOrWhiteSpace(heroineId)) throw new ArgumentException("A heroine id is required.", nameof(heroineId));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            _affectionValues[heroineId] = GetAffection(heroineId) + amount;
        }

        public IReadOnlyCollection<string> RefreshUnlocks(IEnumerable<HeroineEventDefinition> definitions)
        {
            return (definitions ?? throw new ArgumentNullException(nameof(definitions)))
                .Where(definition => definition != null
                    && GetAffection(definition.HeroineId) >= definition.RequiredAffection
                    && definition.PrerequisiteEventIds.All(_readEventIds.Contains)
                    && (definition.Kind != HeroineEventKind.Lover || IsLover(definition.HeroineId))
                    && _unlockedEventIds.Add(definition.Id))
                .Select(definition => definition.Id)
                .ToArray();
        }

        public bool MarkEventRead(HeroineEventDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!_unlockedEventIds.Contains(definition.Id)) return false;
            if (!_readEventIds.Add(definition.Id)) return false;
            if (definition.EstablishesLoverStatus) _loverHeroineIds.Add(definition.HeroineId);
            return true;
        }
    }
}
